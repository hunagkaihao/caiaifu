using Volo.Abp.Application.Dtos;

namespace Ecs.Stations;

public class DelStepParaDto : EntityDto
{
    public string stepClsName { get; set; }

    public string paraName { get; set; }
}