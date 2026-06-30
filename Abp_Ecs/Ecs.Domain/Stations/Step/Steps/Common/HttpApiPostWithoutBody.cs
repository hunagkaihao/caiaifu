using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecs.HttpApiTool;
using Ecs.LogTool;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Ecs.Stations;

public class HttpApiPostWithoutBody : IStep, ITransientDependency
{
    public Dictionary<string, string> ParaVariableMaps { get; set; } = new Dictionary<string, string>(){
        {"BaseUrl", null}, 
        {"Resource", null}
    };

    public StationStep RelativeStep { get; set; }
    public int StepResult { get; set; } = 0;

    
    private ILogger<HttpApiPostWithoutBody> _logger;
    private VariableManager _variableManager;
    private StationMonitorManager _monitorManager;
    private StationStepManager _stationStepManager;
    
    public HttpApiPostWithoutBody(
        VariableManager variableManager,
        StationMonitorManager monitorManager,
        StationStepManager stationStepManager,
        ILogger<HttpApiPostWithoutBody> logger)
    {
        _variableManager = variableManager;
        _monitorManager = monitorManager;
        _stationStepManager = stationStepManager;
        _logger = logger;
    }

    public async Task<bool> LinkParas()
    {
        try
        {
            var paraLinks = await _stationStepManager.GetParaLinksOfStationStepAsync(
                RelativeStep.StationCode, 
                RelativeStep.StepNo)
                .ConfigureAwait(false);

            //判断配置的参数数量与实际的参数数量是否一致
            if(paraLinks.Count != ParaVariableMaps.Count) 
                throw new Exception($"步骤存在{ParaVariableMaps.Count}个参数需要关联变量，但实际配置了{paraLinks.Count}个");

            if(paraLinks.Count == 0) 
                return true;
            
            Dictionary<string, string> settingDic = new Dictionary<string, string>();
            foreach(var paraLink in paraLinks)
            {
                settingDic.Add(paraLink.StepParaName, paraLink.VariableName);
            }

            foreach(var pvm in ParaVariableMaps)
            {
                if(!settingDic.Keys.Contains(pvm.Key))
                    throw new Exception($"没有为参数{pvm.Key}配置对应变量");
                ParaVariableMaps[pvm.Key] = settingDic[pvm.Key];
            }
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    public async Task<bool> Execute()
    {
        try
        {
            var baseUrlTag = await _variableManager.GetVariableByNameAsync(ParaVariableMaps["BaseUrl"]).ConfigureAwait(false);
            if(baseUrlTag == null)
                throw new Exception($"不存在名为{ParaVariableMaps["BaseUrl"]}的变量定义");

            var resourceTag = await _variableManager.GetVariableByNameAsync(ParaVariableMaps["Resource"]).ConfigureAwait(false);
            if(resourceTag == null)
                throw new Exception($"不存在名为{ParaVariableMaps["Resource"]}的变量定义");

            var ret = await HttpApiHelper.PostAsync(baseUrlTag.Value, resourceTag.Value, null).ConfigureAwait(false);
            if(!ret.IsSuccessful)
            {
                await _monitorManager.UpdateStationStepInfoAsync(
                    RelativeStep.StationCode,
                    $"POST方法，路径{baseUrlTag.Value}，资源{resourceTag.Value}，执行失败，错误信息{ret.ErrorException.Message}"
                );
                await Task.Delay(1000).ConfigureAwait(false);//防止太频繁的调用接口
                return false;
            }
            
            await _monitorManager.UpdateStationStepInfoAsync(
                RelativeStep.StationCode,
                $"POST方法，路径{baseUrlTag.Value}，资源{resourceTag.Value}，执行成功"
            );
            _logger.Info($"POST方法，路径{baseUrlTag.Value}，资源{resourceTag.Value}，执行成功");
            return true;
        }
        catch(Exception ex)
        {
            await _monitorManager.UpdateStationStepInfoAsync(RelativeStep.StationCode, $"步骤执行失败：{ex.Message}");
            return false;
        }
    }
}