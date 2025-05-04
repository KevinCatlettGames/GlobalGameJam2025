using FMODUnity;
using UnityEngine.Events;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Netcode; 

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Sound Events")]
    [SerializeField] private EventReference knockBackEvent;
    [SerializeField] private EventReference deathEvent;

    [Header("Spells")]
    [SerializeField]
    private SO_Spell[] allSpells;
    
    [SerializeField] private GameObject spellSpawnEffect;
    private SO_Spell firstSpell;
    private SO_Spell secondSpell;

    private bool isFirstSpellReady = true;
    private bool isSecondSpellReady = true;
    private Coroutine firstSpellCoroutine;
    private Coroutine secondSpellCoroutine;
    private Item itemToEquip;
    private bool isSlippery = false;
    private PlayerHUD playerHUD;

    private int killCreditID = -1;
    private int playerID = 0;
    public int PlayerID { get { return playerID; } }
    private bool isDead = false;

    private float damage = 0;
    [Header("Damage")]
    [SerializeField] float damageModifier = .05f;
    [SerializeField] float slipperyModifier = 1.5f;
    [SerializeField] float rumbleDurationFactor = .01f;
    [SerializeField] private ParticleSystem damageParticleSystem;
    [SerializeField] private NetworkObject dashStartEffectPrefab;
    private ControllerRumbler controllerRumbler = null;

    [Header("Player Stats")]
    #region Player Physics
    [SerializeField] private float playerSpeed = 2.0f;
    [SerializeField] private float gravityValue = -9.81f;
    [SerializeField] private float playerSprintSpeed = 24f;
    [SerializeField] private float playerSprintDuration = .5f;
    [SerializeField] private float sprintCooldown = 3f;
    public float SprintCooldown { get { return sprintCooldown; } }
    public UnityEvent OnBeginSprint;
    public UnityEvent OnEndSprint; 

    private bool canSprint = true;
    private Coroutine sprintCoroutine;
    #endregion

    #region Player Controller
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    #endregion

    #region Input Movement 
    private Vector2 movementInput = Vector2.zero;
    private Vector3 targetDirection = Vector3.zero;
    private Vector3 smoothMoveDirection = Vector3.zero;
    [SerializeField] private float rotationSpeed = 10f; // Adjust for smoother rotation
    [SerializeField] private float moveSmoothTime = 0.1f; // Smoothing duration
    private Vector3 moveVelocity = Vector3.zero;
    #endregion

    #region Knockback
    [SerializeField]
    private float knockbackDecaySpeed = 5f; // Speed at which knockback decays
    private Vector3 knockbackVelocity = Vector3.zero; // Current knockback force
    #endregion

    private Animator mainAnimator;
    [SerializeField] private GameObject[] characters;
    [SerializeField] private Image aimIndicator;
    
    public bool initialized = false; 
    
    #region Unity
    
    private void Start()
    {
        foreach(GameObject character in characters)
            character.SetActive(false);
        
        if (IsOwner)
        {
            var netObj = GetComponent<NetworkObject>();
            PlayerManager.Instance.AddPlayerServerRpc(new NetworkObjectReference(netObj));
        }
    }

    public void Initialize()
    {
        if(IsOwner) GetComponent<PlayerInput>().enabled = true;
        controller = gameObject.GetComponent<CharacterController>(); 
        PlayerManager.Instance.OnPlayerJoined(GetComponent<PlayerInput>());
        initialized = true;
    }
    
    private void Update()
    {
        if (!initialized || !IsOwner) return; 
        
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        // Handle input movement
        if (!isDead)
        {
            targetDirection = new Vector3(movementInput.x, 0, movementInput.y);
            targetDirection = Vector3.ClampMagnitude(targetDirection, 1f);
        }
        else
        {
            targetDirection = knockbackVelocity * 0.1f;
        }
        if (targetDirection.sqrMagnitude > 0)
        {
            mainAnimator?.SetBool("IsWalking", true);
        }
        else
        {
            mainAnimator?.SetBool("IsWalking", false);
        }

        // Smoothly interpolate movement direction
        smoothMoveDirection = Vector3.SmoothDamp(smoothMoveDirection, targetDirection, ref moveVelocity, moveSmoothTime);
        Vector3 move = smoothMoveDirection * (playerSpeed * Time.deltaTime);

        // Apply knockback if it exists
        if (knockbackVelocity.magnitude > 0.1f)
        {
            move += knockbackVelocity * Time.deltaTime; // Add knockback to movement
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecaySpeed * Time.deltaTime); // Decay knockback over time
        }
        else
        {
            if(killCreditID != -1 && controller.isGrounded)
            {
                killCreditID = -1;
            }
        }

        if (controller.enabled)
            controller.Move(move);

        // Smoothly rotate the player to face the movement direction
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // Gravity
        playerVelocity.y += gravityValue * Time.deltaTime;

        if (controller.enabled)
            controller.Move(playerVelocity * Time.deltaTime);
    }
    
    #endregion

    #region Inputs
    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }
    public void OnFirstSpell(InputAction.CallbackContext context)
{
    if (isFirstSpellReady && context.performed && !isDead)
    {
        CastSpellServerRpc(true); // Request to cast first spell
    }
}

public void OnSecondSpell(InputAction.CallbackContext context)
{
    if (isSecondSpellReady && context.performed && !isDead)
    {
        CastSpellServerRpc(false); // Request to cast second spell
    }
}

[ServerRpc]
private void CastSpellServerRpc(bool isFirstSpell)
{
    SO_Spell spell = isFirstSpell ? firstSpell : secondSpell;

    // Server validates if spell is ready (you can add logic here)
    float cooldown = spell.CastSpell(playerID, transform.position, transform.forward, controller);

    // Trigger client VFX/SFX and start local cooldown visuals
    CastSpellClientRpc(isFirstSpell, cooldown);

    // Start server cooldown logic (authoritative)
    if (isFirstSpell)
        firstSpellCoroutine = StartCoroutine(ServerCooldownCoroutine(cooldown, 1));
    else
        secondSpellCoroutine = StartCoroutine(ServerCooldownCoroutine(cooldown, 2));
}

[ClientRpc]
private void CastSpellClientRpc(bool isFirstSpell, float cooldown)
{
    mainAnimator.SetTrigger("SlapTrigger");
    Instantiate(spellSpawnEffect, transform.position, Quaternion.identity);

    SO_Spell spell = isFirstSpell ? firstSpell : secondSpell;
    RuntimeManager.PlayOneShotAttached(spell.GetSpellEventStruct(), gameObject);

    // Start local cooldown visuals
    StartCoroutine(SpellCooldown(cooldown, isFirstSpell ? 1 : 2));

    // Lock the spell input locally
    if (isFirstSpell)
        isFirstSpellReady = false;
    else
        isSecondSpellReady = false;
}

private IEnumerator ServerCooldownCoroutine(float time, int spellID)
{
    yield return new WaitForSeconds(time);

    // Notify clients when cooldown is over
    CooldownCompleteClientRpc(spellID);
}

[ClientRpc]
private void CooldownCompleteClientRpc(int spellID)
{
    ResetSpell(spellID);
    if (spellID == 1)
        isFirstSpellReady = true;
    else
        isSecondSpellReady = true;
}


    
    public void OnFistSpellEquip(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EquipSpellServerRpc(1);
        }
    }

    public void OnSecondSpellEquip(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EquipSpellServerRpc(2);
        }
    }

    [ServerRpc]
    private void EquipSpellServerRpc(int spellID)
    {
        if (itemToEquip == null) return;

        switch (spellID)
        {
            case 1:
                firstSpell = itemToEquip.EquipSpell();
                playerHUD.SetSpell(1, firstSpell.SpellIcon);
                break;
            case 2:
                secondSpell = itemToEquip.EquipSpell();
                playerHUD.SetSpell(2, secondSpell.SpellIcon);
                break;
            default:
                Debug.LogWarning("Invalid spell ID");
                return;
        }
        
        ResetSpell(spellID);

        // Call the ClientRpc to send the spell data to the client
        EquipSpellClientRpc(spellID);
          itemToEquip = null;
    }

    // ClientRpc to notify the client about the spell change
    [ClientRpc]
    private void EquipSpellClientRpc(int spellID)
    {
        // Retrieve the correct spell for the client side
        SO_Spell equippedSpell = itemToEquip.spell;
        if (equippedSpell == null)
        {
            Debug.LogWarning("Spell ID not found on client.");
            return;
        }

        switch (spellID)
        {
            case 1:
                firstSpell = equippedSpell;
                playerHUD.SetSpell(1, firstSpell.SpellIcon);
                break;
            case 2:
                secondSpell = equippedSpell;
                playerHUD.SetSpell(2, secondSpell.SpellIcon);
                break;
            default:
                Debug.LogWarning("Invalid spell ID");
                return;
        }
    }
    
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (canSprint && context.performed && sprintCoroutine == null)
        {
            sprintCoroutine = StartCoroutine(SprintCoroutine());
            SpawnDashEffectServerRpc();
        }
    }
    
    [ServerRpc]
    private void SpawnDashEffectServerRpc()
    {
        if (dashStartEffectPrefab != null)
        {
            NetworkObject dashEffect = Instantiate(dashStartEffectPrefab, transform.position, transform.rotation);
            dashEffect.Spawn(true); // true = spawn with ownership default (server owns it)
        }
    }
    
    private IEnumerator SprintCoroutine()
    {
        canSprint = false;
        float moveSpeed = playerSpeed;
        playerSpeed = playerSprintSpeed;
        OnBeginSprint?.Invoke();
        yield return new WaitForSeconds(playerSprintDuration);
        playerSpeed = moveSpeed;
        OnEndSprint?.Invoke();
        yield return new WaitForSeconds(sprintCooldown);
        sprintCoroutine = null;
        canSprint = true;
    }
    #endregion

    #region Items
    public void UpdateItemToEquip(Item item, bool isInRange)
    {
        if (isInRange)
        {
            itemToEquip = item;
            var itemNetworkObject = item.GetComponent<NetworkObject>();
            UpdateItemToEquipClientRpc(itemNetworkObject);
        }
        else if (!isInRange && item == itemToEquip)
        {
            itemToEquip = null;
        }
    }

    [ClientRpc]
    public void UpdateItemToEquipClientRpc(NetworkObjectReference item)
    {
        if (item.TryGet(out NetworkObject itemNetObj))
            itemToEquip = itemNetObj.GetComponent<Item>();
    }
    
    #endregion

    #region Spells
    private IEnumerator SpellCooldown(float time, int spellID)
    {
        float cooldownRate = 1f/time;
        playerHUD.SetSpellCooldown(spellID, cooldownRate);
        yield return new WaitForSeconds(time);
        ResetSpell(spellID);
    }
    public void SetSpells(SO_Spell firstSpell, SO_Spell secondSpell)
    {
        ApplySpells(firstSpell, secondSpell);

        if (IsServer)
            SetSpellsClientRpc(firstSpell.spellIndex, secondSpell.spellIndex);
    }

    [ClientRpc]
    public void SetSpellsClientRpc(int firstSpellIndex, int secondSpellIndex)
    {
        SO_Spell first = null, second = null;

        foreach (SO_Spell spell in allSpells)
        {
            if (spell.spellIndex == firstSpellIndex)
                first = spell;
            if (spell.spellIndex == secondSpellIndex)
                second = spell;
        }

        if (first != null && second != null)
        {
            ApplySpells(first, second);
        }
        else
        {
            Debug.LogError("Could not resolve one or both spells from index!");
        }
    }

    private void ApplySpells(SO_Spell firstSpell, SO_Spell secondSpell)
    {
        this.firstSpell = firstSpell;
        this.secondSpell = secondSpell;

        ResetSpell(1);
        ResetSpell(2);

        playerHUD.SetSpell(1, firstSpell.SpellIcon);
        playerHUD.SetSpell(2, secondSpell.SpellIcon);
    }

    
    private void ResetSpell(int spellID)
    {
        switch (spellID)
        {
            case 1:
                if (firstSpellCoroutine != null) StopCoroutine(firstSpellCoroutine);
                firstSpellCoroutine = null;
                isFirstSpellReady = true;
                break;
            case 2:
                if (secondSpellCoroutine != null) StopCoroutine(secondSpellCoroutine);
                secondSpellCoroutine = null;
                isSecondSpellReady = true;
                break;
            default:
                Debug.Log("Spell Reset Error");
                break;
        }
    }
    #endregion

    #region Damage
    public void ApplyKnockback(int ID, Vector3 direction, float force, float dmg)
    {
        if (isSlippery) force *= slipperyModifier;
        direction.y = 0;
        Vector3 knockback = direction.normalized * (force * (1 + (damage * damageModifier)));
        if (knockback.sqrMagnitude >= knockbackVelocity.sqrMagnitude)
        {
            killCreditID = ID;
        }
        knockbackVelocity += knockback;
        RuntimeManager.PlayOneShotAttached(knockBackEvent, gameObject);
        damage += dmg;
        playerHUD.UpdateDamageText((int)damage);
        damageParticleSystem.Play();
        if (controllerRumbler != null) 
        {
            float duration = knockbackVelocity.magnitude * rumbleDurationFactor;
            controllerRumbler.Rumble(duration, force, dmg);
        }
    }
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        mainAnimator.SetBool("IsDead", true);
        RuntimeManager.PlayOneShotAttached(deathEvent, gameObject);
        if (playerID == killCreditID) killCreditID = -1;
        GameManager.Instance.DeathReport(playerID, killCreditID);  
        playerHUD.DisplayDeath();
    }
    public void SetSlippy(bool slippy)
    {
        if (slippy)
        {
            knockbackVelocity *= slipperyModifier;
            isSlippery = true;
        }
        else
        {
            isSlippery = false;
        }
    }
    #endregion

    #region PlayerManager
    public void Victory()
    {
        if (mainAnimator.gameObject.activeSelf)
        {
            mainAnimator.SetBool("Victory", true);
        }
    }
    public void ResetPlayerController()
    {
        damage = 0;
        playerHUD.ResetHUD();
        isDead = false;
        isSlippery = false;
        killCreditID = -1;
        
        if (mainAnimator.gameObject.activeSelf)
        {
            mainAnimator.SetBool("Victory", false);
        }
    }
    public void SetUpPlayer(int playerID,PlayerHUD playerHUD, ControllerRumbler controllerRumbler, Color color)
    {
        this.playerHUD = playerHUD;
        this.playerID = playerID;
        characters[playerID].SetActive(true);
        mainAnimator = characters[playerID].GetComponent<Animator>();
        aimIndicator.color = color;

        playerHUD.UpdateDamageText((int)damage);
        
        if (controllerRumbler != null)
        {
            this.controllerRumbler = controllerRumbler;
        }
    }
    #endregion
}