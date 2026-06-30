using Volo.Abp.Application.Dtos;

namespace Ecs.Stations;

public class GetParaLinksOfStationStepDto : EntityDto
{
    public string stationCode { get; set; }

    public int stepNo { get; set; }
}