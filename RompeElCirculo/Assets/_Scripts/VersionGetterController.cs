using AwesomeAttributes;
using TMPro;
using UnityEngine;

[ExecuteAlways]
public class VersionGetterController : MonoBehaviour
{
    [Readonly] public TextMeshProUGUI texto;

    private void Awake()
    {
        if (texto == null)
        {
            texto = GetComponent<TextMeshProUGUI>();
        }

        if (texto != null)
        {
            texto.text = "v " + Application.version;
        }
    }

    private void OnEnable()
    {
        if (texto == null)
        {
            texto = GetComponent<TextMeshProUGUI>();
        }

        if (texto != null)
        {
            texto.text = "v " + Application.version;
        }
    }
}
