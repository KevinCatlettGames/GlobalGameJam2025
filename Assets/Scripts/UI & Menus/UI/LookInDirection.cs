using UnityEngine;

public class LookInDirection : MonoBehaviour
{
    [SerializeField] private Vector3 lookDirection = Vector3.forward; 

    void Update()
    {
        transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
    }
}