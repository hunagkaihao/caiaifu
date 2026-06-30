using Volo.Abp.Application.Dtos;

namespace Ecs;

public class ResponseDto : EntityDto
{
    public bool success { get; set; }   

    public string message { get; set; }
}