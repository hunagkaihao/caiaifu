using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecs.AgvPlcTcp;
using Ecs.WorkPositions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Ecs.AgvPlc;

public sealed class AgvTaskZonePair
{
    public AgvTaskZonePair(string sourceZoneCode, string targetZoneCode)
    {
        SourceZoneCode = sourceZoneCode;
        TargetZoneCode = targetZoneCode;
    }

    public string SourceZoneCode { get; }

    public string TargetZoneCode { get; }
}

public interface IAgvTaskZoneResolver
{
    Task<AgvTaskZonePair> ResolveAsync(
        AgvTransportTask task,
        CancellationToken cancellationToken = default);

    Task<string> ResolvePointAsync(
        string pointCode,
        CancellationToken cancellationToken = default);
}

public class AgvTaskZoneResolver : IAgvTaskZoneResolver, ITransientDependency
{
    private readonly IRepository<WorkPosition, int> _repository;

    public AgvTaskZoneResolver(IRepository<WorkPosition, int> repository)
    {
        _repository = repository;
    }

    public async Task<AgvTaskZonePair> ResolveAsync(
        AgvTransportTask task,
        CancellationToken cancellationToken = default)
    {
        var rows = await _repository.GetListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return new AgvTaskZonePair(
            ResolveOne(rows, task.SourcePointCode),
            ResolveOne(rows, task.TargetPointCode));
    }

    public async Task<string> ResolvePointAsync(
        string pointCode,
        CancellationToken cancellationToken = default)
    {
        var rows = await _repository.GetListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return ResolveOne(rows, pointCode);
    }

    private static string ResolveOne(System.Collections.Generic.IEnumerable<WorkPosition> rows, string pointCode)
    {
        var matches = rows
            .Where(x => string.Equals(x.DeviceName?.Trim(), pointCode?.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x => !string.Equals(x.Status?.Trim(), WorkPositionStatus.Disabled,
                StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matches.Count != 1 || string.IsNullOrWhiteSpace(matches[0].SiteName))
        {
            throw new InvalidOperationException($"点位 {pointCode} 必须且只能配置一个可用的 WorkPositions 端口");
        }

        return matches[0].SiteName.Trim();
    }
}
