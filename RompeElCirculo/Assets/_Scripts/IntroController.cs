using DG.Tweening;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UtilidadesLaEME;

public class IntroController : MonoBehaviour, IPointerClickHandler
{
    public Image icono, frente, fondo;

    public static string filePath; // Ruta del archivo de activación

    public TextMeshProUGUI TMP_textoInicio;

    public bool isStartedIntro;
    public bool isStarted;

    public static IntroController singleton;

    private void Awake()
    {
        singleton = this;
        filePath = Application.persistentDataPath + "/userData.json";
        UnityEngine.Debug.Log($"[IntroController] Awake. filePath: {filePath}");
    }

    private void Start()
    {
        if (AutoUpdaterController.singleton == null)
        {
            UnityEngine.Debug.LogError("[IntroController] AutoUpdaterController.singleton es null en Start.");
            return;
        }
        UnityEngine.Debug.Log("[IntroController] Start. Buscando actualizaciones...");

        AutoUpdaterController.singleton.BuscarActualizaciones();
    }

    public void InicializarApp()
    {
        if (AutoUpdaterController.singleton == null)
        {
            UnityEngine.Debug.LogError("[IntroController] AutoUpdaterController.singleton es null en InicializarApp.");
            return;
        }

        AutoUpdaterController.singleton.imageFrente.DOKill();
        AutoUpdaterController.singleton.imageFrente.DOFade(1, 0.3f).SetDelay(1).OnComplete(() =>
        {
            AutoUpdaterController.singleton.imageFrente.gameObject.SetActive(false);
            AutoUpdaterController.singleton.interfazActualizacion.SetActive(false);

            if (File.Exists(filePath))
            {
                UnityEngine.Debug.Log("[IntroController] Archivo de usuario encontrado, leyendo datos...");
                string json = string.Empty;
                try
                {
                    json = File.ReadAllText(filePath);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[IntroController] Error al leer el archivo: {ex.Message}");
                    LogginMenuController.singleton.InicializarLoggin();
                    return;
                }

                try
                {
                    AppManager.userData = JsonConvert.DeserializeObject<UserData>(json);
                    if (AppManager.userData == null)
                    {
                        UnityEngine.Debug.LogError("[IntroController] userData.json deserializado como null.");
                        LogginMenuController.singleton.InicializarLoggin();
                        return;
                    }
                    AppManager.userData.ultimoDispositivo = AppManager.GetDeviceID();

                    if (FirebaseStorageManager.singleton == null)
                    {
                        UnityEngine.Debug.LogError("[IntroController] FirebaseStorageManager.singleton es null.");
                        LogginMenuController.singleton.InicializarLoggin();
                        return;
                    }

                    FirebaseStorageManager.singleton.LoadData(AppManager.userData.Nombres, (datos, error) =>
                    {
                        AppManager.data = datos;

                        if (AppManager.data == null)
                        {
                            UnityEngine.Debug.LogError("[IntroController] AppManager.data es null tras LoadData.");
                            LogginMenuController.singleton.InicializarLoggin();
                            return;
                        }

                        if (AppManager.userData.ultimoDispositivo != AppManager.data.ultimoDispositivo && !AppManager.data.esAdmin)
                        {
                            UnityEngine.Debug.LogWarning("[IntroController] Dispositivo diferente detectado. Eliminando archivo y reiniciando login.");
                            File.Delete(filePath);
                            LogginMenuController.singleton.InicializarLoggin();
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(error))
                            {
                                UnityEngine.Debug.LogWarning($"[IntroController] Error al cargar datos remotos: {error}");
                                AutoUpdaterController.singleton.SetProgress(1, error);
                                LogginMenuController.singleton.InicializarLoggin();
                            }
                            else
                            {
                                if (AppManager.data.hizoFormulario)
                                {
                                    UnityEngine.Debug.Log("[IntroController] El usuario ya completó el formulario. Iniciando animación de introducción.");
                                    IntroAnimation();
                                }
                                else
                                {
                                    UnityEngine.Debug.Log("[IntroController] Mostrando formulario de login.");
                                    LogginMenuController.singleton.letras.gameObject.SetActive(true);
                                    StartCoroutine(Secuencia());
                                }
                            }
                        }
                    });

                    IEnumerator Secuencia()
                    {
                        yield return new WaitForSeconds(1.5f);
                        LogginMenuController.singleton.letras.DesactivarTexto();
                        yield return new WaitForSeconds(0.5f);
                        LogginMenuController.singleton.formulario.SetActive(true);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[IntroController] Error al deserializar userData.json: {ex.Message}");
                    LogginMenuController.singleton.InicializarLoggin();
                }
            }
            else
            {
                UnityEngine.Debug.Log("[IntroController] No existe archivo de usuario. Inicializando login.");
                LogginMenuController.singleton.InicializarLoggin();
            }
        });
    }

    public void GuardarDatosDispositivo()
    {
        try
        {
            string json = JsonConvert.SerializeObject(AppManager.userData);
            File.WriteAllText(filePath, json);
            UnityEngine.Debug.Log("[IntroController] Se guardó en el disco correctamente.");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[IntroController] Error al guardar datos: {ex.Message}");
        }
    }

    public void IntroAnimation()
    {
        if (SerialCheckerController.singleton == null)
        {
            UnityEngine.Debug.LogError("[IntroController] SerialCheckerController.singleton es null en IntroAnimation.");
            return;
        }

        if (AppManager.data == null)
        {
            UnityEngine.Debug.LogError("[IntroController] AppManager.data es null en IntroAnimation.");
            return;
        }

        SerialCheckerController.singleton.CargarYCompararLicencia(AppManager.data.numeroDocumento, (vencida, licencia, diasFaltantes, error2) =>
        {
            if (string.IsNullOrEmpty(error2))
                AppManager.data.activo = !vencida;
            else
                AppManager.data.activo = false;

            AppManager.diasFaltantes = diasFaltantes;
            if (!string.IsNullOrEmpty(error2))
                UnityEngine.Debug.LogWarning($"[IntroController] Error de licencia: {error2}");
        });

        if (TMP_textoInicio == null)
        {
            UnityEngine.Debug.LogError("[IntroController] TMP_textoInicio es null en IntroAnimation.");
            return;
        }

        TMP_textoInicio.DOColor(Color.white, 0.3f).OnComplete(() =>
        {
            isStartedIntro = true;

            if (AppManager.userData.ultimaVersion != Application.version)
            {
                AppManager.userData.vioNoticia = false;
                AppManager.userData.ultimaVersion = Application.version;
            }
            else
            {
                NewsManager.singleton.targetImage.gameObject.Disable();
            }

            if (!AppManager.userData.vioNoticia) NewsManager.singleton.CargarNoticias();
            TMP_textoInicio.DOColor(Color.clear, 0.3f).SetDelay(3).OnComplete(() =>
            {
                Intro();
            });
        });
    }

    private void Intro()
    {
        if (isStarted)
        {
            UnityEngine.Debug.Log("[IntroController] Intro ya ha sido iniciado, se ignora llamada duplicada.");
            return;
        }

        if (frente == null || icono == null || fondo == null)
        {
            UnityEngine.Debug.LogError("[IntroController] Uno o más componentes de imagen no están asignados en el inspector.");
            return;
        }

        frente.DOColor(Color.clear, 0.3f).OnComplete(() =>
        {
            if (AudioPlayer.singleton == null)
            {
                UnityEngine.Debug.LogError("[IntroController] AudioPlayer.instance es null en Intro.");
            }
            else
            {
                AudioPlayer.singleton.PlayAudio(2);
            }

            icono.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetDelay(0.3f).OnComplete(() =>
            {
                frente.color = new Color(1, 1, 1, 0);
                icono.transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack).SetDelay(0.4f);
                frente.DOColor(Color.white, 0.5f).SetDelay(0.5f).OnComplete(() =>
                {
                    icono.enabled = false;
                    fondo.enabled = false;
                    if (AppManager.singleton == null || AppManager.singleton.listaPestañas == null || AppManager.singleton.listaPestañas.Length == 0)
                    {
                        UnityEngine.Debug.LogError("[IntroController] AppManager.instance o listaPestañas no está correctamente inicializado.");
                    }
                    else
                    {
                        AppManager.singleton.listaPestañas[0].SetActive(true);
                    }
                    frente.DOColor(new Color(1, 1, 1, 0), 0.3f).OnComplete(() =>
                    {
                        frente.raycastTarget = false;
                        var img = GetComponent<Image>();
                        if (img != null)
                            img.raycastTarget = false;
                    });
                });
            });
        });

        isStarted = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isStarted && isStartedIntro)
        {
            if (TMP_textoInicio == null)
            {
                UnityEngine.Debug.LogError("[IntroController] TMP_textoInicio es null en OnPointerClick.");
                return;
            }
            TMP_textoInicio.DOKill();
            TMP_textoInicio.DOColor(Color.clear, 0.3f).OnComplete(() =>
            {
                Intro();
            });
        }
    }
}
