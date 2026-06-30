using Volo.Abp.Application.Dtos;

namespace Ecs.Stations;

public class StepDto : EntityDto 
{
    public string stepClsName { get; set; }

    public string describe { get; set; }
}