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

    private void OnEnable()
    {
        if (refPregunta == null)
        {
            //Debug.LogError($"El objeto {name} no tiene asignada una referencia al sistema de preguntas", gameObject);
            return;
        }

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
        if (refPregunta == null) return;
        if (refPregunta.items == null) return;
        if (refPregunta.items.Contains(this)) refPregunta.items.Remove(this);
    }
}
