using UnityEngine;
using System.Diagnostics;

public static class CustomLogger
{
    // Il metodo verrà eseguito ed elaborato SOLO in Editor o nelle Development Build
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

    //I log di errore possono essere mantenuti attivi anche in Release se lo desideri
    public static void LogError(object message)
    {
        UnityEngine.Debug.LogError(message);
    }
}