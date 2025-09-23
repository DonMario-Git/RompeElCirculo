using UnityEngine;
using UnityEngine.UI;
using TMPro; // Importa TextMeshPro
using Firebase.Database;
using Newtonsoft.Json;
using System;
using System.IO;
using DG.Tweening;
using UnityEngine.Networking;
using UtilidadesLaEME;

public class AutoUpdaterController : MonoBehaviour
{
    [System.Serializable]
    public class AppUpdateInfo
    {
        public string latestVersion;
        public string githubLink;
    }

    [Header("UI Progreso de actualización")]
    public Image updateProgressImage; // Asigna la imagen desde el inspector
    public TextMeshProUGUI updateStatusText; // Asigna el texto desde el inspector
    public UnityEngine.GameObject interfazActualizacion;
    public Image imageFrente;
    public static AutoUpdaterController singleton;

    private float targetFill = 0f;
    private float fillSpeed = 5f; // Velocidad de relleno

    public bool permitirActualizar;

    public GameObject objWindowActualizar;

    private void Awake()
    {
        singleton = this;

        permitirActualizar = !Application.version.EndsWithChar('b');
    }

    public void SetPermitirActualizar(bool value)
    {
        permitirActualizar = value;
    }

    public void BuscarActualizaciones()
    {
        interfazActualizacion.SetActive(true);
        imageFrente.DOFade(0, 0.3f).OnComplete(() => { 
            if (updateProgressImage != null)
                updateProgressImage.fillAmount = 0f;

            if (updateStatusText != null)
                updateStatusText.text = "Buscando actualizaciones...";

            StartCoroutine(UpdateProgressRoutine());
            CheckForUpdate();
        });     
    }

    private System.Collections.IEnumerator UpdateProgressRoutine()
    {
        while (updateProgressImage != null && updateProgressImage.fillAmount < 1f)
        {
            updateProgressImage.fillAmount = Mathf.Lerp(updateProgressImage.fillAmount, targetFill, Time.deltaTime * fillSpeed);
            yield return null;
        }
    }

    public void SetProgress(float progress, string status = null)
    {
        targetFill = Mathf.Clamp01(progress);
        if (updateStatusText != null && status != null)
            updateStatusText.text = status;
    }

    private void CheckForUpdate()
    {
        SetProgress(0.2f, "Conectando con el servidor...");

        if (FirebaseStorageManager.singleton == null || !FirebaseStorageManager.singleton.isInitialized)
        {
            SetProgress(0.2f, "Inicializando Firebase...");
            Invoke(nameof(CheckForUpdate), 1f);
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            SetProgress(1f, "Sin conexión a internet.");
            Debug.LogWarning("No hay conexión a internet. No se puede verificar actualizaciones.");
            return;
        }

        SetProgress(0.5f, "Consultando base de datos...");
        FirebaseStorageManager.singleton.StartCoroutine(GetUpdateInfoCoroutine());
    }

    private System.Collections.IEnumerator GetUpdateInfoCoroutine()
    {
        var dbRef = FirebaseStorageManager.singleton.GetType()
            .GetField("dbReference", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(FirebaseStorageManager.singleton) as DatabaseReference;

        var task = dbRef.Child("app_update").GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        SetProgress(0.8f, "Procesando información...");

        if (task.IsFaulted || task.IsCanceled)
        {
            LogError("Error al consultar la actualización: " + task.Exception);
            SetProgress(1f, "Error al consultar la actualización.");
            yield break;
        }

        if (task.Result.Exists)
        {
            string json = task.Result.GetRawJsonValue();
            AppUpdateInfo updateInfo = JsonConvert.DeserializeObject<AppUpdateInfo>(json);
            if (updateInfo != null && !string.IsNullOrEmpty(updateInfo.latestVersion) && !string.IsNullOrEmpty(updateInfo.githubLink))
            {
                if (updateInfo.latestVersion != Application.version)
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                    SetProgress(1f, $"Nueva versión disponible: {updateInfo.latestVersion}");
                    Debug.Log($"Nueva versión disponible: {updateInfo.latestVersion}. Descargando desde: {updateInfo.githubLink}");

                    yield return new WaitForSeconds(1);

                    // Solicita permisos antes de descargar
                    using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (AndroidJavaObject packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager"))
                    {
                        string packageName = currentActivity.Call<string>("getPackageName");
                        bool canInstall = packageManager.Call<bool>("canRequestPackageInstalls");

                        if (!canInstall)
                        {
                            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.settings.MANAGE_UNKNOWN_APP_SOURCES"))
                            using (AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri"))
                            {
                                AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + packageName);
                                intent.Call<AndroidJavaObject>("setData", uri);
                                currentActivity.Call("startActivity", intent);
                            }
                            SetProgress(1f, "Por favor, otorga permiso para instalar apps desconocidas...");
                            // Espera hasta que el permiso sea concedido
                            yield return new WaitUntil(() =>
                            {
                                using (AndroidJavaObject pm = currentActivity.Call<AndroidJavaObject>("getPackageManager"))
                                {
                                    return pm.Call<bool>("canRequestPackageInstalls");
                                }
                            });
                            SetProgress(1f, "Permiso concedido, continuando...");
                            yield return new WaitForSeconds(0.5f);
                        }
                    }

                    StartCoroutine(DownloadAndInstallApk(updateInfo.githubLink));
#else
                    SetProgress(1, $"EXISTE {updateInfo.latestVersion} pero Iniciando");
                    Debug.Log($"Nueva versión disponible: {updateInfo.latestVersion} en {updateInfo.githubLink}");

                    yield return new WaitForSeconds(1);

                    IntroController.singleton.InicializarApp();
#endif
                }
                else
                {
                    SetProgress(1f, "La aplicación está actualizada.");
                    Debug.Log("La aplicación está actualizada.");
                    yield return new WaitForSeconds(1);
                    IntroController.singleton.InicializarApp();
                }
            }
            else
            {
                SetProgress(1f, "Información de actualización no válida.");
                LogError("No se encontró información válida de actualización.");
            }
        }
        else
        {
            SetProgress(1f, "No existe información de actualización.");
            LogError("No existe la colección 'app_update' en la base de datos.");
            SaveAppUpdateInfo(new AppUpdateInfo()
            {
                latestVersion = "1.0.0",
                githubLink = "google.com"
            }, (result) =>
            {
                Debug.Log(result);
            });
        }
    }

    private System.Collections.IEnumerator DownloadAndInstallApk(string url)
    {
        if (Application.version.EndsWithChar('b')) objWindowActualizar.gameObject.Enable();

        while (!permitirActualizar)
        {
            yield return null;
        }

        SetProgress(0f, "Descargando actualización...");

        string filePath = Path.Combine(Application.persistentDataPath, "update.apk");
        if (File.Exists(filePath))
            File.Delete(filePath);

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.downloadHandler = new DownloadHandlerFile(filePath);
            www.SendWebRequest();

            while (!www.isDone)
            {
                // Actualiza el progreso visual
                SetProgress(Mathf.Lerp(0, 1, www.downloadProgress));
                if (updateStatusText != null)
                    updateStatusText.text = $"Descargando actualización... {Mathf.RoundToInt(www.downloadProgress * 100)}%";
                yield return null;
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                SetProgress(1f, "Error al descargar la actualización.");
                LogError("Error al descargar APK: " + www.error);
                yield return new WaitForSeconds(1);
                //IntroController.singleton.InicializarApp();
                yield break;
            }
        }

        SetProgress(1f, "Instalando actualización...");
        yield return new WaitForSeconds(1);

#if UNITY_ANDROID && !UNITY_EDITOR
        // Verifica si tiene permiso para instalar paquetes
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager"))
        {
            string packageName = currentActivity.Call<string>("getPackageName");
            bool canInstall = packageManager.Call<bool>("canRequestPackageInstalls");

            if (!canInstall)
            {
                // Solicita al usuario que habilite el permiso
                using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.settings.MANAGE_UNKNOWN_APP_SOURCES"))
                using (AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri"))
                {
                    AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + packageName);
                    intent.Call<AndroidJavaObject>("setData", uri);
                    currentActivity.Call("startActivity", intent);
                }
                SetProgress(1f, "Por favor, otorga permiso para instalar apps desconocidas e intenta de nuevo.");
                yield break;
            }
        }
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaClass fileProvider = new AndroidJavaClass("androidx.core.content.FileProvider"))
            using (AndroidJavaObject file = new AndroidJavaObject("java.io.File", filePath))
            {
                string authority = Application.identifier + ".fileprovider";
                AndroidJavaObject uri = fileProvider.CallStatic<AndroidJavaObject>(
                    "getUriForFile", currentActivity, authority, file);

                using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW"))
                {
                    intent.Call<AndroidJavaObject>("setDataAndType", uri, "application/vnd.android.package-archive");
                    intent.Call<AndroidJavaObject>("addFlags", 0x10000000); // FLAG_ACTIVITY_NEW_TASK
                    intent.Call<AndroidJavaObject>("addFlags", 0x1);        // FLAG_GRANT_READ_URI_PERMISSION

                    currentActivity.Call("startActivity", intent);
                    Debug.Log("Intent de instalación lanzado");
                }
            }
        }
        catch (Exception e)
        {
            LogError("Error al intentar instalar el APK: " + e.Message);
        }
// Application.Quit(); // Comenta esta línea temporalmente para depurar
#endif
    }

    private void LogError(string message)
    {
        Debug.LogError(message);
        string logPath = Path.Combine(Application.persistentDataPath, "errorlog.txt");
        try
        {
            File.AppendAllText(logPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + message + Environment.NewLine);
        }
        catch (Exception e)
        {
            Debug.LogError("No se pudo escribir el error en el log: " + e.Message);
        }
    }

    public async void SaveAppUpdateInfo(AppUpdateInfo info, Action<string> onResult)
    {
        if (!FirebaseStorageManager.singleton.isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("No hay conexión a internet.");
            return;
        }

        string json = JsonConvert.SerializeObject(info);
        var dbReference = FirebaseStorageManager.singleton.GetType()
            .GetField("dbReference", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(FirebaseStorageManager.singleton) as DatabaseReference;

        if (dbReference == null)
        {
            onResult?.Invoke("No se pudo obtener la referencia de la base de datos.");
            return;
        }

        var saveTask = dbReference.Child("app_update").SetRawJsonValueAsync(json);
        await saveTask;

        if (saveTask.IsFaulted || saveTask.IsCanceled)
            onResult?.Invoke("Error al guardar: " + saveTask.Exception);
        else
            onResult?.Invoke(null);
    }
}
