using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private GameObject scoreScreen;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private Timer timer;
    private float delay = .2f;
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }
    public void SetScoreScreenActive(bool isActive, bool useDelay)
    {
        if (!useDelay)
        {
            scoreScreen.SetActive(isActive);
            gameUI.SetActive(!isActive);
        }
        else
        {
            StartCoroutine(SetScoreScreenDelayed(isActive));
        }
    }
    private IEnumerator SetScoreScreenDelayed(bool isActive)
    {
        yield return new WaitForSeconds(delay);
        scoreScreen.SetActive(isActive);
        gameUI.SetActive(!isActive);
    }

    public Timer GetTimer()
    {
        timer.gameObject.SetActive(true);
        return timer;
    }
}
