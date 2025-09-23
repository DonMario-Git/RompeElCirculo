using AwesomeAttributes;
using UnityEngine;
using UnityEngine.UI;

public class TargetSlideController : MonoBehaviour
{
    [Readonly]public Image image;
    public float radius;
    [Readonly]public bool complete;

    private void Reset()
    {
        image = GetComponent<Image>();
    }

    private void OnValidate()
    {
        image = GetComponent<Image>();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
