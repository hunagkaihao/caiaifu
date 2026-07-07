using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecs.ConfigTool;
using Ecs.Rcs;
using Ecs.WorkPositions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp.DependencyInjection;

namespace Ecs.AgvPlcTcp;

/// <summary>
/// 从 Redis 读点位快照，按四点位八向边匹配后派发 RCS 搬运任务。
/// 起终点 SITE 取自 <see cref="WorkPosition.SiteName"/>（<see cref="WorkPosition.DeviceName"/> 对应点位编码 O1A 等）；
/// A→C/A→D/B→C/B→D 使用 qu_tozhi，C→A/C→B/D→A/D→B 使用 ces。
/// </summary>
public class AgvPlcTaskDispatchService : ISingletonDependency
{
    private readonly AgvPlcRedisStore _redisStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AgvPlcTaskDispatchService> _logger;
    private readonly IOptionsMonitor<AgvPlcTcpOptions> _optionsMonitor;
    private readonly AgvPlcTaskDispatchSuppressionRegistry _suppressionRegistry;
    private readonly SemaphoreSlim _dispatchLock = new(1, 1);

    public AgvPlcTaskDispatchService(
        AgvPlcRedisStore redisStore,
        IServiceScopeFactory scopeFactory,
        ILogger<AgvPlcTaskDispatchService> logger,
        IOptionsMonitor<AgvPlcTcpOptions> optionsMonitor,
        AgvPlcTaskDispatchSuppressionRegistry suppressionRegistry)
    {
        _redisStore = redisStore;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _suppressionRegistry = suppressionRegistry;
    }

    /// <param name="lineKey">线别：O1、O2、O3、O4（与配置 Lines 键一致）。</param>
    public async Task TryDispatchAsync(string lineKey)
    {
        var opts = _optionsMonitor.CurrentValue;
        if (!opts.Enabled || !opts.TryGetLine(lineKey, out var lineOpts) || lineOpts == null || !lineOpts.EdgeDispatchEnabled)
        {
            return;
        }

        if (!AgvPlcPointCodes.TryGetQuadPoints(lineKey, out var quad))
        {
            return;
        }

        await _dispatchLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await TryDispatchEdgeMatchedTasksAsync(lineKey, quad).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AGV 搬运任务派发失败 Line={Line}", lineKey);
        }
        finally
        {
            _dispatchLock.Release();
        }
    }

    private async Task TryDispatchEdgeMatchedTasksAsync(string lineKey, string[] quad)
    {
        var workPositionsByDevice = await LoadWorkPositionsByDeviceNameAsync().ConfigureAwait(false);
        var snapshots = _redisStore.GetSnapshotsForPoints(quad);

        PointProtocolSnapshot GetSnap(string code)
        {
            if (snapshots.TryGetValue(code, out var s) && s != null)
            {
                return s;
            }

            return new PointProtocolSnapshot
            {
                PointCode = code,
                RunState = AgvPlcRunStates.Available
            };
        }

        static bool RequestPickupTaskActive(PointProtocolSnapshot s) =>
            s.StatusSeqBits is { Length: > 8 } bits && bits[8];

        static bool RequestPlaceTaskActive(PointProtocolSnapshot s) =>
            s.StatusSeqBits is { Length: > 9 } bits && bits[9];

        bool CanUseAsSource(PointProtocolSnapshot s) =>
            AgvPlcRunStates.Normalize(s.RunState) == AgvPlcRunStates.Available &&
            RequestPickupTaskActive(s) &&
            TryGetSiteName(s.PointCode, workPositionsByDevice, out _);

        bool CanUseAsTarget(PointProtocolSnapshot s) =>
            AgvPlcRunStates.Normalize(s.RunState) == AgvPlcRunStates.Available &&
            RequestPlaceTaskActive(s) &&
            TryGetSiteName(s.PointCode, workPositionsByDevice, out _);

        var busy = new HashSet<string>(StringComparer.Ordinal);
        var toCreate = new List<(string From, string To, string Code)>();

        var edges = TransportEdgeDefinitions.GetEdgesForQuad(quad[0], quad[1], quad[2], quad[3]);
        foreach (var edge in edges)
        {
            if (busy.Contains(edge.From) || busy.Contains(edge.To))
            {
                continue;
            }

            var fs = GetSnap(edge.From);
            var ts = GetSnap(edge.To);
            var requestPickupActive = RequestPickupTaskActive(fs);
            var requestPlaceActive = RequestPlaceTaskActive(ts);
            if (_suppressionRegistry.IsSuppressed(edge.From, edge.To, edge.Code))
            {
                if (!requestPickupActive && !requestPlaceActive)
                {
                    _suppressionRegistry.Release(edge.From, edge.To, edge.Code);
                    _logger.LogInformation(
                        "取消任务抑制已释放 Line={Line} Edge={Edge} {From}->{To}",
                        lineKey,
                        edge.Code,
                        edge.From,
                        edge.To);
                }
                else
                {
                    continue;
                }
            }

            if (AgvPlcRunStates.Normalize(fs.RunState) == AgvPlcRunStates.Disabled ||
                AgvPlcRunStates.Normalize(ts.RunState) == AgvPlcRunStates.Disabled)
            {
                continue;
            }

            if (!CanUseAsSource(fs) || !CanUseAsTarget(ts))
            {
                continue;
            }

            if (!TransportEdgeDefinitions.TryGetTaskTypeForEdgeCode(edge.Code, out _))
            {
                _logger.LogWarning("未知边编码，跳过派发 Line={Line} Edge={Edge}", lineKey, edge.Code);
                continue;
            }

            toCreate.Add(edge);
            busy.Add(edge.From);
            busy.Add(edge.To);
        }

        foreach (var e in toCreate)
        {
            if (!TryGetSiteName(e.From, workPositionsByDevice, out var fromSite) ||
                !TryGetSiteName(e.To, workPositionsByDevice, out var toSite) ||
                !TransportEdgeDefinitions.TryGetTaskTypeForEdgeCode(e.Code, out var taskType))
            {
                continue;
            }

            await SubmitEdgeRcsTaskAsync(e.From, e.To, e.Code, taskType, fromSite, toSite).ConfigureAwait(false);
        }
    }

    private async Task<Dictionary<string, WorkPosition>> LoadWorkPositionsByDeviceNameAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<WorkPosition, int>>();
        var all = await repo.GetListAsync().ConfigureAwait(false);
        return all
            .Where(w => !string.IsNullOrWhiteSpace(w.DeviceName))
            .GroupBy(w => w.DeviceName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool TryGetSiteName(
        string pointCode,
        IReadOnlyDictionary<string, WorkPosition> workPositionsByDevice,
        out string siteName)
    {
        siteName = string.Empty;
        if (!TryGetWorkPosition(pointCode, workPositionsByDevice, out var wp) ||
            IsWorkPositionDisabled(wp) ||
            string.IsNullOrWhiteSpace(wp.SiteName))
        {
            return false;
        }

        siteName = wp.SiteName.Trim();
        return true;
    }

    private static bool TryGetWorkPosition(
        string pointCode,
        IReadOnlyDictionary<string, WorkPosition> workPositionsByDevice,
        out WorkPosition workPosition)
    {
        workPosition = null!;
        if (string.IsNullOrWhiteSpace(pointCode))
        {
            return false;
        }

        return workPositionsByDevice.TryGetValue(pointCode.Trim(), out workPosition!) && workPosition != null;
    }

    private static bool IsWorkPositionDisabled(WorkPosition workPosition)
    {
        return string.Equals(
            workPosition.Status?.Trim(),
            WorkPositionStatus.Disabled,
            StringComparison.Ordinal);
    }

    private async Task SubmitEdgeRcsTaskAsync(
        string fromPointCode,
        string toPointCode,
        string edgeCode,
        string taskType,
        string fromSite,
        string toSite)
    {
        var taskId = Guid.NewGuid();
        var robotTaskCode = taskId.ToString("N");

        var rcsRequest = new RcsTaskSubmitRequest
        {
            TaskType = taskType,
            RobotTaskCode = robotTaskCode,
            TargetRoute = new List<RcsTargetRouteStepDto>
            {
                new()
                {
                    Type = "SITE",
                    Code = fromSite,
                    Extra = new RcsTargetRouteStepExtraDto()
                },
                new()
                {
                    Type = "SITE",
                    Code = toSite,
                    Extra = new RcsTargetRouteStepExtraDto()
                }
            }
        };

        RcsApiResponse<RcsTaskSubmitResponseData> rcsResult;
        using (var scope = _scopeFactory.CreateScope())
        {
            var rcsClient = scope.ServiceProvider.GetRequiredService<IRcsApiClient>();
            rcsResult = await rcsClient.SubmitTaskAsync(rcsRequest).ConfigureAwait(false);
        }

        var submitted = IsRcsSubmitSuccess(rcsResult);
        var entity = new AgvTransportTask(taskId)
        {
            SourcePointCode = fromPointCode,
            TargetPointCode = toPointCode,
            EdgeCode = edgeCode,
            Status = submitted ? AgvTransportTaskStatuses.Submitted : AgvTransportTaskStatuses.RcsFailed,
            CreationTime = ChinaDateTime.Now
        };

        using (var scope = _scopeFactory.CreateScope())
        {
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            using var uow = uowManager.Begin(requiresNew: true);
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<AgvTransportTask, Guid>>();
            await repo.InsertAsync(entity, autoSave: true).ConfigureAwait(false);
            await uow.CompleteAsync().ConfigureAwait(false);
        }

        if (submitted)
        {
            _redisStore.SetRunState(fromPointCode, AgvPlcRunStates.Selected);
            _redisStore.SetRunState(toPointCode, AgvPlcRunStates.Selected);
            _logger.LogInformation(
                "边匹配已下发 RCS {Edge} {FromPoint}->{ToPoint} {TaskType} {FromSite}->{ToSite} RobotTaskCode={RobotTaskCode} Id={Id}",
                edgeCode,
                fromPointCode,
                toPointCode,
                taskType,
                fromSite,
                toSite,
                robotTaskCode,
                taskId);
        }
        else
        {
            _logger.LogWarning(
                "边匹配 RCS 下发失败 {Edge} {FromPoint}->{ToPoint} {TaskType} RobotTaskCode={RobotTaskCode} Code={Code} Message={Message}",
                edgeCode,
                fromPointCode,
                toPointCode,
                taskType,
                robotTaskCode,
                rcsResult?.Code,
                rcsResult?.Message);
        }
    }

    private static bool IsRcsSubmitSuccess(RcsApiResponse<RcsTaskSubmitResponseData> response)
    {
        if (response == null)
        {
            return false;
        }

        if (response.Success == true)
        {
            return true;
        }

        return string.Equals(response.Code, "SUCCESS", StringComparison.OrdinalIgnoreCase) ||
               response.Code == "0";
    }
}
