using System.Collections;
using FMODUnity;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Netcode;

public class JumpingFish : NetworkBehaviour
{
    private SplineAnimate splineAnimate;
    private bool isJumping = false;
    [SerializeField] private GameObject fish;
    [SerializeField] private ParticleSystem jumpVFX;
    [SerializeField] private ParticleSystem announceVFX;
    [SerializeField] private ParticleSystem dipVFX;
    [SerializeField] private float vfxDelay = .5f;
    [SerializeField] private EventReference emergeEvent;

    void Start()
    {
        splineAnimate = fish.GetComponent<SplineAnimate>();
    }

    public void Jump(Transform startPos)
    {
        // --- FIXED FOR RELAY AND STANDALONE ---
        // If the server calls this, immediately broadcast to everyone
        if (IsServer)
        {
            JumpClientRpc(startPos.position, startPos.rotation);
        }
        else if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
        {
            // Fallback: If a client ever triggers this (unlikely for a map event)
            JumpServerRpc(startPos.position, startPos.rotation);
        }
        else
        {
            // Local fallback execution logic
            ExecuteLocalJump(startPos.position, startPos.rotation);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void JumpServerRpc(Vector3 pos, Quaternion rot)
    {
        JumpClientRpc(pos, rot);
    }

    [ClientRpc]
    void JumpClientRpc(Vector3 pos, Quaternion rot)
    {
        ExecuteLocalJump(pos, rot);
    }

    // Extracted logic into a single method so ClientRpc and local paths match perfectly
    private void ExecuteLocalJump(Vector3 pos, Quaternion rot)
    {
        if (isJumping) return;

        transform.position = pos;
        transform.rotation = rot;

        if (announceVFX)
            announceVFX.Play();

        RuntimeManager.PlayOneShotAttached(emergeEvent, gameObject);
        StartCoroutine(JumpCoroutine());
    }

    private IEnumerator JumpCoroutine()
    {
        yield return new WaitForSeconds(vfxDelay);
        if (jumpVFX)
            jumpVFX.Play();

        isJumping = true;
        fish.SetActive(true);
        splineAnimate.Restart(true);

        yield return new WaitForSeconds(splineAnimate.Duration - .2f);
        if (dipVFX)
            dipVFX.Play();

        yield return new WaitForSeconds(.2f);
        fish.SetActive(false);
        isJumping = false;
    }
}