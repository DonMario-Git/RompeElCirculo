using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FormularioLonida
{
    public class FormularioController : MonoBehaviour
    {
        public PreguntaFormularioController[] preguntas;
        public ButtonExtrasController botonSiguiente, botonAnterior;
        public TextMeshProUGUI textoIndice;
        public int indicePreguntaActual;
        public Image imagen;
        public Color colorEmpresarial1 = Color.black, colorEmpresarial2 = Color.gray, colorEmpresarial3 = Color.blue;

        private void OnEnable()
        {
            ReiniciarFormulario();
            Verificar();

            imagen.color = Color.black;
            imagen.DOFade(0, 0.3f);
        }

        public void ReiniciarFormulario()
        {
            foreach (var item in preguntas)
            {
                item.respuestaSeleccionada = false;

                foreach (var item2 in item.posiblesRespuestas)
                {
                    item2.objetoActivar.SetActive(false);
                }
            }
        }

        public void Verificar()
        {
            botonSiguiente.button.interactable = preguntas[indicePreguntaActual].respuestaSeleccionada;
            textoIndice.text = $"{indicePreguntaActual + 1} de {preguntas.Length}";
            botonAnterior.button.interactable = indicePreguntaActual > 0;
            botonSiguiente.textButton.text = indicePreguntaActual == preguntas.Length - 1 ? "Terminar" : "Siguiente";
        }

        public void Siguiente()
        {
            if (indicePreguntaActual == preguntas.Length - 1)
            {
                List<int> listaResultados = new();

                foreach (var item in preguntas)
                {
                    listaResultados.Add(item.indiceRespuesta);
                }

                AppManager.data.hizoFormulario = true;
                AppManager.data.respuestasFormularioInicial = listaResultados.ToArray();

                FirebaseStorageManager.singleton.SaveData(AppManager.data, AppManager.data.Nombres, true, (resultado) => {
                    Debug.LogWarning(resultado);
                    imagen.raycastTarget = true;
                    imagen.color = Color.clear;
                    imagen.DOFade(1, 0.5f).OnComplete(() => { 
                        IntroController.singleton.IntroAnimation();
                        gameObject.SetActive(false);
                    });
                });
            }
            else
            {
                preguntas[indicePreguntaActual].gameObject.SetActive(false);

                indicePreguntaActual++;
                preguntas[indicePreguntaActual].animacionAtras = false;
                preguntas[indicePreguntaActual].gameObject.SetActive(true);
            }  
        }

        public void Atras()
        {
            preguntas[indicePreguntaActual].gameObject.SetActive(false);

            indicePreguntaActual--;
            preguntas[indicePreguntaActual].animacionAtras = true;
            preguntas[indicePreguntaActual].gameObject.SetActive(true);
        }
    }
}