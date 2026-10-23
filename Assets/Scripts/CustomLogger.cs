using UnityEngine;
using System.Diagnostics;

public static class CustomLogger
{
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void Log(object message)
    {
        UnityEngine.Debug.Log(message);
    }

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void LogWarning(object message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

    public static void LogError(object message)
    {
        UnityEngine.Debug.LogError(message);
    }
}