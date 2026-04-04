using DG.Tweening;
using System;
using System.Net;
using TMPro; 
using UnityEngine;
using UtilidadesLaEME;

public class ReporteCasosController : MonoBehaviour
{
    public string[] entidadesReceptoras;

    public PreguntaSeleccionMultipleController incluirNombre, contactarParaApoyo;
    public InputFieldUtilities descripcionReporte;
    public PreguntaSeleccionMultipleController tipoViolencia;
    public ButtonExtrasController botonReportar;

    [Header("MensajeListo")]

    public GameObject pantallaNegra;
    public Transform ventanaListo;
    public Transform ruedaCarga;

    public Transform ventanaError;
    public TextMeshProUGUI textoError;

    private void OnEnable()
    {
        pantallaNegra.DesactivarObjeto();
        ventanaListo.gameObject.DesactivarObjeto();
        ventanaError.gameObject.DesactivarObjeto();
    }

    public void VerificarRespuestas()
    {
        botonReportar.button.interactable = incluirNombre.contestado && contactarParaApoyo.contestado && descripcionReporte.contestado && tipoViolencia.contestado;
    }

    public void Reportar()
    {
        Caso nuevoCaso = new()
        {
            nombreCompleto = incluirNombre.cuadroSeleccionado.indiceRespuesta == 1 ? "[Anonimo]" : AppManager.UserData.nombreCompleto,
            tipoDocumento = incluirNombre.cuadroSeleccionado.indiceRespuesta == 1 ? "[Anonimo]" : AppManager.UserData.tipoDocumento,
            numeroDocumento = incluirNombre.cuadroSeleccionado.indiceRespuesta == 1 ? "[Anonimo]" : AppManager.UserData.numeroDocumento,
            numeroCelular = contactarParaApoyo.cuadroSeleccionado.indiceRespuesta == 1 ? "[Anonimo]" : AppManager.UserData.numeroCelular,
            sexo = incluirNombre.cuadroSeleccionado.indiceRespuesta == 1 ? "[Anonimo]" : AppManager.UserData.sexo,
            direccion = incluirNombre.cuadroSeleccionado.indiceRespuesta == 1 ? "[Anonimo]" : AppManager.UserData.direccion,
            fechaCaso = Utilities.DateTimeToString(DateTime.Now),
            fechaNacimiento = incluirNombre.cuadroSeleccionado.indiceRespuesta == 1 ? "[Anonimo]" : AppManager.UserData.fechaNacimiento,

            hechoAReportar = descripcionReporte.inputField.text,
            tipoAvance = 0,
            estadoDelCaso = 0
        };

        ruedaCarga.gameObject.ActivarObjeto();
        ruedaCarga.DOKill();
        ruedaCarga.DORotate(new Vector3(0, 0, 360), 1, RotateMode.FastBeyond360)
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Restart);

        pantallaNegra.ActivarObjeto();

        SubirReporteCaso(nuevoCaso, (error) => {
            if (!string.IsNullOrEmpty(error))
            {
                ruedaCarga.transform.DOKill();
                ruedaCarga.gameObject.DesactivarObjeto();
                Debug.LogWarning(error);
                textoError.text = error;
                ventanaError.localScale = Vector3.one * 1.2f;
                ventanaError.gameObject.ActivarObjeto();
                ventanaError.DOKill();
                ventanaError.DOScale(1, 0.3f);
            }
            else
            {
                // Cuerpo en HTML; escapamos entradas de usuario y convertimos saltos a <br/>
                string hechoEscapado = WebUtility.HtmlEncode(nuevoCaso.hechoAReportar).Replace("\r\n", "<br/>").Replace("\n", "<br/>");
                string contactoHtml = contactarParaApoyo.cuadroSeleccionado.indiceRespuesta == 1
                    ? string.Empty
                    : $"<p><strong>Solicitar contacto al emisor:</strong> {WebUtility.HtmlEncode(nuevoCaso.numeroCelular)}</p>";

                string cuerpo = $@"<html><body>
<p>Se reportó un caso en la fecha {WebUtility.HtmlEncode(nuevoCaso.fechaCaso)}.</p>

<p><strong>A nombre de:</strong> {WebUtility.HtmlEncode(nuevoCaso.nombreCompleto)}<br/>
<strong>Documento:</strong> {WebUtility.HtmlEncode(nuevoCaso.tipoDocumento)} {WebUtility.HtmlEncode(nuevoCaso.numeroDocumento)}</p>

<p><strong>Hecho a reportar:</strong><br/>{hechoEscapado}</p>

{contactoHtml}

<p><em>ID del caso: {WebUtility.HtmlEncode(nuevoCaso.ID)}</em></p>
</body></html>";

                EmailSender.singleton.EnviarReporte("Reporte de botón violeta", cuerpo, "alta", EmailSender.singleton.mailsEmpresasRemitentes, (error2) => {
                    ruedaCarga.transform.DOKill();
                    ruedaCarga.gameObject.DesactivarObjeto();
                    CentroNotificacionesController.singleton.EnviarNotificacion(entidadesReceptoras, $"Reporte de botón violeta", $"De: '{nuevoCaso.nombreCompleto}'");

                    if (string.IsNullOrEmpty(error2))
                    {
                        ventanaListo.localScale = Vector3.one * 1.2f;
                        ventanaListo.gameObject.ActivarObjeto();
                        ventanaListo.DOKill();
                        ventanaListo.DOScale(1, 0.3f);
                    }
                    else
                    {
                        textoError.text = error2;
                        ventanaError.localScale = Vector3.one * 1.2f;
                        ventanaError.gameObject.ActivarObjeto();
                        ventanaError.DOKill();
                        ventanaError.DOScale(1, 0.3f);
                    }
                });   
            }
        });
    }

    // Subir un ReporteCaso a Firebase with unqiue ID
    public void SubirReporteCaso(Caso reporte, Action<string> onResult)
    {
        FirebaseStorageManager.singleton.AddReporteCaso(reporte, onResult);
    }  
}
