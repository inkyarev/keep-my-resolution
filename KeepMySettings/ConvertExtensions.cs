using System;
using UnityEngine;

namespace KeepMySettings;

public static class ConvertExtensions
{
    public static string ToCfgString(this Resolution resolution)
    {
        return $"{resolution.width}x{resolution.height}x{resolution.refreshRate}";
    }

    public static bool ToBool(this string str)
    {
        return str switch
        {
            "0" => false,
            "1" => true,
            _ => false
        };
    }

    public static string ToCfgString(this bool boolean)
    {
        return boolean switch
        {
            false => "0",
            true => "1"
        };
    }

    public static int ToInt32(this string str)
    {
        return Convert.ToInt32(str);
    }
    public static float ToSingle(this string str)
    {
        return Convert.ToSingle(str);
    }
}