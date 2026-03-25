
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ViolentometroController : MonoBehaviour
{
    public PreguntaSeleccionMultipleController pregunta1, pregunta2, pregunta3;
    public ButtonExtrasController botonEnviar;

    public GameObject[] respuestas;

    public Image imagenNegraFondo;
    public Transform ruedaCarga;

    public void AlSeleccionar()
    {
        botonEnviar.button.interactable = pregunta1.cuadrosMultiplesSeleccionados.Count > 0 || pregunta2.cuadrosMultiplesSeleccionados.Count > 0 || pregunta3.cuadrosMultiplesSeleccionados.Count > 0;
    }

    public void AlEnviar()
    {
        imagenNegraFondo.gameObject.SetActive(true);
        ruedaCarga.DOKill();
        ruedaCarga.localEulerAngles = Vector3.zero;
        ruedaCarga.DORotate(new Vector3(0, 0, -360), 1f, RotateMode.FastBeyond360).SetLoops(-1).SetEase(Ease.Linear);

        AppManager.UserData.respuestasViolentometro ??= new bool[15];

        List<bool> respuestasLista = new();

        for (int i = 0; i < pregunta1.items.Count; i++)
        {
            respuestasLista.Add(pregunta1.cuadrosMultiplesSeleccionados.Contains(pregunta1.items[i]));
        }

        for (int i = 0; i < pregunta2.items.Count; i++)
        {
            respuestasLista.Add(pregunta2.cuadrosMultiplesSeleccionados.Contains(pregunta2.items[i]));
        }

        for (int i = 0; i < pregunta3.items.Count; i++)
        {
            respuestasLista.Add(pregunta3.cuadrosMultiplesSeleccionados.Contains(pregunta3.items[i]));
        }

        AppManager.UserData.respuestasViolentometro = respuestasLista.ToArray();

        FirebaseStorageManager.singleton.SaveData(AppManager.UserData, AppManager.UserData.nombreCompleto, true, (error) => {
            if (!string.IsNullOrEmpty(error)) Debug.LogWarning(error);
            ruedaCarga.DOKill();
            MostrarRespuesta();
        }, false);
    }

    private void MostrarRespuesta()
    {
        if (pregunta3.cuadrosMultiplesSeleccionados.Count != 0)
        {
            respuestas[2].SetActive(true);
        }
        else if (pregunta2.cuadrosMultiplesSeleccionados.Count != 0)
        {
            respuestas[1].SetActive(true);
        }
        else if (pregunta1.cuadrosMultiplesSeleccionados.Count != 0)
        {
            respuestas[0].SetActive(true);
        }

        respuestas[0].transform.parent.gameObject.SetActive(true);
        respuestas[0].transform.parent.DOKill();
        respuestas[0].transform.parent.localScale = new Vector3(1.1f, 1.1f, 1);
        respuestas[0].transform.parent.DOScale(1, 0.2f);
    }

    private void OnDisable()
    {
        imagenNegraFondo.gameObject.SetActive(false);
        respuestas[0].transform.parent.gameObject.SetActive(false);
        respuestas[2].SetActive(false);
        respuestas[1].SetActive(false);
        respuestas[0].SetActive(false);
    }
}
