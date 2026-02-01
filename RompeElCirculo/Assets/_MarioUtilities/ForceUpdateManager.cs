using System.Collections;
using Google.Play.AppUpdate;
using Google.Play.Common;
using UnityEngine;

public class ForceUpdateManager : MonoBehaviour
{
    private AppUpdateManager appUpdateManager;

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        appUpdateManager = new AppUpdateManager();
        StartCoroutine(CheckForUpdateCoroutine());
#endif
    }

    IEnumerator CheckForUpdateCoroutine()
    {
        var updateRequest = appUpdateManager.GetAppUpdateInfo();
        yield return updateRequest;

        if (!updateRequest.IsSuccessful)
        {
            Debug.LogWarning("Error al comprobar actualización: " + updateRequest.Error);
            yield break;
        }

        var info = updateRequest.GetResult();
        if (info.UpdateAvailability == UpdateAvailability.UpdateAvailable &&
            info.IsUpdateTypeAllowed(AppUpdateOptions.ImmediateAppUpdateOptions()))
        {
            StartCoroutine(StartImmediateUpdateCoroutine(info));
        }
    }

    IEnumerator StartImmediateUpdateCoroutine(AppUpdateInfo info)
    {
        var options = AppUpdateOptions.ImmediateAppUpdateOptions();
        var startRequest = appUpdateManager.StartUpdate(info, options);
        yield return startRequest;

        if (startRequest.Status == AppUpdateStatus.Failed || startRequest.Error != AppUpdateErrorCode.NoError)
        {
            Debug.LogWarning("Actualización cancelada o fallida: " + startRequest.Error);
        }

        // En flujo inmediato, si la actualización tiene éxito la app se reiniciará y
        // este punto no se alcanzará.
    }
}
