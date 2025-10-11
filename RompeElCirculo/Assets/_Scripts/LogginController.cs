using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtilidadesLaEME;

public class LogginController : MonoBehaviour
{
    public InputFieldUtilities gmail, contraseña;
    public ButtonExtrasController btn_IntentarIniciarSesion;
    public TextMeshProUGUI mensajeError;
    public Image ruedaCarga;

    public void IniciarSesion()
    {
        ruedaCarga.gameObject.ActivarObjeto();
        ruedaCarga.transform.DOKill();
        ruedaCarga.transform.DORotate(new Vector3(0, 0, 360), 1, RotateMode.FastBeyond360)
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Restart);
        mensajeError.text = string.Empty;
        string emailIngresado = gmail.inputField.text;
        string contrasenaIngresada = contraseña.inputField.text;

        btn_IntentarIniciarSesion.button.interactable = false;

        FirebaseStorageManager.singleton.GetAllUsers((usuarios, error) =>
        {
            btn_IntentarIniciarSesion.button.interactable = true;
            if (!string.IsNullOrEmpty(error))
            {
                TirarMensaje(error, Color.red);
                ruedaCarga.gameObject.DesactivarObjeto();
                return;
            }
            if (usuarios == null || usuarios.Count == 0)
            {
                TirarMensaje("No se encontraron usuarios registrados", Color.red);
                ruedaCarga.gameObject.DesactivarObjeto();
                return;
            }
            var usuario = usuarios.Find(u => u.email == emailIngresado);
            if (usuario == null)
            {
                TirarMensaje("Usuario no encontrado", Color.red);
                ruedaCarga.gameObject.DesactivarObjeto();
                return;
            }
            if (usuario.contrasena != contrasenaIngresada)
            {
                TirarMensaje("Contraseña incorrecta", Color.red);
                ruedaCarga.gameObject.DesactivarObjeto();
                return;
            }
            // Usuario y contraseña correctos
            TirarMensaje("Inicio de sesión exitoso", Color.green);
            ruedaCarga.gameObject.DesactivarObjeto();
            AppManager.userData = usuario;
            AppManager.singleton.GuardarDatosDisco();

            PestañasManager.singleton._FRENTE.raycastTarget = true;
            PestañasManager.singleton._FRENTE.DOKill();
            PestañasManager.singleton._FRENTE.DOFade(1, 0.3f).SetDelay(0.5f).OnComplete(() => {
                PestañasManager.singleton.CambiarPestañaSinTransicion(0);
                PestañasManager.singleton.AnimacionInicio();
            });            
        });
    }

    private void TirarMensaje(string texto, Color color)
    {
        mensajeError.color = color;
        mensajeError.text = texto;
        mensajeError.transform.DOKill();
        mensajeError.transform.localScale = new Vector3(1.1f, 1.1f, 1);
        mensajeError.transform.DOScale(1, 0.2f);
    }

    public void AlCambiarValoresInputs()
    {
        if (string.IsNullOrEmpty(gmail.inputField.text) || string.IsNullOrEmpty(contraseña.inputField.text))
        {
            btn_IntentarIniciarSesion.button.interactable = false;
        }
        else
        {
            btn_IntentarIniciarSesion.button.interactable = true;
        }
    }
}
