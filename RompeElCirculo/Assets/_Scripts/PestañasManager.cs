using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UtilidadesLaEME;

public class PestañasManager : MonoBehaviour
{
    public static PestañasManager singleton;
    public PestañaController[] pestañas;
    private int? indiceActual = null;
    [SerializeField] private Image _FRENTE;
    [SerializeField] private Image _BLOQUEADOR;

    private void Awake()
    {
        singleton = this;
    }

    public void CambiarPestaña(int indice)
    {
        CambiarPestañaAnimacionAvanzar(indice);
    }

    public void CambiaPestañaSinAnimacion(int indiceEntrante)
    {
        foreach (var item in pestañas)
        {
            item.gameObject.DesactivarObjeto();
        }

        if (indiceActual != null)
        {
            int sale = (int)indiceActual;
            pestañas[sale].gameObject.ActivarObjeto();
        }

        pestañas[indiceEntrante].gameObject.ActivarObjeto();

        indiceActual = indiceEntrante;
    }

    public void EjecutarAnimacionEntrada(int pestañaFinal)
    {
        if (_FRENTE.color.a == 0)
        {
            _BLOQUEADOR.raycastTarget = true;
            _FRENTE.DOKill();
            _FRENTE.DOFade(1, 0.3f).SetDelay(0.4f).OnComplete(() =>
            {
                CambiaPestañaSinAnimacion(pestañaFinal);
                _FRENTE.DOFade(0, 0.3f).OnComplete(() =>
                {
                    _BLOQUEADOR.raycastTarget = false;
                });
            });
        }
        else
        {
            CambiaPestañaSinAnimacion(pestañaFinal);

            _BLOQUEADOR.raycastTarget = true;
            _FRENTE.DOKill();
            _FRENTE.DOFade(0, 0.3f).SetDelay(0.2f).OnComplete(() =>
            { 
                _BLOQUEADOR.raycastTarget = false;
            });
        }
    }

    public void CambiarPestañaAnimacionAvanzar(int indiceEntrante)
    {
        _BLOQUEADOR.raycastTarget = true;
        
        int sale = (int)indiceActual;

        for (int i = 0; i < pestañas.Length; i++)
        {
            if (i != indiceEntrante && i != sale) pestañas[i].gameObject.DesactivarObjeto();
        }

        if (indiceActual != null)
        {
            if (pestañas[sale].orden < pestañas[indiceEntrante].orden)
            {
                pestañas[sale].gameObject.ActivarObjeto();
                pestañas[sale].transform.SetSiblingIndex(0);

                pestañas[indiceEntrante].transform.localPosition = new Vector2(1040, 0);

                pestañas[indiceEntrante].transform.DOKill();
                pestañas[sale].transform.DOKill();

                pestañas[indiceEntrante].transform.DOLocalMoveX(0, 0.3f).SetDelay(0.2f).SetEase(Ease.OutExpo).OnComplete(() => _BLOQUEADOR.raycastTarget = false);


                pestañas[sale].transform.DOLocalMoveX(-240, 0.3f).SetDelay(0.2f).SetEase(Ease.OutExpo).OnComplete(() => pestañas[sale].gameObject.DesactivarObjeto());
                pestañas[indiceEntrante].transform.SetSiblingIndex(1);
            }
            else
            {
                pestañas[sale].gameObject.ActivarObjeto();
                pestañas[sale].transform.SetSiblingIndex(1);

                pestañas[indiceEntrante].transform.localPosition = new Vector2(-240, 0);

                pestañas[indiceEntrante].transform.DOKill();
                pestañas[sale].transform.DOKill();

                pestañas[indiceEntrante].transform.DOLocalMoveX(0, 0.3f).SetDelay(0.2f).SetEase(Ease.OutExpo).OnComplete(() => _BLOQUEADOR.raycastTarget = false);

                pestañas[sale].transform.DOLocalMoveX(1040, 0.3f).SetDelay(0.2f).SetEase(Ease.OutExpo).OnComplete(() => pestañas[sale].gameObject.DesactivarObjeto());
                pestañas[indiceEntrante].transform.SetSiblingIndex(0);
            }
        }

        pestañas[indiceEntrante].gameObject.ActivarObjeto();


        indiceActual = indiceEntrante;
    }
}
