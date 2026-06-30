using System;
using System.Collections.Generic;
using Ecs.ConfigTool;

namespace Ecs.AgvPlcTcp;

/// <summary>
/// TCP 字节流粘包/错位处理：可选「帧同步前缀」，在缓冲区中先定位前缀再按 FrameLength 切帧，丢弃前缀前的杂字节。
/// </summary>
public static class AgvPlcTcpFrameExtractor
{
    /// <summary>同步长期找不到时丢弃最前一字节滑动窗口，避免内存无限增长。</summary>
    private const int MaxBufferSlidingWindow = 65536;

    /// <summary>
    /// 从 buffer 中尽可能切出完整帧并交给 handler；buffer 会被就地修改。
    /// </summary>
    public static void ConsumeFrames(
        List<byte> buffer,
        AgvPlcTcpOptions options,
        Action<byte[]> onFrame)
    {
        var frameLen = Math.Max(1, options.FrameLength);
        var sync = AgvPlcFrameParser.ParseHexToBytes(options.FrameSyncPrefixHex);
        if (sync == null || sync.Length == 0)
        {
            while (buffer.Count >= frameLen)
            {
                var frame = buffer.GetRange(0, frameLen).ToArray();
                buffer.RemoveRange(0, frameLen);
                onFrame(frame);
            }

            return;
        }

        if (sync.Length > frameLen)
        {
            return;
        }

        while (buffer.Count >= sync.Length)
        {
            var idx = FindSubsequence(buffer, sync);
            if (idx < 0)
            {
                TrimBufferIfTooLarge(buffer);
                return;
            }

            if (idx > 0)
            {
                buffer.RemoveRange(0, idx);
            }

            if (buffer.Count < frameLen)
            {
                return;
            }

            var frame = buffer.GetRange(0, frameLen).ToArray();
            buffer.RemoveRange(0, frameLen);
            onFrame(frame);
        }
    }

    private static int FindSubsequence(List<byte> buffer, byte[] pattern)
    {
        if (pattern.Length == 0 || buffer.Count < pattern.Length)
        {
            return -1;
        }

        for (var i = 0; i <= buffer.Count - pattern.Length; i++)
        {
            var match = true;
            for (var j = 0; j < pattern.Length; j++)
            {
                if (buffer[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    private static void TrimBufferIfTooLarge(List<byte> buffer)
    {
        if (buffer.Count <= MaxBufferSlidingWindow)
        {
            return;
        }

        buffer.RemoveRange(0, buffer.Count - MaxBufferSlidingWindow);
    }
}
