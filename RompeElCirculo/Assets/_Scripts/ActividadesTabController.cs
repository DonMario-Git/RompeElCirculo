using AwesomeAttributes;
using DG.Tweening;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UtilidadesLaEME;

public class ActividadesTabController : MonoBehaviour
{
    public static ActividadesTabController singleton;

    public TextMeshProUGUI TMP_textoRango;

    public BotonNivelController[] botones;

    public Image barraProgreso;
    Tween a;

    public ButtonExtrasController botonPlay;

    public TextMeshProUGUI textoRango2;
    public Transform fondoNegro;
    public Image fondoBlanco;
    public GameObject textoContinuar;
    public ParticleSystem particulas;


    [Button(nameof(ActualizarSkin))]
    public int skinID;

    public ImageSkin[] skinImOptions;
    public EnableGameObject[] skinObjOptions;

    [System.Serializable]
    public struct ImageSkin
    {
        public Image imagen;
        public Color[] colores;

        public readonly void Actualizar(int i)
        {
            imagen.color = colores[i];
        }
    }

    [System.Serializable]
    public struct EnableGameObject
    {
        public GameObject obj;
        public int condition;

        public readonly void Actualizar(int i)
        {
            obj.SetActive(condition == i);
        }
    }

    private void Awake()
    {
        singleton = this;
    }

    public void ActualizarSkin()
    {
        foreach (var item in skinImOptions)
        {
            item.Actualizar(skinID);
        }

        foreach (var item in skinObjOptions)
        {
            item.Actualizar(skinID);
        }
    }

    private void OnEnable()
    {
        Actualizar();
    }

    [Header("Degradados fijos")]
    public Gradient gradient1;
    public Gradient gradient2;

    [Header("Sprites posibles")]
    public Sprite[] spriteOptions1; // para sprite1
    public Sprite[] spriteOptions2; // para sprite2

    public Image imagenMed1, imagenMed2;

    public CombinationResult GetCombination(int seed)
    {
        System.Random rng = new System.Random(seed);

        // Colores deterministas de degradados
        float t1 = (float)rng.NextDouble();
        float t2 = (float)rng.NextDouble();

        Color c1 = gradient1.Evaluate(t1);
        Color c2 = gradient2.Evaluate(t2);

        // Sprite1 desde array1
        Sprite s1 = spriteOptions1.Length > 0
            ? spriteOptions1[rng.Next(spriteOptions1.Length)]
            : null;

        // Sprite2 desde array2
        Sprite s2 = spriteOptions2.Length > 0
            ? spriteOptions2[rng.Next(spriteOptions2.Length)]
            : null;

        return new CombinationResult(c1, c2, s1, s2);
    }

    public void Actualizar()
    {
        var medallaSkin = GetCombination(AppManager.data.numeroRango);

        imagenMed1.sprite = medallaSkin.sprite1;
        imagenMed2.sprite = medallaSkin.sprite2;
        imagenMed1.color = medallaSkin.color1;
        imagenMed2.color = medallaSkin.color2;

        TMP_textoRango.text = AppManager.data.numeroRango.ToString();

        if (AppManager.data.numeroSiguienteNivel < 3)
        {
            particulas.Stop();
            a?.Kill();
            botonPlay.button.interactable = false;

            // Datos por cada nivel: valor inicial, valor final, botón a activar
            var datos = new (float inicio, float fin, int boton)[]
            {
                (0f,     0.277f, 0), // Caso 0
                (0.277f, 0.489f, 1), // Caso 1
                (0.489f, 0.677f, 2)  // Caso 2
            };

            // Configurar botones según el nivel
            for (int i = 0; i < botones.Length; i++)
            {
                if (i < AppManager.data.numeroSiguienteNivel) botones[i].Desactivar();
                else botones[i].ActivarNormal();
            }

            // Asignar valores de la barra
            barraProgreso.fillAmount = datos[AppManager.data.numeroSiguienteNivel].inicio;

            // Crear tween
            a = DOTween.To(
                () => barraProgreso.fillAmount,
                x => barraProgreso.fillAmount = x,
                datos[AppManager.data.numeroSiguienteNivel].fin,
                1.5f
            ).OnComplete(() =>
                {
                    botonPlay.button.interactable = true;
                    botonPlay.button.image.raycastTarget = false;
                    botones[datos[AppManager.data.numeroSiguienteNivel].boton].ActivarComoSiguiente();

                    Color colorDefault = botonPlay.button.image.color;
                    botonPlay.button.image.color = Color.white;
                    botonPlay.button.image.DOKill();
                    botonPlay.button.image.DOColor(colorDefault, 0.5f);

                    botonPlay.transform.localScale = Vector3.one * 1.2f;
                    botonPlay.transform.DOKill();
                    botonPlay.transform.DOScale(1, 0.5f).OnComplete(() => botonPlay.button.image.raycastTarget = true);
                }
            );
        }
        else
        {
            a?.Kill();
            botonPlay.button.interactable = false;
            barraProgreso.fillAmount = 0.677f;

            AppManager.data.numeroRango++;
            TMP_textoRango.text = AppManager.data.numeroRango.ToString();

            foreach (var item in botones)
            {
                item.Desactivar();
            }
            
            AppManager.data.numeroSiguienteNivel = 0;

            FirebaseStorageManager.singleton.SaveData(AppManager.data, AppManager.data.Nombres, true, (resultado) => {
                if (!string.IsNullOrEmpty(resultado)) Debug.LogWarning(resultado);
            }, false);

            a = DOTween.To(
                () => barraProgreso.fillAmount,
                x => barraProgreso.fillAmount = x,
                1,
                1.5f
            ).OnComplete(() =>
                {
                    fondoBlanco.raycastTarget = false;
                    textoRango2.text = AppManager.data.numeroRango.ToString();
                    fondoBlanco.gameObject.Enable();
                    fondoNegro.transform.localScale = Vector3.zero;
                    fondoNegro.gameObject.Enable();
                    fondoNegro.DOKill();
                    fondoNegro.DOScale(1, 0.5f).SetEase(Ease.OutBack).OnComplete(() => {
                        textoContinuar.Enable();
                        particulas.Play();
                        fondoBlanco.raycastTarget = true;
                    });

                    Actualizar();
                }
            );     
        }
    }

    public void DesabilitarPestañaRango()
    {
        fondoBlanco.gameObject.Disable();
        fondoNegro.gameObject.Disable();
        fondoBlanco.raycastTarget = false;
    }
}

[System.Serializable]
public struct CombinationResult
{
    public Color color1;
    public Color color2;
    public Sprite sprite1;
    public Sprite sprite2;

    public CombinationResult(Color c1, Color c2, Sprite s1, Sprite s2)
    {
        color1 = c1;
        color2 = c2;
        sprite1 = s1;
        sprite2 = s2;
    }
}