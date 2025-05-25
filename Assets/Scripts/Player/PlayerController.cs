using FMODUnity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    #region Audio

    [Header("Sound Events")] [SerializeField]
    private EventReference knockBackEvent;

    [SerializeField] private EventReference deathEvent;
    [SerializeField] private EventReference dashEvent;
    [SerializeField] private float knockbackDecaySpeed = 5f;

    #endregion

    #region Visuals & Effects

    [Header("Visuals")] [SerializeField] private GameObject[] characters;
    [SerializeField] private Image[] coloredElements;

    [Header("Effects")] [SerializeField] private GameObject dashStartEffect;
    [SerializeField] private ParticleSystem splashEffect;
    [SerializeField] private ParticleSystem wetEffect;
    [SerializeField] private ParticleSystem damageParticleSystem;
    [SerializeField] private GameObject spellSpawnEffect;
    [SerializeField] private float damageColorEffectDuration = 0.1f;
    [SerializeField] private float materialSwapDuration = 0.1f;

    #endregion

    #region Spells

    [Header("Spells")] [SerializeField] private SO_Spell[] allSpells;
    private SO_Spell firstSpell;
    private SO_Spell secondSpell;
    private bool isFirstSpellReady = true;
    private bool isSecondSpellReady = true;
    private Coroutine firstSpellCoroutine;
    private Coroutine secondSpellCoroutine;

    #endregion

    #region Damage

    [Header("Damage")] [SerializeField] private float damageModifier = 0.05f;
    [SerializeField] private float slipperyModifier = 1.5f;
    [SerializeField] private float rumbleDurationFactor = 0.01f;
    private float damage = 0;
    private int killCreditID = -1;
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>();

    #endregion

    #region Sprint

    [Header("Sprint")] [SerializeField] private float playerSprintSpeed = 24f;
    [SerializeField] private float playerSprintDuration = 0.5f;
    [SerializeField] private float sprintCooldown = 3f;
    public float SprintCooldown => sprintCooldown;
    public UnityEvent OnBeginSprint;
    public UnityEvent OnEndSprint;
    private bool canSprint = true;
    private bool isSprinting = false;
    private Coroutine sprintCoroutine;

    #endregion

    #region Movement & Physics

    [Header("Player Stats")] [SerializeField]
    private float playerSpeed = 2.0f;

    [SerializeField] private float gravityValue = -9.81f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float moveSmoothTime = 0.1f;

    private CharacterController controller;
    private bool groundedPlayer = false;
    private Vector3 playerVelocity;
    private Vector3 move = Vector3.zero;
    private Vector2 movementInput = Vector2.zero;
    private Vector3 targetDirection = Vector3.zero;
    private Vector3 smoothMoveDirection = Vector3.zero;
    private Vector3 moveVelocity = Vector3.zero;
    private Vector3 knockbackVelocity = Vector3.zero;

    #endregion

    #region Input & UI

    private PlayerHUD playerHUD;
    private ControllerRumbler controllerRumbler = null;
    private bool isUsingGamepad = false;
    private float mouseInputDeadzoneRadius = 0.4f;
    private float mouseInputVectorLimit = 5f;
    private Vector3 lastPosition;
    private float desyncThreshold = 0.05f;
    private bool wasMovingLastFrame = false;

    #endregion

    #region State & Utility

    private int playerID = 0;
    public int PlayerID => playerID;
    public bool initialized = false;
    private float tempCooldown;
    private Item itemToEquip;
    private int slipperyCounter = 0;
    private bool isSlippery = false;
    public Animator mainAnimator;
    private PlayerShaderManager shaderManager;

    #endregion

    #region Initialization

    [ClientRpc]
    public void InitializeClientRpc()
    {
        DeactivateCharacters();

        if (IsOwner)
        {
            var netObj = GetComponent<NetworkObject>();
            PlayerManager.Instance.AddPlayerServerRpc(new NetworkObjectReference(netObj));
            EnableInput();
        }

        controller = GetComponent<CharacterController>();
        PlayerManager.Instance.OnPlayerJoined(GetComponent<PlayerInput>());
        GameManager.Instance.OnGameStarted += ResetPlayerController;
        initialized = true;
    }

    public void InitializeLocal()
    {
        DeactivateCharacters();

        PlayerManager.Instance.AddPlayerLocal(GetComponent<PlayerInput>());
        EnableInput();

        controller = GetComponent<CharacterController>();
        GameManager.Instance.OnGameStarted += ResetPlayerController;
        initialized = true;
    }

    private void DeactivateCharacters()
    {
        foreach (GameObject character in characters)
            character.SetActive(false);
    }

    private void EnableInput()
    {
        var input = GetComponent<PlayerInput>();
        input.enabled = true;
        input.ActivateInput();
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        if (!initialized || isDead.Value) return;
        if (!GameManager.Instance.playingLocal && !IsOwner) return;

        groundedPlayer = controller.isGrounded;
        HandleGravity();
        HandleMovementAndRotation();
        HandleAnimations();
        ApplyMovement();

        HandleDesyncAndSync();
    }

    #endregion

    #region Movement & Rotation

    private void HandleGravity()
    {
        if (groundedPlayer && playerVelocity.y < 0)
            playerVelocity.y = 0f;
        else
            playerVelocity.y += gravityValue * Time.deltaTime;
    }

    private void HandleMovementAndRotation()
    {
        Vector3 direction = new Vector3(movementInput.x, 0, movementInput.y);
        direction = Vector3.ClampMagnitude(direction, 1f);

        if (!isDead.Value)
        {
            if (!isUsingGamepad && movementInput.magnitude < mouseInputDeadzoneRadius)
            {
                targetDirection = Vector3.zero;
                if (movementInput != Vector2.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation =
                        Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
            }
            else
            {
                targetDirection = direction;
            }

            if (targetDirection == Vector3.zero && isSprinting)
                targetDirection = transform.forward;

            targetDirection = Vector3.ClampMagnitude(targetDirection, 1f);
        }
        else
        {
            targetDirection = Vector3.zero;
        }

        smoothMoveDirection =
            Vector3.SmoothDamp(smoothMoveDirection, targetDirection, ref moveVelocity, moveSmoothTime);
    }

    private void ApplyMovement()
    {
        Vector3 move = smoothMoveDirection * (playerSpeed * Time.deltaTime);

        if (knockbackVelocity.magnitude > 0.1f)
        {
            move += knockbackVelocity * Time.deltaTime;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecaySpeed * Time.deltaTime);
        }
        else if (killCreditID != -1 && controller.isGrounded)
        {
            killCreditID = -1;
        }

        move += playerVelocity * Time.deltaTime;

        if (!isDead.Value && targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        if (controller.enabled)
            controller.Move(move);
    }

    private void HandleAnimations()
    {
        bool isMoving = movementInput.sqrMagnitude > 0.01f;

        if (GameManager.Instance.playingLocal)
            mainAnimator?.SetBool("IsWalking", isMoving);
        else
            WalkingAnimServerRpc(new Vector3(movementInput.x, 0, movementInput.y));

        if (!wasMovingLastFrame && isMoving)
        {
            transform.position += new Vector3(0.0001f, 0, 0);
            transform.position -= new Vector3(0.0001f, 0, 0);
        }

        wasMovingLastFrame = isMoving;
    }

    private void HandleDesyncAndSync()
    {
        if (Vector3.Distance(transform.position, lastPosition) > desyncThreshold)
        {
            lastPosition = transform.position;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void WalkingAnimServerRpc(Vector3 direction)
    {
        mainAnimator?.SetBool("IsWalking", direction.sqrMagnitude > 0.01f);
    }

    #endregion
    
    #region Input Events

    public void OnMove(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused || isDead.Value) return;

        if (isUsingGamepad)
        {
            movementInput = context.ReadValue<Vector2>();
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

    #endregion
    
    #region Spell System

    public void OnFirstSpell(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused || !context.performed || isDead.Value) return;
        if (!isFirstSpellReady)
        {
            controllerRumbler?.Rumble(.15f, 1f, 5f);
            return;
        }

        CastSpell(true);
    }

    public void OnSecondSpell(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused || !context.performed || isDead.Value) return;
        if (!isSecondSpellReady)
        {
            controllerRumbler?.Rumble(.15f, 1f, 5f);
            return;
        }

        CastSpell(false);
    }

    private void CastSpell(bool isFirstSpell)
    {
        SO_Spell spell = isFirstSpell ? firstSpell : secondSpell;

        if (GameManager.Instance.playingLocal)
            CastSpellLocal(isFirstSpell);
        else
            CastSpellServerRpc(isFirstSpell);

        if (isFirstSpell)
            firstSpellCoroutine = StartCoroutine(CooldownCoroutine(tempCooldown, 1));
        else
            secondSpellCoroutine = StartCoroutine(CooldownCoroutine(tempCooldown, 2));

        if (GameManager.Instance.playingLocal)
        {
            mainAnimator.SetTrigger("SlapTrigger");
            RuntimeManager.PlayOneShotAttached(spell.GetSpellEventStruct(), gameObject);
        }
        else
            SlapAnimServerRpc(spell.spellIndex);

        if (isFirstSpell)
            isFirstSpellReady = false;
        else
            isSecondSpellReady = false;
    }

    private void CastSpellLocal(bool isFirstSpell)
    {
        SO_Spell spell = isFirstSpell ? firstSpell : secondSpell;
        tempCooldown = spell.CastSpell(playerID, transform.position, transform.forward, controller);
        StartCoroutine(SpellCooldown(tempCooldown, isFirstSpell ? 1 : 2));
    }

    [ServerRpc(RequireOwnership = false)]
    private void CastSpellServerRpc(bool isFirstSpell)
    {
        CastSpellClientRpc(isFirstSpell);
    }

    [ClientRpc]
    private void CastSpellClientRpc(bool isFirstSpell)
    {
        SO_Spell spell = isFirstSpell ? firstSpell : secondSpell;
        tempCooldown = spell.CastSpell(playerID, transform.position, transform.forward, controller);
        StartCoroutine(SpellCooldown(tempCooldown, isFirstSpell ? 1 : 2));
    }

    [ServerRpc(RequireOwnership = false)]
    private void SlapAnimServerRpc(int spellIndex)
    {
        mainAnimator.SetTrigger("SlapTrigger");
        SlapAnimClientRpc(spellIndex);
    }

    [ClientRpc]
    private void SlapAnimClientRpc(int spellIndex)
    {
        SO_Spell spell = FindSpellByIndex(spellIndex);
        if (spell != null)
            RuntimeManager.PlayOneShotAttached(spell.GetSpellEventStruct(), gameObject);
    }

    private IEnumerator CooldownCoroutine(float time, int spellID)
    {
        yield return new WaitForSeconds(time);
        if (GameManager.Instance.playingLocal)
            CooldownCompleteLocal(spellID);
        else
            CooldownCompleteClientRpc(spellID);
    }

    [ClientRpc]
    private void CooldownCompleteClientRpc(int spellID) => ResetSpellState(spellID);

    private void CooldownCompleteLocal(int spellID) => ResetSpellState(spellID);

    private void ResetSpellState(int spellID)
    {
        ResetSpell(spellID);
        if (spellID == 1) isFirstSpellReady = true;
        else isSecondSpellReady = true;
    }
    
    private SO_Spell FindSpellByIndex(int spellIndex)
    {
        foreach (var spell in allSpells)
        {
            if (spell.spellIndex == spellIndex)
                return spell;
        }
        return null;
    }

    #endregion

    #region Spell Equip

    public void OnFistSpellEquip(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused || itemToEquip == null || !context.performed || isDead.Value) return;

        if (GameManager.Instance.playingLocal)
            EquipSpellLocal(1);
        else
            EquipSpellServerRpc(1);
    }

    public void OnSecondSpellEquip(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused || itemToEquip == null || !context.performed || isDead.Value) return;

        if (GameManager.Instance.playingLocal)
            EquipSpellLocal(2);
        else
            EquipSpellServerRpc(2);
    }

    [ServerRpc]
    private void EquipSpellServerRpc(int spellID)
    {
        if (itemToEquip == null) return;

        SO_Spell spell = itemToEquip.EquipSpell();
        UpdateEquippedSpell(spellID, spell);
        EquipSpellClientRpc(spellID, spell.spellIndex);
        itemToEquip = null;
    }

    [ClientRpc]
    private void EquipSpellClientRpc(int spellID, int spellIndex)
    {
        SO_Spell equippedSpell = FindSpellByIndex(spellIndex);
        if (equippedSpell == null)
        {
            Debug.LogWarning("Spell index not found on client.");
            return;
        }

        UpdateEquippedSpell(spellID, equippedSpell);
    }

    private void EquipSpellLocal(int spellID)
    {
        if (itemToEquip == null) return;

        SO_Spell spell = itemToEquip.EquipSpell();
        UpdateEquippedSpell(spellID, spell);
        itemToEquip = null;
    }

    private void UpdateEquippedSpell(int spellID, SO_Spell spell)
    {
        if (spellID == 1)
        {
            firstSpell = spell;
            playerHUD.SetSpell(1, firstSpell.SpellIcon);
        }
        else
        {
            secondSpell = spell;
            playerHUD.SetSpell(2, secondSpell.SpellIcon);
        }

        ResetSpell(spellID);
    }

    #endregion

    #region Sprinting

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused || !context.performed || isDead.Value) return;

        if (!canSprint)
        {
            controllerRumbler?.Rumble(.15f, 1f, 5f);
            return;
        }

        sprintCoroutine = StartCoroutine(SprintCoroutine());

        if (GameManager.Instance.playingLocal)
        {
            if (dashStartEffect != null)
            {
                Instantiate(dashStartEffect, transform.position, transform.rotation);
                RuntimeManager.PlayOneShotAttached(dashEvent, gameObject);
            }
        }
        else
            SpawnDashEffectServerRpc();
    }

    private IEnumerator SprintCoroutine()
    {
        canSprint = false;
        float originalSpeed = playerSpeed;
        float originalSmooth = moveSmoothTime;

        playerSpeed = playerSprintSpeed;
        moveSmoothTime = 0f;
        isSprinting = true;

        if (GameManager.Instance.playingLocal)
            OnBeginSprint?.Invoke();
        else
            BeginSprintServerRpc();

        yield return new WaitForSeconds(playerSprintDuration);

        playerSpeed = originalSpeed;
        moveSmoothTime = originalSmooth;
        isSprinting = false;

        if (GameManager.Instance.playingLocal)
            OnEndSprint?.Invoke();
        else
            EndSprintServerRpc();

        yield return new WaitForSeconds(sprintCooldown);
        canSprint = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnDashEffectServerRpc() => SpawnDashEffectClientRpc();

    [ClientRpc]
    private void SpawnDashEffectClientRpc()
    {
        if (dashStartEffect != null)
        {
            Instantiate(dashStartEffect, transform.position, transform.rotation);
            RuntimeManager.PlayOneShotAttached(dashEvent, gameObject);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void BeginSprintServerRpc() => BeginSprintClientRpc();

    [ClientRpc]
    private void BeginSprintClientRpc() => OnBeginSprint?.Invoke();

    [ServerRpc(RequireOwnership = false)]
    private void EndSprintServerRpc() => EndSprintClientRpc();

    [ClientRpc]
    private void EndSprintClientRpc() => OnEndSprint?.Invoke();

    #endregion
    
    #region Emotes

    public void OnEmote(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused || !context.performed) return;

        Vector2 value = context.ReadValue<Vector2>();
        if (GameManager.Instance.playingLocal)
            TriggerEmote(value);
        else
            EmoteAnimServerRpc(value);
    }

    [ServerRpc(RequireOwnership = false)]
    private void EmoteAnimServerRpc(Vector2 value) => TriggerEmote(value);

    private void TriggerEmote(Vector2 value)
    {
        switch (value.x, value.y)
        {
            case (0, 1): mainAnimator.SetTrigger("EmoteUp"); break;
            case (0, -1): break; // EmoteDown
            case (-1, 0): break; // EmoteLeft
            case (1, 0): break; // EmoteRight
        }
    }

    #endregion
    
    #region Items

    public void UpdateItemToEquip(Item item, bool isInRange)
    {
        if (GameManager.Instance.playingLocal)
        {
            if (isInRange)
                itemToEquip = item.GetComponent<Item>();
            else if (!isInRange && item == itemToEquip)
                itemToEquip = null;
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
            ApplySpells(first, second);
        else
            Debug.LogError("Could not resolve one or both spells from index!");
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

    private IEnumerator SpellCooldown(float time, int spellID)
    {
        float cooldownRate = 1f / time;
        playerHUD.SetSpellCooldown(spellID, cooldownRate);

        yield return new WaitForSeconds(time);
        ResetSpell(spellID);
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
        if (isDead.Value) return;

        damage += dmg;
        playerHUD.UpdateDamageText((int)damage);
        damageParticleSystem.Play();

        if (!IsOwner) return;

        if (isSlippery) force *= slipperyModifier;

        direction.y = 0;
        Vector3 knockback = direction.normalized * (force * (1 + (damage * damageModifier)));

        if (knockback.sqrMagnitude >= knockbackVelocity.sqrMagnitude)
            killCreditID = ID;

        knockbackVelocity += knockback;

        if (GameManager.Instance.playingLocal)
        {
            mainAnimator.SetTrigger("Flinch");
            RuntimeManager.PlayOneShotAttached(knockBackEvent, gameObject);
            shaderManager.DamageEffect(damageColorEffectDuration);
        }
        else
        {
            FlinchAnimServerRpc(force, dmg);
        }

        float duration = knockbackVelocity.magnitude * rumbleDurationFactor;
        controllerRumbler?.Rumble(duration, force, dmg);
    }

    [ServerRpc(RequireOwnership = false)]
    void FlinchAnimServerRpc(float force, float dmg)
    {
        mainAnimator.SetTrigger("Flinch");
        FlinchAnimClientRpc(force, dmg);
    }

    [ClientRpc]
    void FlinchAnimClientRpc(float force, float dmg)
    {
        RuntimeManager.PlayOneShotAttached(knockBackEvent, gameObject);
        shaderManager.DamageEffect(damageColorEffectDuration);
    }

    public void ApplyKnockbackLocal(int ID, Vector3 direction, float force, float dmg)
    {
        if (isDead.Value) return;

        if (isSlippery)
        {
            force *= slipperyModifier;
            splashEffect.Play();
        }

        direction.y = 0;
        Vector3 knockback = direction.normalized * (force * (1 + (damage * damageModifier)));

        if (knockback.sqrMagnitude >= knockbackVelocity.sqrMagnitude)
            killCreditID = ID;

        knockbackVelocity += knockback;
        damage += dmg;

        playerHUD.UpdateDamageText((int)damage);
        damageParticleSystem.Play();

        if (GameManager.Instance.playingLocal)
        {
            mainAnimator.SetTrigger("Flinch");
            RuntimeManager.PlayOneShotAttached(knockBackEvent, gameObject);
            shaderManager.DamageEffect(damageColorEffectDuration);
        }
        else
        {
            FlinchAnimServerRpc(force, dmg);
        }

        float duration = knockbackVelocity.magnitude * rumbleDurationFactor;
        controllerRumbler?.Rumble(duration, force, dmg);
    }

    [ServerRpc(RequireOwnership = false)]
    void DeadAnimServerRpc(bool activationState)
    {
        mainAnimator.SetBool("IsDead", activationState);
        DeadAnimClientRpc(activationState);
    }

    [ClientRpc]
    void DeadAnimClientRpc(bool activationState)
    {
        if (activationState)
            RuntimeManager.PlayOneShotAttached(deathEvent, gameObject);

        playerHUD?.DisplayDeath();
    }

    public void Die()
    {
        if (isDead.Value) return;
        
        controller.enabled = false;
        
        isDead.Value = true;

        if (GameManager.Instance.playingLocal)
        {
            mainAnimator.SetBool("IsDead", true);
            RuntimeManager.PlayOneShotAttached(deathEvent, gameObject);
        }
        else
        {
            DeadAnimServerRpc(true);
        }

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
        DisableUIElementsClientRpc();
    }

    [ClientRpc]
    void DisableUIElementsClientRpc()
    {
        foreach (var element in coloredElements)
            element.enabled = false;
    }

    void DisableUIElementsLocal()
    {
        foreach (var element in coloredElements)
            element.enabled = false;
    }

    public void SetSlippy(bool slippy)
    {
        if (slippy)
        {
            if (knockbackVelocity.sqrMagnitude > 0.2f)
            {
                knockbackVelocity *= slipperyModifier;
                splashEffect.Play();
            }

            slipperyCounter++;
        }
        else
        {
            slipperyCounter = Mathf.Max(0, slipperyCounter - 1);
        }

        isSlippery = slipperyCounter > 0;

        if (isSlippery)
            wetEffect.Play();
        else
            wetEffect.Stop();

        shaderManager?.WetEffect(isSlippery);
    }

    #endregion

    #region PlayerManager
    public void Victory()
    {
        if (GameManager.Instance.playingLocal)
            mainAnimator.SetBool("Victory", true);
        else
            VictoryAnimServerRpc(true);
    }

    [ServerRpc(RequireOwnership = false)]
    void VictoryAnimServerRpc(bool activationState)
    {
        mainAnimator.SetBool("Victory", activationState);
    }

    public void ResetPlayerController()
    {
        slipperyCounter = 0;
        damage = 0;
        isDead.Value = false;
        isSlippery = false;
        killCreditID = -1;

        shaderManager?.ResetShader();

        if (GameManager.Instance.playingLocal)
        {
            mainAnimator.SetBool("IsDead", false);
            mainAnimator.SetBool("Victory", false);
            playerHUD.ResetHUD();
        }
        else
        {
            DeadAnimServerRpc(false);
            VictoryAnimServerRpc(false);
            ResetHudServerRpc();
        }

        foreach (var element in coloredElements)
            element.enabled = true;

        movementInput = Vector2.zero;
        knockbackVelocity = Vector3.zero;
        controller.enabled = true;

        GetComponent<PlayerStateHandler>().ResetPlayer();
    }

    [ServerRpc(RequireOwnership = false)]
    void ResetHudServerRpc() => ResetHudClientRpc();

    [ClientRpc]
    void ResetHudClientRpc() => playerHUD.ResetHUD();

    public void SetUpPlayer(int playerID, PlayerHUD playerHUD, ControllerRumbler controllerRumbler, Color color)
    {
        this.playerHUD = playerHUD;
        this.playerID = playerID;

        characters[playerID].SetActive(true);
        mainAnimator = characters[playerID].GetComponent<Animator>();
        shaderManager = characters[playerID].GetComponentInChildren<PlayerShaderManager>();

        foreach (var element in coloredElements)
            element.color = color;

        playerHUD.UpdateDamageText((int)damage);

        if (controllerRumbler != null)
        {
            this.controllerRumbler = controllerRumbler;
            isUsingGamepad = true;
        }
    }
    #endregion
}