using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FormularioLonida
{
    [ExecuteAlways]
    public class RespuestaFormulario : MonoBehaviour, IPointerDownHandler
    {
        public GameObject objetoActivar;
        public PreguntaFormularioController pregunta;
        public int indice;

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * 0.8f;
            transform.DOKill();
            transform.DOScale(1, 0.3f);
            pregunta.ResponderPregunta(indice);
        }
    }
}