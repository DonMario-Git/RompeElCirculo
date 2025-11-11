using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UtilidadesLaEME;

public class PestañasManager : MonoBehaviour
{
    public static PestañasManager singleton;
    public PestañaController[] pestañas;
    public Image _FRENTE;
    public Transform _PESTAÑAS;

    private void Awake()
    {
        singleton = this;
    }

    public void CambiarPestaña(int indice)
    {
        PantallaTransicionController.singleton.Transicionar(() => {

            CambiarPestañaSinTransicion(indice);
        });
    }

    public void CambiarPestañaSinTransicion(int indice, System.Action AlCambiar = null)
    {
        foreach (var item in pestañas)
        {
            item.gameObject.DesactivarObjeto();
        }

        pestañas[indice].gameObject.ActivarObjeto();
        pestañas[indice].transform.localScale = Vector3.one * 1.1f;
        pestañas[indice].DOKill();
        pestañas[indice].transform.DOScale(1, 0.2f);

        AlCambiar?.Invoke();
    }

    public void AnimacionInicio()
    {
        _PESTAÑAS.localScale = Vector3.one * 1.2f;
        _PESTAÑAS.DOKill();
        _PESTAÑAS.DOScale(Vector3.one, 0.3f);

        _FRENTE.DOKill();
        _FRENTE.DOFade(0, 0.3f).OnComplete(() => _FRENTE.raycastTarget = false);
    }
}
