using Volo.Abp.Application.Dtos;

namespace Ecs.Stations;

public class StationStepParaLinkDto : EntityDto
{
    public string stationCode { get; set; }

    public int stepNo { get; set; }

    public string stepParaName { get; set; }    

    public string variableName { get; set; }    

    public string targetValue { get; set; }
}