using System;
using System.Threading;
using System.Threading.Tasks;
using Ecs;
using Volo.Abp.Application.Services;

namespace Ecs.AgvPlc;

public interface IAgvTransportTaskAppService : IApplicationService
{
    /// <summary>
    /// 分页查询搬运任务，按创建时间倒序，支持起点/终点/状态/时间范围筛选。
    /// </summary>
    Task<AgvTransportTaskPagedResultDto> GetPagedListAsync(AgvTransportTaskGetListInput input);

    /// <summary>
    /// 按任务 Id 取消搬运任务：先取消 RCS 任务，成功后停止 PLC 后台继续信号并复位两个工位。
    /// </summary>
    Task<ResponseDto> CancelAsync(
        Guid id,
        CancelAgvTransportTaskInput input = null,
        CancellationToken cancellationToken = default);

    /// <summary>恢复已取消任务暂停的起点和终点区域。</summary>
    Task<ResponseDto> ResumeZonesAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>PLC 双端连续三帧健康后，人工恢复故障暂停区域。</summary>
    Task<ResponseDto> ResumeFaultZonesAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
