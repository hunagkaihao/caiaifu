using System;
using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace Ecs.AgvPlcTcp;

/// <summary>
/// 取消任务后临时抑制同一条边再次派发；待 PLC 请求位清零后释放。
/// </summary>
public class AgvPlcTaskDispatchSuppressionRegistry : ISingletonDependency
{
    private readonly ConcurrentDictionary<string, DateTime> _suppressedEdges = new(StringComparer.OrdinalIgnoreCase);

    public void Suppress(string sourcePointCode, string targetPointCode, string edgeCode)
    {
        var key = BuildKey(sourcePointCode, targetPointCode, edgeCode);
        if (key == null)
        {
            return;
        }

        _suppressedEdges[key] = DateTime.UtcNow;
    }

    public bool IsSuppressed(string sourcePointCode, string targetPointCode, string edgeCode)
    {
        var key = BuildKey(sourcePointCode, targetPointCode, edgeCode);
        return key != null && _suppressedEdges.ContainsKey(key);
    }

    public void Release(string sourcePointCode, string targetPointCode, string edgeCode)
    {
        var key = BuildKey(sourcePointCode, targetPointCode, edgeCode);
        if (key == null)
        {
            return;
        }

        _suppressedEdges.TryRemove(key, out _);
    }

    private static string BuildKey(string sourcePointCode, string targetPointCode, string edgeCode)
    {
        if (string.IsNullOrWhiteSpace(sourcePointCode) ||
            string.IsNullOrWhiteSpace(targetPointCode) ||
            string.IsNullOrWhiteSpace(edgeCode))
        {
            return null;
        }

        return $"{sourcePointCode.Trim()}->{targetPointCode.Trim()}:{edgeCode.Trim()}";
    }
}
