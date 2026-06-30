using System;
using System.Collections.Generic;
using Ecs.ConfigTool;

namespace Ecs.AgvPlcTcp;

public static class AgvPlcFrameParser
{
    /// <summary>
    /// 按技术文档：10 字节帧中「允许取」「允许放」各占一字节，值为 0x01 为真（或配置 AllowNonZeroAsActive）。
    /// </summary>
    public static (bool AllowPickup, bool AllowPlace) ParseAllowFlags(byte[] frame, AgvPlcTcpOptions options)
    {
        if (frame == null || frame.Length == 0)
        {
            return (false, false);
        }

        var pickupIdx = Math.Clamp(options.AllowPickupByteIndex, 0, frame.Length - 1);
        var placeIdx = Math.Clamp(options.AllowPlaceByteIndex, 0, frame.Length - 1);
        var active = (byte)options.ActiveStateByteValue;

        bool IsActive(byte b)
        {
            if (options.AllowNonZeroAsActive)
            {
                return b != 0;
            }

            return b == active;
        }

        return (IsActive(frame[pickupIdx]), IsActive(frame[placeIdx]));
    }

    public static string ToHexString(byte[] frame)
    {
        if (frame == null || frame.Length == 0)
        {
            return string.Empty;
        }

        return BitConverter.ToString(frame).Replace("-", "", StringComparison.Ordinal);
    }

    /// <summary>
    /// 日志/界面友好格式：每字节两位 HEX、字节间空格，每 <paramref name="bytesPerGroup"/> 字节用「 - 」分组。
    /// 例如 10 字节 → <c>01 00 00 00 - 00 00 00 00 - 00 00</c>。
    /// </summary>
    public static string ToDisplayHexString(byte[]? frame, int bytesPerGroup = 4)
    {
        if (frame == null || frame.Length == 0 || bytesPerGroup <= 0)
        {
            return string.Empty;
        }

        var groups = new List<string>();
        for (var i = 0; i < frame.Length; i += bytesPerGroup)
        {
            var n = Math.Min(bytesPerGroup, frame.Length - i);
            var parts = new string[n];
            for (var j = 0; j < n; j++)
            {
                parts[j] = $"{frame[i + j]:X2}";
            }

            groups.Add(string.Join(' ', parts));
        }

        return string.Join(" - ", groups);
    }

    /// <summary>单字节展开为 8 位二进制（bit7→bit0，左侧为最高位）。</summary>
    public static string ToBinaryString8(byte value)
    {
        return Convert.ToString(value, 2).PadLeft(8, '0');
    }

    /// <summary>同上，高 4 位与低 4 位之间加「-」，例如 0x19 → <c>0001-1001</c>。</summary>
    public static string ToBinaryString8Nibbles(byte value)
    {
        var s = ToBinaryString8(value);
        return $"{s.AsSpan(0, 4)}-{s.AsSpan(4, 4)}";
    }

    /// <summary>
    /// bit 下标与「序号」对应关系：应答帧 <b>第 1 字节</b> 上序号 1～8 对应 bit0～bit7；
    /// <b>第 2 字节</b> 上序号 9、10 对应 bit0、bit1（例如 0x01=仅取货任务请求，0x02=仅放货任务请求）。
    /// </summary>
    public static bool GetThirdByteBit(byte value, int seq1To8)
    {
        if (seq1To8 < 1 || seq1To8 > 8)
        {
            return false;
        }

        return ((value >> (seq1To8 - 1)) & 1) != 0;
    }

    /// <summary>
    /// 应答帧第 1 字节（状态字节）：序号 1～8 对应 bit0～bit7（序号 1 为最低位）。
    /// 仅汇总 <b>值为 1</b> 的位，用「 + 」连接；无任何位为 1 时返回「无」。
    /// </summary>
    public static string FormatPlcThirdByteStatusForLog(byte value)
    {
        var parts = new List<string>(8);
        for (var slot = 1; slot <= 8; slot++)
        {
            var bit = slot - 1;
            if (((value >> bit) & 1) == 0)
            {
                continue;
            }

            var label = ThirdByteBitOnLabel(slot);
            if (!string.IsNullOrEmpty(label))
            {
                parts.Add(label);
            }
        }

        return parts.Count == 0 ? "无" : string.Join(" + ", parts);
    }

    /// <summary>表中该序号在位值为 1 时的简短说明（急停位 1 = 设备无急停）。</summary>
    private static string? ThirdByteBitOnLabel(int slot)
    {
        return slot switch
        {
            1 => "设备正常",
            2 => "允许取货",
            3 => "允许放货",
            4 => "通讯正常",
            5 => "设备无急停",
            6 => "备用位有效",
            7 => "允许进入预备位",
            8 => "允许离开工作位",
            _ => null
        };
    }

    /// <summary>支持无空格或带空格、可带 0x 前缀的十六进制字符串。</summary>
    public static byte[]? ParseHexToBytes(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return null;
        }

        var s = hex.Trim().Replace(" ", "", StringComparison.Ordinal)
            .Replace("0x", "", StringComparison.OrdinalIgnoreCase);
        if (s.Length % 2 != 0)
        {
            return null;
        }

        var bytes = new byte[s.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
        }

        return bytes;
    }

    /// <summary>
    /// 将较短载荷左侧对齐填充到 <paramref name="frameLength"/> 字节（右侧补 0），与文档中 10 字节帧一致。
    /// </summary>
    public static byte[] PadLeftToFrameLength(byte[]? payload, int frameLength)
    {
        if (frameLength <= 0)
        {
            return Array.Empty<byte>();
        }

        var result = new byte[frameLength];
        if (payload == null || payload.Length == 0)
        {
            return result;
        }

        var n = Math.Min(payload.Length, frameLength);
        Buffer.BlockCopy(payload, 0, result, 0, n);
        return result;
    }
}
