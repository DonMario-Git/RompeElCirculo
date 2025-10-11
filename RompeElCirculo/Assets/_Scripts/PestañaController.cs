using UnityEngine;

[ExecuteAlways]
public class PestañaController : MonoBehaviour
{
    public ScreenOrientation orientacion = ScreenOrientation.Portrait;

    private void OnEnable()
    {
        Screen.orientation = orientacion;
    }
}
