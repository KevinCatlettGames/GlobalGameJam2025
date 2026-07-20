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

        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Spawning server bubble");
            GameObject bubbleInstance = Instantiate(isUlt ? ultBubble : bubble, baseSpawnPos, Quaternion.LookRotation(dir));

            if (ID != 0)
                if (bubbleInstance.GetComponent<Collider>())
                {
                    Debug.Log("Collision ignored between server bubble: " + bubbleInstance.transform.name + " and player with id: " + ID);
                    Physics.IgnoreCollision(bubbleInstance.GetComponent<Collider>(), GameManager.Instance.Players[ID].GetComponent<Collider>());
                }

            BasicBubble serverScript = bubbleInstance.GetComponent<BasicBubble>();
            NetworkObject netObj = bubbleInstance.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            serverScript.InitialiseBubble(ID, dir, playerCollider);
        }
        else if (!NetworkManager.Singleton.IsServer && NetworkManager.Singleton.LocalClientId == senderClientId)
        {
            Debug.Log("Spawning fake bubble");
            GameObject fakeInstance = Instantiate(isUlt ? fakeUltBubble : fakeBubble, baseSpawnPos, Quaternion.LookRotation(dir));

            BasicBubble fakeScript = fakeInstance.GetComponent<BasicBubble>();
            if (fakeScript != null)
            {
                fakeScript.isLocalFake = true;
                fakeScript.InitialiseBubble(ID, dir, playerCollider);
            }

        }

        return spellCooldown;
    }
}