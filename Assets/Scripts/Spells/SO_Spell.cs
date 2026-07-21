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

    [SerializeField] private bool fakeWithServerCaster = false;
    public bool FakeWithServerCaster { get => fakeWithServerCaster; set => fakeWithServerCaster = value; }

    public virtual float CastSpell(int ID, Vector3 pos, Vector3 dir, Collider playerCollider, bool isUlt, ulong senderClientId, int assignedCastID)
    {
        dir.Normalize();
        float safeDistance = playerCollider.bounds.extents.z + .5f;
        Vector3 baseSpawnPos = pos + (dir * safeDistance);

        if (NetworkManager.Singleton.IsServer)
        {
            GameObject bubbleInstance = Instantiate(isUlt ? ultBubble : bubble, baseSpawnPos, Quaternion.LookRotation(dir));
            BasicBubble serverScript = bubbleInstance.GetComponent<BasicBubble>();
            NetworkObject netObj = bubbleInstance.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();
            if(fakeWithServerCaster)
                serverScript.InitialiseBubble(ID, dir, playerCollider, assignedCastID, true);
            else
                serverScript.InitialiseBubble(ID, dir, playerCollider, assignedCastID, false);

        }
        else if (!NetworkManager.Singleton.IsServer && NetworkManager.Singleton.LocalClientId == senderClientId || fakeWithServerCaster)
        {
            GameObject fakeInstance = Instantiate(isUlt ? fakeUltBubble : fakeBubble, baseSpawnPos, Quaternion.LookRotation(dir));
            BasicBubble fakeScript = fakeInstance.GetComponent<BasicBubble>();
            if (fakeScript != null)
            {
                fakeScript.isLocalFake = true;
                fakeScript.InitialiseBubble(ID, dir, playerCollider, assignedCastID, false);
            }
        }
        return spellCooldown;
    }
}