using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
[ExecuteAlways]
public class InputFieldUtilities : MonoBehaviour
{
    public TMP_InputField inputTarget;
    public bool borrarTextoAlHabilitar = true;

    private void OnValidate()
    {
        if (inputTarget == null)
        {
            inputTarget = GetComponent<TMP_InputField>();
        }
    }

    private void OnEnable()
    {
        if (borrarTextoAlHabilitar)
        {
            inputTarget.text = string.Empty;
        }
    }
}
