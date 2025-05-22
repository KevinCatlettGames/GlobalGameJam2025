using FMODUnity;
using UnityEngine.Events;
using System.Collections;
using System.Linq;
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
    [SerializeField] private float knockbackDecaySpeed = 5f; 

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
    private int slipperyCounter = 0;
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
    private float mouseInputDeadzoneRadius = .4f;
    private float mouseInputVectorLimit = 5f;

    [Header("Effects")]
    [SerializeField] private GameObject dashStartEffect;
    [SerializeField] private ParticleSystem splashEffect;
    [SerializeField] private ParticleSystem wetEffect;
    [SerializeField] private float damageColorEffectDuration = .1f;
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
    private bool groundedPlayer = false; 
    private bool isSprinting = false;
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


    private Animator mainAnimator;
    private PlayerShaderManager shaderManager;
    [Header("Visuals")]
    [SerializeField] private GameObject[] characters;
    [SerializeField] private Image[] coloredElements;
    
    public bool initialized = false;

    private float tempCooldown; 
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
            GetComponent<PlayerInput>().enabled = true;
            GetComponent<PlayerInput>().ActivateInput();
        }
        
        controller = gameObject.GetComponent<CharacterController>(); 
        PlayerManager.Instance.OnPlayerJoined(GetComponent<PlayerInput>());
        GameManager.Instance.OnGameStarted += ResetPlayerController; 
        initialized = true;
    }

    public void InitializeLocal()
    {
        foreach(GameObject character in characters)
            character.SetActive(false);
        
        PlayerManager.Instance.AddPlayerLocal(gameObject.GetComponent<PlayerInput>());
        GetComponent<PlayerInput>().enabled = true;
        //GetComponent<PlayerInput>().ActivateInput();  // ← crucial to reinitialize input properly

        controller = gameObject.GetComponent<CharacterController>(); 
        GameManager.Instance.OnGameStarted += ResetPlayerController; 
        initialized = true;
    }
    
    private void Update()
    {
        if (!initialized || isDead || !IsOwner) return;

        // Get input every frame (e.g. from Input system or your own input handler)
        // Here you assume `movementInput` is updated elsewhere (e.g., Input events)
            
        groundedPlayer = controller.isGrounded;
        
            // Normalize input direction
            Vector3 direction = new Vector3(movementInput.x, 0, movementInput.y);
            direction = Vector3.ClampMagnitude(direction, 1f);

            // Ground check and gravity
            if (groundedPlayer && playerVelocity.y < 0)
            {
                playerVelocity.y = 0f;
            }
            else
            {
                playerVelocity.y += gravityValue * Time.deltaTime;
            }

            // Calculate movement vector
            Vector3 move = direction * playerSpeed * Time.deltaTime;

            // Apply knockback velocity if any (optional)
            if (knockbackVelocity.magnitude > 0.1f)
            {
                move += knockbackVelocity * Time.deltaTime;
                knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecaySpeed * Time.deltaTime);
            }
            else if (killCreditID != -1 && controller.isGrounded)
            {
                killCreditID = -1; 
            }
                
                
                
        if (controller.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }
        else
        {
            playerVelocity.y += gravityValue * Time.deltaTime;
        }
        if (!isDead)
        {
            if (!isUsingGamepad && movementInput.magnitude < mouseInputDeadzoneRadius)
            {
                targetDirection = Vector3.zero;
                if (movementInput != Vector2.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(new Vector3 (movementInput.x, 0, movementInput.y));
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
            }
            else
            {
                targetDirection = new Vector3(movementInput.x, 0, movementInput.y);
            }
            if (targetDirection == Vector3.zero && isSprinting)
            {
                targetDirection = transform.forward;
            }
            targetDirection = Vector3.ClampMagnitude(targetDirection, 1f);
            smoothMoveDirection = Vector3.SmoothDamp(smoothMoveDirection, targetDirection, ref moveVelocity, moveSmoothTime);
            move = smoothMoveDirection * (playerSpeed * Time.deltaTime);
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
            if (killCreditID != -1 && controller.isGrounded)
            {
                killCreditID = -1;
            }

            // Add vertical velocity (gravity)
            move += playerVelocity * Time.deltaTime;

            if (!isDead && targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation =
                    Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }

            // Update animation on server (optional, or sync to clients)
            mainAnimator?.SetBool("IsWalking", direction.sqrMagnitude > 0.01f);
        }
        
        
        // Move character controller
        if (controller.enabled)
        {
            controller.Move(move);
        }
    }
    
    #endregion

    #region Inputs
    public void OnMove(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused) return;
        if (isUsingGamepad)
        {
            movementInput = context.ReadValue<Vector2>();
            return;
        }
        else
        {
            movementInput += context.ReadValue<Vector2>() * Time.deltaTime;
            float inputMagnitude = movementInput.magnitude;
            if (inputMagnitude > mouseInputVectorLimit)
            {
                movementInput *= mouseInputVectorLimit / inputMagnitude;
            }
        }
    }

    public void OnFirstSpell(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused) return;
        
        if (!isFirstSpellReady)
        {
            controllerRumbler?.Rumble(.15f, 1f, 5f);
            return;
        }
        
        if (context.performed && !isDead)
        {
            CastSpell(true); // Request to cast first spell
        }
    }

    public void OnSecondSpell(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused) return;
        
        if (!isSecondSpellReady)
        {
            controllerRumbler?.Rumble(.15f, 1f, 5f);
            return;
        }
        
        if (context.performed && !isDead)
        {
            CastSpell(false); // Request to cast second spell
        }
    }
    
private void CastSpell(bool isFirstSpell)
{
    SO_Spell spell = isFirstSpell ? firstSpell : secondSpell;

    if (GameManager.Instance.playingLocal)
        CastSpellLocal(isFirstSpell);
    else
        CastSpellServerRpc(isFirstSpell);

    // Start server cooldown logic (authoritative)
    if (isFirstSpell)
        firstSpellCoroutine = StartCoroutine(CooldownCoroutine(tempCooldown, 1));
    else
        secondSpellCoroutine = StartCoroutine(CooldownCoroutine(tempCooldown, 2));
    
    mainAnimator.SetTrigger("SlapTrigger");
    
    RuntimeManager.PlayOneShotAttached(spell.GetSpellEventStruct(), gameObject);

    // Lock the spell input locally
    if (isFirstSpell)
        isFirstSpellReady = false;
    else
        isSecondSpellReady = false;
}


[ServerRpc(RequireOwnership = false)]
void CastSpellServerRpc(bool isFirstSpell)
{
    CastSpellClientRpc(isFirstSpell);
}

[ClientRpc]
void CastSpellClientRpc(bool isFirstSpell)
{
    SO_Spell spell = isFirstSpell ? firstSpell : secondSpell; 
    tempCooldown =  spell.CastSpell(playerID, transform.position, transform.forward, controller);
    
    // Start local cooldown visuals
    StartCoroutine(SpellCooldown(tempCooldown, isFirstSpell ? 1 : 2));
}

void CastSpellLocal(bool isFirstSpell)
{
    SO_Spell spell = isFirstSpell ? firstSpell : secondSpell; 
    tempCooldown =  spell.CastSpell(playerID, transform.position, transform.forward, controller);
    
    // Start local cooldown visuals
    StartCoroutine(SpellCooldown(tempCooldown, isFirstSpell ? 1 : 2));
}

private IEnumerator CooldownCoroutine(float time, int spellID)
{
    yield return new WaitForSeconds(time);

    if (GameManager.Instance.playingLocal)
        CooldownCompleteLocal(spellID);
    else
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

void CooldownCompleteLocal(int spellID)
{
    ResetSpell(spellID);
    if (spellID == 1)
        isFirstSpellReady = true;
    else
        isSecondSpellReady = true;
}
    
    public void OnFistSpellEquip(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused) return;
        if (itemToEquip != null && context.performed)
        {
            if (GameManager.Instance.playingLocal)
                EquipSpellLocal(1);
            else 
                EquipSpellServerRpc(1);
        }
    }

    public void OnSecondSpellEquip(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused) return;
        if (itemToEquip != null && context.performed)
        if (context.performed)
        {
            if (GameManager.Instance.playingLocal)
                EquipSpellLocal(2);
            else 
                EquipSpellServerRpc(2);
        }
    }

    [ServerRpc]
    private void EquipSpellServerRpc(int spellID)
    {
        if (itemToEquip == null) return;

        SO_Spell spell = itemToEquip.EquipSpell();
        int spellIndex = spell.spellIndex;

        switch (spellID)
        {
            case 1:
                firstSpell = spell;
                playerHUD.SetSpell(1, firstSpell.SpellIcon);
                break;
            case 2:
                secondSpell = spell;
                playerHUD.SetSpell(2, secondSpell.SpellIcon);
                break;
        }

        ResetSpell(spellID);
        EquipSpellClientRpc(spellID, spellIndex); // 🟢 Send the spell index instead
        itemToEquip = null;
    }

    [ClientRpc]
    private void EquipSpellClientRpc(int spellID, int spellIndex)
    {
        SO_Spell equippedSpell = null;

        foreach (SO_Spell spell in allSpells)
        {
            if (spell.spellIndex == spellIndex)
            {
                equippedSpell = spell;
                break;
            }
        }

        if (equippedSpell == null)
        {
            Debug.LogWarning("Spell index not found on client.");
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
        }
    }
    
    void EquipSpellLocal(int spellID)
    {
        if (itemToEquip == null) return;

        SO_Spell spell = itemToEquip.EquipSpell();
        int spellIndex = spell.spellIndex;

        switch (spellID)
        {
            case 1:
                firstSpell = spell;
                playerHUD.SetSpell(1, firstSpell.SpellIcon);
                break;
            case 2:
                secondSpell = spell;
                playerHUD.SetSpell(2, secondSpell.SpellIcon);
                break;
        }

        ResetSpell(spellID);
        
        SO_Spell equippedSpell = null;

        foreach (SO_Spell tempSpell in allSpells)
        {
            if (tempSpell.spellIndex == spellIndex)
            {
                equippedSpell = tempSpell;
                break;
            }
        }

        if (equippedSpell == null)
        {
            Debug.LogWarning("Spell index not found on client.");
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
        }
        
        itemToEquip = null;
    }
    
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused) return;
        if (context.performed)
        {
            if (!canSprint)
            {
                controllerRumbler?.Rumble(.15f, 1f, 5f);
                return;
            }
            sprintCoroutine = StartCoroutine(SprintCoroutine());
            SpawnDashEffectServerRpc();
            Instantiate(dashStartEffect, transform.position, transform.rotation);
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
        float smoothTime = moveSmoothTime;
        playerSpeed = playerSprintSpeed;
        moveSmoothTime = 0f;
        isSprinting = true;
        BeginSprintServerRpc();
        yield return new WaitForSeconds(playerSprintDuration);
        playerSpeed = moveSpeed;
        moveSmoothTime = smoothTime;
        isSprinting = false;
        EndSprintServerRpc();
        yield return new WaitForSeconds(sprintCooldown);
        canSprint = true;
    }

    [ServerRpc(RequireOwnership = false)]
    void BeginSprintServerRpc()
    {
        BeginSprintClientRpc();
    }

    [ClientRpc]
    void BeginSprintClientRpc()
    {
        OnBeginSprint?.Invoke();
    }
    
    [ServerRpc(RequireOwnership = false)]
    void EndSprintServerRpc()
    {
        EndSprintClientRpc();
    }

    [ClientRpc]
    void EndSprintClientRpc()
    {
        OnEndSprint?.Invoke();
    }
    
    public void OnEmote(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused) return;
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
        if (GameManager.Instance.playingLocal)
        {
            if (isInRange)
            {
                itemToEquip = item.GetComponent<Item>();
            }
            else if (!isInRange && item == itemToEquip)
            {
                itemToEquip = null;
            }
        }
        else
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

    public void ApplyKnockbackLocal(int ID, Vector3 direction, float force, float dmg)
    {
        if (isSlippery) 
        {
            force *= slipperyModifier;
            splashEffect.Play();
        }
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
        shaderManager?.DamageEffect(damageColorEffectDuration);
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

        if (GameManager.Instance.playingLocal)
        {
            GameManager.Instance.DeathReportLocal(playerID, killCreditID);
            DisableUIElementsLocal();
        }
        else
        {
            GameManager.Instance.DeathReportServerRpc(playerID, killCreditID);
            DisableUIElementsServerRpc();
        }

        playerHUD.DisplayDeath();
    }

    [ServerRpc(RequireOwnership = false)]
    void DisableUIElementsServerRpc()
    {
        // Server tells all clients to disable UI elements
        DisableUIElementsClientRpc();
    }

    [ClientRpc]
    void DisableUIElementsClientRpc()
    {
        for (int i = 0; i < coloredElements.Length; i++)
        {
            coloredElements[i].enabled = false;
        }
    }

    void DisableUIElementsLocal()
    {
        for (int i = 0; i < coloredElements.Length; i++)
        {
            coloredElements[i].enabled = false;
        }
    }
    
    public void SetSlippy(bool slippy)
    {
        if (slippy)
        {
            if(knockbackVelocity.sqrMagnitude > 0.2f)
            {
                knockbackVelocity *= slipperyModifier;
                splashEffect.Play();
            }
            slipperyCounter++;
        }
        else
        {
            slipperyCounter--;
            if (slipperyCounter < 0) slipperyCounter = 0;
        }
        if (slipperyCounter > 0)
        {
            isSlippery = true;
            wetEffect.Play();
        }
        else
        {
            isSlippery = false;
            wetEffect.Stop();
        }
        shaderManager?.WetEffect(isSlippery);
    }
    #endregion

    #region PlayerManager
    public void Victory()
    {
        mainAnimator.SetBool("Victory", true);        
    }
    public void ResetPlayerController()
    {
        slipperyCounter = 0;
        damage = 0;
        playerHUD.ResetHUD();
        isDead = false;
        isSlippery = false;
        killCreditID = -1;
        shaderManager?.ResetShader();
        mainAnimator.SetBool("IsDead", false);
        mainAnimator.SetBool("Victory", false);
        for (int i = 0; i < coloredElements.Length; i++)
        {
            coloredElements[i].enabled = true;
        }
        movementInput = Vector2.zero;
        controller.enabled = true; 
        GetComponent<PlayerStateHandler>().ResetPlayer();
    }
    public void SetUpPlayer(int playerID,PlayerHUD playerHUD, ControllerRumbler controllerRumbler, Color color)
    {
        this.playerHUD = playerHUD;
        this.playerID = playerID;
        characters[playerID].SetActive(true);
        mainAnimator = characters[playerID].GetComponent<Animator>();
        shaderManager = characters[playerID].GetComponentInChildren<PlayerShaderManager>();
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