using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class DataBaseExcelGenerator : MonoBehaviour
{
    [ContextMenu(nameof(Execute))]
    public void Execute()
    {
        var firebaseManager = FindFirstObjectByType<FirebaseStorageManager>();
        if (firebaseManager == null)
        {
            Debug.LogError("No se encontró FirebaseStorageManager en la escena.");
            return;
        }

        // Descargar todos los usuarios de la colección 'users' de la base de datos y exportar a CSV
        firebaseManager.GetAllUsers((usuarios, error) =>
        {
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError("Error al descargar usuarios: " + error);
                return;
            }
            ExportarComoCSV(usuarios);
        });
    }

    void ExportarComoCSV(List<Data> usuarios)
    {
        //// Ruta al escritorio
        //string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        //string path = Path.Combine(desktopPath, "usuarios.csv");

        //using (StreamWriter writer = new StreamWriter(path))
        //{
        //    // Cabeceras (ajusta según los campos de Data)
        //    writer.WriteLine("Nombres,CorreoElecctronico,Contrasenia,hizoFormulario,numeroDocumento,activo,numeroRango,numeroSiguienteNivel,dispositivoCreacion,ultimoDispositivo");

        //    // Filas de datos
        //    foreach (var u in usuarios)
        //    {
        //        writer.WriteLine($"{u.nombreCompleto},{u.CorreoElecctronico},{u.Contrasenia},{u.hizoFormulario},{u.numeroDocumento},{u.numeroRango},{u.numeroSiguienteNivel},{u.dispositivoCreacion},{u.ultimoDispositivo}");
        //    }
        //}

        //Debug.Log("CSV generado en: " + path);
    }
}
