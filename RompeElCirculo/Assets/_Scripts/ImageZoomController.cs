using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ImageZoomController : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    [Header("Zoom Settings")]
    public float minZoom = 1f;
    public float maxZoom = 4f;
    public float zoomSpeed = 0.1f;
    public float deadTime = 0.2f; // Tiempo muerto tras pinch

    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 lastCenter;
    private float lastPinchTime;
    private bool isPinching;
    private Vector2 dragStartPos;
    private Vector2 imageStartPos;

    private bool isDragging, isZooming;
    private Vector2 lastPinchCenter;
    private bool wasPinching = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        rectTransform.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 t0Prev = t0.position - t0.deltaPosition;
            Vector2 t1Prev = t1.position - t1.deltaPosition;

            float prevDist = Vector2.Distance(t0Prev, t1Prev);
            float currDist = Vector2.Distance(t0.position, t1.position);
            float delta = currDist - prevDist;

            // Centro entre los dos dedos
            Vector2 center = (t0.position + t1.position) * 0.5f;
            Vector2 prevCenter = (t0Prev + t1Prev) * 0.5f;

            // Zoom
            float prevScale = rectTransform.localScale.x;
            float scale = prevScale + delta * zoomSpeed * Time.deltaTime;
            scale = Mathf.Clamp(scale, minZoom, maxZoom);

            // Tiempo muerto para evitar distorsión
            if (!isPinching || Time.time - lastPinchTime > deadTime)
            {
                lastCenter = center;
                isPinching = true;
            }
            lastPinchTime = Time.time;

            // Convertir centro a espacio local de la imagen
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, lastCenter, canvas.worldCamera, out localPoint);

            // Calcular posición antes y después del zoom
            Vector3 beforeZoom = rectTransform.TransformPoint(localPoint);
            rectTransform.localScale = new Vector3(scale, scale, 1);
            Vector3 afterZoom = rectTransform.TransformPoint(localPoint);
            Vector2 diff = (Vector2)(beforeZoom - afterZoom);

            // Calcular desplazamiento del centro entre frames
            RectTransform parentRect = rectTransform.parent as RectTransform;
            Vector2 currLocal, prevLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, center, canvas.worldCamera, out currLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, prevCenter, canvas.worldCamera, out prevLocal);
            Vector2 localMoveDelta = currLocal - prevLocal;

            // Aplicar ambos movimientos en una sola suma
            rectTransform.anchoredPosition += localMoveDelta + diff;

            wasPinching = true;
        }
        else
        {
            if (wasPinching && Input.touchCount == 1)
            {
                // Solo sincronizar el drag al pasar de pinch a drag
                RectTransform parentRect = rectTransform.parent as RectTransform;
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.GetTouch(0).position, canvas.worldCamera, out localPoint);
                dragStartPos = localPoint;
                imageStartPos = rectTransform.anchoredPosition;
            }
            isPinching = false;
            wasPinching = false;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Convertir la posición inicial del toque a espacio local del padre
        RectTransform parentRect = rectTransform.parent as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, canvas.worldCamera, out localPoint);
        dragStartPos = localPoint;
        imageStartPos = rectTransform.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Input.touchCount < 2)
        {
            // Convertir la posición actual del toque a espacio local del padre
            RectTransform parentRect = rectTransform.parent as RectTransform;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, canvas.worldCamera, out localPoint);
            Vector2 delta = localPoint - dragStartPos;
            rectTransform.anchoredPosition = imageStartPos + delta;
        }
    }
}
