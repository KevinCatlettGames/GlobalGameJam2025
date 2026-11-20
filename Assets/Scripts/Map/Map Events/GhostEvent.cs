using UnityEngine;
using UnityEngine.Splines;

public class GhostEvent : MapEvent
{
    [SerializeField] private SplineContainer[] splines;
    [SerializeField] private float timeBetweenGhosts = 10f;
    [SerializeField] private WindGhost windGhost_A;
    [SerializeField] private WindGhost windGhost_B;
    private WindGhost windGhost;
    private int previousPath = -1;
    protected override void StartEvent()
    {
        windGhost = windGhost_A;
        SendGhost();
        Invoke(nameof(SendGhost), timeBetweenGhosts * 1.7f);
    }

    protected override void StopEvent()
    {
        CancelInvoke();
        windGhost_A.StopGhost();
        windGhost_B.StopGhost();
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
        windGhost = windGhost == windGhost_A ? windGhost_B : windGhost_A;
        windGhost.gameObject.SetActive(true);
        windGhost.HauntPath(splines[hauntPath]);
        Invoke(nameof(SendGhost), timeBetweenGhosts);
    }
}
