using AwesomeAttributes;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class DotweenAnimator : MonoBehaviour
{
    public Transform target;

    public float duracion;
    public bool playOnAwake;

    [Space(20)]
    public bool loopInfinitamente; // Nuevo bool para controlar el loop infinito yoyo

    [Space(20)]
    public bool usePosition;

    public bool localPos;
    public Vector2 targetPosition;
    public Ease easePos = Ease.Linear;
    public UnityEvent OnCompletePos;   

    [Space(20)]
    public bool useRotation;
    public bool localRot;
    public Vector3 targetRotation;
    public Ease easeRot = Ease.Linear;
    public UnityEvent OnCompleteRot;

    [Space(20)]
    public bool useScale;
    public Vector3 targetScale;
    public Ease easeEscale = Ease.Linear;
    public UnityEvent OnCompleteScale;

    private void OnEnable()
    {
        if (playOnAwake) Play();
    }

    private void OnDisable()
    {
        if (target == null) target = transform;
        target.DOKill();
    }

    public void Play()
    {
        if (target == null) target = transform;
        
        if (usePosition)
        {
            Tween t;
            if (localPos)
            {
                t = target.DOLocalMove(new Vector3(targetPosition.x, targetPosition.y, target.localPosition.z), duracion).SetEase(easePos);
            }
            else
            {
                t = target.DOMove(new Vector3(targetPosition.x, targetPosition.y, target.position.z), duracion).SetEase(easePos);
            }
            if (loopInfinitamente)
                t.SetLoops(-1, LoopType.Yoyo);
            t.OnComplete(() => OnCompletePos?.Invoke());
        }

        if (useRotation)
        {
            Vector3 rot = targetRotation; // Usar el valor deseado directamente
            Tween t;
            if (localRot)
            {
                t = target.DOLocalRotate(rot, duracion).SetEase(easeRot);
            }
            else
            {
                t = target.DORotate(rot, duracion).SetEase(easeRot);
            }
            if (loopInfinitamente)
                t.SetLoops(-1, LoopType.Yoyo);
            t.OnComplete(() => OnCompleteRot?.Invoke());
        }

        if (useScale)
        {
            Tween t = target.DOScale(new Vector3(targetScale.x, targetScale.y, target.localScale.z), duracion).SetEase(easeEscale);
            if (loopInfinitamente)
                t.SetLoops(-1, LoopType.Yoyo);
            t.OnComplete(() => OnCompleteScale?.Invoke());
        }
    }
}