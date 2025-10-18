using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using AwesomeAttributes;

public class EmailSender : MonoBehaviour
{
    public static EmailSender singleton;
    public string[] mailsEmpresasRemitentes;

    // ¡IMPORTANTE! Reemplaza esto con la URL HTTP generada por tu flujo de Power Automate.
    private const string PowerAutomateURL = "https://default357c54bb70a94d6f82ee69b067933d.3f.environment.api.powerplatform.com:443/powerautomate/automations/direct/workflows/7dacb0cd7fc34bafa0467cb83da47aad/triggers/manual/paths/invoke?api-version=1&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=KV_Pnz8899yTQGin6GM3T1gHcoOmF7VtQ-SYMV5VOB4";

    private void Awake()
    {
        singleton = this;
    }

    /// <summary>
    /// Estructura para serializar los datos del reporte de incidente a JSON.
    /// </summary>
    /// 
    [System.Serializable]
    public struct IncidentReportData
    {
        public string subject;
        public string body;
        public string severity;
        public string[] to;
    }

    /// <summary>
    /// Estructura para serializar los datos del correo a JSON.
    /// </summary>
    [System.Serializable]
    public struct EmailData
    {
        public string Para;    // Destinatario
        public string Asunto;  // Asunto del correo
        public string Cuerpo;  // Contenido del cuerpo del correo
    }

    /// <summary>
    /// Método público para iniciar el envío del reporte de incidente.
    /// </summary>
    /// <param name="subject">Asunto del reporte.</param>
    /// <param name="body">Descripción del caso.</param>
    /// <param name="severity">Nivel de severidad.</param>
    /// <param name="recipients">Array de destinatarios.</param>
    public void EnviarReporte(string subject, string body, string severity, string[] recipients)
    {
        IncidentReportData data = new IncidentReportData
        {
            subject = subject,
            body = body,
            severity = severity,
            to = recipients
        };

        string jsonPayload = JsonUtility.ToJson(data);
        StartCoroutine(PostRequest(jsonPayload));
    }

    /// <summary>
    /// Corrutina para enviar la solicitud POST a la URL de Power Automate.
    /// </summary>
    /// <param name="jsonPayload">La cadena JSON con los datos del reporte.</param>
    /// <returns></returns>
    IEnumerator PostRequest(string jsonPayload)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest www = new UnityWebRequest(PowerAutomateURL, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();

            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("x-api-key", "RC-Unity-Reporte-2025#K9");

            Debug.Log("Enviando datos a Power Automate: " + jsonPayload);

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al enviar el reporte: " + www.error);
                Debug.LogError("Respuesta del servidor: " + www.downloadHandler.text);
            }
            else
            {
                Debug.Log("Reporte enviado con éxito.");
                Debug.Log("Respuesta del servidor: " + www.downloadHandler.text);
            }
        }
    }
}
