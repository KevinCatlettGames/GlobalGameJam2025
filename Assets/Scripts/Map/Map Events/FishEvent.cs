using Unity.Netcode;
using UnityEngine;


public class FishEvent : MapEvent
{
    [SerializeField] private JumpingFish jumpingFish;
    [SerializeField] private Transform[] jumpingPoints;
    [SerializeField] private float startDelay = 3f;
    [SerializeField] private float jumpDelay = 10f;
    private bool isLeft = false;
    private int jp_offset = 0;

    void Start()
    {
        if (!NetworkManager.Singleton.IsServer) return; 
        jp_offset = jumpingPoints.Length / 2;
    }
    protected override void StartEvent()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        Invoke(nameof(FishGo), startDelay);
    }
    protected override void StopEvent()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        CancelInvoke();
    }
    private void FishGo()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        int r = Random.Range(0, jp_offset);
        r = isLeft ? r : r + jp_offset;
        isLeft = !isLeft;
        jumpingFish.Jump(jumpingPoints[r]);
        Invoke(nameof(FishGo), jumpDelay);
    }
}
