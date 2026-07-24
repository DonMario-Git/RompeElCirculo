using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UtilidadesLaEME;

public class ViolentometroController : MonoBehaviour
{
    public PreguntaSeleccionMultipleController pregunta1, pregunta2, pregunta3;
    public ButtonExtrasController botonEnviar;

    public GameObject[] respuestas;

    public Image imagenNegraFondo;
    public Transform ruedaCarga;

    private Coroutine _saveCoroutine;

    public GameObject objetoError;

    public void AlSeleccionar()
    {
        botonEnviar.button.interactable = pregunta1.cuadrosMultiplesSeleccionados.Count > 0 || pregunta2.cuadrosMultiplesSeleccionados.Count > 0 || pregunta3.cuadrosMultiplesSeleccionados.Count > 0;
    }

    public void AlEnviar()
    {
        imagenNegraFondo.gameObject.SetActive(true);
        ruedaCarga.gameObject.ActivarObjeto();
        ruedaCarga.DOKill();
        ruedaCarga.localEulerAngles = Vector3.zero;
        ruedaCarga.DORotate(new Vector3(0, 0, 360), 1f, RotateMode.FastBeyond360).SetLoops(-1).SetEase(Ease.Linear);

        AppManager.UserData.respuestasViolentometro ??= new bool[15];

        List<bool> respuestasLista = new();

        for (int i = 0; i < pregunta1.items.Count; i++)
            respuestasLista.Add(pregunta1.cuadrosMultiplesSeleccionados.Contains(pregunta1.items[i]));

        for (int i = 0; i < pregunta2.items.Count; i++)
            respuestasLista.Add(pregunta2.cuadrosMultiplesSeleccionados.Contains(pregunta2.items[i]));

        for (int i = 0; i < pregunta3.items.Count; i++)
            respuestasLista.Add(pregunta3.cuadrosMultiplesSeleccionados.Contains(pregunta3.items[i]));

        AppManager.UserData.respuestasViolentometro = respuestasLista.ToArray();

        if (_saveCoroutine != null) StopCoroutine(_saveCoroutine);
        _saveCoroutine = StartCoroutine(SaveConTimeout());
    }

    private IEnumerator SaveConTimeout()
    {
        bool completado = false;
        string errorGuardado = null;

        _ = FirebaseStorageManager.singleton.SaveUsuario(
            AppManager.UserData,
            FirebaseStorageManager.singleton.UserID,
            true,
            (error) => {
                errorGuardado = error ?? string.Empty;
                completado = true;
            },
            false
        );

        float tiempoTranscurrido = 0f;
        const float tiempoLimite = 3f;

        while (!completado && tiempoTranscurrido < tiempoLimite)
        {
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        ruedaCarga.DOKill();
        ruedaCarga.gameObject.DesactivarObjeto();

        if (!completado)
        {
            Debug.LogWarning("SaveData superó el límite de 5 segundos sin responder.");
            objetoError.SetActive(true);
            objetoError.transform.DOKill();
            objetoError.transform.localScale = new Vector3(1.1f, 1.1f, 1);
            objetoError.transform.DOScale(1, 0.2f);
            yield break;
        }

        if (!string.IsNullOrEmpty(errorGuardado))
        {
            Debug.LogWarning(errorGuardado);
            objetoError.SetActive(true);
            objetoError.transform.DOKill();
            objetoError.transform.localScale = new Vector3(1.1f, 1.1f, 1);
            objetoError.transform.DOScale(1, 0.2f);
            yield break;
        }

        MostrarRespuesta();
    }

    private void MostrarRespuesta()
    {
        if (pregunta3.cuadrosMultiplesSeleccionados.Count != 0)
            respuestas[2].SetActive(true);
        else if (pregunta2.cuadrosMultiplesSeleccionados.Count != 0)
            respuestas[1].SetActive(true);
        else if (pregunta1.cuadrosMultiplesSeleccionados.Count != 0)
            respuestas[0].SetActive(true);

        respuestas[0].transform.parent.gameObject.SetActive(true);
        respuestas[0].transform.parent.DOKill();
        respuestas[0].transform.parent.localScale = new Vector3(1.1f, 1.1f, 1);
        respuestas[0].transform.parent.DOScale(1, 0.2f);
    }

    private void OnDisable()
    {
        if (_saveCoroutine != null)
        {
            StopCoroutine(_saveCoroutine);
            _saveCoroutine = null;
        }

        imagenNegraFondo.gameObject.SetActive(false);
        respuestas[0].transform.parent.gameObject.SetActive(false);
        respuestas[2].SetActive(false);
        respuestas[1].SetActive(false);
        respuestas[0].SetActive(false);
        objetoError.SetActive(false);
    }
}