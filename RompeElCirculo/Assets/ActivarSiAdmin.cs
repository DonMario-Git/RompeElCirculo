using UnityEngine;

public class ActivarSiAdmin : MonoBehaviour
{
    private void Awake()
    {
        gameObject.SetActive(AppManager.data.esAdmin);
    }
}
