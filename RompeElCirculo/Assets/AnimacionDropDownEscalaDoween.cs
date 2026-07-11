using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class AnimacionDropDownEscalaDoween : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animación")]
    [SerializeField] private float alturaInicial = 300f;
    [SerializeField] private float alturaFinal = 400f;
    [SerializeField] private float duracionEscala = 0.3f;
    [SerializeField] private float duracionFade = 0.1f;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        Vector2 size = rectTransform.sizeDelta;
        size.y = alturaInicial;
        rectTransform.sizeDelta = size;

        size.y = alturaFinal;
        rectTransform
            .DOSizeDelta(size, duracionEscala)
            .SetEase(Ease.OutQuart);

        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, duracionFade);
    }

    private void OnDestroy()
    {
        rectTransform.DOKill();
        canvasGroup.DOKill();
    }
}