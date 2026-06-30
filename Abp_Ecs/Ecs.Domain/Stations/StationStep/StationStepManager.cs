using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ecs.LogTool;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Ecs.Stations;

public class StationStepManager : ISingletonDependency
{
    private IRepository<StationStep> _stationStepRepository;
    private IRepository<StationStepParaLink> _paraLinkRepository;
    private StepManager _stepManager;
    private VariableManager _variableManager;
    private ILogger<StationStepManager> _logger;

    public StationStepManager(
        IRepository<StationStep> stationStepRepository,
        IRepository<StationStepParaLink> paraLinkRepository,
        StepManager stepManager,
        VariableManager variableManager,
        ILogger<StationStepManager> logger)
    {
        _stationStepRepository = stationStepRepository;
        _paraLinkRepository = paraLinkRepository;
        _stepManager = stepManager;
        _variableManager = variableManager;
        _logger = logger;
    }

    /// <summary>
    /// 站点添加步骤
    /// </summary>
    /// <param name="step"></param>
    /// <returns></returns>
    public async Task<bool> AddOneStationStepAsync(StationStep step)
    {
        try
        {
            Check.NotNullOrEmpty(step.StepName, nameof(step.StepName));
            Check.NotNullOrEmpty(step.StationCode, nameof(step.StationCode));
            Check.NotNullOrEmpty(step.StepClsName, nameof(step.StepClsName));
            Check.NotNullOrEmpty(step.NextStepNo, nameof(step.NextStepNo));
            Check.Range(step.StepNo, nameof(step.StepNo), 1, 1024);
            Regex regex = new Regex("^0:[1-9][0-9]*,1:[1-9][0-9]*,2:[1-9][0-9]*,3:[1-9][0-9]*,4:[1-9][0-9]*,5:[1-9][0-9]*,6:[1-9][0-9]*,7:[1-9][0-9]*,8:[1-9][0-9]*,9:[1-9][0-9]*$");
            if(!regex.IsMatch(step.NextStepNo))
                throw new Exception("NextStepNo不符合格式：^0:[1-9][0-9]*,1:[1-9][0-9]*,2:[1-9][0-9]*,3:[1-9][0-9]*,4:[1-9][0-9]*,5:[1-9][0-9]*,6:[1-9][0-9]*,7:[1-9][0-9]*,8:[1-9][0-9]*,9:[1-9][0-9]*$");

            var stepsExist = await _stationStepRepository
                .GetListAsync(o => o.StationCode == step.StationCode && o.StepNo == step.StepNo)
                .ConfigureAwait(false);
            if(stepsExist.Count > 0)
                throw new Exception($"站点为{step.StationCode}，步骤号为{step.StepNo}的步骤已经存在，添加失败");
            
            await _stationStepRepository.InsertAsync(step).ConfigureAwait(false);
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 一次性添加站点的全部步骤
    /// </summary>
    /// <param name="steps"></param>
    /// <returns></returns>
    [UnitOfWork]
    public async Task<bool> AddAllStationStepsOfStationAsync(List<StationStep> steps)
    {
        try
        {
            if(steps == null || steps.Count == 0)
                throw new Exception("步骤数量不能为零");

            foreach(var step in steps)
            {
                Check.NotNullOrEmpty(step.StepName, nameof(step.StepName));
                Check.NotNullOrEmpty(step.StationCode, nameof(step.StationCode));
                Check.NotNullOrEmpty(step.StepClsName, nameof(step.StepClsName));
                Check.NotNullOrEmpty(step.NextStepNo, nameof(step.NextStepNo));
                Check.Range(step.StepNo, nameof(step.StepNo), 1, 1024);
                Regex regex = new Regex("^0:[1-9][0-9]*,1:[1-9][0-9]*,2:[1-9][0-9]*,3:[1-9][0-9]*,4:[1-9][0-9]*,5:[1-9][0-9]*,6:[1-9][0-9]*,7:[1-9][0-9]*,8:[1-9][0-9]*,9:[1-9][0-9]*$");
                if(!regex.IsMatch(step.NextStepNo))
                    throw new Exception("存在NextStepNo不符合格式：^0:[1-9][0-9]*,1:[1-9][0-9]*,2:[1-9][0-9]*,3:[1-9][0-9]*,4:[1-9][0-9]*,5:[1-9][0-9]*,6:[1-9][0-9]*,7:[1-9][0-9]*,8:[1-9][0-9]*,9:[1-9][0-9]*$");
                if(steps.Count != steps.Where(o => o.StationCode == step.StationCode).ToList().Count)
                    throw new Exception("每个步骤的StationCode不完全相同");
                if(steps.Where(o => o.StepNo == step.StepNo).ToList().Count() > 1)
                    throw new Exception("存在步骤号重复的步骤");   
            }

            var stepsExist = await _stationStepRepository
                .GetListAsync(o => o.StationCode == steps[0].StationCode)
                .ConfigureAwait(false);
            if(stepsExist.Count > 0)
                throw new Exception($"站点{steps[0].StationCode}已经存在步骤");
            
            foreach(var step in steps)
            {
                await _stationStepRepository.InsertAsync(step).ConfigureAwait(false);
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
    /// 删除所有站点的所有步骤
    /// </summary>
    /// <returns></returns>
    public async Task<bool> DelAllStationStepsAsync()
    {
        try
        {
            var stepsExist = await _stationStepRepository
                .GetListAsync(o => o.Id > 0)
                .ConfigureAwait(false);
            if(stepsExist.Count == 0)
                return true;
            
            await _stationStepRepository.DeleteAsync(o => o.Id > 0).ConfigureAwait(false);
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 删除一个站点的所有步骤，若原本不存在该站点，也返回true
    /// </summary>
    /// <param name="stationCode"></param>
    /// <returns></returns>
    public async Task<bool> DelAllStationStepsOfStationAsync(string stationCode)
    {
        try
        {
            if(string.IsNullOrEmpty(stationCode))
                throw new ArgumentNullException(nameof(stationCode));

            var stepsExist = await _stationStepRepository
                .GetListAsync(o => o.StationCode == stationCode)
                .ConfigureAwait(false);
            if(stepsExist.Count == 0)
                return true;
            
            await _stationStepRepository.DeleteAsync(o => o.StationCode == stationCode).ConfigureAwait(false);
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 删除站点的某一个步骤
    /// </summary>
    /// <param name="stationCode"></param>
    /// <param name="stepNo"></param>
    /// <returns></returns>
    public async Task<bool> DelOneStationStepAsync(string stationCode, int stepNo)
    {
        try
        {
            if(string.IsNullOrEmpty(stationCode))
                throw new ArgumentNullException(nameof(stationCode));
            
            if(stepNo <= 0)
                throw new ArgumentOutOfRangeException(nameof(stepNo));

            var stepsExist = await _stationStepRepository
                .GetListAsync(o => o.StationCode == stationCode && o.StepNo == stepNo)
                .ConfigureAwait(false);
            if(stepsExist.Count == 0)
                return true;
            
            await _stationStepRepository.DeleteAsync(o => o.StationCode == stationCode && o.StepNo == stepNo).ConfigureAwait(false);
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 查询所有站点中的所有步骤，按Id升序排列，未查询到步骤或发生错误，返回空集合
    /// </summary>
    /// <returns></returns>
    public async Task<List<StationStep>> GetAllStationStepsAsync()
    {
        try
        {
            var steps = await _stationStepRepository
                .GetListAsync(o => o.Id > 0)
                .ConfigureAwait(false);
            
            return steps.OrderBy(o => o.Id).ToList();
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StationStep>();
        }
    }

    /// <summary>
    /// 查询站点中的所有步骤，按Id升序排列，未查询到步骤或发生错误，返回空集合
    /// </summary>
    /// <param name="stationCode"></param>
    /// <returns></returns>
    public async Task<List<StationStep>> GetAllStationStepsOfStationAsync(string stationCode)
    {
        try
        {
            if(string.IsNullOrEmpty(stationCode))
                throw new ArgumentNullException(nameof(stationCode));

            var steps = await _stationStepRepository
                .GetListAsync(o => o.StationCode == stationCode)
                .ConfigureAwait(false);

            if(steps.Count == 0)
                return new List<StationStep>();
            
            return steps.OrderBy(o => o.Id).ToList();
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StationStep>();
        }
    }

    /// <summary>
    /// 查询站点的指定步骤，未查询到数据或发生错误，返回null
    /// </summary>
    /// <param name="stationCode"></param>
    /// <param name="stepNo"></param>
    /// <returns></returns>
    public async Task<StationStep> GetOneStationStepAsync(string stationCode, int stepNo)
    {
        try
        {
            if(string.IsNullOrEmpty(stationCode))
                throw new ArgumentNullException(nameof(stationCode));

            if(stepNo <= 0)
                throw new ArgumentOutOfRangeException(nameof(stepNo));

            var steps = await _stationStepRepository
                .GetListAsync(o => o.StationCode == stationCode && o.StepNo == stepNo)
                .ConfigureAwait(false);

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
    /// 为步骤参数绑定变量
    /// </summary>
    /// <returns></returns>
    public async Task<bool> AddOneParaLinkAsync(StationStepParaLink link)
    {
        try
        {
            Check.NotNullOrEmpty(link.StationCode, nameof(link.StationCode));
            Check.Range(link.StepNo, nameof(link.StepNo), 0, 1024);
            Check.NotNullOrEmpty(link.StepParaName, nameof(link.StepParaName));
            Check.NotNullOrEmpty(link.VariableName, nameof(link.VariableName));

            var stationSteps = await _stationStepRepository
                .GetListAsync(o => o.StationCode == link.StationCode && o.StepNo == link.StepNo)
                .ConfigureAwait(false);
            if(stationSteps.Count == 0)
                throw new Exception($"不存在站点为{link.StationCode}，步骤号为{link.StepNo}的步骤");
            if(stationSteps.Count > 1)
                throw new Exception($"站点为{link.StationCode}，步骤号为{link.StepNo}的步骤多于1个");


            var stepPara = await _stepManager.GetOneStepParaAsync(stationSteps[0].StepClsName ,link.StepParaName).ConfigureAwait(false);
            if(stepPara == null)
                throw new Exception($"步骤{stationSteps[0].StepClsName}不存在名为{link.StepParaName}的参数");


            var variable = await _variableManager.GetVariableByNameAsync(link.VariableName).ConfigureAwait(false);
            if(variable == null)
                throw new Exception($"不存在变量名为{link.VariableName}的变量");

            
            var links = await _paraLinkRepository
                .GetListAsync(o => o.StationCode == link.StationCode && o.StepNo == link.StepNo && o.StepParaName == link.StepParaName)
                .ConfigureAwait(false);
            if(links.Count > 0)
                throw new Exception($"站点为{link.StationCode}，步骤号为{link.StepNo}的步骤已为步骤参数{link.StepParaName}关联变量");

            
            await _paraLinkRepository.InsertAsync(link).ConfigureAwait(false);
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 添加多个步骤参数关联
    /// </summary>
    /// <param name="links"></param>
    /// <returns></returns>
    [UnitOfWork]
    public async Task<bool> AddParaLinksAsync(List<StationStepParaLink> links)
    {
        try
        {
            foreach(var link in links)
            {
                Check.NotNullOrEmpty(link.StationCode, nameof(link.StationCode));
                Check.Range(link.StepNo, nameof(link.StepNo), 0, 1024);
                Check.NotNullOrEmpty(link.StepParaName, nameof(link.StepParaName));
                Check.NotNullOrEmpty(link.VariableName, nameof(link.VariableName));

                //判断需要增加的关联中是否存在重复关联
                if(links.Where(o => 
                    o.StationCode == link.StationCode && 
                    o.StepNo == link.StepNo && 
                    o.StepParaName == link.StepParaName).ToList().Count > 1)
                    throw new Exception($"准备添加的参数关联中，站点{link.StationCode}，步骤{link.StepNo}，参数{link.StepParaName}重复关联变量");
                
                //判断是否有这个站点和这个步骤
                var stationSteps = await _stationStepRepository
                    .GetListAsync(o => o.StationCode == link.StationCode && o.StepNo == link.StepNo)
                    .ConfigureAwait(false);
                if(stationSteps.Count == 0)
                    throw new Exception($"不存在站点为{link.StationCode}，步骤号为{link.StepNo}的步骤");
                if(stationSteps.Count > 1)
                    throw new Exception($"站点为{link.StationCode}，步骤号为{link.StepNo}的步骤多于1个");

                //判断步骤是否有这个步骤参数
                var stepPara = await _stepManager.GetOneStepParaAsync(stationSteps[0].StepClsName ,link.StepParaName).ConfigureAwait(false);
                if(stepPara == null)
                    throw new Exception($"步骤{stationSteps[0].StepClsName}不存在名为{link.StepParaName}的参数");

                //判断是否有这个变量
                var variable = await _variableManager.GetVariableByNameAsync(link.VariableName).ConfigureAwait(false);
                if(variable == null)
                    throw new Exception($"不存在变量名为{link.VariableName}的变量");

                //判断该参数在站点的这个步骤中，是否已经关联变量
                var lks = await _paraLinkRepository
                    .GetListAsync(o => o.StationCode == link.StationCode && o.StepNo == link.StepNo && o.StepParaName == link.StepParaName)
                    .ConfigureAwait(false);
                if(lks.Count > 0)
                    throw new Exception($"站点为{link.StationCode}，步骤号为{link.StepNo}的步骤已为步骤参数{link.StepParaName}关联变量");
            }
            
            foreach(var link in links)
            {
                await _paraLinkRepository.InsertAsync(link).ConfigureAwait(false);
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
    /// 删除站点步骤的某个参数关联
    /// </summary>
    /// <param name="stationCode"></param>
    /// <param name="stepNo"></param>
    /// <param name="stepPara"></param>
    /// <returns></returns>
    public async Task<bool> DelOneParaLinkAsync(string stationCode, int stepNo, string stepPara)
    {
        try
        {
            Check.NotNullOrEmpty(stationCode, nameof(stationCode));
            Check.NotNullOrEmpty(stepPara, nameof(stepPara));
            Check.Range(stepNo, nameof(stepNo), 1, 1024);

            var linksExist = await _paraLinkRepository
                .GetListAsync(o => o.StationCode == stationCode && o.StepNo == stepNo && o.StepParaName == stepPara)
                .ConfigureAwait(false);
            if(linksExist.Count == 0)
                return true;
            
            await _paraLinkRepository.DeleteAsync(o => 
                o.StationCode == stationCode && 
                o.StepNo == stepNo && 
                o.StepParaName == stepPara)
                .ConfigureAwait(false);

            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 删除站点步骤的所有参数关联
    /// </summary>
    /// <param name="stationCode"></param>
    /// <param name="stepNo"></param>
    /// <returns></returns>
    public async Task<bool> DelParaLinksOfStationStepAsync(string stationCode, int stepNo)
    {
        try
        {
            Check.NotNullOrEmpty(stationCode, nameof(stationCode));
            Check.Range(stepNo, nameof(stepNo), 1, 1024);

            var linksExist = await _paraLinkRepository
                .GetListAsync(o => o.StationCode == stationCode && o.StepNo == stepNo)
                .ConfigureAwait(false);
            if(linksExist.Count == 0)
                return true;
            
            await _paraLinkRepository.DeleteAsync(o => 
                o.StationCode == stationCode && 
                o.StepNo == stepNo)
                .ConfigureAwait(false);

            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 删除站点的所有参数关联
    /// </summary>
    /// <param name="stationCode"></param>
    /// <returns></returns>
    public async Task<bool> DelParaLinksOfStationAsync(string stationCode)
    {
        try
        {
            Check.NotNullOrEmpty(stationCode, nameof(stationCode));

            var linksExist = await _paraLinkRepository
                .GetListAsync(o => o.StationCode == stationCode)
                .ConfigureAwait(false);
            if(linksExist.Count == 0)
                return true;
            
            await _paraLinkRepository.DeleteAsync(o => 
                o.StationCode == stationCode)
                .ConfigureAwait(false);

            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 删除所有站点的所有参数关联
    /// </summary>
    /// <returns></returns>
    public async Task<bool> DelAllParaLinksAsync()
    {
        try
        {
            var linksExist = await _paraLinkRepository.GetListAsync(o => o.Id > 0).ConfigureAwait(false);
            
            if(linksExist.Count == 0)
                return true;
            
            await _paraLinkRepository.DeleteAsync(o => o.Id > 0).ConfigureAwait(false);

            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 查询站点步骤的某个参数关联
    /// </summary>
    /// <param name="stationCode"></param>
    /// <param name="stepNo"></param>
    /// <param name="paraName"></param>
    /// <returns></returns>
    public async Task<StationStepParaLink> GetOneParaLinkAsync(string stationCode, int stepNo, string paraName)
    {
        try
        {
            Check.NotNullOrEmpty(stationCode, nameof(stationCode));
            Check.NotNullOrEmpty(paraName, nameof(paraName));
            Check.Range(stepNo, nameof(stepNo), 1, 1024);

            var links = await _paraLinkRepository
                .GetListAsync(o => o.StationCode == stationCode && o.StepNo == stepNo && o.StepParaName == paraName)
                .ConfigureAwait(false);

            if(links.Count == 0)
                return null;

            if(links.Count > 1)
                throw new Exception($"站点{stationCode}，步骤{stepNo}，参数{paraName}关联的变量多于1个");

            return links[0];
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 查询站点步骤的所有参数关联，按照Id升序排列
    /// </summary>
    /// <param name="stationCode"></param>
    /// <param name="stepNo"></param>
    /// <returns></returns>
    public async Task<List<StationStepParaLink>> GetParaLinksOfStationStepAsync(string stationCode, int stepNo)
    {
        try
        {
            Check.NotNullOrEmpty(stationCode, nameof(stationCode));
            Check.Range(stepNo, nameof(stepNo), 1, 1024);

            var links = await _paraLinkRepository
                .GetListAsync(o => o.StationCode == stationCode && o.StepNo == stepNo)
                .ConfigureAwait(false);

            return links.OrderBy(o => o.Id).ToList();
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StationStepParaLink>();
        }
    }

    /// <summary>
    /// 查询站点的所有参数关联，按照Id升序排列
    /// </summary>
    /// <param name="stationCode"></param>
    /// <returns></returns>
    public async Task<List<StationStepParaLink>> GetParaLinksOfStationAsync(string stationCode)
    {
        try
        {
            Check.NotNullOrEmpty(stationCode, nameof(stationCode));

            var links = await _paraLinkRepository
                .GetListAsync(o => o.StationCode == stationCode)
                .ConfigureAwait(false);

            return links.OrderBy(o => o.Id).ToList();
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StationStepParaLink>();
        }
    }

    /// <summary>
    /// 获取所有站点所有步骤的参数关联，未查询到或发生错误，返回空集合
    /// </summary>
    /// <returns></returns>
    public async Task<List<StationStepParaLink>> GetAllParaLinksAsync()
    {
        try
        {
            var links = await _paraLinkRepository
                .GetListAsync(o => o.Id > 0)
                .ConfigureAwait(false);

            return links.OrderBy(o => o.Id).ToList();
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<StationStepParaLink>();
        }
    }
}