using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Ecs.Stations;

public interface IStepService : IApplicationService
{
    public Task<List<StepDto>> GetAllStepsAsync();

    public Task<ResponseDto> AddStepAsync(StepDto step);

    public Task<ResponseDto> AddStepsAsync(List<StepDto> steps);

    public Task<ResponseDto> DelStepAsync(string stepClsName);
    
    public Task<ResponseDto> DelAllStepAsync();

    public Task<ResponseDto> AddStepParaAsync(StepParaDto para);

    public Task<ResponseDto> AddStepParasAsync(List<StepParaDto> paras);

    public Task<ResponseDto> DelOneStepParaAsync(DelStepParaDto para);

    public Task<ResponseDto> DelParasOfStepAsync(string stepClsName);

    public Task<ResponseDto> DelAllParasAsync();

    public Task<List<StepParaDto>> GetStepParasOfStepAsync(string stepClsName);

    public Task<List<StepParaDto>> GetAllStepParasAsync();
}