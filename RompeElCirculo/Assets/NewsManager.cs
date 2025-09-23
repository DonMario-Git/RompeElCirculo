using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UtilidadesLaEME;

public class NewsManager : MonoBehaviour
{
    public static NewsManager singleton;

    public RawImage targetImage;
    public string imageUrl;

    public Transform trCargando;
    public GameObject objAdvertencia;
    public GameObject objX;

    private void Awake()
    {
        singleton = this;
    }

    public void CargarNoticias()
    {
        StartCoroutine(LoadImageFromURL(imageUrl));
    }

    IEnumerator LoadImageFromURL(string url)
    {
        trCargando.gameObject.Enable();
        trCargando.DOKill();
        trCargando.DORotate(new Vector3(0, 0, 360), 2f, RotateMode.FastBeyond360)
                 .SetLoops(-1, LoopType.Restart) // -1 = infinito
                 .SetEase(Ease.Linear);

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogWarning("Error al cargar imagen: " + request.error);
            objAdvertencia.Enable();
            objX.Enable();
        }
        else
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            ApplyTextureToRawImage(texture);
            trCargando.gameObject.Disable();
            objX.Enable();
            AppManager.userData.vioNoticia = true;
            IntroController.singleton.GuardarDatosDispositivo();
        }
    }

    void ApplyTextureToRawImage(Texture2D texture)
    {
        if (texture != null)
        {
            targetImage.texture = texture; 
        }
        else
        {
            Debug.LogWarning("Error al cargar imagen: " + "No se pudo convertir la imagen correctamente");
            objAdvertencia.Enable();
            objX.Enable();
        }
    }
}
