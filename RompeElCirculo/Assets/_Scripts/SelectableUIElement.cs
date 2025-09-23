using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using DG.Tweening;

public class SelectableUIElement : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool autoDeselect;
    public bool canInteract = true;
    public UnityEvent OnPointerDown, OnPointerUp, OnSelectedUpdate;
    public bool selected;
    public bool allowAnimations = true;

    public bool useStringID;
    public string stringID;

    public AudioEffect effectOnDown;

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (canInteract)
        {
            OnPointerDown?.Invoke();
            selected = true;

            if (effectOnDown != null) AudioPlayer.singleton.PlayAudioEffect(effectOnDown);

            if (allowAnimations)
            {
                transform.localScale = Vector3.one;
                transform.DOScale(Vector3.one * 1.4f, 0.3f).SetEase(Ease.OutBack);
            }
        }    
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        if (canInteract)
        {
            OnPointerUp?.Invoke();
            selected = false;

            if (allowAnimations)
            {
                transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }
        }      
    }

    private void Update()
    {
        if (selected)
        {     
            transform.position = AppManager.GetMousePos(transform.position);
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
            OnSelectedUpdate?.Invoke();
        }

        if (autoDeselect)
        {
            if (Input.GetMouseButtonUp(0) && selected)
            {
                OnPointerUp?.Invoke();
                selected = false;
            }
        }     
    }
}
