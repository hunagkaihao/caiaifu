using Volo.Abp.Application.Dtos;

namespace Ecs.Stations;

public class StationInfoDto : EntityDto
{
    public string staInfoName { get; set; }

    public string staInformation { get; set; }
}