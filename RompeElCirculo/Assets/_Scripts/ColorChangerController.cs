using UnityEngine;
using UnityEngine.UI;

namespace LonidaMinigames.FreeDrawing
{
    public class ColorChangerController : MonoBehaviour
    {
        public LienzoDibujoController canvasDrawing;

        public Color colorToChange = Color.white;

        public Image colorRepresentImage;

        private void OnValidate()
        {
            colorRepresentImage.color = colorToChange;
        }

        public void ChangeColor()
        {
            canvasDrawing.ChangeMainColor(colorToChange);
        }
    }
}