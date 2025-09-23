using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtilidadesLaEME;

public class BotonNivelController : MonoBehaviour
{
    public TextMeshProUGUI TMP_numeroNivel;
    public Image chuloIm;
    public ParticleSystem particulasListo;
    public Image im;

    private void OnDisable()
    {
        particulasListo.Stop();
    }

    [ContextMenu(nameof(ActivarComoSiguiente))]
    public void ActivarComoSiguiente()
    {   
        im.color = new Color(0.73f, 0.42f, 1);
        particulasListo.Play();
        chuloIm.gameObject.Disable();
        TMP_numeroNivel.Enable();
        TMP_numeroNivel.color = new Color(0.7f, 1f, 0.28f);
        transform.localScale = Vector3.one * 1.2f;
        transform.DOKill();
        transform.DOScale(1, 0.3f);
    }

    [ContextMenu(nameof(ActivarNormal))]
    public void ActivarNormal()
    {
        im.color = new Color(0.65f, 0.61f, 0.8f);
        particulasListo.Stop();
        chuloIm.gameObject.Disable();
        TMP_numeroNivel.Enable();
        TMP_numeroNivel.color = Color.white;
        transform.localScale = Vector3.one * 0.8f;
        transform.DOKill();
    }

    [ContextMenu(nameof(Desactivar))]
    public void Desactivar()
    {
        im.color = new Color(0.63f, 0.63f, 0.63f);
        TMP_numeroNivel.Disable();
        particulasListo.Stop();
        chuloIm.gameObject.Enable();
        transform.localScale = Vector3.one * 0.8f;
        transform.DOKill();
    }
}
