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
        clams = GetComponentsInChildren<Clam>(true);
        for (int i = 0; i < clams.Length; i++)
        {
            clams[i].OnSnap += DecreaseActiveClams;
            clams[i].gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsServer) return;

        if (isClaming && currentActiveClams < maxClams)
        {
            if (timer >= clamRiseCooldown)
            {
                int r = Random.Range(0, clams.Length);
                Clam clam = clams[r];
                int tries = 0;

                while (!clam.IsAvailable)
                {
                    r++;
                    if (r >= clams.Length)
                        r = 0;
                    clam = clams[r];
                    tries++;
                    if (tries > clams.Length)
                        break;
                }
                int chosenSpellID = ItemSpawner.Instance.GetRandomLegalSpellID();
                ToggleClamClientRpc(r, true, chosenSpellID);

                currentActiveClams++;
                timer = 0;
            }
            else
            {
                timer += Time.deltaTime;
            }
        }
    }

    [ClientRpc]
    void ToggleClamClientRpc(int index, bool value, int spellID)
    {
        if (index < 0 || index >= clams.Length) return;

        if (value)
        {
            clams[index].gameObject.SetActive(true);
            clams[index].Rise(spellID);
        }
        else
            clams[index].DisableClam();
    }

    protected override void StartEvent()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (clams.Length <= 0) return;
        isClaming = true;
    }

    protected override void StopEvent()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        isClaming = false;
        if(clams.Length <= 0) return;
        for (int i = 0; i < clams.Length; i++)
            ToggleClamClientRpc(i, false, 0);
    }

    private void DecreaseActiveClams()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        currentActiveClams--;
    }

    private void OnDestroy()
    {
        if (clams == null) return;
        for (int i = 0; i < clams.Length; i++)
        {
            if (clams[i] != null)
                clams[i].OnSnap -= DecreaseActiveClams;
        }
    }
}