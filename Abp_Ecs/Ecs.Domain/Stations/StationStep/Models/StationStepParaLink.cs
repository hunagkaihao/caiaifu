using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace Ecs.Stations;

public class StationStepParaLink : Entity<int>
{
    /// <summary>
    /// 站点编号
    /// </summary>
    /// <value></value>
    [StringLength(50)]
    [Required]
    public string StationCode { get; set; }

    /// <summary>
    /// 站点步骤号
    /// </summary>
    /// <value></value>
    [Required]
    public int StepNo { get; set; }

    /// <summary>
    /// 步骤拥有的参数的名称
    /// </summary>
    /// <value></value>
    [StringLength(128)]
    [Required]
    public string StepParaName { get; set; }    

    /// <summary>
    /// 与步骤参数关联的变量名称
    /// </summary>
    /// <value></value>
    [StringLength(128)]
    [Required]
    public string VariableName { get; set; }    

    /// <summary>
    /// 步骤参数的目标值，用于判断变量是否变化为指定的值
    /// </summary>
    /// <value></value>
    public string TargetValue { get; set; }
}