using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ecs.LogTool;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Ecs.Stations;

public class StepManager : ISingletonDependency
{
    private IRepository<Step> _stepRepository;
    private IRepository<StepPara> _stepParaRepository;
    private IServiceProvider _serviceProvider;
    private ILogger<StepManager> _logger;

    public StepManager(
        IRepository<Step> stepRepository,
        IRepository<StepPara> stepParaRepository,
        IServiceProvider serviceProvider,
        ILogger<StepManager> logger)
    {
        _stepRepository = stepRepository;
        _stepParaRepository = stepParaRepository;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 增加步骤
    /// </summary>
    /// <param name="step"></param>
    /// <returns></returns>
    public async Task<bool> AddOneStepAsync(Step step)
    {
        try
        {
            Check.NotNullOrEmpty(step.StepClsName, nameof(step.StepClsName));
            var steps = await _stepRepository.GetListAsync(o => o.StepClsName == step.StepClsName).ConfigureAwait(false);
            if(steps.Count > 0)
                throw new Exception($"步骤{step.StepClsName}已经存在");

            await _stepRepository.InsertAsync(step).ConfigureAwait(false);
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 增加多个步骤
    /// </summary>
    /// <param name="steps"></param>
    /// <returns></returns>
    [UnitOfWork]
    public async Task<bool> AddStepsAsync(List<Step> steps)
    {
        try
        {
            foreach(var step in steps)
            {
                Check.NotNullOrEmpty(step.StepClsName, nameof(step.StepClsName));
                if(steps.Where(o => o.StepClsName == step.StepClsName).ToList().Count > 1)
                    throw new Exception($"存在重复的步骤类名{step.StepClsName}");
                var ss = await _stepRepository.GetListAsync(o => o.StepClsName == step.StepClsName).ConfigureAwait(false);
                if(ss.Count > 0)
                    throw new Exception($"步骤类名{step.StepClsName}已经存在");
            }
            
            foreach(var step in steps)
            {
                await _stepRepository.InsertAsync(step).ConfigureAwait(false);
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
    /// 删除指定名称的步骤
    /// </summary>
    /// <param name="stepClsName"></param>
    /// <returns></returns>
    public async Task<bool> DelOneStepAsync(string stepClsName)
    {
        try
        {
            var steps = await _stepRepository.GetListAsync(o => o.StepClsName == stepClsName).ConfigureAwait(false);
            if(steps.Count == 0)
                return true;

            await _stepRepository.DeleteAsync(o => o.StepClsName == stepClsName).ConfigureAwait(false);
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 删除所有步骤
    /// </summary>
    /// <returns></returns>
    public async Task<bool> DelAllStepsAsync()
    {
        try
        {
            await _stepRepository.DeleteAsync(o => o.Id > 0).ConfigureAwait(false);
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 查询指定名称的步骤，未找到或发生错误返回null
    /// </summary>
    /// <param name="stepClsName"></param>
    /// <returns></returns>
    public async Task<Step> GetOneStepAsync(string stepClsName)
    {
        try
        {
            var steps = await _stepRepository.GetListAsync(o => o.StepClsName == stepClsName).ConfigureAwait(false);
            if(steps.Count == 0)
                return null;

            return steps[0];
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 查询所有步骤，按Id升序排列，若没有步骤或发生错误返回空集合
    /// </summary>
    /// <returns></returns>
    public async Task<List<Step>> GetAllStepsAsync()
    {
        try
        {
            var steps = await _stepRepository.GetListAsync().ConfigureAwait(false);
            return steps.OrderBy(o => o.Id).ToList();
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<Step>();
        }
    }

    /// <summary>
    /// 增加一个步骤参数
    /// </summary>
    /// <param name="para"></param>
    /// <returns></returns>
    public async Task<bool> AddOneStepParaAsync(StepPara para)
    {
        try
        {
            Check.NotNullOrEmpty(para.StepClsName, nameof(para.StepClsName));
            Check.NotNullOrEmpty(para.ParaName, nameof(para.ParaName));

            var steps = await _stepRepository.GetListAsync(o => o.StepClsName == para.StepClsName).ConfigureAwait(false);
            if(steps.Count == 0)
                throw new Exception($"步骤类名{para.StepClsName}不存在");

            await _stepParaRepository.InsertAsync(para).ConfigureAwait(false);
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }
    
    /// <summary>
    /// 增加多个步骤参数
    /// </summary>
    /// <param name="paras"></param>
    /// <returns></returns>
    [UnitOfWork]
    public async Task<bool> AddStepParasAsync(List<StepPara> paras)
    {
        try
        {
            foreach(var para in paras)
            {
                Check.NotNullOrEmpty(para.StepClsName, nameof(para.StepClsName));
                Check.NotNullOrEmpty(para.ParaName, nameof(para.ParaName));

                var steps = await _stepRepository.GetListAsync(o => o.StepClsName == para.StepClsName).ConfigureAwait(false);
                if(steps.Count == 0)
                    throw new Exception($"步骤{para.StepClsName}不存在");

                if(paras.Where(o => o.StepClsName == para.StepClsName && o.ParaName == para.ParaName).ToList().Count > 1)
                    throw new Exception($"准备添加的参数中，步骤{para.StepClsName}的参数{para.ParaName}重复");

                var ps = await _stepParaRepository
                    .GetListAsync(o => o.StepClsName == para.StepClsName && o.ParaName == para.ParaName)
                    .ConfigureAwait(false);
                if(ps.Count > 0)
                    throw new Exception($"步骤{para.StepClsName}的参数{para.ParaName}已存在");
            }
            
            foreach(var para in paras)
            {
                await _stepParaRepository.InsertAsync(para).ConfigureAwait(false);
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
    /// 删除步骤的一个参数，若原本没有该参数，也返回true
    /// </summary>
    /// <param name="stepClsName"></param>
    /// <param name="paraName"></param>
    /// <returns></returns>
    public async Task<bool> DelOneStepParaAsync(string stepClsName, string paraName)
    {
        try
        {
            var steps = await _stepParaRepository.GetListAsync(o => o.StepClsName == stepClsName && o.ParaName == paraName).ConfigureAwait(false);
            if(steps.Count == 0)
                return true;

            await _stepParaRepository.DeleteAsync(o => o.StepClsName == stepClsName && o.ParaName == paraName).ConfigureAwait(false);
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 删除某个步骤的所有参数，若原本没有参数，也返回true
    /// </summary>
    /// <param name="stepClsName"></param>
    /// <returns></returns>
    public async Task<bool> DelAllParasOfStepAsync(string stepClsName)
    {
        try
        {
            var steps = await _stepParaRepository.GetListAsync(o => o.StepClsName == stepClsName).ConfigureAwait(false);
            if(steps.Count == 0)
                return true;

            await _stepParaRepository.DeleteAsync(o => o.StepClsName == stepClsName).ConfigureAwait(false);
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 删除所有的参数，若原本没有参数，也返回true
    /// </summary>
    /// <returns></returns>
    public async Task<bool> DelAllParasAsync()
    {
        try
        {
            var steps = await _stepParaRepository.GetListAsync(o => o.Id > 0).ConfigureAwait(false);
            if(steps.Count == 0)
                return true;

            await _stepParaRepository.DeleteAsync(o => o.Id > 0).ConfigureAwait(false);
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 根据步骤名和参数名查询步骤参数，若未查到或发生错误，返回null
    /// </summary>
    /// <param name="stepClsName"></param>
    /// <param name="paraName"></param>
    /// <returns></returns>
    public async Task<StepPara> GetOneStepParaAsync(string stepClsName, string paraName)
    {
        try
        {
            var paras = await _stepParaRepository
                .GetListAsync(o => o.StepClsName == stepClsName && o.ParaName == paraName)
                .ConfigureAwait(false);
            if(paras.Count == 0)
                return null;

            return paras[0];
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 查询步骤的所有参数，按照Id升序排序，若未查到或发生错误，返回空集合
    /// </summary>
    /// <param name="stepClsName"></param>
    /// <returns></returns>
    public async Task<List<StepPara>> GetAllParasOfStepAsync(string stepClsName)
    {
        try
        {
            var paras = await _stepParaRepository.GetListAsync(o => o.StepClsName == stepClsName).ConfigureAwait(false);
            return paras.OrderBy(o => o.Id).ToList();
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StepPara>();
        }
    }

    public async Task<List<StepPara>> GetAllStepParasAsync()
    {
        try
        {
            var paras = await _stepParaRepository.GetListAsync().ConfigureAwait(false);
            return paras.OrderBy(o => o.Id).ToList();
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StepPara>();
        }
    }

    /// <summary>
    /// 创建Step实例
    /// </summary>
    /// <param name="stepClsName"></param>
    /// <returns></returns>
    public IStep CreateStep(string stepClsName)
    {
        try
        {
            Assembly ass = Assembly.Load("Ecs.Domain");
            Type stepClassType = ass.GetType($"Ecs.Stations.{stepClsName}");
            if (stepClassType == null)
            {
                return null;
            }

            return (IStep)_serviceProvider.GetService(stepClassType);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

}