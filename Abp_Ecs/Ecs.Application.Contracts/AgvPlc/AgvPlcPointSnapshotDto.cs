using System;

namespace Ecs.AgvPlc;

/// <summary>
/// 单点位 Redis Hash（AgvPlc:Point:O1A 等）对外展示用结构，与界面 19 项字段一致。
/// </summary>
public class AgvPlcPointSnapshotDto
{
    /// <summary>点位编码，如 O1A。</summary>
    public string PointCode { get; set; } = string.Empty;

    /// <summary>运行状态（0/1/2 等，与 Redis 一致）。</summary>
    public string RunState { get; set; } = string.Empty;

    public string FirstByteHex { get; set; } = string.Empty;

    public string FirstByteBinary { get; set; } = string.Empty;

    public string StatusActiveSummary { get; set; } = string.Empty;

    public int Seq1_DeviceStatus { get; set; }

    public int Seq2_AllowPickup { get; set; }

    public int Seq3_AllowPlace { get; set; }

    public int Seq4_CommDiagnosis { get; set; }

    public int Seq5_EmergencyStop { get; set; }

    public int Seq6_Spare { get; set; }

    public int Seq7_ReadyPosition { get; set; }

    public int Seq8_WorkingPosition { get; set; }

    public int Seq9_RequestPickupTask { get; set; }

    public int Seq10_RequestPlaceTask { get; set; }

    public string RawFrameHex { get; set; } = string.Empty;

    public bool TcpConnected { get; set; }

    public string LastError { get; set; } = string.Empty;

    public DateTime? LastUpdateUtc { get; set; }
}
