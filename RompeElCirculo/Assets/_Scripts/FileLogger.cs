using System;
using System.IO;
using UnityEngine;

public class FileLogger : MonoBehaviour
{
    private string logFilePath;

    void Awake()
    {
        // Ruta del archivo en persistentDataPath
        logFilePath = Path.Combine(Application.persistentDataPath, "app_log.txt");

        // Suscribirse al evento de logs
        Application.logMessageReceived += HandleLog;

        // Log inicial
        WriteToFile("===== INICIO DEL LOG =====");
        WriteToFile("Fecha: " + DateTime.Now);
        WriteToFile("Ruta: " + logFilePath);
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string message =
            $"[{DateTime.Now}] [{type}] {logString}\n";

        // Si es error, agrega stacktrace
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
            File.AppendAllText(logFilePath, text + "\n");
        }
        catch
        {
            // Evita crasheos si no se puede escribir
        }
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }
}
