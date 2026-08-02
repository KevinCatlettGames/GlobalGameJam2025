using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WallManager : MapEvent
{
    [SerializeField] private List<WallFormation> wallFormations;
    [SerializeField] private float stayTime = 5f;
    [SerializeField] private float sinkTime = 5f;
    [SerializeField] private float startDelay = 3f;
    private int currentFormation = -1;
    private bool wallsActive = false;

    void Update()
    {
        if (NetworkManager.Singleton && !NetworkManager.Singleton.IsServer) return;
        // Remove in final build!
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (wallsActive)
            {
                SinkWalls();
            }
            CancelInvoke();
            RiseWalls();
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            CancelInvoke();
            SinkWalls();
        }
    }
    private void RiseWalls()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (wallsActive)
            return;

        int r = Random.Range(0, wallFormations.Count);
        if (r == currentFormation)
        {
            r++;
            if(r >= wallFormations.Count) 
                r = 0;
        }
        wallFormations[r].RiseFormation();
        currentFormation = r;
        wallsActive = true;
        Invoke(nameof(SinkWalls), stayTime);
    }
    private void SinkWalls()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (!wallsActive)
            return;

        wallFormations[currentFormation].SinkFormation();
        wallsActive = false;

        Invoke(nameof(RiseWalls), sinkTime);
    }

    protected override void StartEvent()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        Invoke(nameof(RiseWalls), startDelay);
    }
    protected override void StopEvent()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (wallsActive)
        {
            wallFormations[currentFormation].SinkFormation();
            wallsActive = false;
        }

        CancelInvoke();
        currentFormation = -1;
    }
}
