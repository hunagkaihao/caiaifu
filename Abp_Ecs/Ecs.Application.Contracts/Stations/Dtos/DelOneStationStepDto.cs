using Volo.Abp.Application.Dtos;

namespace Ecs.Stations;

public class DelOneStationStepDto : EntityDto
{
    public string stationCode { get; set; }

    public int stepNo { get; set; }
}