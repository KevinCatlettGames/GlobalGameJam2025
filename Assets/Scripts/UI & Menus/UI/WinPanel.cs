using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinPanel : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private TextMeshProUGUI killText;
    [SerializeField] private GameObject wins;
    [SerializeField] private GameObject kills;

    public void SetPanel(Sprite sprite, int winCount, int killCount)
    {
        image.enabled = true;
        image.sprite = sprite;
        wins.SetActive(true);
        kills.SetActive(true);
        winText.text = winCount.ToString();
        killText.text = killCount.ToString();
    }
}
