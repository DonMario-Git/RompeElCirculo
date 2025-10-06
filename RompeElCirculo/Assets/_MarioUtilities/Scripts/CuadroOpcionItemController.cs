using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtilidadesLaEME;

[ExecuteAlways]
public class CuadroOpcionItemController : MonoBehaviour
{
    public TextMeshProUGUI respuestaEMP;
    public PreguntaSeleccionMultipleController refPregunta;
    public Button buton;

    public GameObject objetoCirculo, objetoCuadradoTick;

    public Image circuloInterno, circuloChulo;

    public int indiceRespuesta;

    private void Awake()
    {
        if (!refPregunta.items.Contains(this)) refPregunta.items.Add(this);  
    }

    private void OnEnable()
    {
        if (refPregunta.selcMultiple)
        {
            if (refPregunta.cuadrosMultiplesSeleccionados.Contains(this)) refPregunta.cuadrosMultiplesSeleccionados.Remove(this);
        }
    }

    public void Seleccionar()
    {
        if (refPregunta.selcMultiple)
        {
            if (refPregunta.cuadrosMultiplesSeleccionados.Contains(this))
            {
                refPregunta.cuadrosMultiplesSeleccionados.Remove(this);
            }
            else
            {
                refPregunta.cuadrosMultiplesSeleccionados.Add(this);
            }
        }
        else
        {
            refPregunta.cuadroSeleccionado = refPregunta.cuadroSeleccionado == this ? refPregunta.cuadroSeleccionado = null : refPregunta.cuadroSeleccionado = this;  
        }  
        
        refPregunta.ActualizarTodo();
    }

    public void Actualizar()
    {
        if (refPregunta.selcMultiple)
        {
            circuloChulo.enabled = refPregunta.cuadrosMultiplesSeleccionados.Contains(this);
        }
        else
        {
            if (refPregunta.cuadroSeleccionado == this)
            {
                buton.image.color = buton.colors.pressedColor;
                circuloInterno.ActivarComponente();
            }
            else
            {
                buton.image.color = buton.colors.normalColor;
                circuloInterno.DesactivarComponente();
            }
        }    
    }

    private void OnDestroy()
    {
        if (refPregunta.items.Contains(this)) refPregunta.items.Remove(this);
    }
}
