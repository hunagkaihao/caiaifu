using Volo.Abp.Application.Dtos;

namespace Ecs.Stations;

public class UpdateVariableDto : EntityDto
{
    public string variableName { get; set; }

    public string variableValue { get; set; }
}