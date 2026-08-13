using UnityEngine;
using UnityEngine.UI; 

public class TutorialIndicator : MonoBehaviour
{
    [SerializeField] Image image;
    [SerializeField] Sprite activeSprite;
    [SerializeField] Color activeColor = Color.green;
    [SerializeField] Sprite inactiveSprite;
    [SerializeField] Color inactiveColor = Color.red;

    private void Start()
    {
        int tutorialActive = PlayerPrefs.GetInt("PlayedTutorial");
        image.sprite = tutorialActive == 0 ? activeSprite : inactiveSprite;
        image.color = tutorialActive == 0 ? activeColor : inactiveColor;
    }

    public void EvaluateState()
    {
        if(LobbyManager.instance && TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
        {
            image.sprite = inactiveSprite;
            image.color = inactiveColor;
            return;
        }
        if (LobbyManager.instance)
        {
            image.sprite = LobbyManager.instance.playTutorial ? activeSprite : inactiveSprite;
            image.color = LobbyManager.instance.playTutorial ? activeColor : inactiveColor;
        }
    }
}