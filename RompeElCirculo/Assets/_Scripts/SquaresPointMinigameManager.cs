using UnityEngine;

public class SquaresPointMinigameManager : MonoBehaviour
{
    public SelectTheCorrectController[] selecters;

    public void Evaluate()
    {
        foreach (var item in selecters)
        {
            if (!item.isCorrect)
            {
                return;
            }
            else
            {
                foreach (var item2 in item.objects)
                {
                    item2.GetComponent<SquearePointsController>().SetActive(true);
                }
            }
        }

        FinalScreenController.instance.Evaluate(true);
    }

    public void Lose()
    {
        foreach (var item in selecters)
        {
            foreach (var item2 in item.objects)
            {
                item2.GetComponent<SquearePointsController>().SetActive(true);
            }
        }

        FinalScreenController.instance.Evaluate(false);
    }
}
