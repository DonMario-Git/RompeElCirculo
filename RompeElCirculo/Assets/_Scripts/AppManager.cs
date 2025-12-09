using DG.Tweening;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UtilidadesLaEME;

public class AppManager : MonoBehaviour
{
    public static AppManager singleton;
    private static Data _userData;
    public static Data userData
    {
        get => _userData;
        set
        {
            _userData = value;
            OnDataLoad?.Invoke();
        }
    }
    public static event Action OnDataLoad;
    public static event Action OnBackPressed;
    public static string dataPath;

    public TextMeshProUGUI textoNombre;
    public GameObject objetoVerificado;

    private void Awake()
    {
        dataPath = Application.persistentDataPath + "/userData.json";
        singleton = this;

        OnDataLoad += () => {
            textoNombre.text = Utilities.GetFirstWord(userData.nombreCompleto);
            objetoVerificado.SetActive(userData.verificado);
        };
    }

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        InicializarApp();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackPressed?.Invoke();
        }
    }

    public void CerrarSesion()
    {
        // Ensure UI blocks and animations are stopped immediately
        PestañasManager.singleton._FRENTE.raycastTarget = true;
        PestañasManager.singleton._FRENTE.DOKill();
        // Start coroutine to delete file and wait until it's removed before quitting
        StartCoroutine(CerrarSesionCoroutine());
    }

    private IEnumerator CerrarSesionCoroutine()
    {
        if (File.Exists(dataPath))
        {
            try
            {
                File.Delete(dataPath);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[AppManager] Error al eliminar archivo: {ex.Message}");
            }

            // Wait until the file no longer exists or until a timeout to avoid hanging forever
            float timeout = 5f;
            float startTime = Time.realtimeSinceStartup;
            while (File.Exists(dataPath) && Time.realtimeSinceStartup - startTime < timeout)
            {
                yield return null;
            }

            if (File.Exists(dataPath))
            {
                UnityEngine.Debug.LogError("[AppManager] No se pudo eliminar el archivo antes del timeout.");
                yield break;
            }
        }

        Application.Quit();     
    }

    public void InicializarApp()
    {
        if (CargarDatosDisco() != null)
        {
            PestañasManager.singleton.CambiarPestañaSinTransicion(0);
        }
        else
        {
            PestañasManager.singleton.CambiarPestañaSinTransicion(2);   
        }

        PestañasManager.singleton.AnimacionInicio();
    }

    public void GuardarDatosDisco()
    {
        try
        {
            string json = JsonConvert.SerializeObject(userData);
            File.WriteAllText(dataPath, json);
            UnityEngine.Debug.Log("[IntroController] Se guardó en el disco correctamente.");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[IntroController] Error al guardar datos: {ex.Message}");
        }
    }

    public void AbrirLink(string link)
    {
        Application.OpenURL(link);
    }

    public void LlamarPorWhatsApp(string numero)
    {
        string url = $"https://wa.me/{numero}";
        Application.OpenURL(url);
    }

    public void LlamarPorTelefono(string numero)
    {
        string url = $"tel:{numero}";
        Application.OpenURL(url);
    }

    public Data CargarDatosDisco()
    {
        if (File.Exists(dataPath))
        {
            UnityEngine.Debug.Log("[IntroController] Archivo de usuario encontrado, leyendo datos...");
            string json = string.Empty;
            try
            {
                json = File.ReadAllText(dataPath);
                Data data = JsonConvert.DeserializeObject<Data>(json);

                if (data != null)
                {
                    userData = data;
                    UnityEngine.Debug.Log("Datos del disco cargados correctamente");
                    return data;
                }
                else
                {
                    UnityEngine.Debug.LogError($"[IntroController] Error al leer el archivo: conversion incorrecta");
                    return null;
                }  
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[IntroController] Error al leer el archivo: {ex.Message}");
                return null;
            }
        }
        else
        {
            return null;
        }
    } 
}
