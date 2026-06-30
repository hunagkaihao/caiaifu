using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Ecs.Stations;

public interface IStationStepService : IApplicationService
{
    public Task<ResponseDto> AddOneStationStepAsync(StationStepDto step);

    public Task<ResponseDto> AddAllStationStepsOfStationAsync(List<StationStepDto> steps);

    public Task<ResponseDto> DelOneStationStepAsync(DelOneStationStepDto para);

    public Task<ResponseDto> DelAllStationStepsOfStationAsync(string stationCode);

    public Task<ResponseDto> DelAllStationStepsAsync();

    public Task<StationStepDto> GetOneStationStepAsync(GetOneStationStepDto para);

    public Task<List<StationStepDto>> GetAllStationStepsOfStationAsync(string stationCode);

    public Task<List<StationStepDto>> GetAllStationStepsAsync();

    public Task<ResponseDto> AddOneParaLinkAsync(StationStepParaLinkDto link);

    public Task<ResponseDto> AddParaLinksAsync(List<StationStepParaLinkDto> links);

    public Task<ResponseDto> DelOneParaLinkAsync(DelOneParaLinkDto para);

    public Task<ResponseDto> DelParaLinksOfStationStepAsync(DelParaLinksOfStationStepDto para);

    public Task<ResponseDto> DelParaLinksOfStationAsync(string stationCode);

    public Task<ResponseDto> DelAllParaLinksAsync();

    public Task<StationStepParaLinkDto> GetOneParaLinkAsync(GetOneParaLinkDto para);

    public Task<List<StationStepParaLinkDto>> GetParaLinksOfStationStepAsync(GetParaLinksOfStationStepDto para);

    public Task<List<StationStepParaLinkDto>> GetParaLinksOfStationAsync(string stationCode);

    public Task<List<StationStepParaLinkDto>> GetAllParaLinksAsync();

}