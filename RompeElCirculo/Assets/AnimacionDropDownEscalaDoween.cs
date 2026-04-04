using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class AnimacionDropDownEscalaDoween : MonoBehaviour
{
    public RectTransform recTransform;
    public CanvasGroup canvasGroup;
    public float porDefecto = 400;
    public float inicial = 300;

    private void Awake()
    {
        recTransform = recTransform != null ? recTransform : (RectTransform)transform;
        canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();

        recTransform.sizeDelta = new Vector2(recTransform.sizeDelta.x, inicial);
        recTransform.DOSizeDelta(new Vector2(recTransform.sizeDelta.x, porDefecto), 0.3f).SetEase(Ease.OutQuart);

        canvasGroup.alpha = 0;
        canvasGroup.DOFade(1, 0.3f);
    }
}
