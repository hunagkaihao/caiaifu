using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace Ecs.Stations;

public class StationStep : Entity<int>
{
    [StringLength(50)]
    [Required]
    public string StepName { get; set; }

    [StringLength(50)]
    [Required]
    public string StationCode { get; set; }

    public int StepNo { get; set; }

    [StringLength(50)]
    [Required]
    public string StepClsName { get; set; }

    /// <summary>
    /// 下一个步骤号
    /// 逗号分隔，包含10段，每段以冒号分隔（包含2段，第1段为步骤执行结果，固定0-9，第2段为下一个步骤号）
    /// 格式：0:step0,1:step1,2:step2,3:step3,4:step4,5:step5,6:step6,7:step7,8:step8,9:step9
    /// 例如：0:3,1:6,2:8,3:8,4:8,5:8,6:8,7:8,8:8,9:8
    /// </summary>
    /// <value></value>
    [StringLength(128)]
    [Required]
    public string NextStepNo { get; set; }

    [StringLength(512)]
    public string Describe { get; set; }

    public int GetNextStepNoByStepResult(int stepResult)
    {
        if(stepResult < 0 || stepResult > 9)
            return -1;

        string[] sects = NextStepNo.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
        if(sects.Length != 10)
            return -1;
        
        Dictionary<int, int> resultStepNoDic = new Dictionary<int, int>();
        for(int i = 0; i < sects.Length; i++)
        {
            string[] ss = sects[i].Split(':', System.StringSplitOptions.RemoveEmptyEntries);
            if(ss.Length != 2)
                return -1;
            if(ss[0] != i.ToString())
                return -1;
            if(!int.TryParse(ss[0], out int result))
                return -1;
            if(!int.TryParse(ss[1], out int stepNo))
                return -1;
            resultStepNoDic.Add(result, stepNo);
        }

        return resultStepNoDic[stepResult];
    }
}