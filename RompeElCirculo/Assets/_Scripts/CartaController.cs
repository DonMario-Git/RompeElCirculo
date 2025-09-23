using AwesomeAttributes;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CartaController : MonoBehaviour
{
    public MemoryMinigameManager memoryManager;
    public ButtonExtrasController buttonExtras;
    [SerializeField] private GameObject otheSideCard;
    [SerializeField] private Image imageObject;
    [SerializeField] private TextMeshProUGUI textObject;
    [Readonly] [SerializeField] private bool isFliped;

    [Readonly]public string itemName;

    [SerializeField] private Transform trTick;
    [Readonly] public bool Complete;

    public void ChancheValues(MemoryObjects memoryItem)
    {
        imageObject.sprite = memoryItem.sprite;
        textObject.text = memoryItem.name;
        itemName = memoryItem.name;
    }

    public void EnableTick()
    {
        trTick.DOScale(1, 0.2f).SetEase(Ease.OutBack);
    }

    public void FlipCard()
    {
        if (!Complete)
        {
            transform.DOKill();
        
            memoryManager.EnableInteractCards(false);
            AudioPlayer.singleton.PlayAudio(7);

            if (!isFliped)
            {    
                transform.DOScaleX(0, 0.2f).OnComplete(() => {
                    otheSideCard.SetActive(true);        
                    transform.DOScaleX(1, 0.3f).SetEase(Ease.OutBack).OnComplete(() => { 
                        memoryManager.EnableInteractCards(true); 
                        memoryManager.CheckCard(this);            
                    });
                });

                isFliped = true;
            }
            else
            {
                if (memoryManager.card1 == this) memoryManager.UnCheckCard(this);

                transform.DOScaleX(0, 0.2f).OnComplete(() => {
                    otheSideCard.SetActive(false);
                    transform.DOScaleX(1, 0.3f).SetEase(Ease.OutBack).OnComplete(() => {
                        memoryManager.EnableInteractCards(true);                  
                    });
                });

                isFliped = false;
            }
        }  
    }
}
