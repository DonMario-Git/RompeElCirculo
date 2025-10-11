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
    public float periodoBuscarNotificaciones = 60;

    [Title("Contenido")]

    public GameObject objetoMenuNotificaciones;
    public TextMeshProUGUI textoNumeroNotificaciones;
    public Image ruedaCarga;
    public GameObject plantillaNotificacion;
    public Transform objetoPadre;

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

    public void IniciarSistemaNotificaciones()
    {
        if (singleton != this) return;

        StartCoroutine(Corrutina());

        IEnumerator Corrutina()
        {
            yield return new WaitForSeconds(1);

            while (true)
            {
                VerificarCantidadNotificacionesNuevas();

                yield return new WaitForSeconds(periodoBuscarNotificaciones);
            }
        } 
    }

    [ContextMenu(nameof(EnviarNotificacionPrueba))]
    public void EnviarNotificacionPrueba()
    {
        if (string.IsNullOrEmpty(AppManager.userData.nombreCompleto))
        {
            Debug.LogWarning("userId vacío. No se puede enviar la notificación.");
            return;
        }

        Notificacion noti = new Notificacion
        {
            titulo = "Notificación de Prueba",
            mensaje = "Este es un mensaje de prueba.",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            leido = false
        };

        FirebaseStorageManager.singleton.AddNotification(AppManager.userData.nombreCompleto, noti, (error) =>
        {
            if (!string.IsNullOrEmpty(error))
                Debug.LogError("Error al enviar notificación de prueba: " + error);
            else
                Debug.Log("Notificación de prueba enviada correctamente.");
        });
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
