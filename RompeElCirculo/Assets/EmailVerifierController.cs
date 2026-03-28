using UnityEngine;
using System;
using System.Collections;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class EmailVerifierController : MonoBehaviour
{
    private FirebaseAuth auth;
    private FirebaseUser user;
    public float checkInterval = 5f;

    private Coroutine pollingCoroutine;

    private void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
    }

    // Envía el correo de verificación que contiene el link estándar de Firebase
    public void SendVerificationEmail(Action<string> onResult)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onResult?.Invoke("No hay conexión a internet.");
            return;
        }

        user = auth.CurrentUser;
        if (user == null)
        {
            onResult?.Invoke("No hay usuario autenticado.");
            return;
        }

        if (user.IsEmailVerified)
        {
            onResult?.Invoke("Correo ya verificado.");
            return;
        }

        user.SendEmailVerificationAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onResult?.Invoke("Error al enviar email: " + task.Exception);
            }
            else
            {
                onResult?.Invoke(null);
            }
        });
    }

    // Inicia el polling periódico para comprobar si el usuario verificó su correo
    public void StartPollingVerification(Action<bool, string> onVerified)
    {
        if (pollingCoroutine != null) StopCoroutine(pollingCoroutine);
        pollingCoroutine = StartCoroutine(PollVerification(onVerified));
    }

    public void StopPollingVerification()
    {
        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
            pollingCoroutine = null;
        }
    }

    private IEnumerator PollVerification(Action<bool, string> onVerified)
    {
        while (true)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                onVerified?.Invoke(false, "No hay conexión a internet.");
                yield return new WaitForSeconds(checkInterval);
                continue;
            }

            user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null)
            {
                onVerified?.Invoke(false, "No hay usuario autenticado.");
                yield break;
            }

            var reloadTask = user.ReloadAsync();
            while (!reloadTask.IsCompleted) yield return null;

            if (reloadTask.IsFaulted || reloadTask.IsCanceled)
            {
                onVerified?.Invoke(false, "Error al refrescar usuario: " + reloadTask.Exception);
                yield return new WaitForSeconds(checkInterval);
                continue;
            }

            if (user.IsEmailVerified)
            {
                // Actualizar campo 'correoVerificado' en Realtime Database si es posible
                if (FirebaseStorageManager.singleton != null && FirebaseStorageManager.singleton.isInitialized)
                {
                    var dbRef = typeof(FirebaseStorageManager)
                        .GetField("dbReference", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .GetValue(FirebaseStorageManager.singleton) as DatabaseReference;

                    if (dbRef != null)
                    {
                        var updates = new System.Collections.Generic.Dictionary<string, object>()
                        {
                            { "correoVerificado", true },
                            { "verificado", true }
                        };

                        var setTask = dbRef.Child("usuarios").Child(user.UserId).UpdateChildrenAsync(updates);
                        while (!setTask.IsCompleted) yield return null;

                        if (setTask.IsFaulted || setTask.IsCanceled)
                        {
                            onVerified?.Invoke(true, "Verificado localmente, pero error al actualizar DB: " + setTask.Exception);
                        }
                        else
                        {
                            onVerified?.Invoke(true, null);
                        }
                    }
                    else
                    {
                        onVerified?.Invoke(true, "Verificado pero no fue posible obtener referencia a la base de datos.");
                    }
                }
                else
                {
                    onVerified?.Invoke(true, "Verificado pero Firebase DB no está inicializado.");
                }

                yield break;
            }

            onVerified?.Invoke(false, null);
            yield return new WaitForSeconds(checkInterval);
        }
    }
}
