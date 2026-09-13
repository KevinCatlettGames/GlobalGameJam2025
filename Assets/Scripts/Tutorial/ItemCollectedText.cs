using UnityEngine;

public class ItemCollectedText : MonoBehaviour
{
    [SerializeField] Item[] items;
    [SerializeField] private TutorialTextBox tutorialTextBox;
    [SerializeField] private TutorialPopUp tutorialPopUp;
    private bool isActive = false;

    private void Start()
    {
        foreach (Item item in items)
        {
            item.OnCollected += EnableText;
        }
    }

    private void EnableText()
    {
        if (isActive)
            return;
        if (tutorialPopUp.CheckForDummySpawn())
        {
            isActive = true;
            tutorialTextBox.AdvanceTextBox();
            foreach (Item item in items)
            {
                item.OnCollected -= EnableText;
            }
        }
    }

    private void OnDestroy()
    {
        if (isActive)
            return;
        foreach (Item item in items)
        {
            item.OnCollected -= EnableText;
        }
    }
}
