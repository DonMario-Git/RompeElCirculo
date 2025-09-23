using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public static MainMenuController instance;

    public TextMeshProUGUI nombre;
    public TextMeshProUGUI advertencia;
    public Image x;
    public RectTransform rtr;

    public bool yaSalio;

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        nombre.text = AppManager.data.Nombres;

        string textoAdvertencia = null;
        rtr.localScale = new Vector3(1, 0, 1);

        if (!yaSalio)
        {
            if (AppManager.diasFaltantes > 0 && AppManager.diasFaltantes < 5)
            {
                textoAdvertencia = $"Su licencia termina en {AppManager.diasFaltantes} dias";
            }
            else if (AppManager.diasFaltantes == 0)
            {
                textoAdvertencia = $"Su licencia ha finalizado";
            }
            else if (AppManager.diasFaltantes == -1)
            {
                textoAdvertencia = $"Ocurrió un error al comprobar su licencia";
            }

            if (!string.IsNullOrEmpty(textoAdvertencia))
            {               
                advertencia.text = textoAdvertencia;
                rtr.DOKill();  
                rtr.DOScaleY(1, 0.5f).SetEase(Ease.OutBack);

                StartCoroutine(Animacion());
                yaSalio = true;
            }

            IEnumerator Animacion()
            {
                yield return new WaitForSeconds(2);

                x.raycastTarget = true;
                x.gameObject.SetActive(true);
            }
        }
    }

    public void OcultarPestañaAdvertencia()
    {
        rtr.DOScaleY(0, 0.5f).SetEase(Ease.InBack).OnComplete(() => { 
            x.gameObject.SetActive(false);
            x.raycastTarget = false;
        });
    }
}