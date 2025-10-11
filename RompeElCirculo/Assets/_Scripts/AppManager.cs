using DG.Tweening;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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

    private void Awake()
    {
        dataPath = Application.persistentDataPath + "/userData.json";
        singleton = this;
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
        if (File.Exists(dataPath))
        {
            PestañasManager.singleton._FRENTE.raycastTarget = true;
            PestañasManager.singleton._FRENTE.DOKill();
            PestañasManager.singleton._FRENTE.DOFade(1, 0.3f).SetDelay(0.2f).OnComplete(() => { 
                File.Delete(dataPath);                
                SceneManager.LoadScene(0); 
                PestañasManager.singleton._FRENTE.DOKill();
            });        
        }
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
