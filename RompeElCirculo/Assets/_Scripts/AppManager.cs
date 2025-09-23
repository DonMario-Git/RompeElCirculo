using DG.Tweening;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UtilidadesLaEME;
using RangeAttribute = UnityEngine.RangeAttribute;

public class AppManager : MonoBehaviour
{
    public static AppManager singleton;
    public Image front_im;
    public GameObject[] listaPestañas;
    private List<MinigameController> listaMinijuegosSeleccionada = new();

    public GameObject[] listaMinijuegosSeleccionada1, listaMinijuegosSeleccionada2, listaMinijuegosSeleccionada3;

    public Transform trSpawnMinijuegos;


    public Image portadaIm;
    public int indexMinigame;

    public IDIOMA idiomaActual;

    [Range(0, 10)] public float timeScale = 1;

    [Header("Datos")]

    public static Data data;
    public static UserData userData;
    public static int diasFaltantes;

    public TMP_FontAsset font;

    public static ScreenOrientation ScreenOrientacion {get; private set;} = ScreenOrientation.Portrait;


    [ContextMenu(nameof(CambiarTodasFuentes))]
    public void CambiarTodasFuentes()
    {
        var textos = FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var item in textos)
        {
            item.characterLimit = 99;
        }
    }

    private void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    private void Update()
    {
        Time.timeScale = timeScale;     
        Screen.orientation = ScreenOrientacion;
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // La app fue minimizada  forzar cierre
            Application.Quit();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            // También sirve si pierde el foco
            Application.Quit();
        }
    }

    void OnApplicationQuit()
    {
        // Cuando el juego se cierre, vuelve al comportamiento normal
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
    }

    public void RotarPantalla(ScreenOrientation screen)
    {
        if (ScreenOrientacion != screen)
        {
            ScreenOrientacion = screen;
        }    
    }

    public void AbrirURL(string URL)
    {
        Application.OpenURL(URL);
    }

    [ContextMenu(nameof(Inicializar))]
    public void Inicializar()
    {
        singleton = this;

        TextMeshProUGUI[] textos = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var item in textos)
        {
            if (!item.TryGetComponent(out LenguajeTMP a))
            {
                LenguajeTMP newLenguaje = item.gameObject.AddComponent<LenguajeTMP>();

                newLenguaje.tmpro = item;
                newLenguaje.textoIdioma.ES = item.text;     
            }
            else
            {
                if (string.IsNullOrEmpty(a.textoIdioma.ES)) a.textoIdioma.ES = item.text;
            }
        }
    }

    [ContextMenu(nameof(Pruba))]
    public void Pruba()
    {
        TMP_InputField[] inputs = FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var item in inputs)
        {
            var a = item.placeholder as TextMeshProUGUI;
            var B = a.GetComponent<LenguajeTMP>();

            B.textoIdioma.ES = "Escribir...";
            B.textoIdioma.EN = "Write...";
            B.tmpro.text = "Escribir...";
        }
    }

    public void CambiarIdiomaDropDown(int index)
    {
        switch (index)
        {
            case 0:
                idiomaActual = IDIOMA.ESPAÑOL;
                break;
            case 1:
                idiomaActual = IDIOMA.ENGLISH;
                break;
        }

        ActualizarIidioma();
    }

    [ContextMenu(nameof(ActualizarIidioma))]
    public void ActualizarIidioma()
    {
        singleton = this;

        LenguajeTMP[] textos = FindObjectsByType<LenguajeTMP>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var item in textos)
        {
             item.tmpro.text = item.textoIdioma.GetValue();
        }
    }

    public void StartExercises()
    {
        foreach (var item in listaMinijuegosSeleccionada)
        {
            Destroy(item.gameObject);
        }

        listaMinijuegosSeleccionada.Clear();
        indexMinigame = 0;


        List<GameObject> prefabsSeleccionados = new();

        GameObject[] arraySeleccionado = data.numeroSiguienteNivel switch
        {
            0 => listaMinijuegosSeleccionada1,
            1 => listaMinijuegosSeleccionada2,
            2 => listaMinijuegosSeleccionada3,
            _ => null,
        };

        arraySeleccionado.Shuffle();

        for (int i = 0; i < 5; i++)
        {
            prefabsSeleccionados.Add(arraySeleccionado[i]);
        }

        foreach (var item in prefabsSeleccionados)
        {
            listaMinijuegosSeleccionada.Add(Instantiate(item, trSpawnMinijuegos.position, Quaternion.identity, trSpawnMinijuegos).GetComponent<MinigameController>());
        }

        ChangeMinigame(0);
    }

    public void ChangeTab(int index)
    {
        front_im.rectTransform.localPosition = new Vector3(1200f, 0f, 0);
        front_im.raycastTarget = true;
        front_im.color = Color.black;
        front_im.rectTransform.DOMoveX(0, 0.3f).SetEase(Ease.OutCubic).OnComplete(() => {

            foreach (var item in listaPestañas)
            {
                if (item != null) item.SetActive(false);
            }

            if (index <= listaPestañas.Length - 1) listaPestañas[index].SetActive(true);

            front_im.DOColor(Color.clear, 0.1f).OnComplete(() => front_im.raycastTarget = false);
        });

        ActualizarIidioma();
    }

    public void NextVideogame()
    {
        indexMinigame++;

        ChangeMinigame(indexMinigame);

        ActualizarIidioma();
    }

    public void ResetMinigame()
    {
        ChangeMinigame(indexMinigame);

        ActualizarIidioma();
    }

    public void ChangeMinigame(int index)
    {
        if (indexMinigame == 0)
        {
            foreach (var item in listaPestañas)
            {
                if (item != null) item.SetActive(false);
            }
        }

        if (index < listaMinijuegosSeleccionada.Count)
        {
            front_im.rectTransform.localPosition = new Vector3(0, 0, 0);
            front_im.DOColor(Color.black, 0.3f).OnComplete(() => {

                foreach (var item in listaMinijuegosSeleccionada)
                {
                    item.gameObject.SetActive(false);
                }

                listaMinijuegosSeleccionada[index].gameObject.SetActive(true);
                portadaIm.transform.localScale = Vector3.one;
                portadaIm.raycastTarget = true;
                portadaIm.sprite = listaMinijuegosSeleccionada[index].spritePortada;
                portadaIm.color = Color.white;

                front_im.DOColor(Color.clear, 0.3f).OnComplete(() => front_im.raycastTarget = false).OnComplete(() => {
                    OpenHelpWindow(false); 
                    portadaIm.DOFade(0, 0.4f).SetDelay(2.7f);
                    portadaIm.transform.DOScale(2, 0.4f).SetDelay(2.5f).SetEase(Ease.InBack).OnComplete(() => portadaIm.raycastTarget = false);    
                });
            });
        }
        else
        {
            data.numeroSiguienteNivel++;
            VolverAMenuActividades();

            FirebaseStorageManager.singleton.SaveData(data, data.Nombres, true, (resultado) => {
                if (!string.IsNullOrEmpty(resultado)) Debug.LogWarning(resultado);
            }, false);
        }

        ActualizarIidioma();
    }

    public static string GetDeviceID()
    {
        return SystemInfo.deviceUniqueIdentifier; // ID único del dispositivo
    }

    public void CerrarSesion()
    {
        FirebaseStorageManager.singleton.SaveData(data, data.Nombres, false, (resultado) => {
            if (File.Exists(IntroController.filePath))
            {
                File.Delete(IntroController.filePath);
                Application.Quit();
            }
        });
    }

    public void VolverAMenuActividades()
    {
        if (HelpWindowController.singleton.isOpened) HelpWindowController.singleton.CloseHelpMenu(false);

        foreach (var item in listaMinijuegosSeleccionada)
        {
            item.gameObject.SetActive(false);
        }

        ChangeTab(1);
    }

    public void OpenHelpWindow(bool playSound)
    {
        HelpWindowController.singleton.OpenHelpMenu(playSound, listaMinijuegosSeleccionada[indexMinigame]);
    }

    public void VoicePlayer()
    {
        AudioPlayer.singleton.PlayVoiceClip(listaMinijuegosSeleccionada[indexMinigame].voiceHelpTexts);
    }

    private void Awake()
    {
        singleton = this;
        Application.targetFrameRate = 60;     
        Input.multiTouchEnabled = false;
    }

    public static Vector2 GetMousePos(Vector2 currentPosition)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0); // Obtiene el primer toque

            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                return Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 0));
            }
            else
            {
                return currentPosition;
            }
        }
        else
        {
            return currentPosition;
        }
    }
}