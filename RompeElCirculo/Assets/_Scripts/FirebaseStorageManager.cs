using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Auth;
using System.Threading.Tasks;
using UnityEngine.Events;
using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

public class FirebaseStorageManager : Singleton<FirebaseStorageManager>
{
    private DatabaseReference dbReference;
    public bool isInitialized;
    public FirebaseUser currentAuthUser;
    public UnityEvent OnInitialized;

    #region Inicio

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);
    }
    
    private async void Start()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable) return;

        if (singleton == this) await InitializeFirebase();
    }

    private async Task InitializeFirebase()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (dependencyStatus == DependencyStatus.Available)
        {
            dbReference = FirebaseDatabase.DefaultInstance.RootReference;
            FirebaseDatabase.DefaultInstance.GoOnline();
            isInitialized = true;
            OnInitialized?.Invoke();
            Debug.Log("Firebase Realtime Database listo.");
        }
        else
        {
            isInitialized = false;
            OnInitialized?.Invoke();
            Debug.LogError("Error en dependencias: " + dependencyStatus);
        }
    }

    #endregion

    #region Autenticacion

    // Crear usuario con FirebaseAuth y guardar sus datos en la base de datos
    public async void CreateAuthUser(string email, string password, Action<FirebaseUser, string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke(null, "No hay conexión a internet. No se puede crear el usuario.");
            return;
        }

        if (!isInitialized)
        {
            onResult?.Invoke(null, "Firebase no está inicializado.");
            return;
        }

        var auth = FirebaseAuth.DefaultInstance;
        try
        {
            // Esperar a que se cree el usuario. Dependiendo de la versión del SDK
            // este método puede devolver un AuthResult o FirebaseUser, pero auth.CurrentUser
            // se actualizará en ambos casos.
            await auth.CreateUserWithEmailAndPasswordAsync(email, password);

            FirebaseUser newUser = auth.CurrentUser;
            if (newUser == null)
            {
                onResult?.Invoke(null, "No se pudo obtener el usuario creado.");
                return;
            }
            else
            {
                onResult?.Invoke(newUser, string.Empty);
                Debug.Log($"Usuario registrado correctamente con nombre {newUser.UserId}");
            }
        }
        catch (Exception e)
        {
            onResult?.Invoke(null, "Error al crear usuario: " + e.Message);
        }
    }

    // Iniciar sesión con email/clave, almacenar el usuario autenticado y sus datos en variables
    public async void SignInAuthUser(string email, string password, Action<FirebaseUser, string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke(null, "No hay conexión a internet.");
            return;
        }

        if (!isInitialized)
        {
            onResult?.Invoke(null, "Firebase no está inicializado.");
            return;
        }

        var auth = FirebaseAuth.DefaultInstance;
        try
        {
            await auth.SignInWithEmailAndPasswordAsync(email, password);
            FirebaseUser newUser = auth.CurrentUser;
            if (newUser == null)
            {
                onResult?.Invoke(null, "No se pudo obtener el usuario autenticado.");
                return;
            }
            else
            {
                onResult?.Invoke(newUser, "InicioSesionCorrectamente");
                return;
            }
        }
        catch (Exception e)
        {
            onResult?.Invoke(null, "Error al iniciar sesión: " + e.Message);
        }
    }

    public void SendEmailVerification(Action<bool, string> onResult)
    {
        if (currentAuthUser == null)
        {
            onResult?.Invoke(false, "No hay usuario logueado.");
            return;
        }

        currentAuthUser.SendEmailVerificationAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                onResult?.Invoke(false, "Operación cancelada.");
                return;
            }

            if (task.IsFaulted)
            {
                var msg = task.Exception?.GetBaseException()?.Message;
                Debug.LogError("Error enviando verificación: " + msg);
                onResult?.Invoke(false, msg);
                return;
            }

            Debug.Log("Email de verificación enviado.");
            onResult?.Invoke(true, null);
        });
    }

    /// <summary>
    /// Verifica si el correo esta verificado
    /// </summary>
    /// <param name="onResult"></param>
    public void CheckEmailVerified(Action<bool, string> onResult)
    {
        if (currentAuthUser == null)
        {
            onResult?.Invoke(false, "No hay usuario logueado.");
            return;
        }

        currentAuthUser.ReloadAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                onResult?.Invoke(false, "Operación cancelada.");
                return;
            }

            if (task.IsFaulted)
            {
                var msg = task.Exception?.GetBaseException()?.Message;
                Debug.LogError("Error recargando usuario: " + msg);
                onResult?.Invoke(false, msg);
                return;
            }

            bool isVerified = currentAuthUser.IsEmailVerified;

            Debug.Log("Email verificado: " + isVerified);

            onResult?.Invoke(isVerified, null);
        });
    }

    #endregion

    #region DataBase

    private float lastFirebaseCallTime = -5f;
    private const float firebaseCallCooldown = 5f;

    /// <summary>
    /// Esto es lo de esperar los 5 segundos
    /// </summary>
    /// <returns></returns>
    private bool CanCallFirebase()
    {
        if (Time.time - lastFirebaseCallTime < firebaseCallCooldown)
            return false;
        lastFirebaseCallTime = Time.time;
        return true;
    }

    public void LoadData(string userId, System.Action<Data, string> onSuccess)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            string debug = "No hay conexión a internet. No se pueden cargar los datos.";
            onSuccess?.Invoke(null, debug);
            return;
        }

        if (!CanCallFirebase())
        {
            onSuccess?.Invoke(null, "Debes esperar 5 segundos entre operaciones.");
            return;
        }

        if (!isInitialized)
        {
            onSuccess?.Invoke(null, "Error de sistemas no iniciados");
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
                    Debug.Log("Datos cargados correctamenste");
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
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("No hay conexión a internet. No se pueden guardar los datos.");
            return;
        }

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
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke(null, "No hay conexión a internet.");
            return;
        }

        if (!isInitialized)
        {
            onResult?.Invoke(null, "Firebase no está inicializado.");
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

    #endregion

    #region GestionNotificaciones

    // Método para obtener notificaciones de un usuario
    public void GetNotifications(string userId, Action<List<Notificacion>, string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke(null, "No hay conexión a internet.");
            return;
        }

        if (!isInitialized)
        {
            onResult?.Invoke(null, "Firebase no está inicializado.");
            return;
        } 

        dbReference.Child("notificaciones").Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onResult?.Invoke(null, "Error al descargar notificaciones: " + task.Exception);
                return;
            }

            DataSnapshot snapshot = task.Result;
            List<Notificacion> notificaciones = new List<Notificacion>();
            foreach (var child in snapshot.Children)
            {
                string json = child.GetRawJsonValue();
                Notificacion noti = JsonConvert.DeserializeObject<Notificacion>(json);
                if (noti != null)
                {
                    noti.id = child.Key;
                    notificaciones.Add(noti);
                }
            }
            onResult?.Invoke(notificaciones, null);
        });
    }

    // Método para añadir una notificación a un usuario
    public void AddNotification(string userId, Notificacion notificacion, Action<string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("No hay conexión a internet.");
            return;
        }

        if (!isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }

        var notiRef = dbReference.Child("notificaciones").Child(userId).Push();
        notificacion.id = notiRef.Key;
        string json = JsonConvert.SerializeObject(notificacion);
        notiRef.SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onResult?.Invoke("Error al añadir notificación: " + task.Exception);
            }
            else
            {
                onResult?.Invoke(null);
            }
        });
    }

    // Método para marcar notificaciones como leídas
    public void MarkNotificationsAsRead(string userId, List<string> notificationIds, Action<string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("No hay conexión a internet.");
            return;
        }

        if (!isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }
        
        int total = notificationIds.Count;
        int done = 0;
        string errorMsg = null;
        foreach (var id in notificationIds)
        {
            var notiRef = dbReference.Child("notificaciones").Child(userId).Child(id).Child("leido");
            notiRef.SetValueAsync(true).ContinueWithOnMainThread(task =>
            {
                done++;
                if (task.IsFaulted || task.IsCanceled)
                {
                    errorMsg = "Error al marcar como leída: " + task.Exception;
                }
                if (done == total)
                {
                    onResult?.Invoke(errorMsg);
                }
            });
        }
    }

    // Método para borrar notificaciones
    public void DeleteNotifications(string userId, List<string> notificationIds, Action<string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("No hay conexión a internet.");
            return;
        }

        if (!isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }
        
        int total = notificationIds.Count;
        int done = 0;
        string errorMsg = null;
        foreach (var id in notificationIds)
        {
            var notiRef = dbReference.Child("notificaciones").Child(userId).Child(id);
            notiRef.RemoveValueAsync().ContinueWithOnMainThread(task =>
            {
                done++;
                if (task.IsFaulted || task.IsCanceled)
                {
                    errorMsg = "Error al borrar notificación: " + task.Exception;
                }
                if (done == total)
                {
                    onResult?.Invoke(errorMsg);
                }
            });
        }
    }

    // Límite personalizable para notificaciones leídas
    public int maxReadNotifications = 30;

    // Método para limpiar notificaciones leídas dejando solo las 'maxRead' más recientes
    public void CleanupReadNotifications(string userId, Action<string> onResult)
    {
        CleanupReadNotifications(userId, maxReadNotifications, onResult);
    }

    // Método para limpiar notificaciones leídas dejando solo las 'maxRead' más recientes (sobrecarga interna)
    public void CleanupReadNotifications(string userId, int maxRead, Action<string> onResult)
    {
        GetNotifications(userId, (notificaciones, error) =>
        {
            if (!string.IsNullOrEmpty(error))
            {
                onResult?.Invoke(error);
                return;
            }
            var leidas = notificaciones.Where(n => n.leido).OrderByDescending(n => n.timestamp).ToList();
            if (leidas.Count <= maxRead)
            {
                onResult?.Invoke(null);
                return;
            }
            // Eliminar las más antiguas, dejando solo las 'maxRead' más recientes
            var aEliminar = leidas.Skip(maxRead).Select(n => n.id).ToList();
            DeleteNotifications(userId, aEliminar, onResult);
        });
    }

    // Método para obtener la cantidad de notificaciones de un usuario
    public void GetNewNotificationCount(string userId, Action<int, string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke(0, "No hay conexión a internet.");
            return;
        }

        if (!isInitialized)
        {
            onResult?.Invoke(0, "Firebase no está inicializado.");
            return;
        }

        dbReference.Child("notificaciones").Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onResult?.Invoke(0, "Error al contar notificaciones: " + task.Exception);
                return;
            }

            DataSnapshot snapshot = task.Result;
            int count = 0;
            foreach (var child in snapshot.Children)
            {
                string json = child.GetRawJsonValue();
                Notificacion noti = JsonConvert.DeserializeObject<Notificacion>(json);
                if (noti != null && !noti.leido)
                {
                    count++;
                }
            }

            Debug.Log($"Se encontraron {count} notificaciones en el usuario {userId}");

            onResult?.Invoke(count, null);
        });
    }

    #endregion

    #region GestionCasos

    // Método para añadir un ReporteCaso con ID único a Firebase
    public void AddReporteCaso(Caso reporte, Action<string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("No hay conexión a internet.");
            return;
        }

        if (!isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }

        var reporteRef = dbReference.Child("reportes").Push();
        reporte.ID = reporteRef.Key; // Asigna el ID único generado por Firebase
        string json = JsonConvert.SerializeObject(reporte);
        reporteRef.SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onResult?.Invoke("Error al subir el reporte");
            }
            else
            {
                onResult?.Invoke(null); // Éxito
            }
        });
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

    // Obtener un ReporteCaso por ID
    public void BuscarCasoPorID(string reporteId, Action<Caso, string> onResult)
    {
        if (!FirebaseStorageManager.singleton.isInitialized)
        {
            onResult?.Invoke(null, "Firebase no está inicializado.");
            return;
        }
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke(null, "No hay conexión a internet.");
            return;
        }
        var dbRef = typeof(FirebaseStorageManager)
            .GetField("dbReference", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(FirebaseStorageManager.singleton) as DatabaseReference;
        dbRef.Child("reportes").Child(reporteId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onResult?.Invoke(null, "Error al obtener el reporte: " + task.Exception);
            }
            else
            {
                var snapshot = task.Result;
                if (snapshot.Exists)
                {
                    var json = snapshot.GetRawJsonValue();
                    Caso caso = JsonUtility.FromJson<Caso>(json);
                    onResult?.Invoke(caso, null);
                }
                else
                {
                    onResult?.Invoke(null, "No se encontró el reporte con el ID especificado.");
                }
            }
        });
    }

    #endregion
}

[System.Serializable]
public class Data
{
    public string versionDatosUsuario;
    public string nombreCompleto;
    public string tipoDocumento;
    public string numeroDocumento;
    public string numeroCelular;
    public string sexo;
    public bool correoAutenticado;
    public bool verificado;
    public string fechaNacimiento;
    public string nacionalidad;
    public string departamento;
    public string direccion;
    public string email;
    public string contrasena;
    public bool[] respuestasViolentometro;
}

public class Notificacion
{
    public string id;
    public string titulo;
    public string mensaje;
    public int ID_Icono;
    public long timestamp;
    public bool leido;
}

[SerializeField]
public class Caso
{
    public string ID;
    public string nombreCompleto;
    public string tipoDocumento;
    public string numeroDocumento;
    public string numeroCelular;
    public string fechaNacimiento;
    public string sexo;
    public string direccion;
    public string hechoAReportar;

    public int[] tipoViolencia;

    public int tipoAvance;
    public string descripcionDeAvance;
    public int estadoDelCaso;
    public string fechaCaso;
}