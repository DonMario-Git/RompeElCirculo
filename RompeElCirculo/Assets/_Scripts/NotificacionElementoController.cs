using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotificacionElementoController : MonoBehaviour
{
    public GameObject nuevoIcono;
    public TextMeshProUGUI titulo;
    public TextMeshProUGUI contenido;
    public Image icono;

    private void Awake()
    {
        CentroNotificacionesController.singleton.notificacionesAbiertas.Add(this);
    }
}
