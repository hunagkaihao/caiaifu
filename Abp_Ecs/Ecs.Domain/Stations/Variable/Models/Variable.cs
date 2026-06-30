using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace Ecs.Stations;

public class Variable : Entity<int>
{
    [StringLength(128)]
    [Required]
    public string VName { get; set; }

    public VariableType VType { get; set; }

    [Required]
    public string DefaultValue { get; set; }

    [Required]
    public string Value { get; set; }

    [StringLength(512)]
    public string Describe { get; set; }
}