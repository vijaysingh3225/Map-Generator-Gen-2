using System;
using WorldGen.Core;
using UnityEngine;

namespace WorldGen.Debug
{
    public static class DebugLog
    {
        public static void Log(WorldContext ctx, string message)
        {
            Append(ctx, "INFO", message, UnityLogType.Log);
        }

        public static void Warn(WorldContext ctx, string message)
        {
            Append(ctx, "WARN", message, UnityLogType.Warning);
        }

        public static void Error(WorldContext ctx, string message)
        {
            Append(ctx, "ERROR", message, UnityLogType.Error);
        }

        private enum UnityLogType { Log, Warning, Error }

        private static void Append(WorldContext ctx, string level, string message, UnityLogType unityLog)
        {
            if (ctx == null) return;
            if (message == null) message = string.Empty;

            var line = $"[{DateTime.Now:HH:mm:ss}] {level}: {message}";
            ctx.runLog.AppendLine(line);

            if (ctx.settings != null && ctx.settings.logToConsole)
            {
                switch (unityLog)
                {
                    case UnityLogType.Warning:
                        UnityEngine.Debug.LogWarning(line);
                        break;
                    case UnityLogType.Error:
                        UnityEngine.Debug.LogError(line);
                        break;
                    default:
                        UnityEngine.Debug.Log(line);
                        break;
                }
            }
        }
    }
}


