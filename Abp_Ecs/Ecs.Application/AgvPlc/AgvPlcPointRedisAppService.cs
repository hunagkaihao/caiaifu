using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecs;
using Ecs.AgvPlcTcp;
using Volo.Abp;

namespace Ecs.AgvPlc;

public class AgvPlcPointRedisAppService : EcsAppService, IAgvPlcPointRedisAppService
{
    private readonly AgvPlcRedisStore _redisStore;

    public AgvPlcPointRedisAppService(AgvPlcRedisStore redisStore)
    {
        _redisStore = redisStore;
    }

    public Task<List<AgvPlcPointSnapshotDto>> GetSnapshotsByLineAsync(string lineKey)
    {
        if (!AgvPlcPointCodes.TryGetQuadPoints(lineKey, out var quad) || quad.Length != 4)
        {
            throw new UserFriendlyException($"无效的线别参数，请使用：{string.Join('、', AgvPlcPointCodes.LineKeys)}");
        }

        var list = new List<AgvPlcPointSnapshotDto>(4);
        foreach (var code in quad)
        {
            var snap = _redisStore.GetSnapshot(code);
            list.Add(snap != null ? ToDto(snap, code) : EmptyDto(code));
        }

        return Task.FromResult(list);
    }

    public Task<ResponseDto> ResetPointRunStateToZeroAsync(string pointCode)
    {
        if (!AgvPlcPointCodes.TryResolvePointCode(pointCode, out var canonical))
        {
            throw new UserFriendlyException("无效的点位编码，请使用 O1A～O4D（也支持 01A、1A 等形式）");
        }

        _redisStore.SetRunState(canonical, AgvPlcRunStates.Available);

        return Task.FromResult(new ResponseDto
        {
            success = true,
            message = $"已将点位 {canonical} 的运行状态重置为 0"
        });
    }

    private static AgvPlcPointSnapshotDto EmptyDto(string pointCode)
    {
        return new AgvPlcPointSnapshotDto
        {
            PointCode = pointCode,
            RunState = AgvPlcRunStates.Available
        };
    }

    private static AgvPlcPointSnapshotDto ToDto(PointProtocolSnapshot s, string expectedPointCode)
    {
        var bits = s.StatusSeqBits;
        if (bits == null || bits.Length < 10)
        {
            bits = new bool[10];
        }

        int Bit(int i) => bits.Length > i && bits[i] ? 1 : 0;

        return new AgvPlcPointSnapshotDto
        {
            PointCode = string.IsNullOrEmpty(s.PointCode) ? expectedPointCode : s.PointCode,
            RunState = s.RunState ?? AgvPlcRunStates.Available,
            FirstByteHex = s.ThirdByteHex ?? string.Empty,
            FirstByteBinary = s.ThirdByteBinaryNibbles ?? string.Empty,
            StatusActiveSummary = s.StatusActiveSummary ?? string.Empty,
            Seq1_DeviceStatus = Bit(0),
            Seq2_AllowPickup = Bit(1),
            Seq3_AllowPlace = Bit(2),
            Seq4_CommDiagnosis = Bit(3),
            Seq5_EmergencyStop = Bit(4),
            Seq6_Spare = Bit(5),
            Seq7_ReadyPosition = Bit(6),
            Seq8_WorkingPosition = Bit(7),
            Seq9_RequestPickupTask = Bit(8),
            Seq10_RequestPlaceTask = Bit(9),
            RawFrameHex = s.RawFrameHex ?? string.Empty,
            TcpConnected = s.TcpConnected,
            LastError = s.LastError ?? string.Empty,
            LastUpdateUtc = s.LastUpdateUtc == default ? null : s.LastUpdateUtc
        };
    }
}
