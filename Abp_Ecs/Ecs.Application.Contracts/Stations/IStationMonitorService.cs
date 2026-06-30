using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Ecs.Stations
{
    public interface IStationMonitorService : IApplicationService
    {
        public Task<List<StationInfoDto>> GetInformationsAsync(string stationCode);
    }
}