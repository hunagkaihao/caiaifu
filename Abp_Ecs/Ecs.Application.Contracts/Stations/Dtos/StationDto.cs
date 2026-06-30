using Volo.Abp.Application.Dtos;

namespace Ecs.Stations;

public class StationDto : EntityDto
{
    public string stationCode { get; set; }

    public string stationName { get; set; }

    public string description { get; set; }
}