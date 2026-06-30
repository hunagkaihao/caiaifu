using Ecs.ConfigTool;
using Ecs.RedisTool;
using Ecs.LogTool;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using Volo.Abp.DependencyInjection;

namespace Ecs.Stations;

public class StationNotifierManager : ISingletonDependency
{
    private readonly ILogger<StationNotifierManager> _logger;
    private readonly IOptions<ConfigOptions> _options;
    private readonly IRedisClient _ecsRedisClient;

    public StationNotifierManager(
        ILogger<StationNotifierManager> logger, 
        IOptions<ConfigOptions> options,
        IRedisClient redisClient)
    {
        _logger = logger;
        _options = options;
        _ecsRedisClient = redisClient;
        _ecsRedisClient.Build(_options.Value.RedisConnStr, _options.Value.DefaultRedisNo);
    }
    
    public void NotifyStation(string notifierName)
    {
        try
        {
            string notifierVal = _ecsRedisClient.GetHashValue(EcsConsts.StationNotifierChannel, notifierName);
            if (notifierVal == null)
                _ecsRedisClient.SetHashValue(EcsConsts.StationNotifierChannel, notifierName, "1");
            else
            {
                if (!int.TryParse(notifierVal, out int val))
                    _ecsRedisClient.SetHashValue(EcsConsts.StationNotifierChannel, notifierName, "1");
                else
                {
                    val++;
                    if (val == int.MaxValue)
                        val = 1;
                    _ecsRedisClient.SetHashValue(EcsConsts.StationNotifierChannel, notifierName, val.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }

    public bool? IsNotifierValChanged(string notifierName)
    {
        try
        {
            string notifierVal = _ecsRedisClient.GetHashValue(EcsConsts.StationNotifierChannel, notifierName);
            string notifierTempVal = _ecsRedisClient.GetHashValue(EcsConsts.StationNotifierChannel, $"{notifierName}Temp");
            if (notifierVal != notifierTempVal)
            {
                _ecsRedisClient.SetHashValue(EcsConsts.StationNotifierChannel, $"{notifierName}Temp", notifierVal);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

    public void NotifyStationWithPara(string notifierName, string para)
    {
        try
        {
            string notifierVal = _ecsRedisClient.GetHashValue(EcsConsts.StationNotifierWithParaChannel, notifierName);
            if (notifierVal == null)
                _ecsRedisClient.SetHashValue(EcsConsts.StationNotifierWithParaChannel, notifierName, $"1@#${para}");
            else
            {
                string[] sections = notifierVal.Split("@#$");

                if (sections.Length != 2)
                    _ecsRedisClient.SetHashValue(EcsConsts.StationNotifierWithParaChannel, notifierName, $"1@#${para}");
                else if (!int.TryParse(sections[0], out int val))
                    _ecsRedisClient.SetHashValue(EcsConsts.StationNotifierWithParaChannel, notifierName, $"1@#${para}");
                else
                {
                    val++;
                    if (val == int.MaxValue)
                        val = 1;
                    _ecsRedisClient.SetHashValue(EcsConsts.StationNotifierWithParaChannel, notifierName, $"{val}@#${para}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }

    public bool? IsNotifierValWithParaChanged(string notifierName, out string para)
    {
        try
        {
            para = string.Empty;

            string notifierVal = _ecsRedisClient.GetHashValue(EcsConsts.StationNotifierWithParaChannel, notifierName);
            string notifierTempVal = _ecsRedisClient.GetHashValue(EcsConsts.StationNotifierWithParaChannel, $"{notifierName}Temp");
            
            if(notifierVal == null)
                return false;

            string[] sections = notifierVal.Split("@#$");
            if (sections.Length != 2)
                return false;
            
            int val = -1;
            if (!int.TryParse(sections[0], out val))
                return false;

            if (val.ToString() != notifierTempVal)
            {
                para = sections[1];
                _ecsRedisClient.SetHashValue(EcsConsts.StationNotifierWithParaChannel, notifierName, val.ToString());
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            para = string.Empty; 
            _logger.Error(ex.Message);
            return null;
        }
    }
}