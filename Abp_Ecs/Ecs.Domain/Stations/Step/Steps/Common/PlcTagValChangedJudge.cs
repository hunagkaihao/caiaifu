using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecs.LogTool;
using Ecs.PlcTool;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Ecs.Stations;

public class PlcTagValChangedJudge : IStep, ITransientDependency
{
    public Dictionary<string, string> ParaVariableMaps { get; set; } = new Dictionary<string, string>(){
        {"PlcName", null}, 
        {"PlcTagName", null}
    };

    public StationStep RelativeStep { get; set; }
    public int StepResult { get; set; } = 0;

    
    private ILogger<PlcTagValChangedJudge> _logger;
    private VariableManager _variableManager;
    private StationMonitorManager _monitorManager;
    private StationStepManager _stationStepManager;
    private PlcHelper _plcHelper;
    
    public PlcTagValChangedJudge(
        VariableManager variableManager,
        StationMonitorManager monitorManager,
        StationStepManager stationStepManager,
        PlcHelper plcHelper,
        ILogger<PlcTagValChangedJudge> logger)
    {
        _variableManager = variableManager;
        _monitorManager = monitorManager;
        _stationStepManager = stationStepManager;
        _plcHelper = plcHelper;
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
            var plcNameTag = await _variableManager.GetVariableByNameAsync(ParaVariableMaps["PlcName"]).ConfigureAwait(false);
            if(plcNameTag == null)
                throw new Exception($"不存在名为{ParaVariableMaps["PlcName"]}的变量定义");

            var tagNameTag = await _variableManager.GetVariableByNameAsync(ParaVariableMaps["PlcTagName"]).ConfigureAwait(false);
            if(tagNameTag == null)
                throw new Exception($"不存在名为{ParaVariableMaps["PlcTagName"]}的变量定义");

            string plcName = plcNameTag.Value;
            string tagName = tagNameTag.Value;
            
            if(false == _plcHelper.IsPlcTagExist(plcName, tagName))
            {
                throw new Exception($"{plcName}.{tagName}不存在");
            }
            bool ret = _plcHelper.IsPlcTagValueChange(plcName, tagName);
            if(!ret)
            {
                await _monitorManager.UpdateStationStepInfoAsync(RelativeStep.StationCode, $"等待Plc变量{plcName}.{tagName}发生变化");
                return false;
            }

            var tagVal = _plcHelper.ReadPlcTag(plcName, tagName);
            _logger.Info($"捕捉到Plc变量{plcName}.{tagName}发生变化，新值为{tagVal?.Value}");
            await _monitorManager.UpdateStationStepInfoAsync(RelativeStep.StationCode, $"捕捉到Plc变量{plcName}.{tagName}发生变化，新值为{tagVal.Value}");

            return true;
        }
        catch(Exception ex)
        {
            await _monitorManager.UpdateStationStepInfoAsync(RelativeStep.StationCode, $"步骤执行失败：{ex.Message}");
            return false;
        }
    }
}