using UnityEngine;
using System.Collections.Generic;

public class CrabHuntingGrounds : MonoBehaviour
{
    [SerializeField] private bool isMapEventEnabled = true;
    [SerializeField] private float maxRange = 20f;

    private List<Transform> playersInRange = new List<Transform>();
    void Start()
    {
        if (!isMapEventEnabled)
            Destroy(gameObject);
    }

    public Vector3 GetClosestTargetPosition(Vector3 clawPosition)
    {
        Vector3 targetPosition = Vector3.zero;
        if (playersInRange.Count <= 0)
        {
            return targetPosition;
        }
        float lowestDistance = 1000f;
        Vector3 pos;
        for (int i = 0; i < playersInRange.Count; i++)
        {
            pos = playersInRange[i].position;
            float distance = Vector3.Distance(clawPosition, pos);
            if (distance < lowestDistance)
            {
                lowestDistance = distance;
                targetPosition = pos;
            }
        }
        float magnitude = targetPosition.magnitude;
        if (magnitude > maxRange)
        {
            targetPosition *= maxRange / magnitude;
        }
        targetPosition.y = 0;
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
