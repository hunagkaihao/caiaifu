using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Ecs.PlcMonitor
{
    public interface IPlcMonitorService : IApplicationService
    {
        public Task<List<MonitorDto>> GetMonitorsAsync();

        // public Task<Dictionary<string, string>> GetMonitorsInDicFormAsync();
    }
}
