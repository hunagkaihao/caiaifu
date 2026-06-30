using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Ecs.AgvPlc;

public interface IAgvPlcPointRedisAppService : IApplicationService
{
    /// <summary>
    /// 按线别 O1～O4 读取该线四个点位（A～D）的 Redis 快照，顺序固定为 A、B、C、D。
    /// </summary>
    /// <param name="lineKey">O1、O2、O3 或 O4（大小写不敏感）。</param>
    Task<List<AgvPlcPointSnapshotDto>> GetSnapshotsByLineAsync(string lineKey);

    /// <summary>
    /// 将指定点位 Redis Hash 中的「运行状态」重置为 0（可用）。
    /// </summary>
    /// <param name="pointCode">如 O1A、01A、1A（O1～O4，A～D）</param>
    Task<Ecs.ResponseDto> ResetPointRunStateToZeroAsync(string pointCode);
}
