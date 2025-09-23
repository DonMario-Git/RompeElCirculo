using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FormularioLonida
{
    public class PreguntaFormularioController : MonoBehaviour
    {
        public bool respuestaSeleccionada;
        public int indiceRespuesta;

        public RespuestaFormulario[] posiblesRespuestas;
        public FormularioController formulario;

        public Image imagenFrentePregunta;
        public bool animacionAtras;

        private void OnEnable()
        {
            formulario.Verificar();
            transform.localPosition = new Vector3(animacionAtras ? -512 : 512, 0, 0);
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOLocalMove(Vector3.zero, 0.4f);
            imagenFrentePregunta.color = formulario.colorEmpresarial1;
            imagenFrentePregunta.raycastTarget = true;
            imagenFrentePregunta.DOKill();
            imagenFrentePregunta.DOFade(0, 0.4f).OnComplete(() => imagenFrentePregunta.raycastTarget = false);
        }

        public void ResponderPregunta(int indice)
        {
            respuestaSeleccionada = true;
            indiceRespuesta = indice;

            foreach (var item in posiblesRespuestas)
            {
                item.objetoActivar.SetActive(false);
            }

            posiblesRespuestas[indice].objetoActivar.SetActive(true);
            formulario.Verificar();
        }
    }
}