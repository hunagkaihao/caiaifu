using Ecs.ConfigTool;
using Ecs.RedisTool;
using Ecs.LogTool;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace Ecs.Stations;

public class StationMonitorManager : ISingletonDependency
{
    private readonly ILogger<StationMonitorManager> _logger;
    private readonly IRedisClient _redisClient;
    private readonly IOptions<ConfigOptions> _options;

    public StationMonitorManager(
        ILogger<StationMonitorManager> logger, 
        IRedisClient redisClient,
        IOptions<ConfigOptions> options)
    {
        _logger = logger;
        _options = options;
        _redisClient = redisClient;
        _redisClient.Build(options.Value.RedisConnStr, options.Value.DefaultRedisNo);
    }

    /// <summary>
    /// 更新站点信息
    /// </summary>
    /// <param name="stationCode"></param>
    /// <param name="infoMark"></param>
    /// <param name="info"></param>
    public void UpdateStationInfo(string stationCode, string infoMark, string info)
    {
        try
        {
            _redisClient.SetHashValue(EcsConsts.StationInfoChannel, $"{stationCode}.{infoMark}", info);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }

    /// <summary>
    /// 异步更新站点信息
    /// </summary>
    /// <param name="stationCode"></param>
    /// <param name="infoMark"></param>
    /// <param name="info"></param>
    /// <returns></returns>
    public async Task UpdateStationInfoAsync(string stationCode, string infoMark, string info)
    {
        try
        {
            await _redisClient.SetHashValueAsync(EcsConsts.StationInfoChannel, $"{stationCode}.{infoMark}", info);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }

    /// <summary>
    /// 获取站点信息
    /// </summary>
    /// <param name="stationCode"></param>
    /// <param name="infoMark"></param>
    /// <returns>若发生错误返回null</returns>
    public StationInfo GetStationInfo(string stationCode, string infoMark)
    {
        try
        {
            string info = _redisClient.GetHashValue(EcsConsts.StationInfoChannel, $"{stationCode}.{infoMark}");
            StationInfo staInfo = new StationInfo(){
                StaInfoName = $"{stationCode}.{infoMark}",
                StaInformation = info
            };
            return staInfo;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 异步获取站点信息
    /// </summary>
    /// <param name="stationCode"></param>
    /// <param name="infoMark"></param>
    /// <returns>若发生错误返回null</returns>
    public async Task<StationInfo> GetStationInfoAsync(string stationCode, string infoMark)
    {
        try
        {
            string info = await _redisClient.GetHashValueAsync(EcsConsts.StationInfoChannel, $"{stationCode}.{infoMark}");
            StationInfo staInfo = new StationInfo(){
                StaInfoName = $"{stationCode}.{infoMark}",
                StaInformation = info
            };
            return staInfo;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

    public List<StationInfo> GetAllStationInfo(string stationCode)
    {
        try
        {
            KeyValuePair<string, string>[] pairs = _redisClient.GetAllHashFieldValuePairs(EcsConsts.StationInfoChannel);
            if (pairs.Length == 0)
                return new List<StationInfo>();

            List<StationInfo> staInfos = new List<StationInfo>();
            foreach(var pair in pairs)
            {
                StationInfo staInfo = new StationInfo()
                {
                    StaInfoName = $"{stationCode}.{pair.Key}",
                    StaInformation = pair.Value
                };
                staInfos.Add(staInfo);
            }
            return staInfos;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

    public async Task<List<StationInfo>> GetAllStationInfoAsync(string stationCode)
    {
        try
        {
            KeyValuePair<string, string>[] pairs = await _redisClient.GetAllHashFieldValuePairsAsync(EcsConsts.StationInfoChannel);
            if (pairs.Length == 0)
                return new List<StationInfo>();

            List<StationInfo> staInfos = new List<StationInfo>();
            foreach(var pair in pairs)
            {
                StationInfo staInfo = new StationInfo()
                {
                    StaInfoName = $"{stationCode}.{pair.Key}",
                    StaInformation = pair.Value
                };
                staInfos.Add(staInfo);
            }
            return staInfos;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

    #region 站点步骤号
    public void UpdateStationStepNo(string stationCode, int stepNo)
    {
        try
        {
            _redisClient.SetHashValue(EcsConsts.StationInfoChannel, $"{stationCode}.StepNo", stepNo.ToString());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }

    public async Task UpdateStationStepNoAsync(string stationCode, int stepNo)
    {
        try
        {
            await _redisClient.SetHashValueAsync(EcsConsts.StationInfoChannel, $"{stationCode}.StepNo", stepNo.ToString());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }

    public StationInfo GetStationStepNo(string stationCode)
    {
        try
        {
            string info = _redisClient.GetHashValue(EcsConsts.StationInfoChannel, $"{stationCode}.StepNo");
            StationInfo staInfo = new StationInfo(){
                StaInfoName = $"{stationCode}.StepNo",
                StaInformation = info
            };
            return staInfo;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

    public async Task<StationInfo> GetStationStepNoAsync(string stationCode)
    {
        try
        {
            string info = await _redisClient.GetHashValueAsync(EcsConsts.StationInfoChannel, $"{stationCode}.StepNo");
            StationInfo staInfo = new StationInfo(){
                StaInfoName = $"{stationCode}.StepNo",
                StaInformation = info
            };
            return staInfo;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }
    #endregion

    #region  站点步骤名称
    public void UpdateStationStepName(string stationCode, string stepName)
    {
        try
        {
            _redisClient.SetHashValue(EcsConsts.StationInfoChannel, $"{stationCode}.StepName", stepName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }

    public async Task UpdateStationStepNameAsync(string stationCode, string stepName)
    {
        try
        {
            await _redisClient.SetHashValueAsync(EcsConsts.StationInfoChannel, $"{stationCode}.StepName", stepName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }

    public StationInfo GetStationStepName(string stationCode)
    {
        try
        {
            string info = _redisClient.GetHashValue(EcsConsts.StationInfoChannel, $"{stationCode}.StepName");
            StationInfo staInfo = new StationInfo(){
                StaInfoName = $"{stationCode}.StepName",
                StaInformation = info
            };
            return staInfo;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

    public async Task<StationInfo> GetStationStepNameAsync(string stationCode)
    {
        try
        {
            string info = await _redisClient.GetHashValueAsync(EcsConsts.StationInfoChannel, $"{stationCode}.StepName");
            StationInfo staInfo = new StationInfo(){
                StaInfoName = $"{stationCode}.StepName",
                StaInformation = info
            };
            return staInfo;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }
    #endregion 


    #region 站点步骤信息
    public void UpdateStationStepInfo(string stationCode, string stepInfo)
    {
        try
        {
            _redisClient.SetHashValue(EcsConsts.StationInfoChannel, $"{stationCode}.StepInfo", stepInfo);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }

    public async Task UpdateStationStepInfoAsync(string stationCode, string stepInfo)
    {
        try
        {
            await _redisClient.SetHashValueAsync(EcsConsts.StationInfoChannel, $"{stationCode}.StepInfo", stepInfo);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }

    public StationInfo GetStationStepInfo(string stationCode)
    {
        try
        {
            string info = _redisClient.GetHashValue(EcsConsts.StationInfoChannel, $"{stationCode}.StepInfo");
            StationInfo staInfo = new StationInfo(){
                StaInfoName = $"{stationCode}.StepInfo",
                StaInformation = info
            };
            return staInfo;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

    public async Task<StationInfo> GetStationStepInfoAsync(string stationCode)
    {
        try
        {
            string info = await _redisClient.GetHashValueAsync(EcsConsts.StationInfoChannel, $"{stationCode}.StepInfo");
            StationInfo staInfo = new StationInfo(){
                StaInfoName = $"{stationCode}.StepInfo",
                StaInformation = info
            };
            return staInfo;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }
    #endregion
}
