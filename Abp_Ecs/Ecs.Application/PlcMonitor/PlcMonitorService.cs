using Ecs.LogTool;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ecs.PlcMonitor
{
    public class PlcMonitorService : EcsAppService, IPlcMonitorService
    {
        private readonly PlcMonitorManager _plcMonitorManager;
        private readonly ILogger<PlcMonitorService> _logger;

        public PlcMonitorService(
            ILogger<PlcMonitorService> logger,
            PlcMonitorManager plcMonitorManager)
        {
            _logger = logger;
            _plcMonitorManager = plcMonitorManager;
        }

        public async Task<List<MonitorDto>> GetMonitorsAsync()
        {
            try
            {
                List<MonitorValue> result = await _plcMonitorManager.GetAllMonitorValuesAsync().ConfigureAwait(false);
                return ObjectMapper.Map<List<MonitorValue>, List<MonitorDto>>(result);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                return new List<MonitorDto>();
            }            
        }

        // public async Task<Dictionary<string, string>> GetMonitorsInDicFormAsync()
        // {
        //     try
        //     {
        //         List<MonitorValue> values = await _plcMonitorManager.GetAllMonitorValuesAsync().ConfigureAwait(false);
        //         if(values == null || values.Count == 0)
        //             return new Dictionary<string, string>();
        //         else
        //         {
        //             Dictionary<string, string> result = new Dictionary<string, string>();
        //             foreach(var val in values)
        //             {

        //             }
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         _logger.Error(e.Message);
        //         return new Dictionary<string, string>();
        //     }
        // }
    }
}
