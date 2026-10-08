using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class DevLogs
{
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message, Object context = null)
    {
        Debug.Log(message, context);
    }

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message, Color color, Object context = null)
    {
        Debug.Log(Colorize(message, color), context);
    }

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message, Object context = null)
    {
        Debug.LogWarning(message, context);
    }

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message, Color color, Object context = null)
    {
        Debug.LogWarning(Colorize(message, color), context);
    }

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string message, Object context = null)
    {
        Debug.LogError(message, context);
    }

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string message, Color color, Object context = null)
    {
        Debug.LogError(Colorize(message, color), context);
    }

    private static string Colorize(string message, Color color)
    {
#if UNITY_EDITOR
        string hex = ColorUtility.ToHtmlStringRGB(color);
        return $"<color=#{hex}>{message}</color>";
#else
    return message;
#endif
    }
}