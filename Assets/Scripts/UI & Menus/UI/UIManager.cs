using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private Animator victoryAnimator;
    [SerializeField] private GameObject scoreScreen;
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
    }
    public Timer GetTimer()
    {
        timer.gameObject.SetActive(true);
        return timer;
    }
}
