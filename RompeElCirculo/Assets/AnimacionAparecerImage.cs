using AwesomeAttributes;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AnimacionAparecerImage : MonoBehaviour
{
    public bool canvasGroup;
    private Image im;
    private CanvasGroup cg;
    private Vector2? posicionOriginal;

    private void OnValidate()
    {
        im = im != null ? im : GetComponent<Image>();
        cg = cg != null ? cg : GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (canvasGroup)
        {
            var cgRtr = (RectTransform)cg.transform;
            posicionOriginal ??= cgRtr.anchoredPosition;

            cg.alpha = 0;
            cg.DOKill();
            cg.DOFade(1, 0.4f).SetDelay(0.1f);


            Vector2 pos = (Vector2)posicionOriginal;

            cgRtr.anchoredPosition = new Vector2(pos.x, pos.y - 40);
            cgRtr.DOKill();
            cgRtr.DOAnchorPosY(pos.y, 0.8f).SetEase(Ease.OutBack).SetDelay(0.1f);
        }
        else
        {
            posicionOriginal ??= im.rectTransform.anchoredPosition;

            im.color = new Color(1, 1, 1, 0);
            im.DOKill();
            im.DOFade(1, 0.4f).SetDelay(0.1f);
        

            Vector2 pos = (Vector2)posicionOriginal;

            im.rectTransform.anchoredPosition = new Vector2(pos.x, pos.y - 40);
            im.rectTransform.DOKill();
            im.rectTransform.DOAnchorPosY(pos.y, 0.8f).SetEase(Ease.OutBack).SetDelay(0.1f);
        }
    }
}
