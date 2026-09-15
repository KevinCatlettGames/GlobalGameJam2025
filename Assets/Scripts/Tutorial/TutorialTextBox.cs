using UnityEngine;

public class TutorialTextBox : MonoBehaviour
{
    [SerializeField] GameObject[] boxes;
    int currentBox = -1;
    private DotweenAnchorTransition transition;

    private void Start()
    {
        transition = GetComponent<DotweenAnchorTransition>();
    }
    public void AdvanceTextBox()
    {
        if (currentBox == -1)
            ToggleTutorialText(true);
        else
            boxes[currentBox].SetActive(false);

        currentBox++;
        if (currentBox < boxes.Length)
        {
            boxes[currentBox].SetActive(true);
        }
        else
        {
            boxes[0].SetActive(false);
        }
    }

    public void ToggleTutorialText(bool enable)
    {
        if (enable)
            transition.DoIntro();
        else
            transition.DoOutro();
    }
}
