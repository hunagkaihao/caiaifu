using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecs.LogTool;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Ecs.Stations;

public class StationExecutor : ITransientDependency
{
    private Dictionary<int, IStep> mStepDic; //步骤号、步骤 映射字典
    private int mSequence;

    private readonly StepManager _stepManager;
    private readonly StationStepManager _stationStepManager;
    private readonly StationMonitorManager _stationMonitorManager;
    private readonly ILogger<StationExecutor> _logger;

    public string StationCode { get; set; }

    public StationExecutor(
        StepManager stepManager,
        StationStepManager stationStepManager,
        StationMonitorManager stationMonitorManager,
        ILogger<StationExecutor> logger)
    {
        _stepManager = stepManager;
        _stationStepManager = stationStepManager;
        _stationMonitorManager = stationMonitorManager;
        _logger = logger;

        mStepDic = new Dictionary<int, IStep>();
        mSequence = -1;
    }

    /// <summary>
    /// 初始化站点执行器，在初始化前，需要先为StationCode属性赋值
    /// </summary>
    /// <returns></returns>
    public async Task<bool> InitExecutor()
    {
        try
        {
            //根据配置生成Step实例
            var steps = await _stationStepManager.GetAllStationStepsOfStationAsync(StationCode).ConfigureAwait(false);
            if(steps.Count == 0)
                throw new Exception($"站点未设置步骤");
            
            foreach(var step in steps)
            {
                IStep s = _stepManager.CreateStep(step.StepClsName);
                if(s == null)
                    throw new Exception($"第{step.StepNo}步骤从步骤类名{step.StepClsName}创建步骤失败");
                s.RelativeStep = step;
                bool r = await s.LinkParas().ConfigureAwait(false);
                if(!r)
                    throw new Exception($"第{step.StepNo}步骤为步骤参数连接变量失败");
                mStepDic.Add(step.StepNo, s);
            }

            //初始化步骤号
            var stepNoInfo = await _stationMonitorManager.GetStationStepNoAsync(StationCode).ConfigureAwait(false);
            if(string.IsNullOrEmpty(stepNoInfo.StaInformation)) //没有步骤号信息，默认从第一个步骤开始
            {
                mSequence = mStepDic.First().Key;
                await _stationMonitorManager.UpdateStationStepNoAsync(StationCode, mSequence).ConfigureAwait(false);
                await _stationMonitorManager.UpdateStationStepNameAsync(StationCode, mStepDic.First().Value.RelativeStep.StepName).ConfigureAwait(false);
                await _stationMonitorManager.UpdateStationStepInfoAsync(StationCode, string.Empty).ConfigureAwait(false);
            }
            else
            {
                if(!int.TryParse(stepNoInfo.StaInformation, out int stepNo)) //步骤号非整数
                {
                    mSequence = mStepDic.First().Key;
                    await _stationMonitorManager.UpdateStationStepNoAsync(StationCode, mSequence).ConfigureAwait(false);
                    await _stationMonitorManager.UpdateStationStepNameAsync(StationCode, mStepDic.First().Value.RelativeStep.StepName).ConfigureAwait(false);
                    await _stationMonitorManager.UpdateStationStepInfoAsync(StationCode, string.Empty).ConfigureAwait(false);
                }
                else if(!mStepDic.Keys.Contains(stepNo)) //步骤号不在范围内
                {
                    mSequence = mStepDic.First().Key;
                    await _stationMonitorManager.UpdateStationStepNoAsync(StationCode, mSequence).ConfigureAwait(false);
                    await _stationMonitorManager.UpdateStationStepNameAsync(StationCode, mStepDic.First().Value.RelativeStep.StepName).ConfigureAwait(false);
                    await _stationMonitorManager.UpdateStationStepInfoAsync(StationCode, string.Empty).ConfigureAwait(false);
                }
                else
                {
                    mSequence = stepNo;
                    await _stationMonitorManager.UpdateStationStepNameAsync(StationCode, mStepDic[stepNo].RelativeStep.StepName).ConfigureAwait(false);
                    await _stationMonitorManager.UpdateStationStepInfoAsync(StationCode, string.Empty).ConfigureAwait(false);
                }
            }
            
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error($"{StationCode}站点初始化失败，停止运行：{ex.Message}");
            await _stationMonitorManager.UpdateStationStepInfoAsync(StationCode, $"站点初始化失败，停止运行：{ex.Message}").ConfigureAwait(false);
            return false;
        }
    }
    
    /// <summary>
    /// 站点执行
    /// </summary>
    /// <returns></returns>
    public async Task Execute()
    {
        await Task.Run(async() => {
            while(true)
            {
                await Task.Delay(50).ConfigureAwait(false);

                IStep curStep = mStepDic[mSequence];
                bool isFinished = await curStep.Execute();

                if(isFinished) //步骤执行完成
                {
                    int nextStepNo = curStep.RelativeStep.GetNextStepNoByStepResult(curStep.StepResult);
                    if(!mStepDic.Keys.Contains(nextStepNo))
                    {
                        await _stationMonitorManager.UpdateStationStepInfoAsync(
                            StationCode, 
                            $"当前步骤已执行完成，但根据步骤结果{curStep.StepResult}获取的下一步骤号{nextStepNo}无效")
                            .ConfigureAwait(false);
                    }  
                    else
                    {
                        mSequence = nextStepNo;
                        await _stationMonitorManager.UpdateStationStepNoAsync(StationCode, mSequence).ConfigureAwait(false);
                        await _stationMonitorManager.UpdateStationStepNameAsync(StationCode, mStepDic[mSequence].RelativeStep.StepName).ConfigureAwait(false);
                        await _stationMonitorManager.UpdateStationStepInfoAsync(StationCode, $"开始执行步骤：{mStepDic[mSequence].RelativeStep.StepName}").ConfigureAwait(false);
                    }
                }    
            }
        });
    }
}