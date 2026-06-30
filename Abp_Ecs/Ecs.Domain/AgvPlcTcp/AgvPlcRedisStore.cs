using System;
using System.Collections.Generic;
using System.Globalization;
using Ecs.ConfigTool;
using Ecs;
using Ecs.RedisTool;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Ecs.AgvPlcTcp;

/// <summary>
/// 四点位协议快照与运行态：每个点位一个 Redis Hash（Key = AgvPlc:Point:O1A），field 拆开存储。
/// </summary>
public class AgvPlcRedisStore : ISingletonDependency
{
    private readonly IRedisClient _redisClient;
    private readonly ILogger<AgvPlcRedisStore> _logger;

    public AgvPlcRedisStore(
        IRedisClient redisClient,
        IOptions<ConfigOptions> ecsOptions,
        ILogger<AgvPlcRedisStore> logger)
    {
        _redisClient = redisClient;
        _logger = logger;
        _redisClient.Build(ecsOptions.Value.RedisConnStr, ecsOptions.Value.DefaultRedisNo);
    }

    private static string PointKey(string pointCode) => EcsConsts.AgvPlcPointRedisKey(pointCode);

    public PointProtocolSnapshot? GetSnapshot(string pointCode)
    {
        try
        {
            var key = PointKey(pointCode);
            if (!_redisClient.IsKeyExist(key))
            {
                return null;
            }

            var pairs = _redisClient.GetAllHashFieldValuePairs(key);
            if (pairs == null || pairs.Length == 0)
            {
                return null;
            }

            return ParseSnapshotFromPairs(pointCode, pairs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取 Redis 点位快照失败 {Point}", pointCode);
            return null;
        }
    }

    /// <summary>
    /// 仅读取指定点位的快照（用于某条生产线派发）。
    /// </summary>
    public Dictionary<string, PointProtocolSnapshot> GetSnapshotsForPoints(IReadOnlyList<string> pointCodes)
    {
        var dict = new Dictionary<string, PointProtocolSnapshot>(StringComparer.Ordinal);
        foreach (var code in pointCodes)
        {
            var snap = GetSnapshot(code);
            if (snap != null)
            {
                dict[code] = snap;
            }
        }

        return dict;
    }

    /// <summary>遍历全部已定义点位（O1A～O4D）中有 Redis 键的快照。</summary>
    public Dictionary<string, PointProtocolSnapshot> GetAllSnapshots()
    {
        return GetSnapshotsForPoints(AgvPlcPointCodes.AllPointsAcrossLines);
    }

    public void SaveSnapshot(PointProtocolSnapshot snapshot)
    {
        try
        {
            snapshot.LastUpdateUtc = DateTime.UtcNow;
            var key = PointKey(snapshot.PointCode);
            _redisClient.SetHashValue(key, AgvPlcRedisHashFields.PointCode, snapshot.PointCode);
            _redisClient.SetHashValue(key, AgvPlcRedisHashFields.RunState, AgvPlcRunStates.Normalize(snapshot.RunState));
            _redisClient.SetHashValue(key, AgvPlcRedisHashFields.ThirdByteHex, snapshot.ThirdByteHex ?? string.Empty);
            _redisClient.SetHashValue(key, AgvPlcRedisHashFields.ThirdByteBin, snapshot.ThirdByteBinaryNibbles ?? string.Empty);
            _redisClient.SetHashValue(key, AgvPlcRedisHashFields.StatusActiveSummary, snapshot.StatusActiveSummary ?? string.Empty);
            var seqBits = snapshot.StatusSeqBits ?? Array.Empty<bool>();
            for (var i = 1; i <= 10; i++)
            {
                var on = seqBits.Length >= i && seqBits[i - 1];
                _redisClient.SetHashValue(key, AgvPlcRedisHashFields.SeqField(i), on ? "1" : "0");
            }

            _redisClient.SetHashValue(key, AgvPlcRedisHashFields.RawFrameHex, snapshot.RawFrameHex ?? string.Empty);
            _redisClient.SetHashValue(key, AgvPlcRedisHashFields.TcpConnected, snapshot.TcpConnected ? "1" : "0");
            _redisClient.SetHashValue(key, AgvPlcRedisHashFields.LastError, snapshot.LastError ?? string.Empty);
            _redisClient.SetHashValue(
                key,
                AgvPlcRedisHashFields.LastUpdateUtc,
                snapshot.LastUpdateUtc.ToString("o", CultureInfo.InvariantCulture));

            try
            {
                _redisClient.RemoveHashFields(key, AgvPlcRedisHashFields.ObsoleteHashFieldNamesToDelete);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "移除 Redis 旧英文字段名失败（可忽略） {Point}", snapshot.PointCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "写入 Redis 点位快照失败 {Point}", snapshot.PointCode);
        }
    }

    /// <summary>
    /// TCP 线程更新协议位时调用：合并写，保留 RunState（由任务派发/回调修改）。
    /// </summary>
    public void MergeProtocolFields(
        string pointCode,
        bool allowPickup,
        bool allowPlace,
        string rawFrameHex,
        bool tcpConnected,
        string? lastError)
    {
        var snap = GetSnapshot(pointCode) ?? new PointProtocolSnapshot
        {
            PointCode = pointCode,
            RunState = AgvPlcRunStates.Available
        };
        snap.PointCode = pointCode;
        snap.AllowPickup = allowPickup;
        snap.AllowPlace = allowPlace;
        snap.RawFrameHex = rawFrameHex;
        snap.TcpConnected = tcpConnected;
        snap.LastError = lastError;
        snap.StatusSeqBits = new bool[10];
        snap.StatusSeqBits[1] = allowPickup;
        snap.StatusSeqBits[2] = allowPlace;
        SaveSnapshot(snap);
    }

    /// <summary>
    /// 根据应答帧写入 Redis：第 1 字节为序号 1～8（位图，与原先一致）；第 2 字节位 0/位 1 为序号 9/10（请求取货/放货任务激活）。
    /// AllowPickup/AllowPlace 仍取自序号 2、3；Redis Hash 含序号 1～10。
    /// </summary>
    public void MergeProtocolFieldsFromPlcFrame(
        string pointCode,
        ReadOnlySpan<byte> frame,
        string rawFrameHex,
        bool tcpConnected,
        string? lastError)
    {
        var statusByte = frame.Length > 0 ? frame[0] : (byte)0;
        var taskByte = frame.Length > 1 ? frame[1] : (byte)0;

        var snap = GetSnapshot(pointCode) ?? new PointProtocolSnapshot
        {
            PointCode = pointCode,
            RunState = AgvPlcRunStates.Available
        };
        snap.PointCode = pointCode;
        snap.RawFrameHex = rawFrameHex;
        snap.TcpConnected = tcpConnected;
        snap.LastError = lastError;
        snap.ThirdByteHex = $"{statusByte:X2}";
        snap.ThirdByteBinaryNibbles = AgvPlcFrameParser.ToBinaryString8Nibbles(statusByte);
        snap.StatusActiveSummary = AgvPlcFrameParser.FormatPlcThirdByteStatusForLog(statusByte);
        snap.StatusSeqBits = new bool[10];
        for (var i = 0; i < 8; i++)
        {
            snap.StatusSeqBits[i] = AgvPlcFrameParser.GetThirdByteBit(statusByte, i + 1);
        }

        snap.StatusSeqBits[8] = AgvPlcFrameParser.GetThirdByteBit(taskByte, 1);
        snap.StatusSeqBits[9] = AgvPlcFrameParser.GetThirdByteBit(taskByte, 2);

        snap.AllowPickup = snap.StatusSeqBits[1];
        snap.AllowPlace = snap.StatusSeqBits[2];
        SaveSnapshot(snap);
    }

    public void SetRunState(string pointCode, string runState)
    {
        var snap = GetSnapshot(pointCode) ?? new PointProtocolSnapshot
        {
            PointCode = pointCode,
            RunState = AgvPlcRunStates.Available
        };
        snap.RunState = AgvPlcRunStates.Normalize(runState);
        SaveSnapshot(snap);
    }

    private static PointProtocolSnapshot ParseSnapshotFromPairs(string pointCode, KeyValuePair<string, string>[] pairs)
    {
        string? Get(string field)
        {
            foreach (var kv in pairs)
            {
                if (string.Equals(kv.Key, field, StringComparison.Ordinal))
                {
                    return kv.Value;
                }
            }

            return null;
        }

        string? GetCnOrEn(string chineseField, string englishField)
        {
            return Get(chineseField) ?? Get(englishField);
        }

        var legacyRun = GetCnOrEn(AgvPlcRedisHashFields.RunState, "RunState");
        var seqBits = new bool[10];
        for (var i = 0; i < 10; i++)
        {
            var seqNum = i + 1;
            var v = Get(AgvPlcRedisHashFields.SeqField(seqNum));
            if (v == null && seqNum == 2)
            {
                v = Get("序号2_取货");
            }
            else if (v == null && seqNum == 3)
            {
                v = Get("序号3_放货");
            }

            seqBits[i] = v == "1" || ParseBool(v);
        }

        var seq2Present = Get(AgvPlcRedisHashFields.SeqField(2)) != null || Get("序号2_取货") != null;
        var seq3Present = Get(AgvPlcRedisHashFields.SeqField(3)) != null || Get("序号3_放货") != null;
        var allowPickup = seq2Present ? seqBits[1] : ParseBool(GetCnOrEn(AgvPlcRedisHashFields.AllowPickup, "AllowPickup"));
        var allowPlace = seq3Present ? seqBits[2] : ParseBool(GetCnOrEn(AgvPlcRedisHashFields.AllowPlace, "AllowPlace"));

        var snap = new PointProtocolSnapshot
        {
            PointCode = GetCnOrEn(AgvPlcRedisHashFields.PointCode, "PointCode") ?? pointCode,
            RunState = AgvPlcRunStates.Normalize(legacyRun),
            AllowPickup = allowPickup,
            AllowPlace = allowPlace,
            ThirdByteHex = Get(AgvPlcRedisHashFields.ThirdByteHex) ?? Get("第三字节HEX") ?? string.Empty,
            ThirdByteBinaryNibbles = Get(AgvPlcRedisHashFields.ThirdByteBin) ?? Get("第三字节二进制") ?? string.Empty,
            StatusActiveSummary = Get(AgvPlcRedisHashFields.StatusActiveSummary) ?? string.Empty,
            StatusSeqBits = seqBits,
            RawFrameHex = GetCnOrEn(AgvPlcRedisHashFields.RawFrameHex, "RawFrameHex")
                ?? Get("原始数据HEX")
                ?? string.Empty,
            TcpConnected = ParseBool(GetCnOrEn(AgvPlcRedisHashFields.TcpConnected, "TcpConnected")),
            LastError = GetCnOrEn(AgvPlcRedisHashFields.LastError, "LastError")
        };
        var utcStr = GetCnOrEn(AgvPlcRedisHashFields.LastUpdateUtc, "LastUpdateUtc");
        if (!string.IsNullOrEmpty(utcStr) &&
            DateTime.TryParse(utcStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var utc))
        {
            snap.LastUpdateUtc = utc;
        }

        return snap;
    }

    private static bool ParseBool(string? s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return false;
        }

        return s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               s.Equals("1", StringComparison.Ordinal) ||
               s.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}
