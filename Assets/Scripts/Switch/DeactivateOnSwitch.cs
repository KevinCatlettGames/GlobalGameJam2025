using UnityEngine;

public class DeactivateOnSwitch : MonoBehaviour
{
    public float delay = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Invoke(nameof(Deactivate), delay);
    }

    void Deactivate()
    {
#if UNITY_SWITCH
        gameObject.SetActive(false);
#endif 
    }
}