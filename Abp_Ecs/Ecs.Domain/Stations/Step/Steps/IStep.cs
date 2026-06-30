using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ecs.Stations;

public interface IStep
{
    /// <summary>
    /// 关联的站点步骤
    /// </summary>
    /// <value></value>
    public StationStep RelativeStep { get; set; }

    /// <summary>
    /// 属于该步骤的参数名称与关联的变量名称映射
    /// </summary>
    /// <value></value>
    public Dictionary<string, string> ParaVariableMaps { get; set; }

    /// <summary>
    /// 步骤执行结果，默认为0，该结果用于步骤跳转
    /// </summary>
    public int StepResult { get; set; }

    /// <summary>
    /// 为步骤的参数分配Redis通道
    /// </summary>
    /// <param name="paraNmParaChMaps"></param>
    /// <returns>返回false表示分配失败，true表示分配成功</returns>
    public Task<bool> LinkParas();

    /// <summary>
    /// 执行步骤
    /// </summary>
    /// <returns>返回true表示执行完成，返回false表示执行为完成</returns>
    public Task<bool> Execute();
}