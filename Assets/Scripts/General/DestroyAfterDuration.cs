using UnityEngine;

public class DestroyAfterDuration : MonoBehaviour
{
    [SerializeField] private float waitDuration;
    [SerializeField] private bool destroyOnRestart = true;
    
    void Start()
    {
        if(destroyOnRestart) GameManager.Instance.OnGameStarted += DestroyOnRestart;
        Destroy(gameObject, waitDuration);
    }

    private void DestroyOnRestart()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if(destroyOnRestart) GameManager.Instance.OnGameStarted -= DestroyOnRestart;
    }
}
