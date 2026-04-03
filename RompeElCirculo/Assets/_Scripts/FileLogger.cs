using System;
using System.IO;
using UnityEngine;
using System.Text;

public class FileLogger : MonoBehaviour
{
    private string logFilePath;
    [SerializeField]
    private int maxEntries = 10;

    private System.Collections.Generic.List<string> logEntries = new System.Collections.Generic.List<string>();

    void Awake()
    {
        logFilePath = Path.Combine(Application.persistentDataPath, $"app_log (v{Application.version}).txt");


        try
        {
            if (File.Exists(logFilePath))
            {
                var text = File.ReadAllText(logFilePath);
                var parts = text.Split(new string[] { Environment.NewLine + Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    logEntries.Add(p.Trim());
                }

                while (logEntries.Count > maxEntries)
                    logEntries.RemoveAt(0);
            }
        }
        catch
        {

        }


        Application.logMessageReceived += HandleLog;

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("===== INFO SISTEMA =====");
            sb.AppendLine("Fecha: " + DateTime.Now);
            sb.AppendLine("Ruta del log: " + logFilePath);
            sb.AppendLine("Plataforma: " + Application.platform);
            sb.AppendLine("Sistema operativo: " + SystemInfo.operatingSystem);
            sb.AppendLine("Dispositivo: " + SystemInfo.deviceModel + " (" + SystemInfo.deviceName + ")");
            sb.AppendLine("Tipo de dispositivo: " + SystemInfo.deviceType);
            sb.AppendLine("Procesador: " + SystemInfo.processorType + " (" + SystemInfo.processorCount + " cores)");
            sb.AppendLine("Memoria sistema (MB): " + SystemInfo.systemMemorySize);
            sb.AppendLine("GPU: " + SystemInfo.graphicsDeviceName + " - " + SystemInfo.graphicsDeviceVersion);

            if (Application.platform == RuntimePlatform.Android)
            {
                try
                {
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    {
                        int sdk = version.GetStatic<int>("SDK_INT");
                        string release = version.GetStatic<string>("RELEASE");
                        sb.AppendLine("Android release: " + release);
                        sb.AppendLine("Android API level: " + sdk);
                    }
                }
                catch
                {
                    
                }
            }

            WriteToFile(sb.ToString());
        }
        catch
        {
            
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string message =
            $"[{DateTime.Now}] [{type}] {logString}\n";

        if (type == LogType.Error || type == LogType.Exception)
        {
            message += stackTrace + "\n";
        }

        WriteToFile(message);
    }

    void WriteToFile(string text)
    {
        try
        {
            AddEntry(text);
            SaveEntriesToFile();
        }
        catch
        {
            
        }
    }

    void AddEntry(string text)
    {
        logEntries.Add(text.Trim());
        while (logEntries.Count > maxEntries)
        {
            logEntries.RemoveAt(0);
        }
    }

    void SaveEntriesToFile()
    {
        try
        {
            var content = string.Join(Environment.NewLine + Environment.NewLine, logEntries);
            File.WriteAllText(logFilePath, content + Environment.NewLine);
        }
        catch
        {
            
        }
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }
}
