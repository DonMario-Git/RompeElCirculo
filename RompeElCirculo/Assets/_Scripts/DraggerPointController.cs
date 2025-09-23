using UnityEngine;
using UnityEngine.UI;

public class DraggerPointController : MonoBehaviour
{
    public Image image;
    public LineRenderer line;

    private void OnValidate()
    {
        image = GetComponent<Image>();
        line.startColor = image.color;
        line.endColor = image.color;
    }

    public void EnableLine()
    {
        line.SetPosition(1, Vector3.zero);
        line.gameObject.SetActive(true);
    }

    public void UpdateLine()
    {
        line.SetPosition(1, (Vector2)transform.localPosition);
    }

    public void DisableLine()
    {
        line.SetPosition(1, Vector3.zero);
        line.gameObject.SetActive(false);
    }
}
