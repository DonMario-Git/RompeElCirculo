using DG.Tweening;
using TMPro;
using UnityEngine;
using UtilidadesLaEME;
using System;
using UnityEngine.UI;

public class RegisterController : MonoBehaviour
{
    public static RegisterController singleton;

    public InputFieldUtilities nombreCompleto, numeroDocumento, numeroCelular, otraNacionalidad, direccion, email, contraseña, confirmarContraseña;
    public TMP_Dropdown tipoDocumento, departamento;
    public PreguntaSeleccionMultipleController sexo, nacionalidad;
    public TextMeshProUGUI textoTituloOtraNacionalidad, mensajeError;

    public InputFieldUtilities diaNacimiento, mesNacimiento, añoNacimiento;

    public RectTransform ruedaCarga;
    public Button botonRegistrar;

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
        botonRegistrar.interactable = false;
        ruedaCarga.gameObject.ActivarObjeto();
        ruedaCarga.transform.DOKill();
        ruedaCarga.transform.rotation = Quaternion.identity;
        ruedaCarga.transform.DORotate(new Vector3(0, 0, 360), 1, RotateMode.FastBeyond360)
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Restart);

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
                municipio = departamento.options[departamento.value].text,
                municipioID = departamento.value,
                direccion = direccion.inputField.text,
                email = email.inputField.text,
                contrasena = contraseña.inputField.text
            };

            FirebaseStorageManager.singleton.CreateAccount(email.inputField.text.TrimEdges(), contraseña.inputField.text.TrimEdges(), (isError, mensaje) => {

                if (isError)
                {
                    Debug.LogWarning(mensaje);
                    TirarMensaje(mensaje, Color.red);
                    botonRegistrar.interactable = true;
                    ruedaCarga.gameObject.DesactivarObjeto();
                    return;
                }
                else
                {
                    _ = FirebaseStorageManager.singleton.SaveUsuario(data, FirebaseStorageManager.singleton.UserID, false, (resultError) =>
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

                        botonRegistrar.interactable = true;
                        ruedaCarga.gameObject.DesactivarObjeto();
                    });
                }
            });
        }
        else
        {
            TirarMensaje(error, Color.red);
            Debug.Log("error: " + error);
            botonRegistrar.interactable = true;
            ruedaCarga.gameObject.DesactivarObjeto();
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
        // 1. Validar que todos los campos estén contestados
        // (Corregido: se agregó .contestado a mes y año)
        if (!(nombreCompleto.contestado && numeroDocumento.contestado && numeroCelular.contestado
            && diaNacimiento.contestado && mesNacimiento.contestado && añoNacimiento.contestado
            && direccion.contestado && email.contestado && contraseña.contestado && confirmarContraseña.contestado
            && sexo.contestado && !(nacionalidad.cuadroSeleccionado.indiceRespuesta == 2 && !otraNacionalidad.contestado)))
        {
            mensajeError = "Por favor rellenar todos los campos";
            return false;
        }

        // 2. Validar que la fecha de nacimiento sea válida
        if (!ValidarFechaNacimiento(out mensajeError))
        {
            return false;
        }

        // 3. Validar formato de email
        if (!email.inputField.text.EsUnEmailValido())
        {
            mensajeError = "El correo electrónico no es válido";
            return false;
        }

        // 4. Validar caracteres de contraseña
        if (!contraseña.inputField.text.ValidarCaracteresContraseña(out string error))
        {
            mensajeError = error;
            return false;
        }

        // 5. Validar que las contraseñas coincidan
        if (contraseña.inputField.text.TrimEdges() != confirmarContraseña.inputField.text.TrimEdges())
        {
            mensajeError = "Las contraseñas no coinciden";
            return false;
        }

        mensajeError = string.Empty;
        return true;
    }

    // Método auxiliar para mantener limpio tu método principal
    private bool ValidarFechaNacimiento(out string mensajeError)
    {
        // Intentamos parsear los textos a números enteros
        if (int.TryParse(diaNacimiento.inputField.text, out int dia) &&
            int.TryParse(mesNacimiento.inputField.text, out int mes) &&
            int.TryParse(añoNacimiento.inputField.text, out int año))
        {
            try
            {
                // El constructor de DateTime lanzará un error si la combinación de día/mes/año es imposible
                DateTime fechaNac = new DateTime(año, mes, dia);

                // Validación extra opcional: Que no sea una fecha en el futuro
                if (fechaNac > DateTime.Today)
                {
                    mensajeError = "La fecha de nacimiento no puede ser en el futuro";
                    return false;
                }
            }
            catch (System.ArgumentOutOfRangeException)
            {
                mensajeError = "La fecha de nacimiento introducida no existe (ej. 31 de febrero)";
                return false;
            }
        }
        else
        {
            mensajeError = "La fecha de nacimiento debe ser numérica";
            return false;
        }

        mensajeError = string.Empty;
        return true;
    }
}
