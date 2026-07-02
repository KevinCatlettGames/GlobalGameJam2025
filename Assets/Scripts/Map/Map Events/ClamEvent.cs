using Unity.Netcode;
using UnityEngine;

public class ClamEvent : MapEvent
{
    [SerializeField] private int maxClams = 3;
    [SerializeField] private float clamRiseCooldown = 5f;
    private Clam[] clams;
    private int currentActiveClams = 0;
    private bool isClaming = false;
    private float timer = 0;
    private void Start()
    {
        clams = GetComponentsInChildren<Clam>();
        for (int i = 0; i < clams.Length; i++)
        {
            clams[i].OnSnap += DecreaseActiveClams;
            clams[i].gameObject.SetActive(false);
        }
    }
    private void Update()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (isClaming && currentActiveClams < maxClams)
        {
            if (timer >= clamRiseCooldown)
            {
                int r = Random.Range(0, maxClams);
                Clam clam = clams[r];
                int tries = 0;
                while (!clam.IsAvailble)
                {
                    r++;
                    if (r >= clams.Length) 
                        r = 0;
                    clam = clams[r];
                    tries++;
                    if (tries > clams.Length)
                        break;
                }

                if(TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
                {
                    ToggleClamServerRpc(r, true);
                }
                else
                {
                    clam.gameObject.SetActive(true);
                    clam.Rise();
                }
                currentActiveClams++;
                timer = 0;
            }
            else
            {
                timer += Time.deltaTime;
            }
        }
    }

    [ServerRpc]
    void ToggleClamServerRpc(int index, bool value)
    {
        ToggleClamClientRpc(index, value);
    }

    [ClientRpc]
    void ToggleClamClientRpc(int index, bool value)
    {
        if(value)
            clams[index].gameObject.SetActive(value);

        if(value)
            clams[index].Rise();
        else
            clams[index].DisableClam();
    }

    protected override void StartEvent()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        isClaming = true;
    }

    protected override void StopEvent()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        isClaming = false;
        for (int i = 0; i < clams.Length; i++)
        {
            if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
                ToggleClamServerRpc(i, false);
            else
                clams[i].DisableClam();          
        }
    }

    private void DecreaseActiveClams()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        currentActiveClams--;
    }

    private void OnDestroy()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        for (int i = 0; i < clams.Length; i++)
        {
            clams[i].OnSnap -= DecreaseActiveClams;
        }
    }
}
