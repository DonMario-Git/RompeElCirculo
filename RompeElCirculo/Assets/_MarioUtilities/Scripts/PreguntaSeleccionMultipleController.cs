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
        foreach (var item in items)
        {
            item.objetoCirculo.SetActive(!selcMultiple);
            item.objetoCuadradoTick.SetActive(selcMultiple);
        }

        ActualizarTodo();
    }

    private void OnDisable()
    {
        cuadroSeleccionado = null;
        cuadrosMultiplesSeleccionados.Clear();
        ActualizarTodo();      
    }

    public void ActualizarTodo()
    {
        for (int i = 0; i < items.Count; i++)
        {
            items[i].Actualizar();
            items[i].indiceRespuesta = i;
        }

        OnSelect?.Invoke(cuadroSeleccionado != null ? cuadroSeleccionado.indiceRespuesta : -1);
        ToggleObligatorio();
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
        obligatorio_TMP.gameObject.SetActive(!EstaContestado() && campoObligatorio);
    }
}
