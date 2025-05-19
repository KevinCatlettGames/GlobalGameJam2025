using UnityEngine;

public class ShaderTimeReset : MonoBehaviour
{
    [SerializeField] private Material material;
    void Start()
    {
        material.SetFloat("_StartTime", Time.time);        
    }
}
