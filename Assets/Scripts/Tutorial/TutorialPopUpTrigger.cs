using UnityEngine;

public class TutorialPopUpTrigger : MonoBehaviour
{
    private bool isActive = false;
    [SerializeField] private TutorialPopUp tutorialPopUp;

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive)
        {
            isActive = true;
            tutorialPopUp.OpenPopUp();
        }
    }
}
