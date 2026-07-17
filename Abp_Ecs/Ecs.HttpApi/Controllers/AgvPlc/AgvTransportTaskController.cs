using System;
using System.Threading;
using System.Threading.Tasks;
using Ecs;
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

    /// <summary>
    /// 按任务 Id 取消搬运任务；后端先取消 RCS 任务，成功后复位起终点工位。
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public Task<ResponseDto> CancelAsync(
        [FromRoute] Guid id,
        [FromBody] CancelAgvTransportTaskInput input,
        CancellationToken cancellationToken)
    {
        return _agvTransportTaskAppService.CancelAsync(id, input, cancellationToken);
    }

    /// <summary>恢复已取消任务对应的起点和终点区域。</summary>
    [HttpPost("{id:guid}/resume-zones")]
    public Task<ResponseDto> ResumeZonesAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        return _agvTransportTaskAppService.ResumeZonesAsync(id, cancellationToken);
    }

    /// <summary>PLC 起终点连续三帧健康后，人工恢复故障暂停区域。</summary>
    [HttpPost("{id:guid}/resume-fault-zones")]
    public Task<ResponseDto> ResumeFaultZonesAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        return _agvTransportTaskAppService.ResumeFaultZonesAsync(id, cancellationToken);
    }
}
