using AwesomeAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilidadesLaEME;

public class MemoryMinigameManager : MonoBehaviour
{
    public MemoryObjects[] memoryObjects;
    public List<CartaController> cardsList;

    [Readonly] public CartaController card1;

    private void Start()
    {
        RestartMinigame();
    }

    public void RestartMinigame()
    {
        // 1. Crear una lista de pares de MemoryObjects
        var pairedMemoryObjects = new List<MemoryObjects>();
        int numPairs = cardsList.Count / 2;

        // Si hay más cartas que pares posibles, limitar al máximo posible
        for (int i = 0; i < numPairs; i++)
        {
            var memoryObj = memoryObjects[i % memoryObjects.Length];
            pairedMemoryObjects.Add(memoryObj);
            pairedMemoryObjects.Add(memoryObj); // Añadir el par
        }

        // Si hay un número impar de cartas, puedes decidir si dejas una sin pareja o la eliminas
        // Aquí simplemente se ignora la última carta si sobra

        // 2. Barajar la lista de pares
        pairedMemoryObjects.Shuffle();

        // 3. Asignar los objetos a las cartas
        for (int i = 0; i < cardsList.Count && i < pairedMemoryObjects.Count; i++)
        {
            cardsList[i].ChancheValues(pairedMemoryObjects[i]);
        }
    }

    public void CheckCard(CartaController card)
    {
        if (card1 == null)
        {
            card1 = card;
        }
        else
        {
            if (card1.itemName == card.itemName)
            {
                card1.buttonExtras.button.interactable = false;
                card.buttonExtras.button.interactable = false;
                card1.EnableTick();
                card.EnableTick();
                AudioPlayer.singleton.PlayAudio(8);
                card1.Complete = true;
                card.Complete = true;
                card1 = null;

                foreach (var item in cardsList)
                {
                    if (!item.Complete)
                    {
                        return;
                    }
                }

                FinalScreenController.instance.Evaluate(true);
            }
            else
            {
                card1.FlipCard();
                card.FlipCard();
                card1 = null;
            }
        }
    }

    public void EnableInteractCards(bool value)
    {
        foreach (var item in cardsList)
        {
            item.buttonExtras.button.interactable = value;
        }
    }

    public void UnCheckCard(CartaController card)
    {
        if (card1 == card)
        {
            card1 = null;
        }
    }
}

[System.Serializable]
public struct Memorias
{
    public MemoryObjects[] memoryObjects;
}

[System.Serializable]
public struct MemoryObjects
{
    public string name;
    public Sprite sprite;
}
