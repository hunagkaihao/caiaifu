using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace Ecs.Stations;

public class StepPara : Entity<int>
{
    [StringLength(50)]
    [Required]
    public string StepClsName { get; set; }

    [StringLength(50)]
    [Required]
    public string ParaName { get; set; }

    [StringLength(512)]
    public string Describe { get; set; }
}