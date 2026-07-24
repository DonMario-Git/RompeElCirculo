using DG.Tweening;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UtilidadesLaEME;

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

    private static string NormalizarTelefono(string numero, bool paraWhatsApp)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return null;

        // Elimina espacios, paréntesis, guiones, etc.
        numero = Regex.Replace(numero, @"\D", "");

        // ========= NÚMEROS DE EMERGENCIA =========
        // 123, 122, 119, 125, etc.
        if (numero.Length <= 3)
            return numero;

        // ========= LÍNEAS 01 8000 =========
        if (numero.StartsWith("018000"))
            return numero;

        // ========= CELULAR COLOMBIANO =========
        if (numero.Length == 10 && numero.StartsWith("3"))
            return paraWhatsApp ? "57" + numero : "+57" + numero;

        // ========= YA VIENE CON +57 =========
        if (numero.Length == 12 && numero.StartsWith("57"))
            return paraWhatsApp ? numero : "+" + numero;

        // ========= FIJOS COLOMBIANOS =========
        // Ej: 601..., 604..., 605..., 606..., 607..., 608...
        if (numero.Length == 10 &&
            (numero.StartsWith("60") || numero.StartsWith("1")))
            return numero;

        // ========= INTERNACIONAL =========
        if (numero.Length > 10)
            return paraWhatsApp ? numero : "+" + numero;

        return numero;
    }

    public void LlamarPorWhatsApp(string numero)
    {
        string telefono = NormalizarTelefono(numero, true);

        if (string.IsNullOrEmpty(telefono))
        {
            Debug.LogError("Número inválido.");
            return;
        }

        // WhatsApp únicamente funciona con números telefónicos normales,
        // no con líneas de emergencia ni 018000.
        if (telefono.Length <= 3 || telefono.StartsWith("018000"))
        {
            Debug.LogWarning("Este número no es compatible con WhatsApp.");
            return;
        }

        Application.OpenURL($"https://wa.me/{telefono}");
    }

    public void LlamarPorTelefono(string numero)
    {
        string telefono = NormalizarTelefono(numero, false);

        if (string.IsNullOrEmpty(telefono))
        {
            Debug.LogError("Número inválido.");
            return;
        }

        Application.OpenURL($"tel:{telefono}");
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
                if (!File.Exists(comisariaArchivoPath) && informacionMunicipios == null)
                {
                    Debug.LogWarning("El archivo no existe o no se pudo descargar, se creará una tabla nueva.");
                    CrearTablaNueva();
                }

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

        GuardarTablaDisco();
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

    public string GuardarTablaDisco()
    {
        string json = JsonConvert.SerializeObject(informacionMunicipios, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include
        });

        File.WriteAllText(comisariaArchivoPath, json);

        string versionString = versionActualInfoMunicipios.ToString();
        File.WriteAllText(versionArchivoComisaria, versionString);

        Debug.Log("Se guardó la tabla localmente", gameObject);

        return json;
    }

    public string GuardarTablaLocalEscrita()
    {
        string json = EditorTablaController.singleton.ObtenerTablaActualJSON();
        File.WriteAllText(comisariaArchivoPath, json);

        string versionString = versionActualInfoMunicipios.ToString();
        File.WriteAllText(versionArchivoComisaria, versionString);

        Debug.Log("Se guardó la tabla localmente de la tabla escrita", gameObject);

        return json;
    }
}
