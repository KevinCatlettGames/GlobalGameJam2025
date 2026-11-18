using UnityEngine;
using UnityEngine.Splines;

public class GhostEvent : MapEvent
{
    [SerializeField] private SplineContainer[] splines;
    [SerializeField] private float timeBetweenGhosts = 10f;
    [SerializeField] private WindGhost windGhost;
    private int previousPath = -1;
    protected override void StartEvent()
    {
        SendGhost();
    }

    protected override void StopEvent()
    {
        CancelInvoke();
        windGhost.gameObject.SetActive(false);
        previousPath = -1;
    }

    private void SendGhost()
    {
        int hauntPath = Random.Range(0, splines.Length);
        if (hauntPath == previousPath)
        {
            hauntPath++;
            if (hauntPath >= splines.Length)
                hauntPath = 0;
        }
        previousPath = hauntPath;
        windGhost.gameObject.SetActive(true);
        windGhost.HauntPath(splines[hauntPath]);
        Invoke(nameof(SendGhost), timeBetweenGhosts);
    }
}
