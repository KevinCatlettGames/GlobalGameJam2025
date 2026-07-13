using FMODUnity;
using Steamworks;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Splines;


public class BoneFish : NetworkBehaviour
{
    private Animator animator;
    [SerializeField] private ParticleSystem hitVFX;
    [SerializeField] private EventReference hitEvent;
    [SerializeField] private float damage = 8f;
    [SerializeField] private Material swapMaterial;
    [SerializeField] private SkinnedMeshRenderer meshRenderer;
    [SerializeField] private float swapDuration = .15f;
    [SerializeField] private Countdown countdown;
    private bool isSwapped = false;

    private void Start()
    {
        if (LobbyManager.instance && !LobbyManager.instance.MapSettings[3].PlayWithMapEvent && IsServer)
            DestroySelfClientRpc();

        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
        {
            SplineAnimate splineAnimate = GetComponent<SplineAnimate>();
            splineAnimate.PlayOnAwake = false;
            splineAnimate.Restart(false);

            if (IsServer && LobbyManager.instance)
                LobbyManager.instance.OnAllPlayersLoadedIn.AddListener(StartOnlineSplineAnimate);
        }

        animator = GetComponent<Animator>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bubble"))
        {
            PlayEffects();
        }
    }

    private void StartOnlineSplineAnimate()
    {
        LobbyManager.instance.OnAllPlayersLoadedIn.RemoveListener(StartOnlineSplineAnimate);
        StartSplineAnimateClientRpc();
    }

    [ClientRpc]
    private void StartSplineAnimateClientRpc()
    {
        SplineAnimate splineAnimate = GetComponent<SplineAnimate>();
        splineAnimate.Play();
    }

    [ClientRpc]
    private void DestroySelfClientRpc()
    {
        Destroy(gameObject);
    }

    public float BoneHit()
    {
        PlayEffects();
        return damage;
    }

    private void PlayEffects()
    {
        animator?.SetTrigger("Hit");
        if(!isSwapped)
            StartCoroutine(MaterialSwap());
        RuntimeManager.PlayOneShotAttached(hitEvent, gameObject);
        if (hitVFX)
            hitVFX.Play();
    }

    private IEnumerator MaterialSwap()
    {
        isSwapped = true;
        Material baseMaterial = meshRenderer.material;
        meshRenderer.material = swapMaterial;
        yield return new WaitForSeconds(swapDuration);
        meshRenderer.material = baseMaterial;
        isSwapped = false;
    }
}
