using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Ecs.WorkPositions;

public interface IWorkPositionAppService : IApplicationService
{
    Task<ResponseDto> AddAsync(WorkPositionDto input);

    Task<ResponseDto> UpdateAsync(WorkPositionDto input);

    Task<ResponseDto> DeleteAsync(int id);

    Task<List<WorkPositionDto>> GetAllAsync();
}
