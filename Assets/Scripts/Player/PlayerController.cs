using FMODUnity;
using UnityEngine.Events;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Netcode; 
using UnityEditor.Timeline;

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
    private bool isUsingGamepad = false;
    private float mouseInputDeadzoneRadius = .5f;
    private float mouseInputVectorLimit = 5f;

    [Header("Effects")]
    [SerializeField] private GameObject dashStartEffect;
    [SerializeField] private float materialSwapDuration = .1f;

    #region Player Physics
    [Header("Player Stats")]
    [SerializeField] private float playerSpeed = 2.0f;
    [SerializeField] private float gravityValue = -9.81f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float moveSmoothTime = 0.1f;

    [Header("Sprint")]
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
    private Vector3 move = Vector3.zero;
    private Vector2 movementInput = Vector2.zero;
    private Vector3 targetDirection = Vector3.zero;
    private Vector3 smoothMoveDirection = Vector3.zero;
    private Vector3 moveVelocity = Vector3.zero;
    private Vector3 knockbackVelocity = Vector3.zero;
    #endregion


    public Animator mainAnimator;
    private MaterialSwapper materialSwapper;
    [Header("Visuals")]
    [SerializeField] private GameObject[] characters;
    [SerializeField] private Image[] coloredElements;
    
    public bool initialized = false; 
    
    #region Unity
    
    [ClientRpc]
    public void InitializeClientRpc()
    {
        foreach(GameObject character in characters)
            character.SetActive(false);
        
        if (IsOwner)
        {
            var netObj = GetComponent<NetworkObject>();
            PlayerManager.Instance.AddPlayerServerRpc(new NetworkObjectReference(netObj));
        }
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
        else
        {
            playerVelocity.y += gravityValue * Time.deltaTime;
        }
        if (!isDead)
        {
            targetDirection = new Vector3(movementInput.x, 0, movementInput.y);
            targetDirection = Vector3.ClampMagnitude(targetDirection, 1f);
            smoothMoveDirection = Vector3.SmoothDamp(smoothMoveDirection, targetDirection, ref moveVelocity, moveSmoothTime);
            move = smoothMoveDirection * (playerSpeed * Time.deltaTime);
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
            targetDirection = Vector3.zero;
            move = Vector3.zero;
        }
        if (knockbackVelocity.magnitude > 0.1f)
        {
            move += knockbackVelocity * Time.deltaTime; 
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecaySpeed * Time.deltaTime); 
        }
        else
        {
            if(killCreditID != -1 && controller.isGrounded)
            {
                killCreditID = -1;
            }
        }

        if (!isDead) move += playerVelocity * Time.deltaTime;
        if (controller.enabled) controller.Move(move);


        if (targetDirection != Vector3.zero && !isDead)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        if (targetDirection.sqrMagnitude > 0)
        {
            mainAnimator?.SetBool("IsWalking", true);
        }
        else
        {
            mainAnimator?.SetBool("IsWalking", false);
        }
    }
    
    #endregion

    #region Inputs
    public void OnMove(InputAction.CallbackContext context)
    {
        if (isUsingGamepad)
        {
            movementInput = context.ReadValue<Vector2>();
            return;
        }
        else
        {
            movementInput += context.ReadValue<Vector2>() * Time.deltaTime;
            float inputMagnitude = movementInput.magnitude;
            if (inputMagnitude < mouseInputDeadzoneRadius)
            {
                movementInput = Vector2.zero;
            }
            else if (inputMagnitude > mouseInputVectorLimit)
            {
                movementInput *= mouseInputVectorLimit / inputMagnitude;
            }
        }
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
    public void OnEmote(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 value = context.ReadValue<Vector2>();
            switch (value.x, value.y)
            {
                case (0,1):
                    mainAnimator.SetTrigger("EmoteUp");
                    break;
                case (0,-1):
                    //EmoteDown
                    break;
                case (-1, 0):
                    //EmoteLeft
                    break;
                case (1, 0):
                    //EmoteRight
                    break;
                default:
                    break;
            }
        }
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

    [ServerRpc(RequireOwnership = false)]
    public void ApplyKnockbackServerRpc(int ID, Vector3 direction, float force, float dmg)
    {
        ApplyKnockbackClientRpc(ID, direction, force, dmg);
    }
    
    [ClientRpc]
    public void ApplyKnockbackClientRpc(int ID, Vector3 direction, float force, float dmg)
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
        mainAnimator.SetTrigger("Flinch");
        materialSwapper?.SwapMaterials(materialSwapDuration);
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
        GameManager.Instance.DeathReportServerRpc(playerID, killCreditID);  
        playerHUD.DisplayDeath();
        for (int i = 0; i < coloredElements.Length; i++)
        {
            coloredElements[i].enabled = false;
        }
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
        mainAnimator.SetBool("Victory", true);        
    }
    public void ResetPlayerController()
    {
        damage = 0;
        playerHUD.ResetHUD();
        isDead = false;
        isSlippery = false;
        killCreditID = -1;
        mainAnimator.SetBool("IsDead", false);
        mainAnimator.SetBool("Victory", false);
        for (int i = 0; i < coloredElements.Length; i++)
        {
            coloredElements[i].enabled = true;
        }
        movementInput = Vector2.zero;
    }
    public void SetUpPlayer(int playerID,PlayerHUD playerHUD, ControllerRumbler controllerRumbler, Color color)
    {
        this.playerHUD = playerHUD;
        this.playerID = playerID;
        characters[playerID].SetActive(true);
        mainAnimator = characters[playerID].GetComponent<Animator>();
        materialSwapper = characters[playerID].GetComponentInChildren<MaterialSwapper>();
        for (int i = 0; i < coloredElements.Length; i++)
        {
            coloredElements[i].color = color;
        }

        playerHUD.UpdateDamageText((int)damage);
        
        if (controllerRumbler != null)
        {
            this.controllerRumbler = controllerRumbler;
            isUsingGamepad = true;
        }
    }
    #endregion
}