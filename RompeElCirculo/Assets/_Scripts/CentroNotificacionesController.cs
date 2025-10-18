using AwesomeAttributes;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtilidadesLaEME;

public class CentroNotificacionesController : MonoBehaviour
{
    public static CentroNotificacionesController singleton;

    [Title("Contenido")]

    public GameObject objetoMenuNotificaciones;
    public TextMeshProUGUI textoNumeroNotificaciones;
    public Image ruedaCarga;
    public GameObject plantillaNotificacion;
    public Transform objetoPadre;
    private bool inicializado;

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else
        {
            Destroy(gameObject);
        }

        AppManager.OnDataLoad += IniciarSistemaNotificaciones;
    }

    private void OnEnable()
    {
        if (inicializado)
        {
            VerificarCantidadNotificacionesNuevas();
        }
    }

    public void IniciarSistemaNotificaciones()
    {
        if (singleton != this) return;

        StartCoroutine(Corrutina());

        IEnumerator Corrutina()
        {
            yield return new WaitForSeconds(1);
            VerificarCantidadNotificacionesNuevas();
            inicializado = true;
        } 
    }

    public void EnviarNotificacion(string para, string titulo, string mensaje)
    {
        if (string.IsNullOrEmpty(para))
        {
            Debug.LogWarning("userId vacío. No se puede enviar la notificación.");
            return;
        }

        Notificacion noti = new()
        {
            titulo = titulo,
            mensaje = mensaje,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            leido = false
        };

        FirebaseStorageManager.singleton.AddNotification(para, noti, (error) =>
        {
            if (!string.IsNullOrEmpty(error))
                Debug.LogError("Error al enviar notificación de prueba: " + error);
            else
                Debug.Log("Notificación de prueba enviada correctamente.");
        });
    }

    public void EnviarNotificacion(string[] para, string titulo, string mensaje)
    {
        if (para == null || para.Length == 0)
        {
            Debug.LogWarning("Array de userId vacío. No se puede enviar la notificación.");
            return;
        }
        foreach (var usuario in para)
        {
            if (string.IsNullOrEmpty(usuario))
            {
                Debug.LogWarning($"userId vacío en el elemento. No se puede enviar la notificación.");
                return;
            }

            EnviarNotificacion(usuario, titulo, mensaje);
        }
    }

    [ContextMenu(nameof(VerificarCantidadNotificacionesNuevas))]
    private void VerificarCantidadNotificacionesNuevas()
    {
        FirebaseStorageManager.singleton.GetNewNotificationCount(AppManager.userData.nombreCompleto, (cantidad, error) =>
        {
            if (cantidad != 0)
            {
                textoNumeroNotificaciones.transform.parent.gameObject.ActivarObjeto();
                textoNumeroNotificaciones.text = cantidad.ToString();
            }
            else
            {
                textoNumeroNotificaciones.transform.parent.gameObject.DesactivarObjeto();
            }
        });
    }

    private void OnNotificationsReceived(List<Notificacion> notificaciones, string error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"Error al obtener notificaciones: {error}");
            return;
        }
        Debug.Log($"Notificaciones recibidas: {notificaciones.Count}");
        if (notificaciones.Count > 0)
        {
            // Procesar y mostrar notificaciones aquí
            // Marcar como leídas
            var ids = notificaciones.Select(n => n.id).ToList();
            FirebaseStorageManager.singleton.MarkNotificationsAsRead(AppManager.userData.nombreCompleto, ids, (err) =>
            {
                if (!string.IsNullOrEmpty(err))
                    Debug.LogError(err);
                else
                    Debug.Log("Notificaciones marcadas como leídas.");
                // Limpiar notificaciones leídas si hay más de 30
                FirebaseStorageManager.singleton.CleanupReadNotifications(AppManager.userData.nombreCompleto, (cleanupErr) =>
                {
                    if (!string.IsNullOrEmpty(cleanupErr))
                        Debug.LogError(cleanupErr);
                    else
                        Debug.Log("Cleanup de notificaciones leídas realizado.");
                });
            });
        }
    }

    public List<NotificacionElementoController> notificacionesAbiertas = new();

    public void AbrirMenuNotificaciones()
    {
        objetoMenuNotificaciones.ActivarObjeto();
        ruedaCarga.gameObject.ActivarObjeto();
        ruedaCarga.transform.DOKill();
        ruedaCarga.transform.DORotate(new Vector3(0, 0, 360), 1, RotateMode.FastBeyond360)
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Restart);

        FirebaseStorageManager.singleton.GetNotifications(AppManager.userData.nombreCompleto, (notificaciones, error) =>
        {
            ruedaCarga.transform.DOKill();
            ruedaCarga.gameObject.DesactivarObjeto();

            foreach (var item in notificaciones)
            {
                var nuevaNotificacion = Instantiate(plantillaNotificacion, objetoPadre).GetComponent<NotificacionElementoController>();
                nuevaNotificacion.titulo.text = item.titulo;
                nuevaNotificacion.contenido.text = item.mensaje;
                nuevaNotificacion.gameObject.ActivarObjeto();
                nuevaNotificacion.nuevoIcono.SetActive(!item.leido);
            }

            OnNotificationsReceived(notificaciones, error);
        });
    }

    public void CerrarMenuNotificaciones()
    {
        objetoMenuNotificaciones.DesactivarObjeto();
        VerificarCantidadNotificacionesNuevas();

        foreach (var item in notificacionesAbiertas)
        {
            Destroy(item.gameObject);
        }

        notificacionesAbiertas.Clear();
    }
}
