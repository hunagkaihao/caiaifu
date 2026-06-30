using System.Collections.Generic;
using System.Threading.Tasks;
using Ecs.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Ecs.PlcMonitor;

[Route("ecs/plc")]
[ApiController]
public class PlcMonitorController : EcsController, IPlcMonitorService
{
    private readonly IPlcMonitorService _plcMonitorService;

    public PlcMonitorController(
        IPlcMonitorService plcMonitorService)
    {
        _plcMonitorService = plcMonitorService;
    }

    [HttpGet("plcMonitor")]
    public async Task<List<MonitorDto>> GetMonitorsAsync()
    {
        return await _plcMonitorService.GetMonitorsAsync().ConfigureAwait(false);
    }
}