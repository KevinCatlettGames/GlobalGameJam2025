using System.Collections.Generic;
using UnityEngine;

public class HomingTargeting : MonoBehaviour
{
    private List<Transform> targetsInRange = new List<Transform>();

    public Vector3 GetTargetVector()
    {
        while (targetsInRange.Count > 0 && targetsInRange[0] == null)
        {
            targetsInRange.RemoveAt(0);
        }

        if (targetsInRange.Count == 0) return Vector3.zero;

        Vector3 targetPosition = targetsInRange[0].position;
        Vector3 targetVector = targetPosition - transform.position;
        targetVector.Normalize();
        return targetVector;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        if (other.transform.root == transform.root) return;

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
                return;
            }
        }

        if (playerCollider != null)
        {
            Physics.IgnoreCollision(homingCollider, playerCollider, true);
        }
    }
}