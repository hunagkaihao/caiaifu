using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecs.LogTool;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Ecs.Stations;

public class VariableManager : ISingletonDependency
{
    private IRepository<Variable> _variableRepository;
    private ILogger<VariableManager> _logger;

    public VariableManager(
        IRepository<Variable> paraRepository,
        ILogger<VariableManager> logger)
    {
        _variableRepository = paraRepository;
        _logger = logger;
    }

    /// <summary>
    /// 添加变量，其中的Value自动改为DefaultValue
    /// </summary>
    /// <param name="v"></param>
    /// <returns>true：添加成功， false：添加失败</returns>
    public async Task<bool> AddOneVariableAsync(Variable v)
    {
        try
        {
            Check.NotNullOrEmpty(v.VName, nameof(v.VName));
            Check.NotNull(v.DefaultValue, nameof(v.DefaultValue));
            Check.NotNull(v.Value, nameof(v.Value));

            var existVariables = await _variableRepository.GetListAsync(o => o.VName == v.VName).ConfigureAwait(false);
            if(existVariables.Count > 0)
                throw new Exception($"名为{v.VName}的变量已经存在");

            v.Value = v.DefaultValue;
            await _variableRepository.InsertAsync(v).ConfigureAwait(false);
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 添加多个变量，其中的Value自动改为DefaultValue
    /// </summary>
    /// <param name="vs"></param>
    /// <returns></returns>
    [UnitOfWork]
    public async Task<bool> AddVariablesAsync(List<Variable> vs)
    {
        try
        {
            foreach(var v in vs)
            {
                Check.NotNullOrEmpty(v.VName, nameof(v.VName));
                Check.NotNull(v.DefaultValue, nameof(v.DefaultValue));
                Check.NotNull(v.Value, nameof(v.Value));

                if(vs.Where(o => o.VName == v.VName).ToList().Count > 1)
                    throw new Exception($"准备添加的变量中，变量名为{v.VName}的变量重复");

                var existVariables = await _variableRepository.GetListAsync(o => o.VName == v.VName).ConfigureAwait(false);
                if(existVariables.Count > 0)
                    throw new Exception($"名为{v.VName}的变量已经存在");
            }
            
            foreach(var v in vs)
            {
                v.Value = v.DefaultValue;
                await _variableRepository.InsertAsync(v).ConfigureAwait(false);
            }

            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 删除指定变量名的变量，若原本没有这个变量，也返回true
    /// </summary>
    /// <param name="vName"></param>
    /// <returns></returns>
    [UnitOfWork]
    public async Task<bool> DelOneVariableAsync(string vName)
    {
        try
        {
            var existVariables = await _variableRepository.GetListAsync(o => o.VName == vName).ConfigureAwait(false);
            if(existVariables.Count == 0)
                return true;

            foreach(var v in existVariables)
                await _variableRepository.DeleteAsync(v).ConfigureAwait(false);

            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 删除所有变量，若原本没有变量，也返回true
    /// </summary>
    /// <returns></returns>
    [UnitOfWork]
    public async Task<bool> DelAllVariablesAsync()
    {
        try
        {
            var existVariables = await _variableRepository.GetListAsync().ConfigureAwait(false);
            if(existVariables.Count == 0)
                return true;

            foreach(var v in existVariables)
                await _variableRepository.DeleteAsync(v).ConfigureAwait(false);

            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 修改变量值，若变量类型为常量，则不能修改
    /// </summary>
    /// <param name="vName"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [UnitOfWork]
    public async Task<bool> UpdateVariableValueAsync(string vName, string value)
    {
        try
        {
            var existVariables = await _variableRepository.GetListAsync(o => o.VName == vName).ConfigureAwait(false);
            if(existVariables.Count == 0)
                return false;

            //事实上，按照变量名只能查询到一个变量
            foreach(var v in existVariables) 
            {
                if(v.VType == VariableType.Constant)
                    throw new Exception($"变量名为{vName}的变量为常量，不能赋值");
            }

            foreach(var v in existVariables)
            {    
                v.Value = value;
                await _variableRepository.UpdateAsync(v).ConfigureAwait(false);
            }

            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 根据变量名查询变量，若未找到或发生错误，返回null
    /// </summary>
    /// <param name="vName"></param>
    /// <returns></returns>
    public async Task<Variable> GetVariableByNameAsync(string vName)
    {
        try
        {
            var existVariables = await _variableRepository.GetListAsync(o => o.VName == vName).ConfigureAwait(false);
            if(existVariables.Count == 0)
                return null;
            if(existVariables.Count > 1)
                throw new Exception($"变量名为{vName}的变量数量多于1个");

            return existVariables[0];
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 查询所有的变量，按Id升序排列，若未找到或发生错误，返回空集合
    /// </summary>
    /// <returns></returns>
    public async Task<List<Variable>> GetAllVariablesAsync()
    {
        try
        {
            var variables = await _variableRepository.GetListAsync().ConfigureAwait(false);
            return variables.OrderBy(o => o.Id).ToList();
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<Variable>();
        }
    }

}