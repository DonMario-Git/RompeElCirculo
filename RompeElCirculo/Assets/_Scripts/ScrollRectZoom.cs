using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollRectZoom : MonoBehaviour, IDragHandler
{
    [Header("Zoom Settings")]
    public float minZoom = 0.5f;
    public float maxZoom = 2.5f;
    public float zoomSpeed = 0.1f;
    [Header("Dead Time After Pinch (seconds)")]
    public float deadTimeAfterPinch = 0.2f;

    private ScrollRect scrollRect;
    private RectTransform contentRect;
    private Vector2 lastTouchCenter;
    private float lastTouchDistance;
    private bool isPinching;
    private float deadTimeTimer = 0f;

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect != null)
            contentRect = scrollRect.content;
    }

    void Update()
    {
        // Dead time logic
        if (deadTimeTimer > 0f)
        {
            deadTimeTimer -= Time.unscaledDeltaTime;
            if (deadTimeTimer <= 0f && scrollRect != null)
                scrollRect.enabled = true;
        }

        // Pinch to zoom (touch only)
        if (Input.touchCount == 2 && contentRect != null)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 touch0 = t0.position;
            Vector2 touch1 = t1.position;
            float currentDistance = Vector2.Distance(touch0, touch1);
            Vector2 currentCenter = (touch0 + touch1) * 0.5f;

            if (!isPinching)
            {
                lastTouchDistance = currentDistance;
                lastTouchCenter = currentCenter;
                isPinching = true;
                if (scrollRect != null)
                    scrollRect.enabled = false; // Disable scroll during pinch
            }
            else
            {
                float delta = currentDistance - lastTouchDistance;
                float scaleChange = 1 + (delta / 300f) * zoomSpeed;
                SetZoom(contentRect.localScale.x * scaleChange, currentCenter);
                lastTouchDistance = currentDistance;
                lastTouchCenter = currentCenter;
            }
        }
        else
        {
            if (isPinching)
            {
                // Start dead time after pinch
                deadTimeTimer = deadTimeAfterPinch;
            }
            isPinching = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Optional: You can implement panning here if needed
    }

    private void SetZoom(float targetScale, Vector2 zoomCenterScreen)
    {
        targetScale = Mathf.Clamp(targetScale, minZoom, maxZoom);
        contentRect.pivot = new Vector2(0.5f, 0.5f);

        // 1. Convertir el punto de zoom de pantalla a local en el content antes de escalar
        Vector2 localPointBefore;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRect, zoomCenterScreen, null, out localPointBefore);
        Vector2 anchoredPosBefore = contentRect.anchoredPosition;
        Vector3 oldScale = contentRect.localScale;

        // 2. Escalar el content
        contentRect.localScale = new Vector3(targetScale, targetScale, 1);

        // 3. Convertir el mismo punto de pantalla a local en el content después de escalar
        Vector2 localPointAfter;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRect, zoomCenterScreen, null, out localPointAfter);

        // 4. Ajustar la posición para que el punto bajo los dedos permanezca fijo
        Vector2 delta = localPointAfter - localPointBefore;
        contentRect.anchoredPosition = anchoredPosBefore - delta;
    }
}
