using System.Collections.Generic;
using UnityEngine;

public class HomingTargeting : MonoBehaviour
{
    private List<Transform> playersInRange = new List<Transform>();

    public Vector3 GetTargetVector()
    {
        if (playersInRange.Count == 0) return Vector3.zero;
        Vector3 tartetVector = playersInRange[0].transform.position;
        tartetVector = tartetVector - transform.position;
        tartetVector.Normalize();
        return tartetVector;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInRange.Add(other.transform);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInRange.Remove(other.transform);
        }
    }
    public void SetTargeting(float radius, Collider playerCollider)
    {
        SphereCollider homigCollider = GetComponent<SphereCollider>();
        if (playerCollider != null) Physics.IgnoreCollision(homigCollider, playerCollider, true);
        homigCollider.radius = radius;
    }
}
