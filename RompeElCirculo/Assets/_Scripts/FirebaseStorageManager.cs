using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Threading.Tasks;
using UnityEngine.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class FirebaseStorageManager : MonoBehaviour
{
    private DatabaseReference dbReference;
    public UnityEvent OnInitialize;
    public bool isInitialized;
    public static FirebaseStorageManager singleton;

    private void Awake()
    {
        if (singleton != null)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
            singleton = this;
            FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);
        }     
    }

    private async void Start()
    {
        if (singleton == this) await InitializeFirebase();
    }

    private async Task InitializeFirebase()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (dependencyStatus == DependencyStatus.Available)
        {
            dbReference = FirebaseDatabase.DefaultInstance.RootReference;
            FirebaseDatabase.DefaultInstance.GoOnline();

            OnInitialize?.Invoke();
            isInitialized = true;
            Debug.Log("Firebase Realtime Database listo.");
        }
        else
        {
            Debug.LogError("Error en dependencias: " + dependencyStatus);
        }
    }

    private float lastFirebaseCallTime = -5f;
    private const float firebaseCallCooldown = 5f;

    private bool CanCallFirebase()
    {
        if (Time.time - lastFirebaseCallTime < firebaseCallCooldown)
            return false;
        lastFirebaseCallTime = Time.time;
        return true;
    }

    public void LoadData(string userId, System.Action<Data, string> onSuccess)
    {
        if (!CanCallFirebase())
        {
            onSuccess?.Invoke(null, "Debes esperar 5 segundos entre operaciones.");
            return;
        }

        if (!isInitialized)
        {
            onSuccess?.Invoke(null, null);
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            string debug = "No hay conexión a internet. No se pueden cargar los datos.";
            Debug.LogError(debug);
            onSuccess?.Invoke(null, debug);
            return;
        }

        var userRef = dbReference.Child("usuarios").Child(userId);
        userRef.KeepSynced(false);

        userRef.GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                string debug;
                if (task.IsFaulted || task.IsCanceled)
                {
                    debug = "Error al cargar datos";
                    Debug.LogError("Error al cargar: " + task.Exception);
                    onSuccess?.Invoke(null, debug);
                }
                else if (task.Result.Exists)
                {
                    string json = task.Result.GetRawJsonValue();
                    Data data = JsonConvert.DeserializeObject<Data>(json);

                    debug = null;
                    Debug.Log("Datos cargados desde servidor.");
                    onSuccess?.Invoke(data, debug);
                }
                else
                {
                    debug = "Nombre o contraseña incorrectos";
                    Debug.LogWarning("No se encontraron datos.");
                    onSuccess?.Invoke(null, debug);
                }
            });
    }

    public async void SaveData(Data data, string userId, bool overwrite, System.Action<string> onResult, bool debeEsperar = true)
    {
        if (!CanCallFirebase() && debeEsperar)
        {
            onResult?.Invoke("Debes esperar 5 segundos entre operaciones.");
            return;
        }

        if (!isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("No hay conexión a internet. No se pueden guardar los datos.");
            return;
        }

        if (!overwrite)
        {
            var snapshotTask = dbReference.Child("usuarios").GetValueAsync();
            await snapshotTask;

            if (snapshotTask.IsFaulted || snapshotTask.IsCanceled)
            {
                onResult?.Invoke("Error al consultar usuarios: " + snapshotTask.Exception);
                return;
            }

            var snapshot = snapshotTask.Result;
            foreach (var child in snapshot.Children)
            {
                string childJson = child.GetRawJsonValue();
                Data existingData = JsonConvert.DeserializeObject<Data>(childJson);
                if (existingData != null &&
                    (existingData.email == data.email ||
                     existingData.nombreCompleto == data.nombreCompleto))
                {
                    onResult?.Invoke("Ya existe un usuario con ese correo o nombre.");
                    return;
                }
            }
        }

        string json = JsonConvert.SerializeObject(data);
        var saveTask = dbReference.Child("usuarios").Child(userId).SetRawJsonValueAsync(json);
        await saveTask;

        if (saveTask.IsFaulted || saveTask.IsCanceled)
        {
            onResult?.Invoke("Error al guardar: " + saveTask.Exception);
        }
        else
        {
            onResult?.Invoke(null);
            print($"Se guardó correctamente {data.nombreCompleto}");
        }
    }

    // Método para obtener todos los usuarios de la base de datos
    public void GetAllUsers(Action<List<Data>, string> onResult)
    {
        if (!isInitialized)
        {
            onResult?.Invoke(null, "Firebase no está inicializado.");
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke(null, "No hay conexión a internet.");
            return;
        }

        dbReference.Child("usuarios").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onResult?.Invoke(null, "Error al descargar usuarios: " + task.Exception);
                return;
            }

            DataSnapshot snapshot = task.Result;
            List<Data> usuarios = new List<Data>();
            foreach (var child in snapshot.Children)
            {
                string json = child.GetRawJsonValue();
                Data usuario = JsonConvert.DeserializeObject<Data>(json);
                if (usuario != null)
                    usuarios.Add(usuario);
            }
            onResult?.Invoke(usuarios, null);
        });
    }
}

[System.Serializable]
public class Data
{
    public string nombreCompleto;
    public string tipoDocumento;
    public string numeroDocumento;
    public string numeroCelular;
    public string sexo;
    public string fechaNacimiento;
    public string nacionalidad;
    public string direccion;
    public string email;
    public string contrasena;
}