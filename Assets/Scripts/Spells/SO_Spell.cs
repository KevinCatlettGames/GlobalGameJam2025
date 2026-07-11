using UnityEngine;
using FMODUnity;
using Unity.Netcode;

[CreateAssetMenu(fileName = "new Spell", menuName = "Scriptable Objects/SO_Spell/Simple")]
public class SO_Spell : ScriptableObject
{
    [Header("UI")]
    [SerializeField] private Sprite spellIcon;
    [SerializeField] private Sprite usedSpellIcon;
    [SerializeField] private Color indicatorColor;
    public Sprite SpellIcon => spellIcon;
    public Sprite UsedSpellIcon => usedSpellIcon;
    public Color IndicatorColor => indicatorColor;

    [Header("Bubble Prefabs (Networked / Authoritative)")]
    [SerializeField] protected GameObject bubble;
    [SerializeField] protected GameObject ultBubble;

    [Header("Bubble Prefabs (Local Prediction Fakes)")]
    [SerializeField] protected GameObject fakeBubble;
    [SerializeField] protected GameObject fakeUltBubble;

    [Header("Spell")]
    [SerializeField] protected float spellCooldown = 1.0f;

    [Header("Pickup")]
    [SerializeField] protected Mesh itemMesh;
    [SerializeField] protected Material[] itemMaterials;
    [SerializeField, ColorUsage(true, true)] protected Color[] effectColors;
    public Color[] EffectColors => effectColors;
    public Mesh ItemMesh => itemMesh;
    public Material[] ItemMaterials => itemMaterials;

    [Header("Sound Events")]
    [SerializeField] protected EventReference spellVoiceEvent;
    public EventReference SpellVoiceEvent => spellVoiceEvent;

    protected BasicBubble bubbleScript;

    [SerializeField] private bool canUse = true;
    public bool CanUse { get => canUse; set => canUse = value; }

    [SerializeField] private bool availableInDemo = true;
    public bool AvailableInDemo { get => availableInDemo; set => availableInDemo = value; }

    public virtual float CastSpell(int ID, Vector3 pos, Vector3 dir, Collider playerCollider, bool isUlt, ulong senderClientId, int assignedCastID)
    {
        dir.Normalize();
        float safeDistance = playerCollider.bounds.extents.z + .5f;
        Vector3 baseSpawnPos = pos + (dir * safeDistance);

        // --- SERVER SIDE ---
        if (NetworkManager.Singleton.IsServer)
        {
            float rttInMs = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(senderClientId);
            float oneWayTime = (rttInMs / 2f) / 1000f + (Time.fixedDeltaTime / 2f);

            GameObject bubbleInstance = Instantiate(isUlt ? ultBubble : bubble, baseSpawnPos, Quaternion.LookRotation(dir));
            BasicBubble serverScript = bubbleInstance.GetComponent<BasicBubble>();

            // 1. FAST-FORWARD position calculations
            bubbleInstance.transform.position += dir * (serverScript.Speed * oneWayTime);

            // 2. SPAWN THE OBJECT ONTO THE NETWORK FIRST
            NetworkObject netObj = bubbleInstance.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            // 3. --- FIX: ASSIGN NETWORKED PROPERTIES AFTER NETWORK SPAWN ---
            // This forces Netcode to dirty the state buffer and broadcast the ID cleanly!
            serverScript.castID = assignedCastID;
            serverScript.InitialiseBubble(ID, dir, playerCollider);
        }

        // --- LOCAL CASTING CLIENT SIDE ---
        if (!NetworkManager.Singleton.IsServer && NetworkManager.Singleton.LocalClientId == senderClientId)
        {
            GameObject fakeInstance = Instantiate(isUlt ? fakeUltBubble : fakeBubble, baseSpawnPos, Quaternion.LookRotation(dir));

            fakeInstance.layer = LayerMask.NameToLayer("FakeProjectiles");

            BasicBubble fakeScript = fakeInstance.GetComponent<BasicBubble>();
            if (fakeScript != null)
            {
                fakeScript.isLocalFake = true;

                // Assign IDs before initialization
                fakeScript.castID = assignedCastID;
                fakeScript.InitialiseBubble(ID, dir, playerCollider);
            }

            var playerCtrl = playerCollider.GetComponent<PlayerController>();
            if (playerCtrl != null && fakeScript != null)
            {
                playerCtrl.RegisterLocalFake(fakeScript);
            }
        }

        return spellCooldown;
    }
}