using AwesomeAttributes;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SelectableUIElement))]
public class DraggableInPlaceController : MonoBehaviour
{
    public Vector2 originalPosition;
    public SelectableUIElement selectableController;
    public TargetSlideController target;
    public bool interactableAfterCorrect;
    [Readonly] public bool isComplete;
    public UnityEvent OnComplete, OnIncorrect;

    public bool useMultitarget;
    public TargetSlideController[] multiTarget;

    public AudioEffect effectOnComplete;

    [ContextMenu(nameof(SetOriginalPos))]
    public void SetOriginalPos()
    {
        originalPosition = (Vector2)transform.position;
    }

    public void Confirm()
    {
        if (target != null)
        {
            if (Vector2.Distance(transform.position, target.transform.position) <= target.radius)
            {
                isComplete = true;
                if (!interactableAfterCorrect) selectableController.canInteract = false;
                transform.position = (Vector2)target.transform.position;
                if (effectOnComplete.clip != null) AudioPlayer.singleton.PlayAudioEffect(effectOnComplete);
                target.image.enabled = true;
                OnComplete?.Invoke();      
            }
            else
            {
                isComplete = false;
                OnIncorrect?.Invoke();      
                transform.position = (Vector2)originalPosition;
                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
            }

            target.image.raycastTarget = isComplete && !interactableAfterCorrect;
        }
        else
        {
            if (useMultitarget)
            {
                foreach (var item in multiTarget)
                {
                    if (Vector2.Distance(transform.position, item.transform.position) <= item.radius && !item.complete)
                    {
                        isComplete = true;
                        if (!interactableAfterCorrect) selectableController.canInteract = false;
                        transform.position = (Vector2)item.transform.position;
                        if (effectOnComplete.clip != null) AudioPlayer.singleton.PlayAudioEffect(effectOnComplete);
                        OnComplete?.Invoke();
                        //item.transform.parent.localScale = new Vector3(1.3f, 0.7f, 1.3f);
                        //item.transform.parent.DOScale(1, 0.4f).SetEase(Ease.OutElastic);
                        item.image.raycastTarget = isComplete && !interactableAfterCorrect;
                        item.image.enabled = true;
                        item.complete = true;
                        return;
                    }               
                }

                isComplete = false;
                OnIncorrect?.Invoke();
                transform.position = (Vector2)originalPosition;
                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
            }
            else
            {
                Debug.LogError($"Falta un target al objeto {name}");
            }     
        }
    }
}
