using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class RutaAtencionController : MonoBehaviour
{
    public GameObject[] pestañasInformacion;
    public RectTransform pestañaDesplegable;
    public Image fondoNegro;

    private void OnEnable()
    {
        pestañaDesplegable.DOKill();
        pestañaDesplegable.anchoredPosition = new Vector2(0, -2163);
        pestañaDesplegable.gameObject.SetActive(false);

        foreach (var pestaña in pestañasInformacion)
        {
            pestaña.SetActive(false);
        }
    }

    public void AbrirPestañaInformacion(int index)
    {
        pestañasInformacion[index].SetActive(true);
        fondoNegro.gameObject.SetActive(true);
        pestañaDesplegable.gameObject.SetActive(true);
        pestañaDesplegable.DOKill();
        pestañaDesplegable.DOAnchorPosY(0, 0.3f).SetEase(Ease.OutQuart);
    }

    public void CerrarTodasLasPestañas()
    {   
        pestañaDesplegable.DOKill();
        pestañaDesplegable.DOAnchorPosY(-2163, 0.2f).SetEase(Ease.Linear).OnComplete(() =>
        {
            pestañaDesplegable.gameObject.SetActive(false);
            foreach (var pestaña in pestañasInformacion)
            {
                pestaña.SetActive(false);
            }

            fondoNegro.gameObject.SetActive(false);
        });
    }
}
