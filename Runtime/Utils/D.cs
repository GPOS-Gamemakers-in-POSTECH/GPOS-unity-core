using UnityEngine;
using System.Diagnostics;

namespace GPOS.Core
{
    public static class D
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message) => UnityEngine.Debug.Log(message);
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message, Object context) => UnityEngine.Debug.Log(message, context);
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(object message) => UnityEngine.Debug.LogWarning(message);
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(object message) => UnityEngine.Debug.LogError(message);
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogColor(object message, string color) => UnityEngine.Debug.Log($"<color={color}>{message}</color>");
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogGreen(object message) => LogColor(message, "#4CAF50");
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogRed(object message) => LogColor(message, "#F44336");
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogYellow(object message) => LogColor(message, "#FFEB3B");
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogCyan(object message) => LogColor(message, "#00BCD4");
    }

}
