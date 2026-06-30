using Volo.Abp.Application.Dtos;

namespace Ecs.Stations;

public class StationStepDto : EntityDto
{
    public string stepName { get; set; }

    public string stationCode { get; set; }

    public int stepNo { get; set; }

    public string stepClsName { get; set; }

    /// <summary>
    /// 下一个步骤号
    /// 逗号分隔，包含10段，每段以冒号分隔（包含2段，第1段为步骤执行结果，固定0-9，第2段为下一个步骤号）
    /// 格式：0:step0,1:step1,2:step2,3:step3,4:step4,5:step5,6:step6,7:step7,8:step8,9:step9
    /// 例如：0:3,1:6,2:8,3:8,4:8,5:8,6:8,7:8,8:8,9:8
    /// </summary>
    /// <value></value>
    public string nextStepNo { get; set; }

    public string describe { get; set; }
}