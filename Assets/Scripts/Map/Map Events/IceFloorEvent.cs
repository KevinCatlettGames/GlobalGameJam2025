using System;
using System.Collections.Generic;
using UnityEngine;

public class IceFloorEvent : MapEvent
{
    private bool initialised = false;
    [SerializeField] IceFloor[] iceFloors;
    protected override void StartEvent()
    {
        if (!initialised)
        {
            List<PlayerController> playerControllers = PlayerManager.Instance.GetPlayers();
            foreach (PlayerController playerController in playerControllers)
            {
                playerController.gameObject.AddComponent<IceBreaker>();
            }
            initialised = true;
        }

        foreach (IceFloor iceFloor in iceFloors)
        {
            iceFloor.ActivateIcePice();
        }
    }

    protected override void StopEvent()
    {
        foreach (IceFloor  iceFloor in iceFloors)
        {
            iceFloor.ResetFloorPiece();
        }
    }
}
