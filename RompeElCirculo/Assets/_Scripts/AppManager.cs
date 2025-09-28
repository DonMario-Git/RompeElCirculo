using UnityEngine;
using System;

public class AppManager : MonoBehaviour
{
    public static event Action OnBackPressed;

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackPressed?.Invoke();
        }
    }
}
