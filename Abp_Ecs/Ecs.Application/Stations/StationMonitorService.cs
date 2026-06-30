using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecs.LogTool;
using Microsoft.Extensions.Logging;

namespace Ecs.Stations;

public class StationMonitorService : EcsAppService, IStationMonitorService
{
    private readonly ILogger<StationMonitorService> _logger;

    private readonly StationMonitorManager _stationManager;

    public StationMonitorService(
        ILogger<StationMonitorService> logger,
        StationMonitorManager stationManager)
    {
        _logger = logger;
        _stationManager = stationManager;
    }
    
    //[RemoteService(false)]
    public async Task<List<StationInfoDto>> GetInformationsAsync(string stationCode)
    {
        try
        {
            List<StationInfo> stationInfos = await _stationManager.GetAllStationInfoAsync(stationCode).ConfigureAwait(false);
            return ObjectMapper.Map<List<StationInfo>, List<StationInfoDto>>(stationInfos);
        }
        catch(Exception e)
        {
            _logger.Error(e.Message);
            return new List<StationInfoDto>();
        }
    }
}