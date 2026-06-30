using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Ecs.Stations;

public interface IStationService : IApplicationService
{
    public Task<ResponseDto> AddOneStationAsync(StationDto station);

    public Task<ResponseDto> AddStationsAsync(List<StationDto> stations);

    public Task<ResponseDto> DelOneStationAsync(string stationCode);

    public Task<ResponseDto> DelAllStationsAsync();
    
    public Task<List<StationDto>> GetAllStationsAsync();
}