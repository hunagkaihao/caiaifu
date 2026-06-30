using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecs.AgvPlcTcp;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace Ecs.AgvPlc;

public class AgvTransportTaskAppService : EcsAppService, IAgvTransportTaskAppService
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 200;

    private readonly IRepository<AgvTransportTask, Guid> _repository;
    private readonly ILogger<AgvTransportTaskAppService> _logger;

    public AgvTransportTaskAppService(
        IRepository<AgvTransportTask, Guid> repository,
        ILogger<AgvTransportTaskAppService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AgvTransportTaskPagedResultDto> GetPagedListAsync(AgvTransportTaskGetListInput input)
    {
        input ??= new AgvTransportTaskGetListInput();

        var page = input.Page < 1 ? 1 : input.Page;
        var pageSize = input.PageSize < 1 ? DefaultPageSize : input.PageSize;
        if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }

        if (!AgvTransportTaskStatusFilter.TryParse(input.TaskStatus, out var isCompletedFilter))
        {
            _logger.LogWarning("无效的任务状态筛选参数 TaskStatus={TaskStatus}", input.TaskStatus);
            return new AgvTransportTaskPagedResultDto
            {
                Page = page,
                PageSize = pageSize
            };
        }

        try
        {
            var queryable = await _repository.GetQueryableAsync().ConfigureAwait(false);
            var query = ApplyFilters(queryable, input, isCompletedFilter);

            var totalCount = await AsyncExecuter.CountAsync(query).ConfigureAwait(false);
            var skip = (page - 1) * pageSize;
            var entities = await AsyncExecuter
                .ToListAsync(
                    query
                        .OrderByDescending(t => t.CreationTime)
                        .Skip(skip)
                        .Take(pageSize))
                .ConfigureAwait(false);

            var items = ObjectMapper.Map<List<AgvTransportTask>, List<AgvTransportTaskDto>>(entities);

            return new AgvTransportTaskPagedResultDto
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = items
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询 AgvTransportTasks 失败");
            return new AgvTransportTaskPagedResultDto
            {
                Page = page,
                PageSize = pageSize
            };
        }
    }

    private static IQueryable<AgvTransportTask> ApplyFilters(
        IQueryable<AgvTransportTask> query,
        AgvTransportTaskGetListInput input,
        bool? isCompletedFilter)
    {
        if (!string.IsNullOrWhiteSpace(input.SourcePointCode))
        {
            var source = AgvPlcPointCodes.TryNormalizePointCode(input.SourcePointCode.Trim(), out var canonical)
                ? canonical
                : input.SourcePointCode.Trim();
            query = query.Where(t => t.SourcePointCode == source);
        }

        if (!string.IsNullOrWhiteSpace(input.TargetPointCode))
        {
            var target = AgvPlcPointCodes.TryNormalizePointCode(input.TargetPointCode.Trim(), out var canonical)
                ? canonical
                : input.TargetPointCode.Trim();
            query = query.Where(t => t.TargetPointCode == target);
        }

        if (isCompletedFilter == true)
        {
            query = query.Where(t => t.Status == AgvTransportTaskStatuses.Completed);
        }
        else if (isCompletedFilter == false)
        {
            query = query.Where(t => t.Status != AgvTransportTaskStatuses.Completed);
        }

        if (input.TimeStart.HasValue)
        {
            var start = input.TimeStart.Value;
            query = query.Where(t => t.CreationTime >= start);
        }

        if (input.TimeEnd.HasValue)
        {
            var end = input.TimeEnd.Value;
            query = query.Where(t => t.CreationTime <= end);
        }

        return query;
    }
}
