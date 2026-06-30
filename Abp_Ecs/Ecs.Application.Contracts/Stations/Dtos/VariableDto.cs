using Volo.Abp.Application.Dtos;

namespace Ecs.Stations;

public class VariableDto : EntityDto
{
    public string vName { get; set; }

    public VariableType vType { get; set; }

    public string defaultValue { get; set; }

    public string value { get; set; }

    public string describe { get; set; }
}