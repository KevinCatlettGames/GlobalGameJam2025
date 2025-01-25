using UnityEngine;

public class DestroyAfterDuration : MonoBehaviour
{
    [SerializeField] private float waitDuration;
    
    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, waitDuration);
    }
}
