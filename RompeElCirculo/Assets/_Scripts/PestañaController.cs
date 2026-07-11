using TMPro;
using UnityEngine;

[ExecuteAlways]
public class PestañaController : MonoBehaviour
{
    public ScreenOrientation orientacion = ScreenOrientation.Portrait;
    public int orden;

    private void OnEnable()
    {
        Screen.orientation = orientacion;
    }
}
