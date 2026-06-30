using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace Ecs.Stations;

public class Station : Entity<int>
{    
    [StringLength(50)]
    [Required]
    public string StationCode { get; set; }

    [StringLength(50)]
    [Required]
    public string StationName { get; set; }
        
    [StringLength(256)]
    public string Description { get; set; }
}