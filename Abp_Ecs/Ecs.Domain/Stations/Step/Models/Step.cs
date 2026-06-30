using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace Ecs.Stations;

public class Step : Entity<int>
{
    [StringLength(50)]
    [Required]
    public string StepClsName { get; set; }

    [StringLength(512)]
    public string Describe { get; set; }
}