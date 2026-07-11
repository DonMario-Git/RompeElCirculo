using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

public class DataBaseExcelGenerator : MonoBehaviour
{
    [ContextMenu(nameof(Execute))]
    public void Execute()
    {
        FirebaseStorageManager.singleton.GetAllUsuarios((usuarios, error) =>
        {
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError("Error al descargar usuarios: " + error);
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
        // El permiso solo aplica en API <29, en API 29+ con MediaStore no hace falta
        if (ObtenerAndroidSDKInt() < 29 && !Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
#endif
            ExportarComoCSV(usuarios);
        });
    }

    void ExportarComoCSV(List<Data> usuarios)
    {
        byte[] datosCSV = GenerarCSVBytes(usuarios);
        string nombreArchivo = "usuarios.csv";

#if UNITY_ANDROID && !UNITY_EDITOR
    if (ObtenerAndroidSDKInt() >= 29)
    {
        bool exito = EscribirEnMediaStore(datosCSV, nombreArchivo);
        MostrarToast(exito
            ? "CSV guardado en Descargas correctamente"
            : "Error al guardar el CSV");
    }
    else
    {
        try
        {
            string path = Path.Combine("/storage/emulated/0/Download", nombreArchivo);
            File.WriteAllBytes(path, datosCSV);
            RefrescarMediaScannerAndroid(path);
            Debug.Log("CSV generado en: " + path);
            MostrarToast("CSV guardado en Descargas correctamente");
        }
        catch (Exception e)
        {
            Debug.LogError("Error al guardar CSV: " + e);
            MostrarToast("Error al guardar el CSV");
        }
    }
#else
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string pathDesktop = Path.Combine(desktopPath, nombreArchivo);
        File.WriteAllBytes(pathDesktop, datosCSV);
        Debug.Log("CSV generado en: " + pathDesktop);
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void MostrarToast(string mensaje)
    {
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try
                {
                    using (AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast"))
                    using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                    {
                        AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", mensaje);
                        AndroidJavaObject toast = toastClass.CallStatic<AndroidJavaObject>(
                            "makeText", context, javaString, 1 /* Toast.LENGTH_LONG */);
                        toast.Call("show");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Error mostrando Toast dentro del runnable: " + e.Message);
                }
            }));
        }
        catch (Exception e)
        {
            Debug.LogWarning("No se pudo mostrar el Toast: " + e.Message);
        }
    }
#endif



    byte[] GenerarCSVBytes(List<Data> usuarios)
    {
        var sb = new StringBuilder();

        sb.AppendLine(string.Join(";",
            "Nombre completo",
            "Tipo documento",
            "Número documento",
            "Número celular",
            "Sexo",
            "Correo autenticado",
            "Verificado",
            "Fecha nacimiento",
            "Nacionalidad",
            "Municipio",
            "Dirección",
            "Correo electrónico",
            "Contraseña",
            "Respuestas violentómetro"
        ));

        foreach (var u in usuarios)
        {
            string violentometro = u.respuestasViolentometro != null
                ? string.Join(",", u.respuestasViolentometro.Select(b => b ? "1" : "0"))
                : "";

            sb.AppendLine(string.Join(";",
                Escapar(u.nombreCompleto.ToUpper()),
                Escapar(u.tipoDocumento),
                Escapar(u.numeroDocumento),
                Escapar(u.numeroCelular),
                Escapar(u.sexo),
                SiNo(u.correoAutenticado),
                SiNo(u.isAdmin),
                Escapar(u.fechaNacimiento),
                Escapar(u.nacionalidad),
                Escapar(string.IsNullOrEmpty(u.municipio) ? "-Sin definir-" : u.municipio),
                Escapar(u.direccion),
                Escapar(u.email),
                Escapar(u.contrasena),
                Escapar(violentometro)
            ));
        }

        // BOM UTF-8 + contenido, para que Excel lo reconozca bien
        var utf8WithBom = new UTF8Encoding(true);
        byte[] bom = utf8WithBom.GetPreamble();
        byte[] contenido = Encoding.UTF8.GetBytes(sb.ToString());

        byte[] resultado = new byte[bom.Length + contenido.Length];
        Buffer.BlockCopy(bom, 0, resultado, 0, bom.Length);
        Buffer.BlockCopy(contenido, 0, resultado, bom.Length, contenido.Length);
        return resultado;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
int ObtenerAndroidSDKInt()
{
    using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
    {
        return version.GetStatic<int>("SDK_INT");
    }
}

#if UNITY_ANDROID && !UNITY_EDITOR
bool EscribirEnMediaStore(byte[] datos, string nombreArchivo)
{
    try
    {
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
        using (AndroidJavaObject contentValues = new AndroidJavaObject("android.content.ContentValues"))
        using (AndroidJavaClass mediaStoreDownloads = new AndroidJavaClass("android.provider.MediaStore$Downloads"))
        {
            contentValues.Call("put", "_display_name", nombreArchivo);
            contentValues.Call("put", "mime_type", "text/csv");
            contentValues.Call("put", "relative_path", "Download/");

            AndroidJavaObject collectionUri = mediaStoreDownloads.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI");
            AndroidJavaObject itemUri = resolver.Call<AndroidJavaObject>("insert", collectionUri, contentValues);

            if (itemUri == null)
            {
                Debug.LogError("No se pudo crear el archivo en MediaStore.");
                return false;
            }

            using (AndroidJavaObject outputStream = resolver.Call<AndroidJavaObject>("openOutputStream", itemUri))
            {
                sbyte[] datosSbyte = new sbyte[datos.Length];
                Buffer.BlockCopy(datos, 0, datosSbyte, 0, datos.Length);
                outputStream.Call("write", datosSbyte);
                outputStream.Call("flush");
                outputStream.Call("close");
            }

            Debug.Log("CSV guardado en Descargas (MediaStore): " + nombreArchivo);
            return true;
        }
    }
    catch (Exception e)
    {
        Debug.LogError("Error escribiendo en MediaStore: " + e);
        return false;
    }
}
#endif

void RefrescarMediaScannerAndroid(string path)
{
    try
    {
        using (AndroidJavaClass mediaScanner = new AndroidJavaClass("android.media.MediaScannerConnection"))
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
        {
            mediaScanner.CallStatic("scanFile", context, new string[] { path }, null, null);
        }
    }
    catch (Exception e)
    {
        Debug.LogWarning("No se pudo refrescar el media scanner: " + e.Message);
    }
}
#endif

    string SiNo(bool valor) => valor ? "Sí" : "No";

    string Escapar(string valor)
    {
        if (string.IsNullOrEmpty(valor)) return "";
        if (valor.Contains(";") || valor.Contains("\"") || valor.Contains("\n"))
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        return valor;
    }
}
