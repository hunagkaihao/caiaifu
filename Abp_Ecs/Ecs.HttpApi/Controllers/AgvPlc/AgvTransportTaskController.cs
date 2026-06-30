using System;
using System.Threading.Tasks;
using Ecs.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Ecs.AgvPlc;

[Route("ecs/agv-transport-tasks")]
[ApiController]
public class AgvTransportTaskController : EcsController
{
    private readonly IAgvTransportTaskAppService _agvTransportTaskAppService;

    public AgvTransportTaskController(IAgvTransportTaskAppService agvTransportTaskAppService)
    {
        _agvTransportTaskAppService = agvTransportTaskAppService;
    }

    /// <summary>
    /// 分页获取 AgvTransportTasks 表任务列表（按创建时间倒序）。
    /// </summary>
    /// <param name="page">页码，从 1 开始，默认 1</param>
    /// <param name="pageSize">每页条数，默认 10，最大 200</param>
    /// <param name="sourcePointCode">起点点位编码，如 O1A</param>
    /// <param name="targetPointCode">终点点位编码，如 O1C</param>
    /// <param name="taskStatus">任务状态：完成 / 未完成（未完成=Status≠Completed）</param>
    /// <param name="timeStart">创建时间起始（含）</param>
    /// <param name="timeEnd">创建时间结束（含）</param>
    [HttpGet]
    public Task<AgvTransportTaskPagedResultDto> GetPagedListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sourcePointCode = null,
        [FromQuery] string? targetPointCode = null,
        [FromQuery] string? taskStatus = null,
        [FromQuery] DateTime? timeStart = null,
        [FromQuery] DateTime? timeEnd = null)
    {
        return _agvTransportTaskAppService.GetPagedListAsync(new AgvTransportTaskGetListInput
        {
            Page = page,
            PageSize = pageSize,
            SourcePointCode = sourcePointCode,
            TargetPointCode = targetPointCode,
            TaskStatus = taskStatus,
            TimeStart = timeStart,
            TimeEnd = timeEnd
        });
    }
}
