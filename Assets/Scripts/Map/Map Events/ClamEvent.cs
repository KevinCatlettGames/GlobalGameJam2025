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
                clam.gameObject.SetActive(true);
                clam.Rise();
                currentActiveClams++;
                timer = 0;
            }
            else
            {
                timer += Time.deltaTime;
            }
        }
    }
    protected override void StartEvent()
    {
        isClaming = true;
    }

    protected override void StopEvent()
    {
        isClaming = false;
        for (int i = 0; i < clams.Length; i++)
        {
            clams[i].DisableClam();
        }
    }

    private void DecreaseActiveClams()
    {
        currentActiveClams--;
    }

    private void OnDestroy()
    {
        for (int i = 0; i < clams.Length; i++)
        {
            clams[i].OnSnap -= DecreaseActiveClams;
        }
    }
}
