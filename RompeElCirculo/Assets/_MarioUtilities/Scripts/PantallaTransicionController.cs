using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;
using UtilidadesLaEME;

public class PantallaTransicionController : MonoBehaviour
{
    public static PantallaTransicionController singleton;
    [SerializeField] private Image im;

    public float offSetXMaximo;

    public bool puedeCambiar;

    private void Awake()
    {
        singleton = this;
        transform.localPosition = new Vector3(offSetXMaximo, 0, 0);
    }

    private void OnValidate()
    {
        transform.localPosition = new Vector3(offSetXMaximo, 0, 0);
    }

    public void Transicionar(Action onComplete = null)
    {
        if (!puedeCambiar) return;
        puedeCambiar = false;
        im.ActivarComponente();
        im.DOKill();
        im.color = Color.black;
        transform.localPosition = new Vector3(offSetXMaximo, 0, 0);
        transform.DOKill();
        transform.DOLocalMoveX(0, 0.2f).SetEase(Ease.Linear).OnComplete(() =>
        {
            im.DOKill();
            im.DOFade(0, 0.1f).OnComplete(() => { 
                im.DesactivarComponente(); 
                puedeCambiar = true;
            });

            onComplete?.Invoke();
        });
    }
}
