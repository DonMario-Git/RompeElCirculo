using System;
using TMPro;
using UnityEngine;
using UtilidadesLaEME;

public class ConfirmacionCorreoController : Singleton<ConfirmacionCorreoController>
{
    [HideInInspector] public Data datosParaAñadir;
    public TextMeshProUGUI texto;

    private void OnEnable()
    {
        texto.gameObject.DesactivarObjeto();
    }

    public void VerificarSiConfirmo()
    {
        FirebaseStorageManager.singleton.CheckEmailVerified((estaConfirmado, mensajeErrorVericacion) => {

            if (!string.IsNullOrEmpty(mensajeErrorVericacion))
            {
                Debug.LogWarning($"Error: {mensajeErrorVericacion}");
                //LogginMenuController.singleton.CambiarPestaña(1);
            }
            else
            {
                if (estaConfirmado)
                {
                    LogginController.singleton.ColocarDatosIniciarApp(datosParaAñadir, "Inicio sesion correctamente");
                    texto.gameObject.ActivarObjeto();
                    texto.text = "Inicio sesion correctamente";
                    texto.color = Color.green;
                }
                else
                {
                    texto.gameObject.ActivarObjeto();
                    texto.text = "Falta verificar el correo";
                    texto.color = Color.red;
                }
            }
        });
    }
}
