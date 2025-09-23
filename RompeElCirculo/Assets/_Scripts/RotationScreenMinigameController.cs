using UnityEngine;

[ExecuteAlways]
public class RotationScreenMinigameController : MonoBehaviour
{
    public bool onActiveObject = true;

    public ScreenOrientation screen = ScreenOrientation.Portrait;

    private void OnEnable()
    {
        if (onActiveObject)
        {
            if (AppManager.singleton != null)
            {
                AppManager.singleton.RotarPantalla(screen);
            }
            else
            {
                Screen.orientation = screen;
            }
        }
    }
}
