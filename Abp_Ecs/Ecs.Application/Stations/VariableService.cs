using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecs.LogTool;
using Microsoft.Extensions.Logging;

namespace Ecs.Stations;

public class VariableService : EcsAppService, IVariableService
{
    private readonly VariableManager _variableManager;
    private readonly ILogger<VariableManager> _logger;

    public VariableService(
        VariableManager variableManager,
        ILogger<VariableManager> logger)
    {
        _variableManager = variableManager;
        _logger = logger;
    }

    public async Task<ResponseDto> AddOneVariableAsync(VariableDto v)
    {
        try
        {
            Variable variable = ObjectMapper.Map<VariableDto, Variable>(v);
            bool ret = await _variableManager.AddOneVariableAsync(variable).ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "添加成功" : "添加失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message };
        }
    }

    public async Task<ResponseDto> AddVariablesAsync(List<VariableDto> vs)
    {
        try
        {
            List<Variable> variables = ObjectMapper.Map<List<VariableDto>, List<Variable>>(vs);
            bool ret = await _variableManager.AddVariablesAsync(variables).ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "添加成功" : "添加失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message };
        }
    }

    public async Task<ResponseDto> DelAllVariablesAsync()
    {
        try
        {
            bool ret = await _variableManager.DelAllVariablesAsync().ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message };
        }
    }

    public async Task<ResponseDto> DelOneVariableAsync(string variableName)
    {
        try
        {
            bool ret = await _variableManager.DelOneVariableAsync(variableName).ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "删除成功" : "删除失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message };
        }
    }

    public async Task<List<VariableDto>> GetAllVariablesAsync()
    {
        try
        {
            var result = await _variableManager.GetAllVariablesAsync().ConfigureAwait(false);
            return ObjectMapper.Map<List<Variable>, List<VariableDto>>(result);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<VariableDto>();
        }
    }

    public async Task<VariableDto> GetVariableAsync(string vName)
    {
        try
        {
            var result = await _variableManager.GetVariableByNameAsync(vName).ConfigureAwait(false);
            if(result == null)
                return null;
            return ObjectMapper.Map<Variable, VariableDto>(result);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

    public async Task<ResponseDto> UpdateVariableValueAsync(UpdateVariableDto para)
    {
        try
        {
            bool ret = await _variableManager.UpdateVariableValueAsync(para.variableName, para.variableValue).ConfigureAwait(false);
            return new ResponseDto(){ success = ret, message = ret ? "更细成功" : "更新失败" };
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new ResponseDto(){ success = false, message = ex.Message };
        }
    }
}