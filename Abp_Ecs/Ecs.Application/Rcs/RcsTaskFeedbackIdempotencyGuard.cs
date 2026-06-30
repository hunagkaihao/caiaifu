using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Ecs.ConfigTool;
using Ecs.RedisTool;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Ecs.Rcs;

/// <summary>
/// 基于 Redis + 进程内锁的 RCS 回调幂等守卫（robotTaskCode + method）。
/// </summary>
public class RcsTaskFeedbackIdempotencyGuard : IRcsTaskFeedbackIdempotencyGuard, ISingletonDependency
{
    private readonly IRedisClient _redisClient;
    private readonly ILogger<RcsTaskFeedbackIdempotencyGuard> _logger;
    private readonly ConcurrentDictionary<string, object> _keyLocks = new(StringComparer.Ordinal);

    public RcsTaskFeedbackIdempotencyGuard(
        IRedisClient redisClient,
        IOptions<ConfigOptions> ecsOptions,
        ILogger<RcsTaskFeedbackIdempotencyGuard> logger)
    {
        _redisClient = redisClient;
        _logger = logger;
        _redisClient.Build(ecsOptions.Value.RedisConnStr, ecsOptions.Value.DefaultRedisNo);
    }

    public Task<bool> TryAcquireAsync(
        string? robotTaskCode,
        string? rcsMethod,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(rcsMethod))
        {
            return Task.FromResult(true);
        }

        if (!RcsTaskFeedbackQuendHandler.TryParseTaskId(robotTaskCode, out var taskId))
        {
            _logger.LogWarning(
                "RCS 回调幂等跳过键生成失败（无法解析 robotTaskCode），按非幂等处理 method={Method} robotTaskCode={RobotTaskCode}",
                rcsMethod,
                robotTaskCode);
            return Task.FromResult(true);
        }

        var redisKey = EcsConsts.RcsTaskFeedbackRedisKey(taskId, rcsMethod);
        var lockObj = _keyLocks.GetOrAdd(redisKey, static _ => new object());

        lock (lockObj)
        {
            if (_redisClient.IsKeyExist(redisKey))
            {
                _logger.LogInformation(
                    "RCS 回调重复，幂等跳过 method={Method} robotTaskCode={RobotTaskCode} Key={Key}",
                    rcsMethod,
                    robotTaskCode,
                    redisKey);
                return Task.FromResult(false);
            }

            var marked = _redisClient.SetStringValue(
                redisKey,
                DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            if (!marked)
            {
                _logger.LogWarning(
                    "RCS 回调幂等标记写入 Redis 失败，仍继续处理 method={Method} Key={Key}",
                    rcsMethod,
                    redisKey);
                return Task.FromResult(true);
            }

            _logger.LogDebug(
                "RCS 回调幂等占用成功 method={Method} robotTaskCode={RobotTaskCode} Key={Key}",
                rcsMethod,
                robotTaskCode,
                redisKey);
            return Task.FromResult(true);
        }
    }
}
