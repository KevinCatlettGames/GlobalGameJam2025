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
    public Sprite SpellIcon { get { return spellIcon; } }
    public Sprite UsedSpellIcon { get { return usedSpellIcon;} }
    public Color IndicatorColor {  get { return indicatorColor; } }
    
    [Header("Bubble")]
    [SerializeField] protected GameObject bubble;
    [SerializeField] protected GameObject ultBubble;

    [Header("Spell")]
    [SerializeField] protected float spellCooldown = 1.0f;

    [Header("Pickup")]
    [SerializeField] protected Mesh itemMesh;
    [SerializeField] protected Material[] itemMaterials;
    [Tooltip("0 = ShadowColor, 1 = EimerColor, 2 = SparkleColor, 3 = WaveColor, 4 = OutlineColor")]
    [SerializeField, ColorUsage(true,true)] protected Color[] effectColors;
    public Color[]  EffectColors { get { return effectColors; } }
    public Mesh ItemMesh { get { return itemMesh; } }
    public Material[] ItemMaterials { get { return itemMaterials; } }

    [Header("Sound Events")]
    [SerializeField] protected EventReference spellVoiceEvent;
    public EventReference SpellVoiceEvent { get { return spellVoiceEvent; } }

    protected BasicBubble bubbleScript;
    
    [SerializeField] private bool canUse = true;
    public bool CanUse { get => canUse; set => canUse = value; }

    [SerializeField] private bool availableInDemo = true;
    public bool AvailableInDemo { get => availableInDemo; set => availableInDemo = value; }

    virtual public float CastSpell(int ID, Vector3 pos, Vector3 dir, Collider playerCollider, bool isUlt)
    {
        dir.Normalize();

        float safeDistance = playerCollider.bounds.extents.z + .5f;
        pos += dir * safeDistance;

        if (NetworkManager.Singleton.IsServer)
        {
            GameObject bubbleInstance;
            if (isUlt)
                bubbleInstance = Instantiate(ultBubble, pos, Quaternion.LookRotation(dir));
            else
                bubbleInstance = Instantiate(bubble, pos, Quaternion.LookRotation(dir));

            bubbleScript = bubbleInstance.GetComponent<BasicBubble>();
            bubbleScript.InitialiseBubble(ID, dir, playerCollider);

            bubbleInstance.GetComponent<NetworkObject>().Spawn();
        }

        return spellCooldown;
    }
}