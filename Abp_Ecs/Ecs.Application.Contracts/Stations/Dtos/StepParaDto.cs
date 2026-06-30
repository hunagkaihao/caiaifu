using Volo.Abp.Application.Dtos;

namespace Ecs.Stations;

public class StepParaDto : EntityDto
{
    public string stepClsName { get; set; }

    public string paraName { get; set; }

    public string describe { get; set; }
}