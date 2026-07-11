using NaughtyAttributes;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UtilidadesLaEME;

public class PreguntaSeleccionMultipleController : AsterizcoObligatorio, ICampoObligatorioComprobacion
{
    public List<CuadroOpcionItemController> items;

    public CuadroOpcionItemController cuadroSeleccionado;
    public List<CuadroOpcionItemController> cuadrosMultiplesSeleccionados;

    public UnityEvent<int> OnSelect;

    public bool selcMultiple;

    private void Start()
    {
        cuadroSeleccionado = null;
        cuadrosMultiplesSeleccionados.Clear();
        ActualizarTodo();
    }

    private void OnValidate()
    {
        if (items != null)

        foreach (var item in items)
        {
            if (item != null)
            {
                if (item.objetoCirculo != null) item.objetoCirculo.SetActive(!selcMultiple);
                if (item.objetoCuadradoTick != null) item.objetoCuadradoTick.SetActive(selcMultiple);
                item.refPregunta = item.refPregunta != null ? item.refPregunta : this;
            }   
        }

        ActualizarTodo();
    }

    private void OnDisable()
    {
        cuadroSeleccionado = null;
        cuadrosMultiplesSeleccionados.Clear();
        ActualizarTodo();      
    }

    [Button]
    public void ActualizarTodo()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null) 
            {
                Debug.LogError($"No se encontro el objeto de indide [ {i} ] en la lista de items", gameObject);
                return;
            }

            items[i].Actualizar();
            items[i].indiceRespuesta = i;
        }
        
        ToggleObligatorio();

        OnSelect?.Invoke(cuadroSeleccionado != null ? cuadroSeleccionado.indiceRespuesta : -1);      
    }

    public bool EstaContestado()
    {
        if (!selcMultiple)
        {
            contestado = cuadroSeleccionado != null;
            return contestado;
        }
        else
        {
            contestado = cuadrosMultiplesSeleccionados.Count > 0;
            return contestado;
        }   
    }

    public void ToggleObligatorio()
    {
        if (obligatorio_TMP != null) obligatorio_TMP.gameObject.SetActive(!EstaContestado() && campoObligatorio);
    }
}
