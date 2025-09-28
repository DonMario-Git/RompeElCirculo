using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UtilidadesLaEME;

public class PestañasManager : MonoBehaviour
{
    public PestañaController[] pestañas;
    public Image _FRENTE;
    public Transform _PESTAÑAS;

    private void Start()
    {
        InicializarApp();
    }

    public void CambiarPestaña(int indice)
    {
        PantallaTransicionController.singleton.Transicionar(() => {

            foreach (var item in pestañas)
            {
                item.gameObject.Disable();
            }

            pestañas[indice].gameObject.Enable();
        });
    }

    public void InicializarApp()
    {
        _PESTAÑAS.localScale = Vector3.one * 1.2f;
        _PESTAÑAS.DOKill();
        _PESTAÑAS.DOScale(Vector3.one, 0.3f);

        _FRENTE.DOKill();
        _FRENTE.DOFade(0, 0.3f).OnComplete(() => _FRENTE.raycastTarget = false);
    }
}
