using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UtilidadesLaEME;

public class PestañasManager : MonoBehaviour
{
    public static PestañasManager singleton;
    public PestañaController[] pestañas;
    private int? indiceActual = null;
    public Image _FRENTE;

    private void Awake()
    {
        singleton = this;
    }

    public void CambiarPestaña(int indice)
    {
        CambiarPestañaAnimacionAvanzar(indice);
    }

    public void CambiarPestañaAnimacionAvanzar(int indiceEntrante)
    {
        foreach (var item in pestañas)
        {
            item.gameObject.DesactivarObjeto();
        }

        if (indiceActual != null)
        {
            int sale = (int)indiceActual;

            if (pestañas[sale].orden < pestañas[indiceEntrante].orden)
            {
                pestañas[sale].gameObject.ActivarObjeto();
                pestañas[sale].transform.SetSiblingIndex(0);

                pestañas[indiceEntrante].transform.localPosition = new Vector2(1040, 0);

                pestañas[indiceEntrante].transform.DOKill();
                pestañas[sale].transform.DOKill();

                pestañas[indiceEntrante].transform.DOLocalMoveX(0, 0.5f).SetEase(Ease.OutExpo);


                pestañas[sale].transform.DOLocalMoveX(-240, 0.5f).SetEase(Ease.OutExpo).OnComplete(() => pestañas[sale].gameObject.DesactivarObjeto());
                pestañas[indiceEntrante].transform.SetSiblingIndex(1);
            }
            else
            {
                pestañas[sale].gameObject.ActivarObjeto();
                pestañas[sale].transform.SetSiblingIndex(1);

                pestañas[sale].transform.localPosition = new Vector2(-240, 0);

                pestañas[indiceEntrante].transform.DOKill();
                pestañas[sale].transform.DOKill();

                pestañas[indiceEntrante].transform.DOLocalMoveX(0, 0.5f).SetEase(Ease.OutExpo);

                pestañas[sale].transform.DOLocalMoveX(1040, 0.5f).SetEase(Ease.OutExpo).OnComplete(() => pestañas[sale].gameObject.DesactivarObjeto());
                pestañas[indiceEntrante].transform.SetSiblingIndex(0);
            }
        }

        
        pestañas[indiceEntrante].gameObject.ActivarObjeto();


        indiceActual = indiceEntrante;
    }
}
