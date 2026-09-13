using UnityEngine;

public class TutorialTextBox : MonoBehaviour
{
    [SerializeField] GameObject[] boxes;
    int currentBox = 0;
    public void AdvanceTextBox()
    {
        if (currentBox == 0)
            boxes[0].SetActive(true); //Enables the Frame
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
}
