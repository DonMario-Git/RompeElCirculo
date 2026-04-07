using System;
using TMPro;
using UnityEngine;
using UtilidadesLaEME;

public class ConfirmacionCorreoController : Singleton<ConfirmacionCorreoController>
{
    [HideInInspector] public Data datosParaAñadir;
    public TextMeshProUGUI textoCorreo;
    public TextMeshProUGUI texto;

    private void OnEnable()
    {
        texto.gameObject.DesactivarObjeto();
        textoCorreo.text = $"Se envió correo de verificacion en {CensurarEmail(datosParaAñadir.email)}";
    }

    public static string CensurarEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            return email;

        string[] partes = email.Split('@');
        string nombre = partes[0];
        string dominio = partes[1];

        int visibles;

        if (nombre.Length <= 2)
        {
            // Muy corto → solo deja 1 visible
            visibles = 1;
        }
        else if (nombre.Length <= 4)
        {
            // Corto → deja 2 visibles
            visibles = 2;
        }
        else
        {
            // Normal → deja 3 o 4 visibles
            visibles = 4;
        }

        visibles = Mathf.Min(visibles, nombre.Length);

        string visible = nombre.Substring(0, visibles);
        string oculto = new string('*', nombre.Length - visibles);

        return visible + oculto + "@" + dominio;
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
                    datosParaAñadir.correoAutenticado = true;
                    LogginController.singleton.ColocarDatosIniciarApp(datosParaAñadir, "Inicio sesion correctamente");
                    texto.gameObject.ActivarObjeto();
                    texto.text = "Inicio sesion correctamente";
                    texto.color = Color.green;
                }
                else
                {
                    datosParaAñadir.verificado = true;
                    texto.gameObject.ActivarObjeto();
                    texto.text = "Falta verificar el correo";
                    texto.color = Color.yellow;
                }
            }
        });
    }
}
