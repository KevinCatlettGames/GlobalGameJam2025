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
    public void PlayVictoryAnimation(int winnerID)
    {
        if (winnerID == -1)
        {
            victoryAnimator.gameObject.SetActive(false);
            return;
        }
        victoryAnimator.gameObject.SetActive(true);
        victoryAnimator.Play($"P{winnerID}");
    }
    public float GetVictoryAnimationDuration()
    {
        AnimatorStateInfo animatorStateInfo = victoryAnimator.GetCurrentAnimatorStateInfo(0);
        return animatorStateInfo.length;
    }
    public Timer GetTimer()
    {
        timer.gameObject.SetActive(true);
        return timer;
    }
}
