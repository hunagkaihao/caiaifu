using System.Collections.Generic;
using System.Threading.Tasks;
using Ecs.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Ecs.WorkPositions;

[Route("ecs/work-positions")]
[ApiController]
public class WorkPositionController : EcsController
{
    private readonly IWorkPositionAppService _workPositionAppService;

    public WorkPositionController(IWorkPositionAppService workPositionAppService)
    {
        _workPositionAppService = workPositionAppService;
    }

    /// <summary>新增工位</summary>
    [HttpPost("add")]
    public Task<ResponseDto> AddAsync([FromBody] WorkPositionDto input)
    {
        return _workPositionAppService.AddAsync(input);
    }

    /// <summary>更新工位</summary>
    [HttpPost("update")]
    public Task<ResponseDto> UpdateAsync([FromBody] WorkPositionDto input)
    {
        return _workPositionAppService.UpdateAsync(input);
    }

    /// <summary>删除工位</summary>
    [HttpPost("delete")]
    public Task<ResponseDto> DeleteAsync([FromQuery] int id)
    {
        return _workPositionAppService.DeleteAsync(id);
    }

    /// <summary>获取全部工位</summary>
    [HttpGet("all")]
    public Task<List<WorkPositionDto>> GetAllAsync()
    {
        return _workPositionAppService.GetAllAsync();
    }
}
