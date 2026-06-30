using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Ecs.Stations;

public interface IVariableService : IApplicationService
{
    public Task<ResponseDto> AddOneVariableAsync(VariableDto v);

    public Task<ResponseDto> AddVariablesAsync(List<VariableDto> vs);

    public Task<ResponseDto> DelOneVariableAsync(string variableName);

    public Task<ResponseDto> DelAllVariablesAsync();

    public Task<ResponseDto> UpdateVariableValueAsync(UpdateVariableDto para);

    public Task<VariableDto> GetVariableAsync(string vName);

    public Task<List<VariableDto>> GetAllVariablesAsync();
}