using DG.Tweening;
using TMPro;
using UnityEngine;
using UtilidadesLaEME;
using System;

public class RegisterController : MonoBehaviour
{
    public static RegisterController singleton;

    public InputFieldUtilities nombreCompleto, numeroDocumento, numeroCelular, otraNacionalidad, direccion, email, contraseña, confirmarContraseña;
    public TMP_Dropdown tipoDocumento;
    public PreguntaSeleccionMultipleController sexo, nacionalidad;
    public TextMeshProUGUI textoTituloOtraNacionalidad, mensajeError;

    public InputFieldUtilities diaNacimiento, mesNacimiento, añoNacimiento;

    public void VerificarNacionalidad(int indice)
    {
        if (indice == 2)
        {
            otraNacionalidad.gameObject.ActivarObjeto();
            textoTituloOtraNacionalidad.gameObject.ActivarObjeto();
        }
        else
        {
            otraNacionalidad.gameObject.DesactivarObjeto();
            textoTituloOtraNacionalidad.gameObject.DesactivarObjeto();
        }
    }

    private void OnEnable()
    {
        mensajeError.text = string.Empty;
    }

    public void RegistrarUsuario()
    {
        if (ValidarDatos(out string error))
        {
            mensajeError.text = string.Empty;

            // Construir objeto Data con los datos del formulario
            var data = new Data
            {
                nombreCompleto = nombreCompleto.inputField.text,
                tipoDocumento = tipoDocumento.options[tipoDocumento.value].text,
                numeroDocumento = numeroDocumento.inputField.text,
                numeroCelular = numeroCelular.inputField.text,
                sexo = sexo.cuadroSeleccionado.respuestaEMP.text,
                fechaNacimiento = Utilities.DateTimeToString(Utilities.CrearFecha(int.Parse(diaNacimiento.inputField.text), int.Parse(mesNacimiento.inputField.text), int.Parse(añoNacimiento.inputField.text))),
                nacionalidad = !otraNacionalidad.gameObject.activeInHierarchy ? nacionalidad.cuadroSeleccionado.respuestaEMP.text : otraNacionalidad.inputField.text,
                direccion = direccion.inputField.text,
                email = email.inputField.text,
                contrasena = contraseña.inputField.text
            };

            // Usar el nombreCompleto como userId
            string userId = nombreCompleto.inputField.text;
            FirebaseStorageManager.singleton.SaveData(data, userId, false, (resultError) =>
            {
                if (!string.IsNullOrEmpty(resultError))
                {
                    TirarMensaje(resultError, Color.red);
                    Debug.Log("error: " + resultError);
                }
                else
                {
                    TirarMensaje("Usuario registrado correctamente.", Color.green);
                    Debug.Log("Usuario registrado correctamente.");
                }
            });
        }
        else
        {
            TirarMensaje(error, Color.red);
            Debug.Log("error: " + error);
        }
    }

    private void TirarMensaje(string texto, Color color)
    {
        mensajeError.color = color;
        mensajeError.text = texto;
        mensajeError.transform.DOKill();
        mensajeError.transform.localScale = new Vector3(1.1f, 1.1f, 1);
        mensajeError.transform.DOScale(1, 0.2f);
    }

    public bool ValidarDatos(out string mensajeError)
    {
        if (!(nombreCompleto.contestado && numeroDocumento.contestado && numeroCelular.contestado && diaNacimiento.contestado && mesNacimiento && añoNacimiento
            && direccion.contestado && email.contestado && contraseña.contestado && confirmarContraseña.contestado 
            && sexo.contestado && !(nacionalidad.cuadroSeleccionado.indiceRespuesta == 2 && !otraNacionalidad.contestado)))
        {
            mensajeError = "Por favor rellenar todos los campos";
            return false;
        }

        if (!email.inputField.text.EsUnEmailValido())
        {
            mensajeError = "El correo electrónico no es válido";
            return false;
        }

        if (!contraseña.inputField.text.ValidarCaracteresContraseña(out string error))
        {
            mensajeError = error;
            return false;
        }

        if (contraseña.inputField.text != confirmarContraseña.inputField.text)
        {
            mensajeError = "Las contraseñas no coinciden";
            return false;
        }

        mensajeError = string.Empty;
        return true;
    }  
}
