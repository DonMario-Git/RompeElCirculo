using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UtilidadesLaEME;

/// <summary>
/// Gestiona la autenticación de Firebase y las operaciones de la base de datos en tiempo real.
/// Hereda de Singleton para garantizar una única instancia persistente.
/// </summary>
public class FirebaseStorageManager : Singleton<FirebaseStorageManager>
{
    // -------------------------------------------------------------------------
    // Campos
    // -------------------------------------------------------------------------

    private FirebaseAuth _auth;
    private FirebaseDatabase _database;
    private DatabaseReference _dbReference;
    private FirebaseUser _currentUser;

    private bool _isInitialized = false;

    // Número máximo de reintentos de inicialización de Firebase
    private const int MAX_INIT_RETRIES = 1;

    // Tiempo límite en segundos para la comprobación de conectividad
    private const float CONNECTIVITY_TIMEOUT = 3f;

    // URL usada para verificar conectividad real a internet
    private const string CONNECTIVITY_CHECK_URL = "https://www.google.com";

    // Prefijo de log para filtrar fácilmente en la consola de Unity
    private const string LOG_TAG = "[FirebaseManager]";

    // Cooldown entre llamadas a la base de datos
    private float _lastFirebaseCallTime = -5f;
    private const float FIREBASE_CALL_COOLDOWN = 5f;

    // -------------------------------------------------------------------------
    // Propiedades
    // -------------------------------------------------------------------------

    /// <summary>Usuario de Firebase actualmente autenticado.</summary>
    public FirebaseUser CurrentUser => _currentUser;

    /// <summary>Devuelve el UID del usuario actual, o null si no hay sesión iniciada.</summary>
    public string UserID => _currentUser?.UserId;

    /// <summary>Indica si Firebase se ha inicializado correctamente.</summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>Límite de notificaciones leídas que se conservan al limpiar.</summary>
    public int maxReadNotifications = 30;

    /// <summary>
    /// Activa los logs internos en la consola de Unity.
    /// Desactivar en producción para evitar ruido.
    /// </summary>
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // -------------------------------------------------------------------------
    // Ciclo de vida de Unity
    // -------------------------------------------------------------------------

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await InitializeFirebase();
    }

    public UnityEvent OnInitialize;

    private async Task InitializeFirebase(int retryCount = 0)
    {
        Log($"Inicializando Firebase... (intento {retryCount + 1}/{MAX_INIT_RETRIES + 1})");

        bool hasInternet = await IsInternetAvailable();
        if (!hasInternet)
        {
            if (retryCount < MAX_INIT_RETRIES)
            {
                LogWarning("Sin conexión al inicializar. Reintentando...");
                await InitializeFirebase(retryCount + 1);
                return;
            }

            _isInitialized = false;
            OnInitialize?.Invoke();
            LogError("Sin conexión a internet tras varios intentos. Inicialización cancelada.");
            return;
        }

        await FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                _auth = FirebaseAuth.DefaultInstance;
                _database = FirebaseDatabase.DefaultInstance;
                _database.SetPersistenceEnabled(false);
                _dbReference = _database.RootReference;
                _isInitialized = true;

                // Suscribirse a los cambios de estado de autenticación
                _auth.StateChanged += OnAuthStateChanged;

                // Restaurar sesión si ya había un usuario autenticado
                _currentUser = _auth.CurrentUser;

                OnInitialize?.Invoke();
                Log("Firebase inicializado correctamente.");

                if (_currentUser != null)
                    Log($"Sesión restaurada para el usuario: {_currentUser.Email} (UID: {_currentUser.UserId})");
                else
                    Log("No se encontró ninguna sesión previa.");
            }
            else
            {
                if (retryCount < MAX_INIT_RETRIES)
                {
                    LogWarning($"Fallo en la inicialización (intento {retryCount + 1}). Reintentando...");
                    _ = InitializeFirebase(retryCount + 1);
                }
                else
                {
                    _isInitialized = false;
                    OnInitialize?.Invoke();
                    LogError($"Error al inicializar Firebase tras {MAX_INIT_RETRIES + 1} intentos. Estado: {task.Result}");
                }
            }
        });
    }

    /// <summary>
    /// Intenta reinicializar Firebase si no está inicializado.
    /// Devuelve true si tras el intento Firebase queda inicializado.
    /// </summary>
    private async Task<bool> TryReinitializeAsync()
    {
        Log("Intentando reinicializar Firebase...");

        bool hasInternet = await IsInternetAvailable();
        if (!hasInternet)
        {
            LogWarning("Sin conexión durante la reinicialización.");
            return false;
        }

        var tcs = new TaskCompletionSource<bool>();

        await FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                _auth = FirebaseAuth.DefaultInstance;
                _database = FirebaseDatabase.DefaultInstance;
                _database.SetPersistenceEnabled(false);
                _dbReference = _database.RootReference;
                _isInitialized = true;

                if (_auth != null)
                    _auth.StateChanged -= OnAuthStateChanged;
                _auth.StateChanged += OnAuthStateChanged;

                _currentUser = _auth.CurrentUser;
                Log("Reinicialización de Firebase exitosa.");
                tcs.SetResult(true);
            }
            else
            {
                _isInitialized = false;
                LogError($"Reinicialización fallida. Estado: {task.Result}");
                tcs.SetResult(false);
            }
        });

        return await tcs.Task;
    }

    private void OnAuthStateChanged(object sender, EventArgs e)
    {
        _currentUser = _auth.CurrentUser;

        if (_currentUser != null)
            Log($"Estado de autenticación cambiado — usuario conectado: {_currentUser.Email}");
        else
            Log("Estado de autenticación cambiado — usuario desconectado.");
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // Al volver a la app, reanudar el polling si estaba activo
        if (hasFocus && _pendingPollingCallback != null &&
            (_pollingCancellationToken == null || _pollingCancellationToken.IsCancellationRequested))
        {
            Log("App recuperada — reiniciando polling de verificación automáticamente.");
            ResumeEmailVerificationPolling();
        }
    }

    private void OnDestroy()
    {
        if (_auth != null)
            _auth.StateChanged -= OnAuthStateChanged;

        _pollingCancellationToken?.Cancel();
        _pollingCancellationToken?.Dispose();

        Log("FirebaseManager destruido, listener de autenticación desuscrito.");
    }

    // =========================================================================
    // SECCIÓN: Conectividad a Internet
    // =========================================================================

    /// <summary>
    /// Comprueba la conectividad real a internet enviando una petición HEAD a Google.
    /// Si no hay respuesta en el tiempo límite, devuelve false.
    /// </summary>
    public static async Task<bool> IsInternetAvailable()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
            return false;

        try
        {
            using UnityWebRequest request = UnityWebRequest.Head(CONNECTIVITY_CHECK_URL);
            request.timeout = (int)CONNECTIVITY_TIMEOUT;

            var operation = request.SendWebRequest();

            float elapsed = 0f;
            while (!operation.isDone)
            {
                await Task.Delay(100);
                elapsed += 0.1f;
                if (elapsed >= CONNECTIVITY_TIMEOUT)
                    return false;
            }

            return request.result == UnityWebRequest.Result.Success;
        }
        catch
        {
            return false;
        }
    }

    // =========================================================================
    // SECCIÓN: Cooldown de llamadas
    // =========================================================================

    /// <summary>
    /// Comprueba si ha pasado el tiempo de cooldown entre llamadas a Firebase.
    /// </summary>
    private bool CanCallFirebase()
    {
        if (Time.time - _lastFirebaseCallTime < FIREBASE_CALL_COOLDOWN)
            return false;
        _lastFirebaseCallTime = Time.time;
        return true;
    }

    // =========================================================================
    // SECCIÓN: Base de datos — Guardar, Cargar y Eliminar
    // =========================================================================

    /// <summary>
    /// Guarda un objeto genérico en la rama de la base de datos indicada.
    /// </summary>
    public async void SaveData<T>(string branch, T data, Action<bool, string> callback, bool respetarCooldown = true, bool COMPRIMIR_BASE64 = false)
    {
        Log($"SaveData llamado — rama: '{branch}', tipo: {typeof(T).Name}");

        if (!await IsInternetAvailable())
        {
            LogWarning("SaveData cancelado — sin conexión a internet.");
            callback?.Invoke(true, "Sin conexión a internet.");
            return;
        }

        if (respetarCooldown && !CanCallFirebase())
        {
            LogWarning("SaveData cancelado — cooldown activo.");
            callback?.Invoke(true, "Debes esperar 5 segundos entre operaciones.");
            return;
        }

        if (!_isInitialized)
        {
            LogWarning("SaveData — Firebase no inicializado, reintentando...");
            bool reinitialized = await TryReinitializeAsync();
            if (!reinitialized)
            {
                callback?.Invoke(true, "Firebase no está inicializado y no se pudo reinicializar.");
                return;
            }
        }

        if (string.IsNullOrEmpty(branch))
        {
            LogWarning("SaveData cancelado — la rama es nula o vacía.");
            callback?.Invoke(true, "La rama de la base de datos no puede ser nula o vacía.");
            return;
        }

        try
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            string json = JsonConvert.SerializeObject(data, settings);
            string jsonFinal = json;

            if (COMPRIMIR_BASE64)
            {
                DataFireBase64 fireData = new DataFireBase64 { datosBase64 = Utilities.ToBase64(json) };
                jsonFinal = JsonConvert.SerializeObject(fireData);
            }

            Log($"SaveData JSON serializado: {json}");

            DatabaseReference reference = _database.GetReference(branch);
            await reference.SetRawJsonValueAsync(jsonFinal);

            string msg = $"Datos guardados correctamente en '{branch}'.";
            Log(msg);
            callback?.Invoke(false, msg);
        }
        catch (Exception ex)
        {
            string msg = $"Error al guardar datos: {ex.Message}";
            LogError(msg);
            callback?.Invoke(true, msg);
        }
    }

    /// <summary>
    /// Carga y deserializa datos genéricos desde la rama de la base de datos indicada.
    /// </summary>
    public async void LoadData<T>(string branch, Action<bool, string, T> callback, bool respetarCooldown = true, bool DESCOMPRIMIR_BASE64 = false)
    {
        Log($"LoadData llamado — rama: '{branch}', tipo: {typeof(T).Name}");

        if (!await IsInternetAvailable())
        {
            LogWarning("LoadData cancelado — sin conexión a internet.");
            callback?.Invoke(true, "Sin conexión a internet.", default);
            return;
        }

        if (respetarCooldown && !CanCallFirebase())
        {
            LogWarning("LoadData cancelado — cooldown activo.");
            callback?.Invoke(true, "Debes esperar 5 segundos entre operaciones.", default);
            return;
        }

        if (!_isInitialized)
        {
            LogWarning("LoadData — Firebase no inicializado, reintentando...");
            bool reinitialized = await TryReinitializeAsync();
            if (!reinitialized)
            {
                callback?.Invoke(true, "Firebase no está inicializado y no se pudo reinicializar.", default);
                return;
            }
        }

        if (string.IsNullOrEmpty(branch))
        {
            LogWarning("LoadData cancelado — la rama es nula o vacía.");
            callback?.Invoke(true, "La rama de la base de datos no puede ser nula o vacía.", default);
            return;
        }

        try
        {
            DatabaseReference reference = _database.GetReference(branch);
            DataSnapshot snapshot = await reference.GetValueAsync();

            if (!snapshot.Exists || snapshot.Value == null)
            {
                string notFound = $"No se encontraron datos en la rama '{branch}'.";
                LogWarning(notFound);
                callback?.Invoke(true, notFound, default);
                return;
            }

            string json = snapshot.GetRawJsonValue();

            if (string.IsNullOrEmpty(json) || json == "null")
            {
                string notFound = $"No se encontraron datos en la rama '{branch}'.";
                LogWarning($"LoadData — JSON nulo en '{branch}'.");
                callback?.Invoke(true, notFound, default);
                return;
            }

            Log($"LoadData JSON recibido: {json}");

            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            T result;

            if (DESCOMPRIMIR_BASE64)
            {
                DataFireBase64 fireData = JsonConvert.DeserializeObject<DataFireBase64>(json, settings);
                string jsonDescomprimido = Utilities.FromBase64(fireData.datosBase64);
                result = JsonConvert.DeserializeObject<T>(jsonDescomprimido, settings);
            }
            else
            {
                result = JsonConvert.DeserializeObject<T>(json, settings);
            }

            string msg = $"Datos cargados correctamente desde '{branch}'.";
            Log(msg);
            callback?.Invoke(false, msg, result);
        }
        catch (Exception ex)
        {
            string msg = $"Error al cargar datos: {ex.Message}";
            LogError(msg);
            callback?.Invoke(true, msg, default);
        }
    }

    /// <summary>
    /// Elimina los datos de la rama de la base de datos indicada.
    /// </summary>
    public async void DeleteData(string branch, Action<bool, string> callback)
    {
        Log($"DeleteData llamado — rama: '{branch}'");

        if (!await IsInternetAvailable())
        {
            LogWarning("DeleteData cancelado — sin conexión a internet.");
            callback?.Invoke(true, "Sin conexión a internet.");
            return;
        }

        if (!_isInitialized)
        {
            LogWarning("DeleteData — Firebase no inicializado, reintentando...");
            bool reinitialized = await TryReinitializeAsync();
            if (!reinitialized)
            {
                callback?.Invoke(true, "Firebase no está inicializado y no se pudo reinicializar.");
                return;
            }
        }

        if (string.IsNullOrEmpty(branch))
        {
            LogWarning("DeleteData cancelado — la rama es nula o vacía.");
            callback?.Invoke(true, "La rama de la base de datos no puede ser nula o vacía.");
            return;
        }

        try
        {
            DatabaseReference reference = _database.GetReference(branch);
            await reference.RemoveValueAsync();

            string msg = $"Datos en '{branch}' eliminados correctamente.";
            Log(msg);
            callback?.Invoke(false, msg);
        }
        catch (Exception ex)
        {
            string msg = $"Error al eliminar datos: {ex.Message}";
            LogError(msg);
            callback?.Invoke(true, msg);
        }
    }

    // =========================================================================
    // SECCIÓN: Base de datos — Usuarios (Data)
    // =========================================================================

    /// <summary>
    /// Carga los datos de un usuario específico por su UID.
    /// </summary>
    public void LoadUsuario(string userId, Action<Data, string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke(null, "Sin conexión a internet.");
            return;
        }

        if (!CanCallFirebase())
        {
            onResult?.Invoke(null, "Debes esperar 5 segundos entre operaciones.");
            return;
        }

        if (!_isInitialized)
        {
            onResult?.Invoke(null, "Firebase no está inicializado.");
            return;
        }

        var userRef = _dbReference.Child("usuarios").Child(userId);
        userRef.KeepSynced(false);

        userRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string err = "Error al cargar datos del usuario.";
                LogError(err);
                onResult?.Invoke(null, err);
                return;
            }

            if (!task.Result.Exists)
            {
                string err = "No se encontraron datos para este usuario.";
                LogWarning(err);
                onResult?.Invoke(null, err);
                return;
            }

            string json = task.Result.GetRawJsonValue();
            Data data = JsonConvert.DeserializeObject<Data>(json);
            Log($"Usuario cargado correctamente: {userId}");
            onResult?.Invoke(data, null);
        });
    }

    /// <summary>
    /// Guarda los datos de un usuario. Si overwrite es false, comprueba duplicados de email y nombre.
    /// </summary>
    public async Task SaveUsuario(Data data, string userId, bool overwrite, Action<string> onResult, bool respetarCooldown = true)
    {
        data.nombreCompleto = data.nombreCompleto.TrimEdges();

        if (!await IsInternetAvailable())
        {
            onResult?.Invoke("Sin conexión a internet.");
            return;
        }

        if (respetarCooldown && !CanCallFirebase())
        {
            onResult?.Invoke("Debes esperar 5 segundos entre operaciones.");
            return;
        }

        if (!_isInitialized)
        {
            bool reinitialized = await TryReinitializeAsync();
            if (!reinitialized)
            {
                onResult?.Invoke("Firebase no está inicializado y no se pudo reinicializar.");
                return;
            }
        }

        if (!overwrite)
        {
            var snapshotTask = _dbReference.Child("usuarios").GetValueAsync();
            await snapshotTask;

            if (snapshotTask.IsFaulted || snapshotTask.IsCanceled)
            {
                onResult?.Invoke("Error al consultar usuarios: " + snapshotTask.Exception);
                return;
            }

            foreach (var child in snapshotTask.Result.Children)
            {
                Data existing = JsonConvert.DeserializeObject<Data>(child.GetRawJsonValue());
                if (existing != null &&
                    (existing.email == data.email || existing.nombreCompleto == data.nombreCompleto))
                {
                    onResult?.Invoke("Ya existe un usuario con ese correo o nombre.");
                    return;
                }
            }
        }

        string json = JsonConvert.SerializeObject(data);
        var saveTask = _dbReference.Child("usuarios").Child(userId).SetRawJsonValueAsync(json);
        await saveTask;

        if (saveTask.IsFaulted || saveTask.IsCanceled)
        {
            string err = "Error al guardar usuario: " + saveTask.Exception;
            LogError(err);
            onResult?.Invoke(err);
        }
        else
        {
            Log($"Usuario guardado correctamente: {data.nombreCompleto}");
            onResult?.Invoke(null);
        }
    }

    /// <summary>
    /// Devuelve la lista de todos los usuarios registrados.
    /// </summary>
    public void GetAllUsuarios(Action<List<Data>, string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke(null, "Sin conexión a internet.");
            return;
        }

        if (!_isInitialized)
        {
            onResult?.Invoke(null, "Firebase no está inicializado.");
            return;
        }

        _dbReference.Child("usuarios").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onResult?.Invoke(null, "Error al obtener usuarios: " + task.Exception);
                return;
            }

            List<Data> usuarios = new List<Data>();
            foreach (var child in task.Result.Children)
            {
                Data usuario = JsonConvert.DeserializeObject<Data>(child.GetRawJsonValue());
                if (usuario != null) usuarios.Add(usuario);
            }

            Log($"Usuarios obtenidos: {usuarios.Count}");
            onResult?.Invoke(usuarios, null);
        });
    }

    // =========================================================================
    // SECCIÓN: Base de datos — Casos
    // =========================================================================

    /// <summary>
    /// Añade un nuevo caso con ID único generado por Firebase.
    /// </summary>
    public void AddCaso(Caso caso, Action<string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("Sin conexión a internet.");
            return;
        }

        if (!_isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }

        var casoRef = _dbReference.Child("reportes").Push();
        caso.ID = casoRef.Key;
        string json = JsonConvert.SerializeObject(caso);

        casoRef.SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string err = "Error al añadir caso: " + task.Exception;
                LogError(err);
                onResult?.Invoke(err);
            }
            else
            {
                Log($"Caso añadido correctamente: {caso.ID}");
                onResult?.Invoke(null);
            }
        });
    }

    /// <summary>
    /// Elimina un caso por su ID.
    /// </summary>
    public void EliminarCaso(string casoId, Action<string> onResult)
    {
        if (!_isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("Sin conexión a internet.");
            return;
        }

        _dbReference.Child("reportes").Child(casoId).RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string err = "Error al eliminar caso: " + task.Exception;
                LogError(err);
                onResult?.Invoke(err);
            }
            else
            {
                Log($"Caso eliminado correctamente: {casoId}");
                onResult?.Invoke(null);
            }
        });
    }

    /// <summary>
    /// Edita un caso existente por su ID.
    /// </summary>
    public void EditarCaso(string casoId, Caso nuevoCaso, Action<string> onResult)
    {
        if (!_isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("Sin conexión a internet.");
            return;
        }

        string json = JsonConvert.SerializeObject(nuevoCaso);
        _dbReference.Child("reportes").Child(casoId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string err = "Error al editar caso: " + task.Exception;
                LogError(err);
                onResult?.Invoke(err);
            }
            else
            {
                Log($"Caso editado correctamente: {casoId}");
                onResult?.Invoke(null);
            }
        });
    }

    /// <summary>
    /// Busca un caso por su ID.
    /// </summary>
    public void BuscarCasoPorID(string casoId, Action<Caso, string> onResult)
    {
        if (!_isInitialized)
        {
            onResult?.Invoke(null, "Firebase no está inicializado.");
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke(null, "Sin conexión a internet.");
            return;
        }

        _dbReference.Child("reportes").Child(casoId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string err = "Error al buscar caso: " + task.Exception;
                LogError(err);
                onResult?.Invoke(null, err);
                return;
            }

            if (!task.Result.Exists)
            {
                onResult?.Invoke(null, "No se encontró ningún caso con ese ID.");
                return;
            }

            Caso caso = JsonConvert.DeserializeObject<Caso>(task.Result.GetRawJsonValue());
            Log($"Caso encontrado: {casoId}");
            onResult?.Invoke(caso, null);
        });
    }

    // =========================================================================
    // SECCIÓN: Base de datos — Notificaciones
    // =========================================================================

    /// <summary>
    /// Obtiene las notificaciones de un usuario.
    /// </summary>
    public void GetNotificaciones(string userId, Action<List<Notificacion>, string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke(null, "Sin conexión a internet.");
            return;
        }

        if (!_isInitialized)
        {
            onResult?.Invoke(null, "Firebase no está inicializado.");
            return;
        }

        _dbReference.Child("notificaciones").Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onResult?.Invoke(null, "Error al obtener notificaciones: " + task.Exception);
                return;
            }

            List<Notificacion> notificaciones = new List<Notificacion>();
            foreach (var child in task.Result.Children)
            {
                Notificacion noti = JsonConvert.DeserializeObject<Notificacion>(child.GetRawJsonValue());
                if (noti != null)
                {
                    noti.id = child.Key;
                    notificaciones.Add(noti);
                }
            }

            Log($"Notificaciones obtenidas para {userId}: {notificaciones.Count}");
            onResult?.Invoke(notificaciones, null);
        });
    }

    /// <summary>
    /// Añade una notificación a un usuario.
    /// </summary>
    public void AddNotificacion(string userId, Notificacion notificacion, Action<string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("Sin conexión a internet.");
            return;
        }

        if (!_isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }

        var notiRef = _dbReference.Child("notificaciones").Child(userId).Push();
        notificacion.id = notiRef.Key;
        string json = JsonConvert.SerializeObject(notificacion);

        notiRef.SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string err = "Error al añadir notificación: " + task.Exception;
                LogError(err);
                onResult?.Invoke(err);
            }
            else
            {
                Log($"Notificación añadida a {userId}.");
                onResult?.Invoke(null);
            }
        });
    }

    /// <summary>
    /// Marca una lista de notificaciones como leídas.
    /// </summary>
    public void MarcarNotificacionesLeidas(string userId, List<string> ids, Action<string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("Sin conexión a internet.");
            return;
        }

        if (!_isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }

        int total = ids.Count;
        int done = 0;
        string errorMsg = null;

        foreach (var id in ids)
        {
            _dbReference.Child("notificaciones").Child(userId).Child(id).Child("leido")
                .SetValueAsync(true).ContinueWithOnMainThread(task =>
                {
                    done++;
                    if (task.IsFaulted || task.IsCanceled)
                        errorMsg = "Error al marcar notificación como leída: " + task.Exception;
                    if (done == total)
                    {
                        if (errorMsg == null) Log($"Notificaciones marcadas como leídas: {userId}");
                        else LogError(errorMsg);
                        onResult?.Invoke(errorMsg);
                    }
                });
        }
    }

    /// <summary>
    /// Elimina una lista de notificaciones por sus IDs.
    /// </summary>
    public void EliminarNotificaciones(string userId, List<string> ids, Action<string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("Sin conexión a internet.");
            return;
        }

        if (!_isInitialized)
        {
            onResult?.Invoke("Firebase no está inicializado.");
            return;
        }

        int total = ids.Count;
        int done = 0;
        string errorMsg = null;

        foreach (var id in ids)
        {
            _dbReference.Child("notificaciones").Child(userId).Child(id)
                .RemoveValueAsync().ContinueWithOnMainThread(task =>
                {
                    done++;
                    if (task.IsFaulted || task.IsCanceled)
                        errorMsg = "Error al eliminar notificación: " + task.Exception;
                    if (done == total)
                    {
                        if (errorMsg == null) Log($"Notificaciones eliminadas: {userId}");
                        else LogError(errorMsg);
                        onResult?.Invoke(errorMsg);
                    }
                });
        }
    }

    /// <summary>
    /// Limpia las notificaciones leídas, conservando solo las más recientes según maxReadNotifications.
    /// </summary>
    public void LimpiarNotificacionesLeidas(string userId, Action<string> onResult)
    {
        GetNotificaciones(userId, (notificaciones, error) =>
        {
            if (!string.IsNullOrEmpty(error))
            {
                onResult?.Invoke(error);
                return;
            }

            var leidas = notificaciones.Where(n => n.leido).OrderByDescending(n => n.timestamp).ToList();
            if (leidas.Count <= maxReadNotifications)
            {
                onResult?.Invoke(null);
                return;
            }

            var aEliminar = leidas.Skip(maxReadNotifications).Select(n => n.id).ToList();
            EliminarNotificaciones(userId, aEliminar, onResult);
        });
    }

    /// <summary>
    /// Devuelve el número de notificaciones no leídas de un usuario.
    /// </summary>
    public void GetContadorNotificaciones(string userId, Action<int, string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke(0, "Sin conexión a internet.");
            return;
        }

        if (!_isInitialized)
        {
            onResult?.Invoke(0, "Firebase no está inicializado.");
            return;
        }

        _dbReference.Child("notificaciones").Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onResult?.Invoke(0, "Error al contar notificaciones: " + task.Exception);
                return;
            }

            int count = 0;
            foreach (var child in task.Result.Children)
            {
                Notificacion noti = JsonConvert.DeserializeObject<Notificacion>(child.GetRawJsonValue());
                if (noti != null && !noti.leido) count++;
            }

            Log($"Notificaciones no leídas de {userId}: {count}");
            onResult?.Invoke(count, null);
        });
    }

    // =========================================================================
    // SECCIÓN: Autenticación
    // =========================================================================

    /// <summary>
    /// Crea una nueva cuenta con email y contraseña.
    /// </summary>
    public async void CreateAccount(string email, string password, Action<bool, string> callback)
    {
        Log($"CreateAccount llamado — email: {email}");

        if (!await IsInternetAvailable())
        {
            LogWarning("CreateAccount cancelado — sin conexión a internet.");
            callback?.Invoke(true, "Sin conexión a internet.");
            return;
        }

        if (!_isInitialized)
        {
            LogWarning("CreateAccount — Firebase no inicializado, reintentando...");
            bool reinitialized = await TryReinitializeAsync();
            if (!reinitialized)
            {
                callback?.Invoke(true, "Error al conectar con sistema.");
                return;
            }
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            LogWarning("CreateAccount cancelado — email o contraseña vacíos.");
            callback?.Invoke(true, "El email y la contraseña no pueden estar vacíos.");
            return;
        }

        try
        {
            AuthResult result = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
            _currentUser = result.User;

            string msg = $"Cuenta creada correctamente para {_currentUser.Email}.";
            Log(msg);
            callback?.Invoke(false, msg);
        }
        catch (FirebaseException ex)
        {
            AuthError errorCode = (AuthError)ex.ErrorCode;
            string detalle = errorCode switch
            {
                AuthError.InvalidEmail => "El formato del email no es válido",
                AuthError.EmailAlreadyInUse => "Este email ya está registrado",
                AuthError.WeakPassword => "La contraseña es demasiado débil. Minimo 6 caracteres y evitar patrones obvios",
                AuthError.NetworkRequestFailed => "No se pudo conectar con el servidor. Revisa tu conexión a internet e inténtalo de nuevo.",
                AuthError.TooManyRequests => "Se han realizado demasiados intentos seguidos. Espera unos minutos antes de volver a intentarlo.",
                AuthError.OperationNotAllowed => "El registro con email y contraseña no está habilitado en este momento. Contacta con el soporte.",
                AuthError.InvalidCredential => "Las credenciales proporcionadas no son válidas. Verifica el email y la contraseña.",
                _ => $"Error inesperado (código {ex.ErrorCode}): {ex.Message}"
            };

            string msg = $"Error al crear cuenta: {detalle}";
            LogError(msg);
            callback?.Invoke(true, msg);
        }
        catch (Exception ex)
        {
            string msg = $"Error al crear la cuenta: {ex.Message}";
            LogError(msg);
            callback?.Invoke(true, msg);
        }
    }

    /// <summary>
    /// Inicia sesión con email y contraseña.
    /// </summary>
    public async void Login(string email, string password, Action<bool, string> callback)
    {
        Log($"Login llamado — email: {email}");

        if (!await IsInternetAvailable())
        {
            LogWarning("Login cancelado — sin conexión a internet.");
            callback?.Invoke(true, "Sin conexión a internet.");
            return;
        }

        if (!_isInitialized)
        {
            LogWarning("Login — Firebase no inicializado, reintentando...");
            bool reinitialized = await TryReinitializeAsync();
            if (!reinitialized)
            {
                callback?.Invoke(true, "Firebase no está inicializado y no se pudo reinicializar.");
                return;
            }
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            LogWarning("Login cancelado — email o contraseña vacíos.");
            callback?.Invoke(true, "El email y la contraseña no pueden estar vacíos.");
            return;
        }

        try
        {
            AuthResult result = await _auth.SignInWithEmailAndPasswordAsync(email, password);
            _currentUser = result.User;

            string msg = $"Sesión iniciada correctamente. Bienvenido, {_currentUser.Email}.";
            Log(msg);
            callback?.Invoke(false, msg);
        }
        catch (FirebaseException ex)
        {
            string msg = $"{GetAuthErrorMessage(ex)}";
            LogError(msg);
            callback?.Invoke(true, msg);
        }
        catch (Exception ex)
        {
            string msg = "Error al iniciar sesión. Verifica tu conexión e intenta de nuevo.";
            LogError($"Excepción inesperada en Login: {ex}");
            callback?.Invoke(true, msg);
        }
    }



    /// <summary>
    /// Cierra la sesión del usuario actual.
    /// </summary>
    public void Logout(Action<bool, string> callback)
    {
        Log("Logout llamado.");

        if (!_isInitialized)
        {
            LogWarning("Logout cancelado — Firebase no inicializado.");
            callback?.Invoke(true, "Firebase no está inicializado.");
            return;
        }

        if (_currentUser == null)
        {
            LogWarning("Logout cancelado — no hay usuario conectado.");
            callback?.Invoke(true, "No hay ningún usuario con sesión iniciada.");
            return;
        }

        try
        {
            string email = _currentUser.Email;
            _auth.SignOut();
            _currentUser = null;

            string msg = "Sesión cerrada correctamente.";
            Log($"{msg} (era: {email})");
            callback?.Invoke(false, msg);
        }
        catch (Exception ex)
        {
            string msg = $"Error al cerrar sesión: {ex.Message}";
            LogError(msg);
            callback?.Invoke(true, msg);
        }
    }

    /// <summary>
    /// Elimina la cuenta del usuario actualmente autenticado.
    /// </summary>
    public async void DeleteAccount(Action<bool, string> callback)
    {
        Log("DeleteAccount llamado.");

        if (!await IsInternetAvailable())
        {
            LogWarning("DeleteAccount cancelado — sin conexión a internet.");
            callback?.Invoke(true, "Sin conexión a internet.");
            return;
        }

        if (!_isInitialized)
        {
            LogWarning("DeleteAccount — Firebase no inicializado, reintentando...");
            bool reinitialized = await TryReinitializeAsync();
            if (!reinitialized)
            {
                callback?.Invoke(true, "Firebase no está inicializado y no se pudo reinicializar.");
                return;
            }
        }

        if (_currentUser == null)
        {
            LogWarning("DeleteAccount cancelado — no hay usuario conectado.");
            callback?.Invoke(true, "No hay ningún usuario con sesión iniciada.");
            return;
        }

        try
        {
            string email = _currentUser.Email;
            await _currentUser.DeleteAsync();
            _currentUser = null;

            string msg = "Cuenta eliminada correctamente.";
            Log($"{msg} (era: {email})");
            callback?.Invoke(false, msg);
        }
        catch (FirebaseException ex)
        {
            string msg = $"Error al eliminar la cuenta: {GetAuthErrorMessage(ex)}";
            LogError(msg);
            callback?.Invoke(true, msg);
        }
        catch (Exception ex)
        {
            string msg = $"Error al eliminar la cuenta: {ex.Message}";
            LogError(msg);
            callback?.Invoke(true, msg);
        }
    }

    /// <summary>
    /// Cambia la contraseña del usuario actualmente autenticado.
    /// </summary>
    public async void ChangePassword(string newPassword, Action<bool, string> callback)
    {
        Log("ChangePassword llamado.");

        if (!await IsInternetAvailable())
        {
            LogWarning("ChangePassword cancelado — sin conexión a internet.");
            callback?.Invoke(true, "Sin conexión a internet.");
            return;
        }

        if (!_isInitialized)
        {
            LogWarning("ChangePassword — Firebase no inicializado, reintentando...");
            bool reinitialized = await TryReinitializeAsync();
            if (!reinitialized)
            {
                callback?.Invoke(true, "Firebase no está inicializado y no se pudo reinicializar.");
                return;
            }
        }

        if (_currentUser == null)
        {
            LogWarning("ChangePassword cancelado — no hay usuario conectado.");
            callback?.Invoke(true, "No hay ningún usuario con sesión iniciada.");
            return;
        }

        if (string.IsNullOrEmpty(newPassword))
        {
            LogWarning("ChangePassword cancelado — contraseña vacía.");
            callback?.Invoke(true, "La nueva contraseña no puede estar vacía.");
            return;
        }

        try
        {
            await _currentUser.UpdatePasswordAsync(newPassword);

            string msg = "Contraseña cambiada correctamente.";
            Log(msg);
            callback?.Invoke(false, msg);
        }
        catch (FirebaseException ex)
        {
            string msg = $"Error al cambiar la contraseña: {GetAuthErrorMessage(ex)}";
            LogError(msg);
            callback?.Invoke(true, msg);
        }
        catch (Exception ex)
        {
            string msg = $"Error al cambiar la contraseña: {ex.Message}";
            LogError(msg);
            callback?.Invoke(true, msg);
        }
    }

    /// <summary>
    /// Envía un correo de verificación al usuario actualmente autenticado.
    /// </summary>
    public async void SendVerificationEmail(Action<bool, string> callback)
    {
        Log("SendVerificationEmail llamado.");

        if (!await IsInternetAvailable())
        {
            LogWarning("SendVerificationEmail cancelado — sin conexión a internet.");
            callback?.Invoke(false, "Sin conexión a internet.");
            return;
        }

        if (!_isInitialized)
        {
            LogWarning("SendVerificationEmail — Firebase no inicializado, reintentando...");
            bool reinitialized = await TryReinitializeAsync();
            if (!reinitialized)
            {
                callback?.Invoke(false, "Firebase no está inicializado y no se pudo reinicializar.");
                return;
            }
        }

        if (_currentUser == null)
        {
            LogWarning("SendVerificationEmail cancelado — no hay usuario conectado.");
            callback?.Invoke(false, "No hay ningún usuario con sesión iniciada.");
            return;
        }

        if (_currentUser.IsEmailVerified)
        {
            Log("SendVerificationEmail omitido — la cuenta ya está verificada.");
            callback?.Invoke(false, "Esta cuenta ya está verificada.");
            return;
        }

        try
        {
            await _currentUser.SendEmailVerificationAsync();

            string msg = $"Correo de verificación enviado a {_currentUser.Email}.";
            Log(msg);
            callback?.Invoke(true, msg);
        }
        catch (FirebaseException ex)
        {
            string msg = $"Error al enviar el correo de verificación: {GetAuthErrorMessage(ex)}";
            LogError(msg);
            callback?.Invoke(false, msg);
        }
        catch (Exception ex)
        {
            string msg = $"Error al enviar el correo de verificación: {ex.Message}";
            LogError(msg);
            callback?.Invoke(false, msg);
        }
    }

    /// <summary>
    /// Devuelve si el email está verificado según el estado local en caché.
    /// </summary>
    public void IsEmailVerified(Action<bool, string, bool> callback)
    {
        Log("IsEmailVerified llamado.");

        if (!_isInitialized)
        {
            LogWarning("IsEmailVerified cancelado — Firebase no inicializado.");
            callback?.Invoke(true, "Firebase no está inicializado.", false);
            return;
        }

        if (_currentUser == null)
        {
            LogWarning("IsEmailVerified cancelado — no hay usuario conectado.");
            callback?.Invoke(true, "No hay ningún usuario con sesión iniciada.", false);
            return;
        }

        bool verified = _currentUser.IsEmailVerified;
        string msg = verified
            ? $"El email '{_currentUser.Email}' está verificado."
            : $"El email '{_currentUser.Email}' aún no está verificado.";

        Log($"IsEmailVerified resultado: {verified}");
        callback?.Invoke(false, msg, verified);
    }

    /// <summary>
    /// Recarga el perfil desde Firebase y comprueba si el email ha sido verificado.
    /// </summary>
    public async void ReloadAndCheckEmailVerified(Action<bool, string> callback)
    {
        Log("ReloadAndCheckEmailVerified llamado.");

        if (!await IsInternetAvailable())
        {
            LogWarning("ReloadAndCheckEmailVerified cancelado — sin conexión.");
            callback?.Invoke(false, "Sin conexión a internet.");
            return;
        }

        if (!_isInitialized)
        {
            LogWarning("ReloadAndCheckEmailVerified — Firebase no inicializado, reintentando...");
            bool reinitialized = await TryReinitializeAsync();
            if (!reinitialized)
            {
                callback?.Invoke(false, "Firebase no está inicializado y no se pudo reinicializar.");
                return;
            }
        }

        if (_currentUser == null)
        {
            LogWarning("ReloadAndCheckEmailVerified cancelado — no hay usuario conectado.");
            callback?.Invoke(false, "No hay ningún usuario con sesión iniciada.");
            return;
        }

        try
        {
            await _currentUser.ReloadAsync();

            bool verified = _currentUser.IsEmailVerified;
            string msg = verified
                ? $"El email '{_currentUser.Email}' ha sido verificado correctamente."
                : $"El email '{_currentUser.Email}' aún no está verificado. Por favor revisa tu bandeja de entrada.";

            Log($"ReloadAndCheckEmailVerified resultado: {verified}");
            callback?.Invoke(verified, null);
        }
        catch (FirebaseException ex)
        {
            string msg = $"Error al recargar el usuario: {GetAuthErrorMessage(ex)}";
            LogError(msg);
            callback?.Invoke(false, msg);
        }
        catch (Exception ex)
        {
            string msg = $"Error al recargar el usuario: {ex.Message}";
            LogError(msg);
            callback?.Invoke(false, msg);
        }
    }

    /// <summary>
    /// Envía un correo de restablecimiento de contraseña.
    /// </summary>
    public async void SendPasswordResetEmail(string email, Action<bool, string> callback)
    {
        Log($"SendPasswordResetEmail llamado — email: {email}");

        if (!await IsInternetAvailable())
        {
            LogWarning("SendPasswordResetEmail cancelado — sin conexión.");
            callback?.Invoke(true, "Sin conexión a internet.");
            return;
        }

        if (!_isInitialized)
        {
            LogWarning("SendPasswordResetEmail — Firebase no inicializado, reintentando...");
            bool reinitialized = await TryReinitializeAsync();
            if (!reinitialized)
            {
                callback?.Invoke(true, "Firebase no está inicializado y no se pudo reinicializar.");
                return;
            }
        }

        if (string.IsNullOrEmpty(email))
        {
            LogWarning("SendPasswordResetEmail cancelado — email vacío.");
            callback?.Invoke(true, "El email no puede estar vacío.");
            return;
        }

        try
        {
            await _auth.SendPasswordResetEmailAsync(email);

            string msg = $"Correo de restablecimiento enviado a {email}.";
            Log(msg);
            callback?.Invoke(false, null);
        }
        catch (FirebaseException ex)
        {
            string msg = $"Error al enviar el correo de restablecimiento: {GetAuthErrorMessage(ex)}";
            LogError(msg);
            callback?.Invoke(true, msg);
        }
        catch (Exception ex)
        {
            string msg = $"Error al enviar el correo de restablecimiento: {ex.Message}";
            LogError(msg);
            callback?.Invoke(true, msg);
        }
    }

    // =========================================================================
    // SECCIÓN: Polling de verificación de email
    // =========================================================================

    private CancellationTokenSource _pollingCancellationToken;
    private Action<bool, string, bool> _pendingPollingCallback;
    private float _pendingPollingInterval;
    private float _pendingPollingTimeout;
    private float _pendingPollingElapsed;

    /// <summary>
    /// Inicia un polling usando Task.Delay para que sobreviva al foco perdido en Android.
    /// </summary>
    public void StartEmailVerificationPolling(Action<bool, string, bool> onResult, float intervalSeconds = 5f, float timeoutSeconds = 300f)
    {
        StopEmailVerificationPolling();

        if (!_isInitialized)
        {
            LogWarning("StartEmailVerificationPolling cancelado — Firebase no inicializado.");
            onResult?.Invoke(true, "Firebase no está inicializado.", false);
            return;
        }

        if (_currentUser == null)
        {
            LogWarning("StartEmailVerificationPolling cancelado — no hay usuario conectado.");
            onResult?.Invoke(true, "No hay ningún usuario con sesión iniciada.", false);
            return;
        }

        if (intervalSeconds <= 0f) intervalSeconds = 5f;

        _pendingPollingCallback = onResult;
        _pendingPollingInterval = intervalSeconds;
        _pendingPollingTimeout = timeoutSeconds;
        _pendingPollingElapsed = 0f;

        Log($"StartEmailVerificationPolling iniciado — intervalo: {intervalSeconds}s, timeout: {(timeoutSeconds > 0 ? timeoutSeconds + "s" : "ninguno")}");

        _pollingCancellationToken = new CancellationTokenSource();
        _ = EmailVerificationPollingTask(_pollingCancellationToken.Token);
    }

    /// <summary>
    /// Detiene el polling de verificación de email.
    /// </summary>
    public void StopEmailVerificationPolling()
    {
        if (_pollingCancellationToken != null)
        {
            _pollingCancellationToken.Cancel();
            _pollingCancellationToken.Dispose();
            _pollingCancellationToken = null;
            Log("Polling de verificación de email detenido manualmente.");
        }

        _pendingPollingCallback = null;
    }

    /// <summary>
    /// Reanuda el polling manualmente, por ejemplo desde un botón en la UI.
    /// </summary>
    public void ResumeEmailVerificationPolling()
    {
        if (_pendingPollingCallback == null)
        {
            LogWarning("ResumeEmailVerificationPolling — no hay ningún polling previo que reanudar.");
            return;
        }

        if (_pollingCancellationToken != null && !_pollingCancellationToken.IsCancellationRequested)
        {
            LogWarning("ResumeEmailVerificationPolling — el polling ya está en ejecución.");
            return;
        }

        float tiempoRestante = _pendingPollingTimeout - _pendingPollingElapsed;

        if (tiempoRestante <= 0f && _pendingPollingTimeout > 0f)
        {
            LogWarning("ResumeEmailVerificationPolling — tiempo límite agotado.");
            _pendingPollingCallback?.Invoke(true, "El tiempo de verificación ha expirado. Por favor solicita un nuevo correo.", false);
            _pendingPollingCallback = null;
            return;
        }

        Log($"ResumeEmailVerificationPolling — reanudando con {tiempoRestante}s restantes.");
        _pollingCancellationToken = new CancellationTokenSource();
        _ = EmailVerificationPollingTask(_pollingCancellationToken.Token, tiempoRestante);
    }

    public void GetUsuariosAdmin(Action<string[], string> callback)
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("usuarios")
            .OrderByChild("isAdmin")
            .EqualTo(true)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    callback?.Invoke(null, task.Exception?.Message ?? "Error al consultar administradores.");
                    return;
                }

                DataSnapshot snapshot = task.Result;
                var ids = new List<string>();

                foreach (var child in snapshot.Children)
                {
                    ids.Add(child.Key); // el key de cada nodo bajo "usuarios" es el userID
                }

                callback?.Invoke(ids.ToArray(), null);
            });
    }

    private async Task EmailVerificationPollingTask(CancellationToken token, float overrideTimeout = -1f)
    {
        float interval = _pendingPollingInterval;
        float timeout = overrideTimeout >= 0f ? overrideTimeout : _pendingPollingTimeout;
        float elapsed = 0f;

        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(interval), token);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            elapsed += interval;
            _pendingPollingElapsed += interval;

            // Comprobar timeout
            if (timeout > 0f && elapsed >= timeout)
            {
                string timeoutMsg = $"El polling de verificación de email agotó el tiempo tras {timeout}s.";
                LogWarning(timeoutMsg);
                _pendingPollingCallback?.Invoke(true, timeoutMsg, false);
                _pendingPollingCallback = null;
                break;
            }

            // Comprobar internet
            bool hasInternet = await IsInternetAvailable();
            if (!hasInternet)
            {
                LogWarning("Polling — sin internet, saltando tick.");
                _pendingPollingCallback?.Invoke(true, "Sin conexión a internet.", false);
                continue;
            }

            // Recargar usuario desde Firebase
            try
            {
                await _currentUser.ReloadAsync();
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error al recargar en polling: {ex.Message}";
                LogError(errorMsg);
                _pendingPollingCallback?.Invoke(true, errorMsg, false);
                continue;
            }

            bool verified = _currentUser.IsEmailVerified;
            Log($"Tick de polling — verificado: {verified} (transcurrido: {_pendingPollingElapsed}s)");

            if (verified)
            {
                string successMsg = $"El email '{_currentUser.Email}' ha sido verificado correctamente.";
                Log(successMsg);
                _pendingPollingCallback?.Invoke(false, successMsg, true);
                _pendingPollingCallback = null;
                break;
            }

            _pendingPollingCallback?.Invoke(false,
                $"El email '{_currentUser.Email}' aún no verificado. Próxima comprobación en {interval}s.",
                false);
        }
    }

    // =========================================================================
    // SECCIÓN: Helpers privados
    // =========================================================================

    /// <summary>
    /// Traduce los códigos de AuthError de Firebase en mensajes legibles.
    /// </summary>
    string GetAuthErrorMessage(FirebaseException ex)
    {
        AuthError errorCode = (AuthError)ex.ErrorCode;

        switch (errorCode)
        {
            case AuthError.WrongPassword:
                return "La contraseña ingresada es incorrecta.";

            case AuthError.InvalidEmail:
                return "El formato del correo electrónico no es válido.";

            case AuthError.UserNotFound:
                return "No existe una cuenta registrada con este correo electrónico.";

            case AuthError.UserDisabled:
                return "Esta cuenta ha sido deshabilitada. Contacta al soporte.";

            case AuthError.TooManyRequests:
                return "Demasiados intentos fallidos. Intenta de nuevo más tarde.";

            case AuthError.NetworkRequestFailed:
                return "Error de red. Verifica tu conexión a internet e intenta de nuevo.";

            case AuthError.InvalidCredential:
                return "Las credenciales ingresadas no son válidas.";

            case AuthError.EmailAlreadyInUse:
                return "Este correo electrónico ya está registrado.";

            case AuthError.WeakPassword:
                return "La contraseña es demasiado débil. Usa al menos 6 caracteres.";

            case AuthError.OperationNotAllowed:
                return "Esta operación no está permitida actualmente. Contacta al soporte.";

            case AuthError.AccountExistsWithDifferentCredentials:
                return "Ya existe una cuenta con este correo usando otro método de inicio de sesión.";

            case AuthError.ExpiredActionCode:
                return "El enlace ha expirado. Solicita uno nuevo.";

            case AuthError.InvalidActionCode:
                return "El enlace no es válido o ya fue utilizado.";

            case AuthError.SessionExpired:
                return "Tu sesión ha expirado. Por favor, inicia sesión nuevamente.";

            case AuthError.RequiresRecentLogin:
                return "Esta acción requiere que vuelvas a iniciar sesión por seguridad.";

            default:
                return "Ocurrió un error al iniciar sesión. Compruebe su contraseña o correo";
        }
    }

    // -------------------------------------------------------------------------
    // Helpers de log — solo imprimen si debugMode está activo
    // -------------------------------------------------------------------------

    private void Log(string message)
    {
        if (debugMode) Debug.Log($"{LOG_TAG} {message}");
    }

    private void LogWarning(string message)
    {
        if (debugMode) Debug.LogWarning($"{LOG_TAG} {message}");
    }

    private void LogError(string message)
    {
        if (debugMode) Debug.LogError($"{LOG_TAG} {message}");
    }
}

// =========================================================================
// MODELOS DE DATOS
// =========================================================================

[Serializable]
public class DataFireBase64
{
    public string datosBase64;
}

[Serializable]
public class Data
{
    public string nombreCompleto;
    public string tipoDocumento;
    public string numeroDocumento;
    public string numeroCelular;
    public string sexo;
    public bool correoAutenticado;
    public bool isAdmin;
    public string fechaNacimiento;
    public string nacionalidad;
    public string municipio;
    public int municipioID;
    public string direccion;
    public string email;
    public string contrasena;
    public bool[] respuestasViolentometro;
}

[Serializable]
public class  InfoTablaMunicipios
{
    public string No;
    public string Municipio;
    public string NumeroTelefonico_ComisariaFamilia;
    public string Direccion_ComisariaFamilia;
    public string CorreoElectronico_ComisariaFamilia;
}

[Serializable]
public class Notificacion
{
    public string id;
    public string titulo;
    public string mensaje;
    public int ID_Icono;
    public long timestamp;
    public bool leido;
}

[Serializable]
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
    public string tipoViolencia;
    public int tipoAvance;
    public string descripcionDeAvance;
    public int estadoDelCaso;
    public string fechaCaso;
    public string municipioUsuario;
}