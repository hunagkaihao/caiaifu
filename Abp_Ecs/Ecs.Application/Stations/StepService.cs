using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecs.LogTool;
using Microsoft.Extensions.Logging;

namespace Ecs.Stations;

public class StepService : EcsAppService, IStepService
{
    private readonly StepManager _stepManager;
    private readonly ILogger<StepService> _logger;

    public StepService(
        StepManager stepManager,
        ILogger<StepService> logger)
    {
        _stepManager = stepManager;
        _logger = logger;
    }

    public async Task<ResponseDto> AddStepAsync(StepDto step)
    {
        try
        {
            Step s = ObjectMapper.Map<StepDto, Step>(step);
            bool r = await _stepManager.AddOneStepAsync(s).ConfigureAwait(false);
            return new ResponseDto(){ success = r, message = r ? "添加成功" : "添加失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message };
        }
    }

    public async Task<ResponseDto> AddStepsAsync(List<StepDto> steps)
    {
        try
        {
            List<Step> ss = ObjectMapper.Map<List<StepDto>, List<Step>>(steps);
            bool r = await _stepManager.AddStepsAsync(ss).ConfigureAwait(false);
            return new ResponseDto(){ success = r, message = r ? "添加成功" : "添加失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message };
        }
    }

    public async Task<ResponseDto> DelStepAsync(string stepClsName)
    {
        try
        {
            bool r = await _stepManager.DelOneStepAsync(stepClsName).ConfigureAwait(false);
            return new ResponseDto(){ success = r, message = r ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message };
        }
    }

    public async Task<ResponseDto> DelAllStepAsync()
    {
        try
        {
            bool r = await _stepManager.DelAllStepsAsync().ConfigureAwait(false);
            return new ResponseDto(){ success = r, message = r ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message };
        }
    }

    public async Task<List<StepDto>> GetAllStepsAsync()
    {
        try
        {
            var steps =  await _stepManager.GetAllStepsAsync().ConfigureAwait(false);
            return ObjectMapper.Map<List<Step>, List<StepDto>>(steps);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StepDto>();
        }
    }

    public async Task<ResponseDto> AddStepParaAsync(StepParaDto para)
    {
        try
        {
            StepPara p = ObjectMapper.Map<StepParaDto, StepPara>(para);
            bool r = await _stepManager.AddOneStepParaAsync(p).ConfigureAwait(false);
            return new ResponseDto(){ success = r, message = r ? "添加成功" : "添加失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message };
        }
    }

    public async Task<ResponseDto> AddStepParasAsync(List<StepParaDto> paras)
    {
        try
        {
            List<StepPara> ps = ObjectMapper.Map<List<StepParaDto>, List<StepPara>>(paras);
            bool r = await _stepManager.AddStepParasAsync(ps).ConfigureAwait(false);
            return new ResponseDto(){ success = r, message = r ? "添加成功" : "添加失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message };
        }
    }

    public async Task<ResponseDto> DelOneStepParaAsync(DelStepParaDto para)
    {
        try
        {
            bool r = await _stepManager.DelOneStepParaAsync(para.stepClsName, para.paraName).ConfigureAwait(false);
            return new ResponseDto(){ success = r, message = r ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message };
        }
    }

    public async Task<ResponseDto> DelParasOfStepAsync(string stepClsName)
    {
        try
        {
            bool r = await _stepManager.DelAllParasOfStepAsync(stepClsName).ConfigureAwait(false);
            return new ResponseDto(){ success = r, message = r ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message };
        }
    }

    public async Task<ResponseDto> DelAllParasAsync()
    {
        try
        {
            bool r = await _stepManager.DelAllParasAsync().ConfigureAwait(false);
            return new ResponseDto(){ success = r, message = r ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message };
        }
    }

    public async Task<List<StepParaDto>> GetStepParasOfStepAsync(string stepClsName)
    {
        try
        {
            List<StepPara> paras = await _stepManager.GetAllParasOfStepAsync(stepClsName).ConfigureAwait(false);
            return ObjectMapper.Map<List<StepPara>, List<StepParaDto>>(paras);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StepParaDto>();
        }
    }

    public async Task<List<StepParaDto>> GetAllStepParasAsync()
    {
        try
        {
            List<StepPara> paras = await _stepManager.GetAllStepParasAsync().ConfigureAwait(false);
            return ObjectMapper.Map<List<StepPara>, List<StepParaDto>>(paras);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StepParaDto>();
        }
    }
}