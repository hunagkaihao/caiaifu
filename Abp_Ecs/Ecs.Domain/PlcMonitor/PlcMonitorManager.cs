using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Services;
using Ecs.LogTool;
using System.Collections.Generic;
using Ecs.RedisTool;
using Microsoft.Extensions.Options;
using Ecs.ConfigTool;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Ecs.PlcMonitor;

public class PlcMonitorManager : IDomainService
{
    private ILogger<PlcMonitorManager> _logger;
    private IRedisClient _ecsRedisClient;
    private IOptions<ConfigOptions> _options;

    public PlcMonitorManager(
        ILogger<PlcMonitorManager> logger, 
        IRedisClient redisClient,
        IOptions<ConfigOptions> options)
    {
        _logger = logger;
        _options = options;
        _ecsRedisClient = redisClient;
        _ecsRedisClient.Build(_options.Value.RedisConnStr, options.Value.DefaultRedisNo);
    }

    public async Task<List<MonitorValue>> GetAllMonitorValuesAsync()
    {
        try
        {
            var pairs = await _ecsRedisClient.GetAllHashFieldValuePairsAsync(EcsConsts.MonitorChannelName);
            if (pairs == null || pairs.Length == 0)
                return new List<MonitorValue>();

            List<MonitorValue> result = new List<MonitorValue>();
            foreach (var pair in pairs)
            {
                MonitorValue value = JsonConvert.DeserializeObject<MonitorValue>(pair.Value);
                if (value == null)
                    continue;
                result.Add(value);
            }

            return result.OrderBy(o => o.monitorTagName).ToList();
        }
        catch (Exception e)
        {
            _logger.Error(e.Message);
            return new List<MonitorValue>();
        } 
    }
}
