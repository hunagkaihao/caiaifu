using Volo.Abp.Application.Dtos;

namespace Ecs.Stations;

public class GetOneParaLinkDto : EntityDto
{
    public string stationCode { get; set; }

    public int stepNo { get; set; }

    public string stepParaName { get; set; }
}