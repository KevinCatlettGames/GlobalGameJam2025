using UnityEngine;
using UnityEngine.Splines;

public class GhostEvent : MapEvent
{
    [SerializeField] private SplineContainer[] splines;
    [SerializeField] private float[] timeBetweenGhosts;
    [SerializeField] private LeafGhost[] leafGhosts;
    [SerializeField] private int[] probabilities;
    private LeafGhost leafGhost;
    private int ghostIndex = 0;
    private int previousPath = -1;
    private int previousPreviousPath = -1;
    private bool isFirstWave = true;
    protected override void StartEvent()
    {
        leafGhost = leafGhosts[0];
        isFirstWave = true;
        Invoke(nameof(SendGhostWave), timeBetweenGhosts[0]);
    }

    protected override void StopEvent()
    {
        CancelInvoke();
        foreach (var ghost in leafGhosts)
        {
            ghost.StopGhost();
        }
        previousPath = -1;
        previousPreviousPath = -1;
        ghostIndex = 0;
    }

    private void SendGhostWave()
    {
        int hauntPath = Random.Range(0, splines.Length);
        if (hauntPath == previousPath)
        {
            hauntPath++;
            if (hauntPath >= splines.Length)
                hauntPath = 0;
        }

        int amountOfGhosts = 0;
        if (isFirstWave)
        {
            amountOfGhosts = 1;
            isFirstWave = false;
        }
        else
        {
            int r = Random.Range(0, 100);
            if (r < probabilities[0])
                amountOfGhosts = 1;
            else if (r < probabilities[1] + probabilities[0])
                amountOfGhosts = 2;
            else
                amountOfGhosts = 3;
        }

        for (int i = 0; i < amountOfGhosts; i++)
        {
            SendGhost();
        }
        Invoke(nameof(SendGhostWave), timeBetweenGhosts[amountOfGhosts - 1]);
    }

    private void SendGhost()
    {
        int hauntPath = Random.Range(0, splines.Length);
        do
        {
            if (hauntPath == previousPath || hauntPath == previousPreviousPath)
            {
                hauntPath++;
                if (hauntPath >= splines.Length)
                    hauntPath = 0;
            }
            else
            {
                break;
            }
        } while (true);

        previousPreviousPath = previousPath;
        previousPath = hauntPath;

        leafGhost.gameObject.SetActive(true);
        leafGhost.HauntPath(splines[hauntPath]);

        ghostIndex++;
        if (ghostIndex >= leafGhosts.Length)
            ghostIndex = 0;
        leafGhost = leafGhosts[ghostIndex];
    }
}
