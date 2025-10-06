using DG.Tweening;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class AppManager : MonoBehaviour
{
    public static AppManager singleton;
    public static Data userData;
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
            Pesta�asManager.singleton._FRENTE.raycastTarget = true;
            Pesta�asManager.singleton._FRENTE.DOKill();
            Pesta�asManager.singleton._FRENTE.DOFade(1, 0.3f).SetDelay(0.2f).OnComplete(() => { 
                File.Delete(dataPath);                
                SceneManager.LoadScene(0); 
                Pesta�asManager.singleton._FRENTE.DOKill();
            });        
        }
    }

    public void InicializarApp()
    {
        if (CargarDatosDisco() != null)
        {
            Pesta�asManager.singleton.CambiarPesta�aSinTransicion(0);
        }
        else
        {
            Pesta�asManager.singleton.CambiarPesta�aSinTransicion(2);   
        }

        Pesta�asManager.singleton.AnimacionInicio();
    }

    public void GuardarDatosDisco()
    {
        try
        {
            string json = JsonConvert.SerializeObject(userData);
            File.WriteAllText(dataPath, json);
            UnityEngine.Debug.Log("[IntroController] Se guard� en el disco correctamente.");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[IntroController] Error al guardar datos: {ex.Message}");
        }
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
                return JsonConvert.DeserializeObject<Data>(json);
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
