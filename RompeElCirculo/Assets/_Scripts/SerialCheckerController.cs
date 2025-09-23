using AwesomeAttributes;
using Firebase.Database;
using System;
using TMPro;
using UnityEngine;

public class SerialCheckerController : MonoBehaviour
{
    [Button(nameof(ActivarLicencia))]
    public int UsuarioBusqueda_Activar;

    // Nuevo: campos para input de usuario y fecha
    public TMP_InputField usuarioInput;
    public TMP_InputField fechaInput; // Formato esperado: yyyy-MM-dd

    public static SerialCheckerController singleton;

    private void Awake()
    {
        singleton = this;
        if (fechaInput != null)
        {
            fechaInput.text = DateTime.UtcNow.AddMonths(1).ToString("dd/MM/yyyy");
        }
    }

    public enum Periodo
    {
        Dias,
        Semanas,
        Meses
    }

    public void ActivarLicencia()
    {
        ActualizarFechaLicencia(UsuarioBusqueda_Activar, FechaDespues(1, Periodo.Meses), (error) => {
            if (!string.IsNullOrEmpty(error)) Debug.Log(error);
            });
    }

    // Nuevo: método para establecer licencia desde los inputs
    [Button("Establecer Licencia Desde Input")]
    public void EstablecerLicenciaDesdeInput()
    {
        int usuario;
        if (!int.TryParse(usuarioInput.text.Trim(), out usuario))
        {
            Debug.LogWarning("Usuario inválido. Debe ser un número de serie entero.");
            return;
        }
        DateTime fecha;
        if (!DateTime.TryParseExact(fechaInput.text.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out fecha))
        {
            Debug.LogWarning("Fecha inválida. Usa el formato dd/MM/yyyy.");
            return;
        }
        ActualizarFechaLicencia(usuario, fecha, (error) => {
            if (!string.IsNullOrEmpty(error)) Debug.LogError(error);
            else Debug.Log("Licencia actualizada correctamente para el usuario " + usuario);
        });
    }

    [System.Serializable]
    public class Licencia
    {
        public int numeroSerie;
        public string fechaVecimiento; // ISO 8601
        public bool activa;

        public DateTime FechaDateTime => DateTime.Parse(fechaVecimiento, null, System.Globalization.DateTimeStyles.RoundtripKind);

        public Licencia(int numeroSerie, DateTime fecha)
        {
            this.numeroSerie = numeroSerie;
            this.fechaVecimiento = fecha.ToString("o");
            this.activa = false;
        }
    }

    public void GuardarLicencia(int numeroSerie, DateTime fecha, string nombreObjeto, Action<string> onResult)
    {
        if (FirebaseStorageManager.singleton == null || !FirebaseStorageManager.singleton.isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }

        Licencia licencia = new Licencia(numeroSerie, fecha);
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(licencia);

        // Usar el nombre proporcionado como ID del objeto
        string licenciaId = nombreObjeto;

        var dbReference = typeof(FirebaseStorageManager)
            .GetField("dbReference", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(FirebaseStorageManager.singleton) as DatabaseReference;

        var saveTask = dbReference.Child("licencias").Child(licenciaId).SetRawJsonValueAsync(json);
        saveTask.ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
                onResult?.Invoke("Error al guardar: " + task.Exception);
            else
                onResult?.Invoke(null);
        });
    }

    // Método para cargar una licencia por número de serie, comparar la fecha y desactivar la licencia
    public void CargarYCompararLicencia(int numeroSerie, Action<bool, Licencia, int, String> onResult)
    {
        if (FirebaseStorageManager.singleton == null || !FirebaseStorageManager.singleton.isInitialized)
        {
            onResult?.Invoke(false, null, -1, "Firebase no está inicializado.");
            return;
        }

        var dbReference = typeof(FirebaseStorageManager)
            .GetField("dbReference", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(FirebaseStorageManager.singleton) as DatabaseReference;

        dbReference.Child("licencias").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onResult?.Invoke(false, null, -1, "Error al cargar licencias: " + task.Exception);
                return;
            }

            DataSnapshot snapshot = task.Result;
            foreach (var child in snapshot.Children)
            {
                string json = child.GetRawJsonValue();
                Licencia licencia = Newtonsoft.Json.JsonConvert.DeserializeObject<Licencia>(json);
                if (licencia != null && licencia.numeroSerie == numeroSerie)
                {
                    DateTime fechaLicencia = licencia.FechaDateTime;
                    bool vencida = DateTime.UtcNow > fechaLicencia;
                    int diasRestantes = vencida ? 0 : (int)Math.Ceiling((fechaLicencia - DateTime.UtcNow).TotalDays);

                    // Desactivar la licencia si está vencida y guardar el cambio
                    licencia.activa = !vencida;
                    string updatedJson = Newtonsoft.Json.JsonConvert.SerializeObject(licencia);
                    dbReference.Child("licencias").Child(child.Key).SetRawJsonValueAsync(updatedJson).ContinueWith(saveTask =>
                    {
                        if (saveTask.IsFaulted || saveTask.IsCanceled)
                            onResult?.Invoke(false, null, -1, "Error al actualizar la licencia: " + saveTask.Exception);
                        else
                            onResult?.Invoke(vencida, licencia, diasRestantes, null);
                    });
                    return;
                }
            }
            onResult?.Invoke(false, null, -1, "No se encontró la licencia.");
        });
    }

    // Método para actualizar la fecha de una licencia existente y activarla
    public void ActualizarFechaLicencia(int numeroSerie, DateTime nuevaFecha, Action<string> onResult)
    {
        if (FirebaseStorageManager.singleton == null || !FirebaseStorageManager.singleton.isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }

        var dbReference = typeof(FirebaseStorageManager)
            .GetField("dbReference", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(FirebaseStorageManager.singleton) as DatabaseReference;

        dbReference.Child("licencias").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onResult?.Invoke("Error al cargar licencias: " + task.Exception);
                return;
            }

            DataSnapshot snapshot = task.Result;
            bool found = false;
            foreach (var child in snapshot.Children)
            {
                string json = child.GetRawJsonValue();
                Licencia licencia = Newtonsoft.Json.JsonConvert.DeserializeObject<Licencia>(json);
                if (licencia != null && licencia.numeroSerie == numeroSerie)
                {
                    // Actualiza la fecha y activa la licencia
                    licencia.fechaVecimiento = nuevaFecha.ToString("o");
                    licencia.activa = true;
                    string updatedJson = Newtonsoft.Json.JsonConvert.SerializeObject(licencia);

                    dbReference.Child("licencias").Child(child.Key).SetRawJsonValueAsync(updatedJson).ContinueWith(saveTask =>
                    {
                        if (saveTask.IsFaulted || saveTask.IsCanceled)
                            onResult?.Invoke("Error al actualizar: " + saveTask.Exception);
                        else
                            Debug.Log("Se actualizo la licencia correctamente");
                            onResult?.Invoke(null);
                    });
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                // Si no se encontró la licencia, se crea una nueva y se activa
                Licencia nuevaLicencia = new Licencia(numeroSerie, nuevaFecha);
                nuevaLicencia.activa = true;
                string jsonNuevaLicencia = Newtonsoft.Json.JsonConvert.SerializeObject(nuevaLicencia);
                string nombreObjeto = numeroSerie.ToString();
                dbReference.Child("licencias").Child(nombreObjeto).SetRawJsonValueAsync(jsonNuevaLicencia).ContinueWith(saveTask =>
                {
                    if (saveTask.IsFaulted || saveTask.IsCanceled)
                        onResult?.Invoke("Error al crear la licencia: " + saveTask.Exception);
                    else
                        Debug.Log("Se creó y activó la licencia correctamente");
                        onResult?.Invoke(null);
                });
            }
        });
    }

    // Nuevo método para obtener una fecha después de cierta cantidad de días, semanas o meses
    public static DateTime FechaDespues(int cantidad, Periodo periodo)
    {
        switch (periodo)
        {
            case Periodo.Dias:
                return DateTime.UtcNow.AddDays(cantidad);
            case Periodo.Semanas:
                return DateTime.UtcNow.AddDays(7 * cantidad);
            case Periodo.Meses:
                return DateTime.UtcNow.AddMonths(cantidad);
            default:
                throw new ArgumentOutOfRangeException(nameof(periodo), periodo, null);
        }
    }
}
