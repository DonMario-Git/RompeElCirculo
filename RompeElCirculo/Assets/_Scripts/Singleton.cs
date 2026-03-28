using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _s;

    public static T singleton
    {
        get
        {
            if (_s == null)
            {
                _s = FindFirstObjectByType<T>();

                if (_s == null)
                {
                    Debug.LogError($"No existe una instancia de {typeof(T)} en la escena.");
                }
            }
            return _s;
        }
    }

    protected virtual void Awake()
    {
        _s = this as T;
    }
}
