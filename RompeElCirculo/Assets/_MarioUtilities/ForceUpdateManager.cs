using System.Collections;
using System.Text;
using Google.Play.AppUpdate;
using Google.Play.Common;
using TMPro;
using UnityEngine;

public class ForceUpdateManager : MonoBehaviour
{
    private AppUpdateManager appUpdateManager;
    private StringBuilder logBuilder = new StringBuilder();

    void Start()
    {
#if !UNITY_EDITOR
        appUpdateManager = new AppUpdateManager();
        StartCoroutine(CheckForUpdateCoroutine());
#endif
    }

    IEnumerator CheckForUpdateCoroutine()
    {
        Log("Requesting update info...");
        var updateRequest = appUpdateManager.GetAppUpdateInfo();
        yield return updateRequest;

        Log($"GetAppUpdateInfo finished. Success={updateRequest.IsSuccessful}, Error={updateRequest.Error}");

        if (!updateRequest.IsSuccessful)
        {
            Log("Error al comprobar actualización: " + updateRequest.Error);
            yield break;
        }

        var info = updateRequest.GetResult();
        Log("AppUpdateInfo: " + info.ToString());
        Log($"AvailableVersionCode={info.AvailableVersionCode}, UpdateAvailability={info.UpdateAvailability}, AppUpdateStatus={info.AppUpdateStatus}, ClientVersionStalenessDays={info.ClientVersionStalenessDays}, UpdatePriority={info.UpdatePriority}, BytesDownloaded={info.BytesDownloaded}, TotalBytesToDownload={info.TotalBytesToDownload}");
        Log($"ImmediateAllowed={info.IsUpdateTypeAllowed(AppUpdateOptions.ImmediateAppUpdateOptions())}, FlexibleAllowed={info.IsUpdateTypeAllowed(AppUpdateOptions.FlexibleAppUpdateOptions())}");

        if (info.UpdateAvailability == UpdateAvailability.UpdateAvailable)
        {
            if (info.IsUpdateTypeAllowed(AppUpdateOptions.ImmediateAppUpdateOptions()))
            {
                Log("Update available and immediate update allowed. Starting immediate update...");
                StartCoroutine(StartImmediateUpdateCoroutine(info));
            }
            else
            {
                Log("Update available but immediate update NOT allowed.");
            }
        }
        else
        {
            Log("No update available. UpdateAvailability=" + info.UpdateAvailability);
        }
    }

    IEnumerator StartImmediateUpdateCoroutine(AppUpdateInfo info)
    {

        Log("Starting immediate update request...");
        var options = AppUpdateOptions.ImmediateAppUpdateOptions();
        var startRequest = appUpdateManager.StartUpdate(info, options);
        yield return startRequest;

        Log($"StartUpdate finished. Status={startRequest.Status}, Error={startRequest.Error}");

        if (startRequest.Status == AppUpdateStatus.Failed || startRequest.Error != AppUpdateErrorCode.NoError)
        {
            Log("Actualización cancelada o fallida: " + startRequest.Error);
        }
        else
        {
            Log("StartUpdate returned status: " + startRequest.Status + " (si la actualización es exitosa la app se reiniciará y este punto puede no alcanzarse)");
        }

        // En flujo inmediato, si la actualización tiene éxito la app se reiniciará y
        // este punto no se alcanzará.
    }

    private void Log(string message)
    {
        var entry = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
        Debug.Log(entry);
    }
}
