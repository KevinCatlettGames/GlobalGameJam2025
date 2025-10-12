using UnityEngine;
using System.Collections.Generic;

public class CrabHuntingGrounds : MonoBehaviour
{
    [SerializeField] private bool isMapEventEnabled = true;
    [SerializeField] private float minRadius = 3.0f;

    private List<Transform> playersInRange = new List<Transform>();
    void Start()
    {
        if (!isMapEventEnabled)
            Destroy(gameObject);    
        Debug.DrawRay(transform.position, transform.forward * minRadius, Color.red, 100f);
    }

    public Vector3 GetClosestTargetPosition(Vector3 clawPosition)
    {
        Vector3 targetPosition = Vector3.zero;
        if (playersInRange.Count <= 0)
        {
            return targetPosition;
        }
        float lowestDistance = 100f;
        for (int i = 0; i < playersInRange.Count; i++)
        {
            Vector3 pos = playersInRange[i].position;
            if (pos.magnitude < minRadius)
                continue;
            float distance = Vector3.Distance(clawPosition, pos);
            if (distance < lowestDistance)
            {
                lowestDistance = distance;
                targetPosition = pos;
            }
        }
        return targetPosition;
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
        if ((other.CompareTag("Player") && playersInRange.Contains(other.transform)))
        {
            playersInRange.Remove(other.transform);
        }
    }
}
