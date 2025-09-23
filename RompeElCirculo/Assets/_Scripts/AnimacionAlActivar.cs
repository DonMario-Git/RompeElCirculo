using AwesomeAttributes;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class AnimacionAlActivar : MonoBehaviour
{
    public TipoAnimacion tipoAnimacion = TipoAnimacion.ESCALA;

    public float intensidad = 1.1f;
    public float duracion = 0.3f;

    [ShowIf(nameof(bool_texto))]public TextMeshProUGUI texto;
    [ShowIf(nameof(bool_texto))]public Color colorDefaul = Color.white;

    [HideInInspector] public bool bool_texto, bool_escala;

    private void OnValidate()
    {
        if (texto == null && bool_texto)
        {
            texto = GetComponent<TextMeshProUGUI>();
            colorDefaul = texto.color;
        }

        switch (tipoAnimacion)
        {
            case TipoAnimacion.ESCALA:

                bool_escala = true;
                bool_texto = false;
                break;

            case TipoAnimacion.TEXTO:

                bool_escala = false;
                bool_texto = true;
                break;
        }
    }

    public void DesactivarTexto()
    {
        texto.DOKill();
        texto.DOFade(0, 0.3f);
        transform.DOKill();
        transform.DOLocalMoveY(transform.localPosition.y + intensidad, 0.3f).OnComplete(() => gameObject.SetActive(false));
    }

    private void OnDisable()
    {
        switch (tipoAnimacion)
        {
            case TipoAnimacion.ESCALA:

                transform.localScale = Vector3.one * intensidad;
                break;

            case TipoAnimacion.TEXTO:

                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y - intensidad, transform.localPosition.z);
                texto.color = new Color(texto.color.r, texto.color.g, texto.color.b, 0);
                break;
        } 
    }

    private void OnEnable()
    {
        switch (tipoAnimacion)
        {
            case TipoAnimacion.ESCALA:

                OnDisable();
                transform.DOKill();
                transform.DOScale(1, duracion);
                break;

            case TipoAnimacion.TEXTO:

                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y - intensidad, transform.localPosition.z);
                texto.color = new Color(texto.color.r, texto.color.g, texto.color.b, 0);
                texto.DOKill();
                texto.DOColor(colorDefaul, 0.3f);
                transform.DOKill();
                transform.DOLocalMoveY(transform.localPosition.y + intensidad, 0.3f);
                break;
        }
    }

    public enum TipoAnimacion
    {
        ESCALA, TEXTO
    }
}