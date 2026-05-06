using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private GameObject scoreScreen;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private Timer timer;
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }
    public void SetScoreScreenActive(bool isActive)
    {
        scoreScreen.SetActive(isActive);
        gameUI.SetActive(!isActive);
    }
    public Timer GetTimer()
    {
        timer.gameObject.SetActive(true);
        return timer;
    }
}
