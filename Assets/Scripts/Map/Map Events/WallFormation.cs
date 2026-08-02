using Unity.Netcode;
using UnityEngine;

public class WallFormation : NetworkBehaviour
{
    private RisingWall[] walls;
    private bool isActive = false;
    private void Awake()
    {
        walls = GetComponentsInChildren<RisingWall>(); 
    }
    public void RiseFormation()
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
        {
            RiseFormationServerRpc();
        }
        else
        {
            if (isActive)
                return;
            isActive = true;
            for (int i = 0; i < walls.Length; i++)
            {
                walls[i].gameObject.SetActive(true);
                walls[i].Rise();
            }
        }
    }

    [ServerRpc]
    void RiseFormationServerRpc()
    {
        RiseFormationClientRpc();
    }

    [ClientRpc]
    void RiseFormationClientRpc()
    {
        if (isActive)
            return;
        isActive = true;
        for (int i = 0; i < walls.Length; i++)
        {
            walls[i].gameObject.SetActive(true);
            walls[i].Rise();
        }
    }

    public void SinkFormation()
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
        {
            SinkFormationServerRpc();
        }
        else
        {
            if (!isActive)
                return;
            isActive = false;
            for (int i = 0; i < walls.Length; i++)
            {
                RisingWall wall = walls[i];
                if (wall.gameObject.activeSelf && wall.IsActive)
                {
                    walls[i].Sink(false);
                }
            }
        }
    }

    [ServerRpc]
    void SinkFormationServerRpc()
    {
        SinkFormationClientRpc();
    }

    [ClientRpc]
    void SinkFormationClientRpc()
    {
        if (!isActive)
            return;
        isActive = false;
        for (int i = 0; i < walls.Length; i++)
        {
            RisingWall wall = walls[i];
            if (wall.gameObject.activeSelf && wall.IsActive)
            {
                walls[i].Sink(false);
            }
        }
    }
}
