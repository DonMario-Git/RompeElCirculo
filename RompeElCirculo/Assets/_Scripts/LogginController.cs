using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtilidadesLaEME;

public class LogginController : Singleton<LogginController>
{
    public InputFieldUtilities gmail, contraseña;
    public ButtonExtrasController btn_IntentarIniciarSesion, btn_VerificacionCorreo;
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

        btn_IntentarIniciarSesion.button.interactable = false;


        IntentarIniciarSesion(gmail.inputField.text, contraseña.inputField.text, ColocarDatosIniciarApp);
    }

    public void ColocarDatosIniciarApp(Data datos, string mensaje)
    {
        if (datos != null)
        {
            AppManager.UserData = datos;
            AppManager.singleton.GuardarDatosDisco();

            EjecutarAnimacionEntrada();
        }

        ruedaCarga.gameObject.DesactivarObjeto();
        TirarMensaje(mensaje, datos == null ? Color.red : Color.green);
        
        btn_IntentarIniciarSesion.button.interactable = true;
    }

    private void EjecutarAnimacionEntrada()
    {
        PestañasManager.singleton._FRENTE.raycastTarget = true;
        PestañasManager.singleton._FRENTE.DOKill();
        PestañasManager.singleton._FRENTE.DOFade(1, 0.3f).SetDelay(0.5f).OnComplete(() =>
        {
            PestañasManager.singleton.CambiarPestañaAnimacionAvanzar(0);
        });
    }

    private void IntentarIniciarSesion(string emailIngresado, string contrasenaIngresada, Action<Data, string> OnComplete)
    {
        FirebaseStorageManager.singleton.SignInAuthUser(emailIngresado, contrasenaIngresada, (usuarioAuth, mensaje) => {

            if (usuarioAuth == null)
            {
                OnComplete?.Invoke(null, mensaje);
                return;
            }
            else
            {
                FirebaseStorageManager.singleton.currentAuthUser = usuarioAuth;
                FirebaseStorageManager.singleton.LoadData(usuarioAuth.UserId, (datos, mensaje) => {

                    if (datos == null)
                    {
                        OnComplete?.Invoke(null, mensaje);
                        return;
                    }
                    else
                    {
                        FirebaseStorageManager.singleton.CheckEmailVerified((estaVerificado, mensajeErrorVericacion) => {

                            if (!string.IsNullOrEmpty(mensajeErrorVericacion))
                            {
                                OnComplete?.Invoke(null, mensajeErrorVericacion);
                                return;
                            }

                            if (estaVerificado)
                            {
                                OnComplete?.Invoke(datos, "Inicio de sesión exitoso");  
                                return;
                            }
                            else
                            {
                                FirebaseStorageManager.singleton.SendEmailVerification((mailEnviado, mensajeErrorAlenviar) => {

                                    if (!mailEnviado)
                                    {
                                        OnComplete?.Invoke(null, mensajeErrorAlenviar);
                                        return;
                                    }
                                    else
                                    {
                                        ConfirmacionCorreoController.singleton.datosParaAñadir = datos;
                                        LogginMenuController.singleton.CambiarPestaña(3);
                                    }
                                });              
                            }
                        });
                    }
                });
            }
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
