using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea)
            ApplySafeArea();
    }

    public void ApplySafeArea()
    {
        lastSafeArea = Screen.safeArea;

        Vector2 anchorMin = new Vector2(
            lastSafeArea.xMin / Screen.width,
            lastSafeArea.yMin / Screen.height);

        Vector2 anchorMax = new Vector2(
            lastSafeArea.xMax / Screen.width,
            lastSafeArea.yMax / Screen.height);

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        // Muy importante
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Debug.Log($"Screen: {Screen.width} x {Screen.height}");
        Debug.Log($"SafeArea: {Screen.safeArea}");
    }
}