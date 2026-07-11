using UnityEngine;
using UnityEngine.UI;

public class ScrollRectFix : MonoBehaviour
{
    public ScrollRect scroll;

    public bool reiniciarAlDesactivar = true;

    private void Reset()
    {
        scroll = GetComponent<ScrollRect>();
    }

    private void OnEnable()
    {
        if (reiniciarAlDesactivar) scroll.content.localPosition = new Vector3(0, 0, 0);
    }
}
