using FMODUnity;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Splines;

public class BoneFish : NetworkBehaviour
{
    [Header("Components & Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private SkinnedMeshRenderer meshRenderer;
    [SerializeField] private ParticleSystem hitVFX;
    [SerializeField] private EventReference hitEvent;
    [SerializeField] private float damage = 8f;

    [Header("Material Swap")]
    [SerializeField] private Material swapMaterial;
    [SerializeField] private float swapDuration = 0.15f;
    private bool isSwapped = false;

    [Header("Spline Sync Settings")]
    [SerializeField] private SplineAnimate splineAnimate;

    public NetworkVariable<double> ServerStartTime = new NetworkVariable<double>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool isSplinePlaying = false;

    private void Awake()
    {
        if (!splineAnimate) splineAnimate = GetComponent<SplineAnimate>();
        if (!animator) animator = GetComponent<Animator>();

        if (splineAnimate != null)
        {
            splineAnimate.PlayOnAwake = false;
        }
    }

    private void Start()
    {
        if (LobbyManager.instance && !LobbyManager.instance.MapSettings[3].PlayWithMapEvent && IsServer)
        {
            DestroySelfClientRpc();
            return;
        }

        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
        {
            splineAnimate.Restart(false);

            if (IsServer && LobbyManager.instance)
            {
                LobbyManager.instance.OnAllPlayersLoadedIn.AddListener(StartOnlineSplineAnimate);
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        ServerStartTime.OnValueChanged += OnServerStartTimeChanged;

        if (ServerStartTime.Value > 0)
        {
            StartLocalSplinePlayback();
        }
    }

    public override void OnNetworkDespawn()
    {
        ServerStartTime.OnValueChanged -= OnServerStartTimeChanged;
    }

    private void Update()
    {
        if (!isSplinePlaying || ServerStartTime.Value <= 0 || splineAnimate == null) return;

        double currentServerTime = NetworkManager.Singleton.ServerTime.Time;
        double elapsedTime = currentServerTime - ServerStartTime.Value;

        if (elapsedTime < 0) return;

        if (splineAnimate.Loop == SplineAnimate.LoopMode.Once)
        {
            splineAnimate.ElapsedTime = Mathf.Clamp((float)elapsedTime, 0f, splineAnimate.Duration);
            if (elapsedTime >= splineAnimate.Duration)
            {
                isSplinePlaying = false;
            }
        }
        else if (splineAnimate.Loop == SplineAnimate.LoopMode.Loop)
        {
            splineAnimate.ElapsedTime = (float)(elapsedTime % splineAnimate.Duration);
        }
    }

    private void StartOnlineSplineAnimate()
    {
        if (LobbyManager.instance)
        {
            LobbyManager.instance.OnAllPlayersLoadedIn.RemoveListener(StartOnlineSplineAnimate);
        }

        ServerStartTime.Value = NetworkManager.Singleton.ServerTime.Time;
    }

    private void OnServerStartTimeChanged(double previousValue, double newValue)
    {
        if (newValue > 0)
        {
            StartLocalSplinePlayback();
        }
    }

    private void StartLocalSplinePlayback()
    {
        isSplinePlaying = true;
        if (splineAnimate != null)
        {
            splineAnimate.Play();
        }
    }

    [ClientRpc]
    private void DestroySelfClientRpc()
    {
        Destroy(gameObject);
    }

    #region Combat & VFX

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bubble"))
        {
            PlayEffects();
        }
    }

    public float BoneHit()
    {
        PlayEffects();
        return damage;
    }

    private void PlayEffects()
    {
        ExecuteVisualEffects();

        if (IsServer)
        {
            PlayEffectsClientRpc();
        }
    }

    [ClientRpc]
    private void PlayEffectsClientRpc()
    {
        if (IsServer) return;
        ExecuteVisualEffects();
    }

    private void ExecuteVisualEffects()
    {
        animator?.SetTrigger("Hit");

        if (!isSwapped && meshRenderer != null)
        {
            StartCoroutine(MaterialSwap());
        }

        RuntimeManager.PlayOneShotAttached(hitEvent, gameObject);

        if (hitVFX != null)
        {
            hitVFX.Play();
        }
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

    #endregion
}