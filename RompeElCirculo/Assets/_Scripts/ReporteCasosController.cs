using DG.Tweening;
using System;
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

    public void VolverPrincipal()
    {
        PestañasManager.singleton.CambiarPestaña(0);
    }

    public void VerificarRespuestas()
    {
        botonReportar.button.interactable = incluirNombre.contestado && contactarParaApoyo.contestado && descripcionReporte.contestado && tipoViolencia.contestado;
    }

    public void Reportar()
    {
        Caso nuevoCaso = new()
        {
            nombreCompleto = incluirNombre.cuadroSeleccionado.indiceRespuesta == 1 ? "[Anonimo]" : AppManager.userData.nombreCompleto,
            tipoDocumento = incluirNombre.cuadroSeleccionado.indiceRespuesta == 1 ? "[Anonimo]" : AppManager.userData.tipoDocumento,
            numeroDocumento = incluirNombre.cuadroSeleccionado.indiceRespuesta == 1 ? "[Anonimo]" : AppManager.userData.numeroDocumento,
            numeroCelular = contactarParaApoyo.cuadroSeleccionado.indiceRespuesta == 1 ? "[Anonimo]" : AppManager.userData.numeroCelular,
            sexo = incluirNombre.cuadroSeleccionado.indiceRespuesta == 1 ? "[Anonimo]" : AppManager.userData.sexo,
            direccion = incluirNombre.cuadroSeleccionado.indiceRespuesta == 1 ? "[Anonimo]" : AppManager.userData.direccion,
            fechaCaso = Utilities.DateTimeToString(DateTime.Now),
            fechaNacimiento = incluirNombre.cuadroSeleccionado.indiceRespuesta == 1 ? "[Anonimo]" : AppManager.userData.fechaNacimiento,

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
                //CentroNotificacionesController.singleton.EnviarNotificacion(AppManager.userData.email, $"Reporte de botón violeta", "Enviado correctamente, espere atención pronto");
                EmailSender.singleton.EnviarReporte($"Reporte de botón violeta [ID : {nuevoCaso.ID}]", $"Se reportó un caso en la fecha {nuevoCaso.fechaCaso}, a nombre de {nuevoCaso.nombreCompleto}, con {nuevoCaso.tipoDocumento} {nuevoCaso.numeroDocumento} en donde se reporta lo siguiente: {nuevoCaso.hechoAReportar}. {(contactarParaApoyo.cuadroSeleccionado.indiceRespuesta == 1 ? "" : $"Se pide contactar al emisor con el numero {nuevoCaso.numeroCelular} para ofrecer apoyo lo más pronto posible")}", "alta", EmailSender.singleton.mailsEmpresasRemitentes, (error2) => {
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
