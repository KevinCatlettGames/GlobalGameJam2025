using System.Collections.Generic;
using UnityEngine;

public class HomingTargeting : MonoBehaviour
{
    private List<Transform> targetsInRange = new List<Transform>();

    public Vector3 GetTargetVector()
    {
        // Clean out any destroyed or null targets from the front of the list
        while (targetsInRange.Count > 0 && targetsInRange[0] == null)
        {
            targetsInRange.RemoveAt(0);
        }

        if (targetsInRange.Count == 0) return Vector3.zero;

        // Calculate the normalized direction vector toward our target
        Vector3 targetPosition = targetsInRange[0].position;
        Vector3 targetVector = targetPosition - transform.position;
        targetVector.Normalize();
        return targetVector;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        if (other.CompareTag("Player"))
        {
            if (!targetsInRange.Contains(other.transform))
            {
                targetsInRange.Add(other.transform);
            }
        }
        else if (other.CompareTag("Bubble"))
        {
            if (other.TryGetComponent<ExplodingBubble>(out ExplodingBubble explodingBubble))
            {
                if (!targetsInRange.Contains(other.transform))
                {
                    targetsInRange.Add(other.transform);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null) return;

        if ((other.CompareTag("Player") || other.CompareTag("Bubble")) && targetsInRange.Contains(other.transform))
        {
            targetsInRange.Remove(other.transform);
        }
    }

    public void SetTargeting(float radius, Collider playerCollider, int ID)
    {
        SphereCollider homingCollider = GetComponent<SphereCollider>();
        if (homingCollider == null) return;

        homingCollider.radius = radius;

        // --- CLIENT/SERVER TEAM SAFE CHECK ---
        // Ensure the GameManager and Team repository exist before filtering team collision rules
        if (GameManager.Instance != null)
        {
            List<PlayerController> team = GameManager.Instance.GetTeam(ID);
            if (team != null)
            {
                foreach (PlayerController player in team)
                {
                    if (player != null && player.Controller != null)
                    {
                        Physics.IgnoreCollision(homingCollider, player.Controller, true);
                    }
                }
                return; // Successfully ignored teammates!
            }
        }

        // Fallback: If team data isn't initialized on the client yet, at least ignore the shooter
        if (playerCollider != null)
        {
            Physics.IgnoreCollision(homingCollider, playerCollider, true);
        }
    }
}