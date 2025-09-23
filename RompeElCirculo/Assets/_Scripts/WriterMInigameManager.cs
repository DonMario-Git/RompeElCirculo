using AwesomeAttributes;
using UnityEngine;

public class WriterMInigameManager : MonoBehaviour
{
    public WordInputControllerr[] woldInputs;

    [Readonly] public bool isComplete;

    private void OnDisable()
    {
        isComplete = false;
    }

    public void Confirm()
    {
        if (!isComplete)
        {
            bool isIncomplete = false;

            foreach (var item in woldInputs)
            {
                if (!item.IsCorrect)
                {
                    isIncomplete = true;
                }
            }

            if (!isIncomplete)
            {
                isComplete = true;
                FinalScreenController.instance.Evaluate(true);
            }   
        }       
    }
}
