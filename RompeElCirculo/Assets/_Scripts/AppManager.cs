using DG.Tweening;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections;
using TMPro;
using UnityEngine;
using UtilidadesLaEME;
using UnityEngine.InputSystem;

public class AppManager : MonoBehaviour
{
    public static AppManager singleton;
    private static Data _userData;
    public static Data UserData
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
    public static string dataPath {get => Application.persistentDataPath + "/userData.json";}

    public TextMeshProUGUI textoNombre;
    public GameObject objetoVerificado;

    public static int versionActualInfoMunicipios;
    public static InfoTablaMunicipios[] informacionMunicipios;


    public static string comisariaArchivoPath { get => Application.persistentDataPath + $"/COMISARIAS_DE_FAMILIA_MUNICIPALES.json"; }
    public static string versionArchivoComisaria { get => Application.persistentDataPath + $"/COMISARIAS_DE_FAMILIA_MUNICIPALES_version.txt"; }

    private void Awake()
    {
        singleton = this;

        OnDataLoad += () => {
            textoNombre.text = Utilities.GetFirstWord(UserData.nombreCompleto);
            objetoVerificado.SetActive(UserData.isAdmin);
        };
    }

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 1000;  
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnBackPressed?.Invoke();
        }
    }

    public void CerrarSesion()
    {
        StartCoroutine(CerrarSesionCoroutine());
    }

    private IEnumerator CerrarSesionCoroutine()
    {
        if (File.Exists(dataPath))
        {
            try
            {
                File.Delete(dataPath);

                if (File.Exists(comisariaArchivoPath))
                {
                    File.Delete(comisariaArchivoPath);
                }

                if (File.Exists(versionArchivoComisaria))
                {
                    File.Delete(versionArchivoComisaria);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppManager] Error al eliminar archivo: {ex.Message}");
            }

            float timeout = 5f;
            float startTime = Time.realtimeSinceStartup;
            while (File.Exists(dataPath) && Time.realtimeSinceStartup - startTime < timeout)
            {
                yield return null;
            }

            if (File.Exists(dataPath))
            {
                Debug.LogError("[AppManager] No se pudo eliminar el archivo antes del timeout.");
                yield break;
            }
        }

        Application.Quit();     
    }

    public void InicializarApp()
    {
        if (CargarDatosDisco() != null)
        {
            LogginController.singleton.IntentarIniciarSesionAuth(UserData.email, UserData.contrasena, (datos, mensaje) =>
            {
                Debug.Log(mensaje);

                if (datos == null) return;

                _ = FirebaseStorageManager.singleton.SaveUsuario(UserData, FirebaseStorageManager.singleton.CurrentUser.UserId, true, null, false);
            }, false);
            
            PestañasManager.singleton.EjecutarAnimacionEntrada(4);
        }
        else
        {
            PestañasManager.singleton.EjecutarAnimacionEntrada(0);
        }
    }

    /// <summary>
    /// Guarda UserData en el disco como un archivo JSON.
    /// </summary>
    public void GuardarDatosDisco()
    {
        try
        {
            string json = JsonConvert.SerializeObject(UserData);
            File.WriteAllText(dataPath, json);
            Debug.Log("[IntroController] Se guardó en el disco correctamente.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[IntroController] Error al guardar datos: {ex.Message}");
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

    public void AbrirDireccion(string direccion)
    {
        if (string.IsNullOrWhiteSpace(direccion))
        {
            Debug.LogWarning("La dirección está vacía.");
            return;
        }

        string direccionCodificada = Uri.EscapeDataString(direccion);
        string url = $"https://www.google.com/maps/search/?api=1&query={direccionCodificada}";

        Application.OpenURL(url);
    }

    /// <summary>
    /// Abre el cliente de correo con un mensaje nuevo vacío.
    /// </summary>
    public void RedactarCorreo(string destinatario)
    {
        string url = $"mailto:{destinatario}";
        Application.OpenURL(url);
    }

    /// <summary>
    /// devuelve true si hay que actualizar
    /// </summary>
    private void VerificarVersionTabla()
    {
        versionActualInfoMunicipios = File.Exists(versionArchivoComisaria) ? int.Parse(File.ReadAllText(versionArchivoComisaria)) : -1;

        FirebaseStorageManager.singleton.LoadData("numeroVersion", (bool esError, string errorMessage, int versionEnBaseDatos) => {

            if (esError)
            {
                Debug.LogError($"[AppManager] Error al verificar la versión de la tabla: {errorMessage}");
                return;
            }

            if (versionActualInfoMunicipios != versionEnBaseDatos)
            {
                FirebaseStorageManager.singleton.LoadData("datosMunicipios", (bool esError, string errorMessage, string infoMunicipiosBaseDatos) => {

                    if (esError)
                    {
                        Debug.LogError($"[AppManager] Error al buscar datos de la tabla: {errorMessage}");
                        return;
                    }

                    informacionMunicipios = JsonConvert.DeserializeObject<InfoTablaMunicipios[]>(infoMunicipiosBaseDatos);
                    versionActualInfoMunicipios = versionEnBaseDatos;

                    string json = JsonConvert.SerializeObject(informacionMunicipios);
                    File.WriteAllText(comisariaArchivoPath, json);

                    File.WriteAllText(versionArchivoComisaria, versionActualInfoMunicipios.ToString());
                }, false, true);
            }
            else
            {
                Debug.Log("El numero de versión es igual al del servidor. iniciando con datos de tabla locales");
            }
        }, false);
    }

    public Data CargarDatosDisco()
    {
        if (File.Exists(comisariaArchivoPath))
        {
            string jsonDesdeArchivo = File.ReadAllText(comisariaArchivoPath);

            informacionMunicipios = JsonConvert.DeserializeObject<InfoTablaMunicipios[]>(jsonDesdeArchivo);  

            // El archivo existe pero está corrupto/vacío/no interpretable -> se crea una tabla nueva.
            if (informacionMunicipios == null || informacionMunicipios.Length == 0)
            {
                Debug.LogWarning("El archivo existe pero no contiene datos válidos, se creará una tabla nueva.");
                CrearTablaNueva();
            }
        }

        VerificarVersionTabla();

        if (File.Exists(dataPath))
        {
            Debug.Log("[IntroController] Archivo de usuario encontrado, leyendo datos...");
            string json;
            try
            {
                json = File.ReadAllText(dataPath);
                Data data = JsonConvert.DeserializeObject<Data>(json);

                if (data != null)
                {
                    UserData = data;
                    Debug.Log("Datos del disco cargados correctamente");
                    return data;
                }
                else
                {
                    Debug.LogError($"[IntroController] Error al leer el archivo: conversion incorrecta");
                    return null;
                }  
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IntroController] Error al leer el archivo: {ex.Message}");
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Crea 40 registros vacíos (no nulos) e inicializa cada campo de texto en ""
    /// en vez de null, para que las celdas vacías se muestren y se guarden como tales
    /// en lugar de perderse o romper la carga del JSON.
    /// </summary>
    private void CrearTablaNueva()
    {
        InfoTablaMunicipios[] nuevaData = new InfoTablaMunicipios[40];
        for (int i = 0; i < nuevaData.Length; i++)
        {
            nuevaData[i] = CrearRegistroVacio();
            nuevaData[i].No = (i + 1).ToString();
        }

        informacionMunicipios = nuevaData;

        // NullValueHandling.Include asegura que cada propiedad aparezca en el JSON
        // (aunque sea ""), en vez de que Newtonsoft omita campos vacíos/nulos.
        string json = JsonConvert.SerializeObject(nuevaData, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include
        });

        GuardarTablaLocal();
    }

    /// <summary>
    /// Crea una instancia de InfoTablaMunicipios con todos sus campos de texto
    /// inicializados en string.Empty en vez de null, usando reflexión para no
    /// depender de conocer cada nombre de campo de la clase.
    /// </summary>
    private static InfoTablaMunicipios CrearRegistroVacio()
    {
        InfoTablaMunicipios instancia = new InfoTablaMunicipios();

        var campos = typeof(InfoTablaMunicipios).GetFields(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        foreach (var campo in campos)
        {
            if (campo.FieldType == typeof(string))
            {
                campo.SetValue(instancia, string.Empty);
            }
        }

        return instancia;
    }

    public string GuardarTablaLocal()
    {
        string json = EditorTablaController.singleton.ObtenerTablaActualJSON();
        File.WriteAllText(comisariaArchivoPath, json);

        string versionString = versionActualInfoMunicipios.ToString();
        File.WriteAllText(versionArchivoComisaria, versionString);

        return json;
    }
}
