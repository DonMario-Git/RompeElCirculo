using UnityEngine;

public class ActivarSiVerificado : MonoBehaviour
{
    private void OnEnable()
    {
        gameObject.SetActive(AppManager.userData.verificado);
    }
}
