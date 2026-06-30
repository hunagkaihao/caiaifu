using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Ecs.LogTool;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Ecs.Stations;

public class StationStepService : EcsAppService, IStationStepService
{
    private readonly StationStepManager _stationStepManager;
    private readonly ILogger<StationStepService> _logger;

    public StationStepService(
        StationStepManager stationStepManager,
        ILogger<StationStepService> logger)
    {
        _stationStepManager = stationStepManager;
        _logger = logger;
    }

    public async Task<ResponseDto> AddOneStationStepAsync(StationStepDto step)
    {
        try
        {
            Check.NotNull(step, nameof(StationStepDto));
            StationStep s = ObjectMapper.Map<StationStepDto, StationStep>(step);
            bool ret = await _stationStepManager.AddOneStationStepAsync(s).ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "添加成功" : "添加失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message }; 
        }
    }

    public async Task<ResponseDto> AddAllStationStepsOfStationAsync(List<StationStepDto> steps)
    {
        try
        {
            Check.NotNull(steps, nameof(steps));
            List<StationStep> stationSteps = ObjectMapper.Map<List<StationStepDto>, List<StationStep>>(steps);
            bool ret = await _stationStepManager.AddAllStationStepsOfStationAsync(stationSteps).ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "添加成功" : "添加失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message }; 
        }
    }

    public async Task<ResponseDto> DelOneStationStepAsync(DelOneStationStepDto para)
    {
        try
        {
            Check.NotNull(para, nameof(DelOneStationStepDto));
            bool ret = await _stationStepManager.DelOneStationStepAsync(para.stationCode, para.stepNo).ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message }; 
        }
    }

    public async Task<ResponseDto> DelAllStationStepsOfStationAsync(string stationCode)
    {
        try
        {
            bool ret = await _stationStepManager.DelAllStationStepsOfStationAsync(stationCode).ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message }; 
        }
    }

    public async Task<ResponseDto> DelAllStationStepsAsync()
    {
        try
        {
            bool ret = await _stationStepManager.DelAllStationStepsAsync().ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message }; 
        }
    }

    public async Task<StationStepDto> GetOneStationStepAsync(GetOneStationStepDto para)
    {
        try
        {
            Check.NotNull(para, nameof(GetOneStationStepDto));
            var result = await _stationStepManager.GetOneStationStepAsync(para.stationCode, para.stepNo).ConfigureAwait(false);
            if(result == null) return null;
            return ObjectMapper.Map<StationStep, StationStepDto>(result);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return null; 
        }
    }

    public async Task<List<StationStepDto>> GetAllStationStepsOfStationAsync(string stationCode)
    {
        try
        {
            Check.NotNullOrEmpty(stationCode, nameof(stationCode));
            var result = await _stationStepManager.GetAllStationStepsOfStationAsync(stationCode).ConfigureAwait(false);
            return ObjectMapper.Map<List<StationStep>, List<StationStepDto>>(result);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StationStepDto>(); 
        }
    }

    public async Task<List<StationStepDto>> GetAllStationStepsAsync()
    {
        try
        {
            var result = await _stationStepManager.GetAllStationStepsAsync().ConfigureAwait(false);
            return ObjectMapper.Map<List<StationStep>, List<StationStepDto>>(result);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StationStepDto>(); 
        }
    }

    

    public async Task<ResponseDto> AddOneParaLinkAsync(StationStepParaLinkDto link)
    {
        try
        {
            Check.NotNull(link, nameof(StationStepParaLinkDto));
            StationStepParaLink lk = ObjectMapper.Map<StationStepParaLinkDto, StationStepParaLink>(link);
            bool ret = await _stationStepManager.AddOneParaLinkAsync(lk).ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "添加成功" : "添加失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message }; 
        }
    }

    public async Task<ResponseDto> AddParaLinksAsync(List<StationStepParaLinkDto> links)
    {
        try
        {
            Check.NotNull(links, nameof(List<StationStepParaLinkDto>));
            List<StationStepParaLink> lks = ObjectMapper.Map<List<StationStepParaLinkDto>, List<StationStepParaLink>>(links);
            bool ret = await _stationStepManager.AddParaLinksAsync(lks).ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "添加成功" : "添加失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message }; 
        }
    }

    public async Task<ResponseDto> DelOneParaLinkAsync(DelOneParaLinkDto para)
    {
        try
        {
            Check.NotNull(para, nameof(DelOneParaLinkDto));
            bool ret = await _stationStepManager.DelOneParaLinkAsync(para.stationCode, para.stepNo, para.stepParaName).ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message }; 
        }
    }

    public async Task<ResponseDto> DelParaLinksOfStationStepAsync(DelParaLinksOfStationStepDto para)
    {
        try
        {
            Check.NotNull(para, nameof(DelParaLinksOfStationStepDto));
            bool ret = await _stationStepManager.DelParaLinksOfStationStepAsync(para.stationCode, para.stepNo).ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message }; 
        }
    }

    public async Task<ResponseDto> DelParaLinksOfStationAsync(string stationCode)
    {
        try
        {
            Check.NotNull(stationCode, nameof(stationCode));
            bool ret = await _stationStepManager.DelParaLinksOfStationAsync(stationCode).ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message }; 
        }
    }

    public async Task<ResponseDto> DelAllParaLinksAsync()
    {
        try
        {
            bool ret = await _stationStepManager.DelAllParaLinksAsync().ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message }; 
        }
    }
    
    public async Task<StationStepParaLinkDto> GetOneParaLinkAsync(GetOneParaLinkDto para)
    {
        try
        {
            Check.NotNull(para, nameof(GetOneParaLinkDto));
            var result = await _stationStepManager.GetOneParaLinkAsync(para.stationCode, para.stepNo, para.stepParaName).ConfigureAwait(false);
            if(result == null) return null;
            return ObjectMapper.Map<StationStepParaLink, StationStepParaLinkDto>(result);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return null; 
        }
    }

    public async Task<List<StationStepParaLinkDto>> GetParaLinksOfStationStepAsync(GetParaLinksOfStationStepDto para)
    {
        try
        {
            Check.NotNull(para, nameof(GetParaLinksOfStationStepDto));
            var result = await _stationStepManager.GetParaLinksOfStationStepAsync(para.stationCode, para.stepNo).ConfigureAwait(false);
            return ObjectMapper.Map<List<StationStepParaLink>, List<StationStepParaLinkDto>>(result);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StationStepParaLinkDto>(); 
        }
    }

    public async Task<List<StationStepParaLinkDto>> GetParaLinksOfStationAsync(string stationCode)
    {
        try
        {
            Check.NotNullOrEmpty(stationCode, nameof(stationCode));
            var result = await _stationStepManager.GetParaLinksOfStationAsync(stationCode).ConfigureAwait(false);
            return ObjectMapper.Map<List<StationStepParaLink>, List<StationStepParaLinkDto>>(result);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StationStepParaLinkDto>(); 
        }
    }

    public async Task<List<StationStepParaLinkDto>> GetAllParaLinksAsync()
    {
        try
        {
            var result = await _stationStepManager.GetAllParaLinksAsync().ConfigureAwait(false);
            return ObjectMapper.Map<List<StationStepParaLink>, List<StationStepParaLinkDto>>(result);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StationStepParaLinkDto>(); 
        }
    }
}