using System;
using System.Collections.Generic;

namespace Ecs.AgvPlcTcp;

/// <summary>
/// 四条线 × 四点位（每线 O{n}A～O{n}D）。配置键名须与线别一致：O1、O2、O3、O4。
/// </summary>
public static class AgvPlcPointCodes
{
    public const string O1A = "O1A";
    public const string O1B = "O1B";
    public const string O1C = "O1C";
    public const string O1D = "O1D";

    public const string O2A = "O2A";
    public const string O2B = "O2B";
    public const string O2C = "O2C";
    public const string O2D = "O2D";

    public const string O3A = "O3A";
    public const string O3B = "O3B";
    public const string O3C = "O3C";
    public const string O3D = "O3D";

    public const string O4A = "O4A";
    public const string O4B = "O4B";
    public const string O4C = "O4C";
    public const string O4D = "O4D";

    /// <summary>线别键（与配置文件 Lines 下键一致）：O1～O4。</summary>
    public static readonly string[] LineKeys = { "O1", "O2", "O3", "O4" };

    private static readonly Dictionary<string, string[]> LineKeyToQuad =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["O1"] = new[] { O1A, O1B, O1C, O1D },
            ["O2"] = new[] { O2A, O2B, O2C, O2D },
            ["O3"] = new[] { O3A, O3B, O3C, O3D },
            ["O4"] = new[] { O4A, O4B, O4C, O4D }
        };

    /// <summary>全部 16 个点位（Redis / 轮询等遍历用）。</summary>
    public static readonly string[] AllPointsAcrossLines =
    {
        O1A, O1B, O1C, O1D,
        O2A, O2B, O2C, O2D,
        O3A, O3B, O3C, O3D,
        O4A, O4B, O4C, O4D
    };

    /// <summary>兼容旧代码：仅 O1 四点位。</summary>
    public static readonly string[] All = { O1A, O1B, O1C, O1D };

    public static bool TryGetQuadPoints(string lineKey, out string[] quad)
    {
        quad = Array.Empty<string>();
        if (string.IsNullOrWhiteSpace(lineKey))
        {
            return false;
        }

        return LineKeyToQuad.TryGetValue(lineKey.Trim(), out quad);
    }

    public static bool TryNormalizePointCode(string input, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var t = input.Trim();
        foreach (var p in AllPointsAcrossLines)
        {
            if (string.Equals(p, t, StringComparison.OrdinalIgnoreCase))
            {
                canonical = p;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 解析点位编码：除 <see cref="TryNormalizePointCode"/> 外，兼容 01A（数字 0）、1A 等写法。
    /// </summary>
    public static bool TryResolvePointCode(string input, out string canonical)
    {
        if (TryNormalizePointCode(input, out canonical))
        {
            return true;
        }

        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var t = input.Trim();

        if (t.Length == 3)
        {
            var c0 = t[0];
            var c1 = t[1];
            var c2 = t[2];
            if ((c0 == 'O' || c0 == 'o' || c0 == '0') &&
                c1 >= '1' && c1 <= '4' &&
                (c2 is >= 'A' and <= 'D' or >= 'a' and <= 'd'))
            {
                canonical = $"O{c1}{char.ToUpperInvariant(c2)}";
                return true;
            }
        }

        if (t.Length == 2)
        {
            var c1 = t[0];
            var c2 = t[1];
            if (c1 >= '1' && c1 <= '4' &&
                (c2 is >= 'A' and <= 'D' or >= 'a' and <= 'd'))
            {
                canonical = $"O{c1}{char.ToUpperInvariant(c2)}";
                return true;
            }
        }

        return false;
    }
}
