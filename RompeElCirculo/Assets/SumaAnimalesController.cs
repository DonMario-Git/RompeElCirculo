using UnityEngine;
using TMPro;
using UnityEngine.UI; 
using System;

public class SumaAnimalesController : MonoBehaviour
{
    [Serializable]
    public struct AnimalData
    {
        public Sprite sprite;
        [HideInInspector] public int valor;
    }

    [Serializable]
    public struct GrupoAnimales
    {
        public AnimalData animalA;
        public AnimalData animalB;
        public AnimalData animalC;
    }

    [Serializable]
    public struct EjerciciosResultado
    {
        public string resultado1;
        public string resultado2;
        public string resultado3;
    }

    [Header("Grupos de Animales")]
    public GrupoAnimales[] gruposDeAnimales;

    private AnimalData animalA, animalB, animalC;

    public int minValor = 3;
    public int maxValor = 9;

    // Campos para UI
    [Header("Referencias UI - Valores de Animales")]
    public TMPro.TextMeshProUGUI valorAnimal1Text;
    public TMPro.TextMeshProUGUI valorAnimal2Text;
    public TMPro.TextMeshProUGUI valorAnimal3Text;

    [Header("Referencias UI - Sprites de Animales")]
    public Image animal1Image;
    public Image animal2Image;
    public Image animal3Image;

    [Header("Referencias UI - Sprites de Operaciones")]
    public Image operacion1AnimalAImage;
    public Image operacion1AnimalBImage;
    public Image operacion2AnimalAImage;
    public Image operacion2AnimalBImage;
    public Image operacion3AnimalAImage;
    public Image operacion3AnimalBImage;

    [Header("Referencias UI - Signos de Operaciones")]
    public TMPro.TextMeshProUGUI signoOperacion1Text;
    public TMPro.TextMeshProUGUI signoOperacion2Text;
    public TMPro.TextMeshProUGUI signoOperacion3Text;

    [Header("Referencias UI - Inputs de Respuesta")]
    public WordInputControllerr inputResultado1;
    public WordInputControllerr inputResultado2;
    public WordInputControllerr inputResultado3;

    void Start()
    {
        SeleccionarGrupoAleatorioYGenerarValores();
        ActualizarUI();
    }

    // Selecciona un grupo aleatorio y asigna valores únicos y mezclados a los animales
    public void SeleccionarGrupoAleatorioYGenerarValores()
    {
        if (gruposDeAnimales == null || gruposDeAnimales.Length == 0)
        {
            Debug.LogError("No hay grupos de animales configurados");
            return;
        }
        int grupoIdx = UnityEngine.Random.Range(0, gruposDeAnimales.Length);
        var grupo = gruposDeAnimales[grupoIdx];
        animalA = grupo.animalA;
        animalB = grupo.animalB;
        animalC = grupo.animalC;

        // Asignar valores únicos y mezclados
        int[] posiblesValores = new int[maxValor - minValor + 1];
        for (int i = 0; i < posiblesValores.Length; i++)
            posiblesValores[i] = minValor + i;
        // Mezclar
        for (int i = 0; i < posiblesValores.Length; i++)
        {
            int j = UnityEngine.Random.Range(i, posiblesValores.Length);
            int temp = posiblesValores[i];
            posiblesValores[i] = posiblesValores[j];
            posiblesValores[j] = temp;
        }
        animalA.valor = posiblesValores[0];
        animalB.valor = posiblesValores[1];
        animalC.valor = posiblesValores[2];
        // Asegurar que animalC >= animalA para que la resta no sea negativa
        if (animalC.valor < animalA.valor)
        {
            int temp = animalC.valor;
            animalC.valor = animalA.valor;
            animalA.valor = temp;
        }
    }

    // Devuelve los resultados de los ejercicios como strings
    public EjerciciosResultado GenerarEjercicios()
    {
        int v0 = animalA.valor;
        int v1 = animalB.valor;
        int v2 = animalC.valor;
        // Ejemplo: (A+B), (C-A), (B+C)
        string r1 = (v0 + v1).ToString();
        string r2 = (v2 - v0).ToString();
        string r3 = (v1 + v2).ToString();
        return new EjerciciosResultado { resultado1 = r1, resultado2 = r2, resultado3 = r3 };
    }

    // Actualiza los campos de UI con los valores y sprites de los animales y ejercicios
    public void ActualizarUI()
    {
        // Actualizar valores
        if (valorAnimal1Text != null) valorAnimal1Text.text = animalA.valor.ToString();
        if (valorAnimal2Text != null) valorAnimal2Text.text = animalB.valor.ToString();
        if (valorAnimal3Text != null) valorAnimal3Text.text = animalC.valor.ToString();
        // Actualizar sprites de animales
        if (animal1Image != null) animal1Image.sprite = animalA.sprite;
        if (animal2Image != null) animal2Image.sprite = animalB.sprite;
        if (animal3Image != null) animal3Image.sprite = animalC.sprite;
        // Actualizar sprites de operaciones (según el ejemplo)
        // Ejercicio 1: animalA + animalB
        if (operacion1AnimalAImage != null) operacion1AnimalAImage.sprite = animalA.sprite;
        if (operacion1AnimalBImage != null) operacion1AnimalBImage.sprite = animalB.sprite;
        // Ejercicio 2: animalC - animalA
        if (operacion2AnimalAImage != null) operacion2AnimalAImage.sprite = animalC.sprite;
        if (operacion2AnimalBImage != null) operacion2AnimalBImage.sprite = animalA.sprite;
        // Ejercicio 3: animalB + animalC
        if (operacion3AnimalAImage != null) operacion3AnimalAImage.sprite = animalB.sprite;
        if (operacion3AnimalBImage != null) operacion3AnimalBImage.sprite = animalC.sprite;
        // Actualizar signos de operaciones
        if (signoOperacion1Text != null) signoOperacion1Text.text = "+";
        if (signoOperacion2Text != null) signoOperacion2Text.text = "-";
        if (signoOperacion3Text != null) signoOperacion3Text.text = "+";
        // Actualizar resultados correctos en los WordInputControllerr
        var resultados = GenerarEjercicios();
        if (inputResultado1 != null && inputResultado1.correctWorld != null) inputResultado1.correctWorld.ES = resultados.resultado1;
        if (inputResultado2 != null && inputResultado2.correctWorld != null) inputResultado2.correctWorld.ES = resultados.resultado2;
        if (inputResultado3 != null && inputResultado3.correctWorld != null) inputResultado3.correctWorld.ES = resultados.resultado3;
    }
}
