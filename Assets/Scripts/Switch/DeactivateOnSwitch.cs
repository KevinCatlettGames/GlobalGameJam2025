using UnityEngine;

public class DeactivateOnSwitch : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
#if UNITY_SWITCH
        gameObject.SetActive(false);
#endif 
    }
}