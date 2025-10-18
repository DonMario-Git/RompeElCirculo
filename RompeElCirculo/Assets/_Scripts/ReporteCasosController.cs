using UnityEngine;
using System;
using Firebase.Database;
using Firebase.Extensions;
using UtilidadesLaEME;
using DG.Tweening;

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

    private void AbrirVentanaListo()
    {
        pantallaNegra.ActivarObjeto();
        ventanaListo.localScale = Vector3.one * 1.2f;
        ventanaListo.gameObject.ActivarObjeto();
        ventanaListo.DOKill();
        ventanaListo.DOScale(1, 0.3f);
    }
        
    private void CerrarVentanaListo()
    {
        pantallaNegra.DesactivarObjeto();
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

            hechoAReportar = descripcionReporte.inputField.text,
            estadoCaso = "pendiente"
        };

        SubirReporteCaso(nuevoCaso, (error) => {
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning(error);
            }
            else
            {
                //CentroNotificacionesController.singleton.EnviarNotificacion(AppManager.userData.email, $"Reporte de botón violeta", "Enviado correctamente, espere atención pronto");
                EmailSender.singleton.EnviarReporte($"Reporte de botón violeta [ID : {nuevoCaso.ID}]", $"Se reportó un caso en la fecha {nuevoCaso.fechaCaso}, a nombre de {nuevoCaso.nombreCompleto}, con {nuevoCaso.tipoDocumento} {nuevoCaso.numeroDocumento} en donde se reporta lo siguiente: {nuevoCaso.hechoAReportar}. {(contactarParaApoyo.cuadroSeleccionado.indiceRespuesta == 1 ? "" : $"Se pide contactar al emisor con el numero {nuevoCaso.numeroCelular} para ofrecer apoyo lo más pronto posible")}", "alta", EmailSender.singleton.mailsEmpresasRemitentes);
                CentroNotificacionesController.singleton.EnviarNotificacion(entidadesReceptoras, $"Reporte de botón violeta", $"De: '{nuevoCaso.nombreCompleto}'");
            }
        });
    }


    // Subir un ReporteCaso a Firebase con ID único
    public void SubirReporteCaso(Caso reporte, Action<string> onResult)
    {
        FirebaseStorageManager.singleton.AddReporteCaso(reporte, onResult);
    }

    // Eliminar un ReporteCaso por ID
    public void EliminarReporteCaso(string reporteId, Action<string> onResult)
    {
        if (!FirebaseStorageManager.singleton.isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("No hay conexión a internet.");
            return;
        }
        var dbRef = typeof(FirebaseStorageManager)
            .GetField("dbReference", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(FirebaseStorageManager.singleton) as DatabaseReference;
        dbRef.Child("reportes").Child(reporteId).RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onResult?.Invoke("Error al eliminar el reporte: " + task.Exception);
            }
            else
            {
                onResult?.Invoke(null); // Éxito
            }
        });
    }

    // Editar un ReporteCaso por ID
    public void EditarReporteCaso(string reporteId, Caso nuevoReporte, Action<string> onResult)
    {
        if (!FirebaseStorageManager.singleton.isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("No hay conexión a internet.");
            return;
        }
        var dbRef = typeof(FirebaseStorageManager)
            .GetField("dbReference", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(FirebaseStorageManager.singleton) as DatabaseReference;
        dbRef.Child("reportes").Child(reporteId).SetRawJsonValueAsync(JsonUtility.ToJson(nuevoReporte)).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onResult?.Invoke("Error al editar el reporte: " + task.Exception);
            }
            else
            {
                onResult?.Invoke(null); // Éxito
            }
        });
    }
}
