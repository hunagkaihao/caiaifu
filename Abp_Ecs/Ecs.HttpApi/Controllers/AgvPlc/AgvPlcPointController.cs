using System.Collections.Generic;
using System.Threading.Tasks;
using Ecs;
using Ecs.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Ecs.AgvPlc;

[Route("ecs/agv-plc")]
[ApiController]
public class AgvPlcPointController : EcsController
{
    private readonly IAgvPlcPointRedisAppService _agvPlcPointRedisAppService;

    public AgvPlcPointController(IAgvPlcPointRedisAppService agvPlcPointRedisAppService)
    {
        _agvPlcPointRedisAppService = agvPlcPointRedisAppService;
    }

    /// <summary>
    /// 按线别获取 Redis 中四个点位（如 O1 → O1A、O1B、O1C、O1D）的快照。
    /// </summary>
    /// <param name="line">O1、O2、O3 或 O4</param>
    [HttpGet("points/{line}")]
    public Task<List<AgvPlcPointSnapshotDto>> GetPointsAsync([FromRoute] string line)
    {
        return _agvPlcPointRedisAppService.GetSnapshotsByLineAsync(line);
    }

    /// <summary>
    /// 将 Redis 中指定点位的「运行状态」写入为 0（可用）。
    /// </summary>
    /// <param name="pointCode">如 O1A、01B、O2C、2c 等</param>
    [HttpPost("points/{pointCode}/reset-run-state")]
    public Task<ResponseDto> ResetPointRunStateAsync([FromRoute] string pointCode)
    {
        return _agvPlcPointRedisAppService.ResetPointRunStateToZeroAsync(pointCode);
    }
}
