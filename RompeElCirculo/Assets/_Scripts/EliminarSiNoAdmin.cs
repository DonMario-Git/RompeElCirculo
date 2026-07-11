using UnityEngine;

public class EliminarSiNoAdmin : MonoBehaviour
{
    private void Start()
    {
        if (!AppManager.UserData.isAdmin)
        {
            gameObject.SetActive(false);
        }
    }
}
