using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Ecs.AgvPlc;

public interface IAgvTransportTaskAppService : IApplicationService
{
    /// <summary>
    /// 分页查询搬运任务，按创建时间倒序，支持起点/终点/状态/时间范围筛选。
    /// </summary>
    Task<AgvTransportTaskPagedResultDto> GetPagedListAsync(AgvTransportTaskGetListInput input);
}
