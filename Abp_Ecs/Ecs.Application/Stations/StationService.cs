using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecs.LogTool;
using Microsoft.Extensions.Logging;

namespace Ecs.Stations;

public class StationService : EcsAppService, IStationService
{
    private StationManager _stationManager;
    private ILogger<StationService> _logger;

    public StationService(
        StationManager stationManager,
        ILogger<StationService> logger)
    {
        _stationManager = stationManager;
        _logger = logger;
    }

    public async Task<ResponseDto> AddOneStationAsync(StationDto station)
    {
        try
        {
            Station s = ObjectMapper.Map<StationDto, Station>(station);
            var r = await _stationManager.AddStationAsync(s).ConfigureAwait(false);
            return new ResponseDto(){ success = r != null, message = r != null ? "添加成功" : "添加失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message }; 
        }
    }

    public async Task<ResponseDto> AddStationsAsync(List<StationDto> stations)
    {
        try
        {
            List<Station> ss = ObjectMapper.Map<List<StationDto>, List<Station>>(stations);
            var r = await _stationManager.AddStationsAsync(ss).ConfigureAwait(false);
            return new ResponseDto(){ success = r, message = r ? "添加成功" : "添加失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message }; 
        }
    }

    public async Task<ResponseDto> DelAllStationsAsync()
    {
        try
        {
            var r = await _stationManager.RemoveAllStationsAsync().ConfigureAwait(false);
            return new ResponseDto(){ success = r, message = r ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message }; 
        }
    }

    public async Task<ResponseDto> DelOneStationAsync(string stationCode)
    {
        try
        {
            var r = await _stationManager.RemoveStationWithCodeAsync(stationCode).ConfigureAwait(false);
            return new ResponseDto(){ success = r, message = r ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message }; 
        }
    }

    public async Task<List<StationDto>> GetAllStationsAsync()
    {
        try
        {
            List<Station> stations = await _stationManager.GetAllStationsAsync().ConfigureAwait(false);
            if(stations == null || stations.Count == 0)
                return new List<StationDto>();

            return ObjectMapper.Map<List<Station>, List<StationDto>>(stations);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StationDto>(); 
        }
    }
}