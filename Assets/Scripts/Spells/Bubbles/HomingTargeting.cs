using System.Collections.Generic;
using UnityEngine;

public class HomingTargeting : MonoBehaviour
{
    private List<Transform> targetsInRange = new List<Transform>();

    public Vector3 GetTargetVector()
    {
        if (targetsInRange.Count == 0) return Vector3.zero;
 
        if(targetsInRange[0] != null)
        {
            Vector3 tartetVector = targetsInRange[0].transform.position;
            tartetVector = tartetVector - transform.position;
            tartetVector.Normalize();
            return tartetVector;
        }
        else
        {
            targetsInRange.RemoveAt(0);
        }
        
        return Vector3.zero;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            targetsInRange.Add(other.transform);
        }
        else if (other.CompareTag("Bubble"))
        {
            if (other.TryGetComponent<ExplodingBubble>(out ExplodingBubble explodingBubble))
            {
                targetsInRange.Add(other.transform);
            }           
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if ((other.CompareTag("Player") || other.CompareTag("Bubble")) && targetsInRange.Contains(other.transform))
        {
            targetsInRange.Remove(other.transform);
        }
    }
    public void SetTargeting(float radius, Collider playerCollider, int ID)
    {
        SphereCollider homigCollider = GetComponent<SphereCollider>();
        if (homigCollider != null)
        {
            List<PlayerController> team = GameManager.Instance.GetTeam(ID);
            if (team != null)
            {
                foreach (PlayerController player in team)
                {
                    if (player != null)
                        Physics.IgnoreCollision(homigCollider, player.Controller, true);
                }
            }
            else
            {
                if (playerCollider != null)
                    Physics.IgnoreCollision(homigCollider, playerCollider, true);
            }
        }
        homigCollider.radius = radius;
    }
}
