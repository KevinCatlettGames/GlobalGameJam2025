using System;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    #region Audio

    [Header("Sound Events")] 
    [SerializeField] private EventReference knockBackEvent;
    [SerializeField] string knockBackEventIntensityParam;
    [SerializeField] int knockBackEventMaxIntensity = 100; 
    [SerializeField] private EventReference deathEvent;
    [SerializeField] private EventReference dashEvent;

    #endregion

    #region Visuals & Effects

    [Header("Visuals")] 
    [SerializeField] private Image[] coloredElements;
    [SerializeField] private GameObject canvas;
    [SerializeField] private PlayerSpellIndicator spellIndicator1;
    [SerializeField] private PlayerSpellIndicator spellIndicator2;
    [SerializeField] private Transform meshParent;

    [Header("Effects")] 
    [SerializeField] private GameObject dashStartEffect;
    [SerializeField] private ParticleSystem splashEffect;
    [SerializeField] private ParticleSystem wetEffect;
    [SerializeField] private ParticleSystem damageParticleSystem;
    [SerializeField] private PlayerDamagedEffect damagedEffect;
    [SerializeField] private GameObject spellSpawnEffect;
    [SerializeField] private float damageColorEffectDuration = 0.1f;
    #endregion

    #region Spells

    [Header("Spells")] 
    private SO_Spell firstSpell;
    private SO_Spell secondSpell;
    private bool isFirstSpellReady = true;
    private bool isSecondSpellReady = true;
    private Coroutine firstSpellCoroutine;
    private Coroutine secondSpellCoroutine;
    private int pickedUpSpellsAmount = 0;
    private List<SO_Spell> usedSpell = new List<SO_Spell>();
    
    #endregion

    #region Damage

    [Header("Damage")] 
    [SerializeField] private float damageModifier = 0.05f;
    [SerializeField] private float rumbleDurationFactor = 0.01f;
    [SerializeField] private float knockbackDecaySpeed = 5f;
    [SerializeField] private float hitStunThreshold = 100f;
    [SerializeField] private float hitStunFactor = .1f;
    [SerializeField] private float maxHitStunDuration = .35f;
    private float damage = 0;
    public float Damage { get { return damage; } }
    
    private int killCreditID = -1;
    
    //public NetworkVariable<bool> isDead = new NetworkVariable<bool>();
    private bool isDead = false;
    private float hitStunDuration = 0;
    private bool canBeBoneFished = true;

    #endregion

    #region Status
    [Header("Status")]
    [SerializeField] private float vulnerableFactor = 1.5f;
    [SerializeField] private float slipperyModifier = 1.5f;
    [SerializeField] private float slowFactor = .4f;
    [SerializeField] private GameObject dashDisabledUI;
    [SerializeField] private GameObject doomedUI;
    private bool isVulnerable = false;
    private int slowCounter = 0;
    private bool isSlowed = false;
    private int slipperyCounter = 0;
    private bool isSlippery = false; 
    private Coroutine vulnerableRoutine = null;
    private float vulnerableTimer = 0f;
    #endregion

    #region Ult
    [Header("Ult")]
    [SerializeField] private float dmgTakenUltFactor = .5f;
    [SerializeField] private float dmgDealtUltFactor = .25f;
    [SerializeField] private float maxUltCharge = 100f;
    private float currentUltCharge = 0f;
    private bool isUltCharged = false;
    #endregion

    #region Sprint

    [Header("Sprint")] 
    [SerializeField] private float playerSprintSpeed = 24f;
    [SerializeField] private float playerSprintDuration = 0.5f;
    [SerializeField] private float sprintCooldown = 3f;
    public float SprintCooldown => sprintCooldown;
    public UnityEvent OnBeginSprint;
    public UnityEvent OnEndSprint;
    private bool canSprint = true;
    private bool isSprinting = false;


    #endregion

    #region Movement & Physics

    [Header("Player Stats")] 
    [SerializeField] private float playerBaseSpeed = 2.0f;
    [SerializeField] private float gravityValue = -9.81f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float moveSmoothTime = 0.1f;
    private float currentPlayerSpeed = 1;

    protected CharacterController controller;
    public CharacterController Controller { get { return controller; } }
    private bool groundedPlayer = false;
    
    [Header("Movement")]
    private Vector3 playerVelocity;
    protected Vector2 movementInput = Vector2.zero;
    private Vector3 targetDirection = Vector3.zero;
    private Vector3 smoothMoveDirection = Vector3.zero;
    private Vector3 moveVelocity = Vector3.zero;
    protected Vector3 knockbackVelocity = Vector3.zero;

    #endregion

    #region Input & UI

    private PlayerHUD playerHUD;
    private ControllerRumbler controllerRumbler = null;
    protected bool isUsingGamepad = false;
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
    private List<Item> itemsToEquip = new List<Item>();
    public bool IsSlippery { get { return isSlippery; } }
    public Animator mainAnimator;
    private PlayerShaderManager shaderManager;
    private PlayerStateHandler playerStateHandler;

    #endregion
    
    #region Achievements
    
    [Header("Achievement Values")]
    [SerializeField] private LayerMask groundMask;
    private float groundCheckDistance = 20f;
    private bool isFirstGroundDetection = true;
    private bool groundRaycastWasDetected;
    private bool groundRaycastIsDetected;
    
    [SerializeField] private Vector3 boxCenterOffset = Vector3.zero;
    [SerializeField] private Vector3 boxHalfExtents = Vector3.one;
    [SerializeField] private LayerMask bubbleLayer;
    private HashSet<Collider> bubblesInside = new HashSet<Collider>();
    [SerializeField] private int shotsHitInARowAmountNeeded = 10;
    private int shotsHitInARowAmount = 0;
    [SerializeField] private int pickedUpSpellsNeeded = 20;
    
    #endregion

    #region Initialization

    [ClientRpc]
    public void InitializeClientRpc()
    {
        if (IsOwner)
        {
            var netObj = GetComponent<NetworkObject>();
            PlayerManager.Instance?.AddPlayerServerRpc(new NetworkObjectReference(netObj));
            EnableInput();
        }

        controller = GetComponent<CharacterController>();
        PlayerManager.Instance.OnPlayerJoined(GetComponent<PlayerInput>());
        GameManager.Instance.OnGameStarted += ResetPlayerController;
        initialized = true;
    }

    public void InitializeLocal()
    {
        PlayerManager.Instance?.AddPlayerLocal(GetComponent<PlayerInput>());
        EnableInput();

        controller = GetComponent<CharacterController>();
        GameManager.Instance.OnGameStarted += ResetPlayerController;
        initialized = true;
    }

    private void EnableInput()
    {
        var input = GetComponent<PlayerInput>();
        input.enabled = true;
        input.ActivateInput();
    }

    #endregion

    #region Update Loop
    protected void Start()
    {
        currentPlayerSpeed = playerBaseSpeed;
    }
    protected void Update()
    {
        if (!initialized || isDead) return;
        if (!GameManager.Instance.PlayingLocal && !IsOwner) return;

        groundedPlayer = controller.isGrounded;
      
        HandleGravity();
        HandleMovementAndRotation();
        HandleAnimations();
        ApplyMovement();
        HandleDesyncAndSync();
        HandleGroundRaycast();
        IncrementDodgeBubbleAchievement();
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

        if (!isDead)
        {
            if (!isUsingGamepad && movementInput.magnitude < mouseInputDeadzoneRadius)
            {
                targetDirection = Vector3.zero;
                if (movementInput != Vector2.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
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

        smoothMoveDirection = Vector3.SmoothDamp(smoothMoveDirection, targetDirection, ref moveVelocity, moveSmoothTime);
    }

    private void ApplyMovement()
    {
        if (hitStunDuration > 0)
        {
            hitStunDuration -= Time.deltaTime;
            if (hitStunDuration <= 0)
            {
                mainAnimator.SetBool("HitStun", false);
            }
            else
            {
                controller.Move(Vector3.zero);
                return;
            }
        }
        Vector3 move = smoothMoveDirection * (currentPlayerSpeed * Time.deltaTime);

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

        if (!isDead && targetDirection != Vector3.zero)
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

        if (GameManager.Instance.PlayingLocal)
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
        if (GameManager.IsGamePaused || isDead) return;
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
        if (GameManager.IsGamePaused || !context.performed || isDead || hitStunDuration > 0) return;
        if (!isFirstSpellReady)
        {
            controllerRumbler?.Rumble(.15f, 1f, 5f);
            playerHUD.AnimateSpellIcon(1);
            return;
        }

        CastSpell(true);
    }

    public void OnSecondSpell(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused || !context.performed || isDead || hitStunDuration > 0) return;
        if (!isSecondSpellReady)
        {
            controllerRumbler?.Rumble(.15f, 1f, 5f);
            playerHUD.AnimateSpellIcon(2);
            return;
        }

        CastSpell(false);
    }

    public void OnUltCharge(InputAction.CallbackContext context)
    {
        return; // Remove when Ult is back
        if (GameManager.IsGamePaused || !context.performed || isDead || hitStunDuration > 0) return;
        if (currentUltCharge >= maxUltCharge)
        {
            isUltCharged = true;
            playerHUD.ChargeUlt(true);
        }
        else
            controllerRumbler?.Rumble(.15f, 1f, 5f);
    }

    private void CastSpell(bool isFirstSpell)
    {
        SO_Spell spell = isFirstSpell ? firstSpell : secondSpell;

        if (GameManager.Instance.PlayingLocal)
            CastSpellLocal(isFirstSpell);
        else
            CastSpellServerRpc(isFirstSpell);

        if (isFirstSpell)
            isFirstSpellReady = false;
        else
            isSecondSpellReady = false;

        if (GameManager.Instance.PlayingLocal)
        {
            mainAnimator.SetTrigger("SlapTrigger");
            RuntimeManager.PlayOneShotAttached(spell.SpellEventStruct, gameObject);
        }
        else
            SlapAnimServerRpc(isFirstSpell);

    }

    private void CastSpellLocal(bool isFirstSpell)
    {
        SO_Spell spell = isFirstSpell ? firstSpell : secondSpell;
        float cooldown = spell.CastSpell(playerID, transform.position, transform.forward, controller, isUltCharged);
        if (isUltCharged)
        {
            currentUltCharge = 0;
            isUltCharged = false;
            playerHUD.ChargeUlt(false);
            playerHUD.SetUltSlider(0);
        }
        if (isFirstSpell)
        {
            firstSpellCoroutine = StartCoroutine(SpellCooldown(cooldown, 1));
        }
        else 
        {
            secondSpellCoroutine = StartCoroutine(SpellCooldown(cooldown, 2));
        }

        if (!usedSpell.Contains(spell))
            usedSpell.Add(spell);

        if (SteamIntegration.instance)
        {
            if (usedSpell.Count >= ItemSpawner.Instance.SpawnableItems.Length)
                SteamIntegration.instance.UnlockAchievement(SteamIntegration.instance.allWeaponsUsedAchievementID);
        }
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
        float cooldown = spell.CastSpell(playerID, transform.position, transform.forward, controller, isUltCharged);
        if (isUltCharged)
        {
            currentUltCharge = 0;
            isUltCharged = false;
            playerHUD.ChargeUlt(false);
            playerHUD.SetUltSlider(0);
        }
        if (isFirstSpell)
        {
            firstSpellCoroutine = StartCoroutine(SpellCooldown(cooldown, 1));
        }
        else
        {
            secondSpellCoroutine = StartCoroutine(SpellCooldown(cooldown, 2));
        }

        if ((ulong)playerID == NetworkManager.Singleton.LocalClientId)
        {
            if (!usedSpell.Contains(spell))
                usedSpell.Add(spell);

            if (SteamIntegration.instance)
            {
                if (usedSpell.Count >= ItemSpawner.Instance.SpawnableItems.Length)
                    SteamIntegration.instance.UnlockAchievement(SteamIntegration.instance.allWeaponsUsedAchievementID);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SlapAnimServerRpc(bool isFirstSpell)
    {
        mainAnimator.SetTrigger("SlapTrigger");
        SlapAnimClientRpc(isFirstSpell);
    }

    [ClientRpc]
    private void SlapAnimClientRpc(bool isFirstSpell)
    {
        SO_Spell spell = isFirstSpell ? firstSpell : secondSpell;
        if (spell != null)
            RuntimeManager.PlayOneShotAttached(spell.SpellEventStruct, gameObject);
    }

    #endregion

    #region Spell Equip
    private SO_Spell FindSpellByIndex(int spellIndex)
    {
        return ItemSpawner.Instance.GetSpellByIndex(spellIndex);
    }

    public void OnFistSpellEquip(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused || itemsToEquip.Count == 0 || !context.performed || isDead) return;

        if (GameManager.Instance.PlayingLocal)
            EquipSpellLocal(1);
        else
            EquipSpellServerRpc(1);
    }

    public void OnSecondSpellEquip(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused || itemsToEquip.Count == 0 || !context.performed || isDead) return;

        if (GameManager.Instance.PlayingLocal)
            EquipSpellLocal(2);
        else
            EquipSpellServerRpc(2);
    }

    [ServerRpc]
    private void EquipSpellServerRpc(int spellSlotID)
    {
        if (itemsToEquip == null || itemsToEquip.Count == 0) return;

        for (int i = itemsToEquip.Count - 1; i >= 0; i--)
        {
            if (itemsToEquip[i] == null) itemsToEquip.RemoveAt(i);
        }
        if (itemsToEquip[0] == null) return;

        EquipSpellClientRpc(spellSlotID, itemsToEquip[0].EquipSpell());
        itemsToEquip.RemoveAt(0);
    }

    [ClientRpc]
    private void EquipSpellClientRpc(int spellSlotID, int spellIndex)
    {
        SO_Spell equippedSpell = FindSpellByIndex(spellIndex);
        if (equippedSpell == null)
        {
            Debug.LogWarning("Spell index not found on client.");
            return;
        }

        UpdateEquippedSpell(spellSlotID, equippedSpell);

        if ((ulong)playerID == NetworkManager.Singleton.LocalClientId)
        {
            pickedUpSpellsAmount++;
            if (pickedUpSpellsAmount >= pickedUpSpellsNeeded)
            {
                if (SteamIntegration.instance)
                    SteamIntegration.instance.UnlockAchievement(SteamIntegration.instance.weaponsPickedUpAchievementID);
            }
        }
    }

    private void EquipSpellLocal(int spellSlotID)
    {
        if (itemsToEquip == null || itemsToEquip.Count == 0) return;

        for (int i = itemsToEquip.Count - 1; i >= 0; i--)
        {
            if (itemsToEquip[i] == null) itemsToEquip.RemoveAt(i);           
        }
        if (itemsToEquip.Count == 0) return;

        SO_Spell spell = FindSpellByIndex(itemsToEquip[0].EquipSpell());
        UpdateEquippedSpell(spellSlotID, spell);
        itemsToEquip.RemoveAt(0);
        
        pickedUpSpellsAmount++;
        if (pickedUpSpellsAmount >= pickedUpSpellsNeeded)
        {
            if (SteamIntegration.instance)
                SteamIntegration.instance.UnlockAchievement(SteamIntegration.instance.weaponsPickedUpAchievementID);
        }
    }

    private void UpdateEquippedSpell(int spellSlotID, SO_Spell spell)
    {
        if (spellSlotID == 1)
        {
            firstSpell = spell;
            playerHUD.SetSpell(1, firstSpell.SpellIcon, firstSpell.UsedSpellIcon);
            spellIndicator1.SetNewSpellColor(firstSpell.IndicatorColor);
        }
        else
        {
            secondSpell = spell;
            playerHUD.SetSpell(2, secondSpell.SpellIcon, secondSpell.UsedSpellIcon);
            spellIndicator2.SetNewSpellColor(secondSpell.IndicatorColor);
        }

        ResetSpell(spellSlotID);
    }

    #endregion

    #region Sprinting

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused || !context.performed || isDead || hitStunDuration > 0) return;

        if (!canSprint || isSlowed)
        {
            controllerRumbler?.Rumble(.15f, 1f, 5f);
            return;
        }

        StartCoroutine(SprintCoroutine());

        if (GameManager.Instance.PlayingLocal)
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
        float originalSmooth = moveSmoothTime;

        currentPlayerSpeed = playerSprintSpeed;
        moveSmoothTime = 0f;
        isSprinting = true;

        if (GameManager.Instance.PlayingLocal)
            OnBeginSprint?.Invoke();
        else
            BeginSprintServerRpc();

        float duration = 0;
        do
        {
            duration += Time.deltaTime;
            if (isSlowed) break;
            yield return null;
        } while (duration <= playerSprintDuration);

        if (!isSlowed)
            currentPlayerSpeed = playerBaseSpeed;
        else
            currentPlayerSpeed = playerBaseSpeed * slowFactor;

        moveSmoothTime = originalSmooth;
        isSprinting = false;

        if (GameManager.Instance.PlayingLocal)
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
        if (GameManager.Instance.PlayingLocal)
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
            case (0, 1): mainAnimator.SetInteger("EmoteID", 1); break;   // EmoteUp
            case (1, 0): mainAnimator.SetInteger("EmoteID", 2); break;  // EmoteRight
            case (0, -1): mainAnimator.SetInteger("EmoteID", 3); break; // EmoteDown
            case (-1, 0): mainAnimator.SetInteger("EmoteID", 4); break; // EmoteLeft
        }
        mainAnimator.SetTrigger("Emote");
    }

    #endregion
    
    #region Items

    public void UpdateItemToEquip(Item item, bool isInRange)
    {
        if (GameManager.Instance.PlayingLocal)
        {
            if (isInRange && !itemsToEquip.Contains(item))
                itemsToEquip.Add(item);
            else if (!isInRange && itemsToEquip.Contains(item))
                itemsToEquip.Remove(item);
        }
        else
        {
            if (isInRange && !itemsToEquip.Contains(item))
            {
                var itemNetworkObject = item.GetComponent<NetworkObject>();
                UpdateItemToEquipClientRpc(itemNetworkObject, isInRange);
            }
            else if (!isInRange && itemsToEquip.Contains(item))
            {
                var itemNetworkObject = item.GetComponent<NetworkObject>();
                UpdateItemToEquipClientRpc(itemNetworkObject, isInRange);
            }
        }
    }

    [ClientRpc]
    public void UpdateItemToEquipClientRpc(NetworkObjectReference item, bool toAdd)
    {
        Item _item = null;
        if (item.TryGet(out NetworkObject itemNetObj))
        {
            _item = itemNetObj.GetComponent<Item>();
        }
        if (_item != null)
        {
            if (toAdd)            
                itemsToEquip.Add(_item);         
            else  
                itemsToEquip.Remove(_item); 
        }

    }

    #endregion

    #region Spells

    public void SetSpells(int firstSpellIndex, int secondSpellIndex)
    {
        ApplySpells(FindSpellByIndex(firstSpellIndex), FindSpellByIndex(secondSpellIndex));

        if (IsServer)
            SetSpellsClientRpc(firstSpellIndex, secondSpellIndex);
    }

    [ClientRpc]
    public void SetSpellsClientRpc(int firstSpellIndex, int secondSpellIndex)
    {
        ApplySpells(FindSpellByIndex(firstSpellIndex), FindSpellByIndex(secondSpellIndex));
    }

    private void ApplySpells(SO_Spell firstSpell, SO_Spell secondSpell)
    {
        this.firstSpell = firstSpell;
        this.secondSpell = secondSpell;

        ResetSpell(1);
        ResetSpell(2);

        playerHUD.SetSpell(1, firstSpell.SpellIcon, firstSpell.UsedSpellIcon);
        playerHUD.SetSpell(2, secondSpell.SpellIcon, secondSpell.UsedSpellIcon);
        spellIndicator1.SetNewSpellColor(firstSpell.IndicatorColor);
        spellIndicator2.SetNewSpellColor(secondSpell.IndicatorColor);
    }

    private IEnumerator SpellCooldown(float time, int spellID)
    {
        float cooldownRate = 1f / time;
        playerHUD.SetSpellCooldown(spellID, cooldownRate);
        if (spellID == 1)
        {
            spellIndicator1.SetSpellCooldown(cooldownRate);
        }
        else 
        {
            spellIndicator2.SetSpellCooldown(cooldownRate);
        }

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
    [ServerRpc]
    public void ApplyImpulseServerRpc(Vector3 direction, float force) => ApplyForceClientRpc(direction, force);
    [ClientRpc]
    public void ApplyForceClientRpc(Vector3 direction, float force)
    {
        direction.y = 0;
        direction.Normalize();
        knockbackVelocity += direction * force;
    }
    public void ApplyImpulseLocal(Vector3 direction, float force)
    {
        direction.y = 0;
        direction.Normalize();
        knockbackVelocity += direction * force; 
    }
    [ServerRpc(RequireOwnership = false)]
    public void ApplyKnockbackServerRpc(int ID, Vector3 direction, float force, float dmg)
    {
        ApplyKnockbackClientRpc(ID, direction, force, dmg);
    }

    [ClientRpc]
    public void ApplyKnockbackClientRpc(int ID, Vector3 direction, float force, float dmg)
    {
        if (isDead && !IsOwner) return;

        if (isVulnerable)
        {
            dmg *= vulnerableFactor;
            force *= vulnerableFactor;
            StopVulnerable();
        }

        if (isSlippery)
        {
            force *= slipperyModifier;
            splashEffect.Play();
        }

        // Use ID -3 to avoid zeroing the y-component of the knockback for specific kockback events
        if (ID != -3) direction.y = 0;
        // Fixed knockback for -2 ID
        float mul = (ID == -2) ? 1 : (1 + (damage * damageModifier));
        Vector3 knockback = direction.normalized * mul * force;

        if (knockback.sqrMagnitude >= knockbackVelocity.sqrMagnitude)
            killCreditID = ID;

        knockbackVelocity += knockback;
        damage += dmg;

        if(damage > 0)
        {
            playerHUD.UpdateDamageText((int)damage);
            damagedEffect.UpdateParticleSystem(damage);
            damageParticleSystem.Play();
            GainUltCharge(dmg, false);

            if (GameManager.Instance.PlayingLocal)
            {
                mainAnimator.SetTrigger("Flinch");

                EventInstance fmodEvent = RuntimeManager.CreateInstance(knockBackEvent);
                RuntimeManager.AttachInstanceToGameObject(fmodEvent, transform, GetComponent<Rigidbody>());

                float normalized = Mathf.InverseLerp(0f, knockBackEventMaxIntensity, knockback.magnitude);
                float knockBackEventValue = Mathf.Clamp(normalized * 2f, 0f, 2f);
                int knockBackEventInt = Mathf.RoundToInt(knockBackEventValue);
                fmodEvent.setParameterByName(knockBackEventIntensityParam, knockBackEventInt);
                fmodEvent.start();
                fmodEvent.release();
                
                shaderManager.DamageEffect(damageColorEffectDuration);
                
                float knbMagnitude = knockbackVelocity.magnitude;
                float duration = knbMagnitude * rumbleDurationFactor;
                controllerRumbler?.Rumble(duration, force, dmg);
                // Use ID -2 to avoid hitstun for specific kockback events 
                //if (knbMagnitude >= hitStunThreshold && ID != -2)
                //{
                //    hitStunDuration = knbMagnitude * hitStunFactor;
                //    hitStunDuration = Mathf.Clamp(hitStunDuration, 0, maxHitStunDuration);
                //    mainAnimator.SetBool("HitStun", true);
                //}
            }
            else
            {
                FlinchAnimServerRpc(force, dmg);
                
                float knbMagnitude = knockbackVelocity.magnitude;
                float duration = knbMagnitude * rumbleDurationFactor;
                controllerRumbler?.Rumble(duration, force, dmg);
                //if (knbMagnitude >= hitStunThreshold && ID != -2)
                //{
                //    float stunDuration = knbMagnitude * hitStunFactor;
                //    HitStunServerRpc(stunDuration);
                //}
            }
        }
    }
    public void ApplyKnockbackLocal(int ID, Vector3 direction, float force, float dmg)
    {
        if (isDead) return;

        if (isVulnerable)
        {
            dmg *= vulnerableFactor;
            force *= vulnerableFactor;
            StopVulnerable();
        }

        if (isSlippery)
        {
            force *= slipperyModifier;
            splashEffect.Play();
        }

        // Use ID -3 to avoid zeroing the y-component of the knockback for specific kockback events
        if(ID != -3) direction.y = 0;
        // Fixed knockback for -2 ID
        float mul = (ID == -2) ? 1 : (1 + (damage * damageModifier));
        Vector3 knockback = direction.normalized * mul * force;

        if (knockback.sqrMagnitude >= knockbackVelocity.sqrMagnitude)
            killCreditID = ID;

        knockbackVelocity += knockback;
        damage += dmg;

        if (damage > 0)
        {
            playerHUD.UpdateDamageText((int)damage);
            damageParticleSystem.Play();
            damagedEffect.UpdateParticleSystem(damage);
            GainUltCharge(dmg, false);

            if (GameManager.Instance.PlayingLocal)
            {
                mainAnimator.SetTrigger("Flinch");

                EventInstance fmodEvent = RuntimeManager.CreateInstance(knockBackEvent);
                RuntimeManager.AttachInstanceToGameObject(fmodEvent, transform, GetComponent<Rigidbody>());

                float normalized = Mathf.InverseLerp(0f, knockBackEventMaxIntensity, knockback.magnitude);
                float knockBackEventValue = Mathf.Clamp(normalized * 2f, 0f, 2f);
                int knockBackEventInt = Mathf.RoundToInt(knockBackEventValue);
                fmodEvent.setParameterByName(knockBackEventIntensityParam, knockBackEventInt);
                fmodEvent.start();
                fmodEvent.release();

                shaderManager?.DamageEffect(damageColorEffectDuration);
                
                float knbMagnitude = knockbackVelocity.magnitude;
                float duration = knbMagnitude * rumbleDurationFactor;
                controllerRumbler?.Rumble(duration, force, dmg);
                // Use ID -2 to avoid hitstun for specific kockback events 
                //if (knbMagnitude >= hitStunThreshold && ID != -2)
                //{
                //    hitStunDuration = knbMagnitude * hitStunFactor;
                //    hitStunDuration = Mathf.Clamp(hitStunDuration, 0, maxHitStunDuration);
                //    mainAnimator.SetBool("HitStun", true);
                //}
            }
            else
            {
                FlinchAnimServerRpc(force, dmg);
                
                float knbMagnitude = knockbackVelocity.magnitude;
                float duration = knbMagnitude * rumbleDurationFactor;
                controllerRumbler?.Rumble(duration, force, dmg);
                //if (knbMagnitude >= hitStunThreshold && ID != -2)
                //{
                //    float stunDuration = knbMagnitude * hitStunFactor;
                //    HitStunServerRpc(stunDuration);
                //}
            }
        }
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
        EventInstance fmodEvent = RuntimeManager.CreateInstance(knockBackEvent);
        RuntimeManager.AttachInstanceToGameObject(fmodEvent, transform, GetComponent<Rigidbody>());

        float normalized = Mathf.InverseLerp(0f, knockBackEventMaxIntensity, force);
        float knockBackEventValue = Mathf.Clamp(normalized * 2f, 0f, 2f);
        int knockBackEventInt = Mathf.RoundToInt(knockBackEventValue);
        fmodEvent.setParameterByName(knockBackEventIntensityParam, knockBackEventInt);
        fmodEvent.start();
        fmodEvent.release();

        shaderManager.DamageEffect(damageColorEffectDuration);
    }

    //[ServerRpc(RequireOwnership = false)]
    //void HitStunServerRpc(float duration)
    //{
    //    HitStunClientRpc(duration);
    //}
    //
    //[ClientRpc]
    //void HitStunClientRpc(float duration)
    //{
    //    hitStunDuration = duration;
    //    hitStunDuration = Mathf.Clamp(hitStunDuration, 0, maxHitStunDuration);
    //    mainAnimator.SetBool("HitStun", true);
    //}

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
    }

    [ClientRpc]
    public void DieClientRpc() => Die();
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        controller.enabled = false;
        damagedEffect.UpdateParticleSystem(-1);

        if (GameManager.Instance.PlayingLocal)
        {
            mainAnimator.SetBool("IsDead", true);
            RuntimeManager.PlayOneShotAttached(deathEvent, gameObject);
        }
        else
        {
            DeadAnimServerRpc(true);
        }

        if (playerID == killCreditID) killCreditID = -1;

        if (GameManager.Instance.PlayingLocal)
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
        canvas.SetActive(false);
    }

    void DisableUIElementsLocal()
    {
        canvas.SetActive(false);
    }

    #endregion

    #region Ult
    public void GainUltCharge(float charge, bool isDamageDealt)
    {
        charge *= isDamageDealt ? dmgDealtUltFactor : dmgTakenUltFactor;
        currentUltCharge += charge;
        currentUltCharge = Mathf.Clamp(currentUltCharge, 0, maxUltCharge);
        playerHUD.SetUltSlider((float)currentUltCharge/maxUltCharge);
    }

    #endregion

    #region MapEvent
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("BoneFish") && canBeBoneFished)
        {
            canBeBoneFished = false;
            if (knockbackVelocity.sqrMagnitude < 1)
            {
                Vector3 v = transform.position - hit.collider.transform.position;
                if (GameManager.Instance.PlayingLocal)
                {
                    ApplyKnockbackLocal(-1, v, 5f, 5f);
                }
                else
                {
                    ApplyKnockbackServerRpc(-1, v, 5f, 5f);
                }
            }
            else
            {
                ReflectKnockback(hit.normal);
                if (GameManager.Instance.PlayingLocal)
                {
                    ApplyKnockbackLocal(-1, knockbackVelocity, 5f, 5f);
                }
                else
                {
                    ApplyKnockbackServerRpc(-1, knockbackVelocity, 5f, 5f);
                }
            }
            StartCoroutine(BoneFishCoroutine());
        }
        
    }
    private IEnumerator BoneFishCoroutine()
    {
        yield return new WaitForSeconds(.2f);
        canBeBoneFished = true;
    }
    public void ReflectKnockback(Vector3 reflectNormal)
    {
        //Effects and Animation go here
        knockbackVelocity = Vector3.Reflect(knockbackVelocity, reflectNormal);
        knockbackVelocity.y = 0;
    }

    public void SetDoomed(bool isDoomed)
    {
        doomedUI.SetActive(isDoomed);
        ShaderState state = (isDoomed) ? ShaderState.inked : ShaderState.sober;
        shaderManager.SetShaderState(state);
    }
    #endregion

    #region StatusConditions
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

        ShaderState state = (isSlippery) ? ShaderState.wet : ShaderState.sober;        
        shaderManager.SetShaderState(state);
 
    }
    public void SetSlowed(bool slow)
    {
        if (slow)
        {
            //Effect on hit
            slowCounter++;
        }
        else
        {
            slowCounter = Mathf.Max(0, slowCounter - 1);
        }

        isSlowed = slowCounter > 0;

        //Effect continuos
        if (isSlowed)
        {
            currentPlayerSpeed = playerBaseSpeed * slowFactor;
            //Effect.Play();
        }
        else
        {
            currentPlayerSpeed = playerBaseSpeed;
            //Effect.Stop();
        }

        ShaderState state = (isSlowed) ? ShaderState.inked : ShaderState.sober;
        shaderManager.SetShaderState(state);
        dashDisabledUI.SetActive(isSlowed);
    }
    public void StartVulnerable(float time)
    {
        if (vulnerableRoutine == null)
            vulnerableRoutine = StartCoroutine(VulnerableCoroutine(time));
        else if (time > vulnerableTimer)
        {
            vulnerableTimer = time;
        }
    }
    private IEnumerator VulnerableCoroutine(float duration)
    {
        vulnerableTimer = duration;
        isVulnerable = true;
        shaderManager.SetShaderState(ShaderState.sauced);
        while (vulnerableTimer > 0)
        {
            vulnerableTimer -= Time.deltaTime;
            yield return null;
        }
        StopVulnerable();
    }
    private void StopVulnerable()
    {
        if (vulnerableRoutine != null)
            StopCoroutine(vulnerableRoutine);
        vulnerableRoutine = null;
        isVulnerable = false;
        shaderManager.SetShaderState(ShaderState.sober);
    }
    #endregion

    #region PlayerManager
    public void Victory()
    {
        if (GameManager.Instance.PlayingLocal)
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
        damage = 0;
        damagedEffect.UpdateParticleSystem(-1);
        killCreditID = -1;
        hitStunDuration = 0;
        currentUltCharge = 0;
        playerHUD.SetUltSlider(0);
        isUltCharged = false;
        slipperyCounter = 0;
        isSlippery = false;
        slowCounter = 0;
        isSlowed = false;
        dashDisabledUI.SetActive(false);
        doomedUI.SetActive(false);
        isVulnerable = false;
        if (vulnerableRoutine != null)
            StopCoroutine(vulnerableRoutine);
        canBeBoneFished = true;

        shaderManager?.ResetShader();

        if (GameManager.Instance.PlayingLocal)
        {
            mainAnimator.SetBool("IsDead", false);
            mainAnimator.SetBool("Victory", false);
            mainAnimator.SetBool("HitStun", false);
            playerHUD.ResetHUD();
        }
        else
        {
            DeadAnimServerRpc(false);
            VictoryAnimServerRpc(false);
            ResetHudServerRpc();
        }

        canvas.SetActive(true);

        movementInput = Vector2.zero;
        knockbackVelocity = Vector3.zero;
        controller.enabled = true;

        pickedUpSpellsAmount = 0; 
        usedSpell.Clear();
        isFirstGroundDetection = true;
        shotsHitInARowAmount = 0; 
        
        playerStateHandler.ResetPlayer();
        isDead = false;
    }

    [ServerRpc(RequireOwnership = false)]
    void ResetHudServerRpc() => ResetHudClientRpc();

    [ClientRpc]
    void ResetHudClientRpc() => playerHUD.ResetHUD();

    public void SetUpPlayer(int playerID, PlayerHUD playerHUD, ControllerRumbler controllerRumbler, SkinSO skinObject)
    {
        this.playerHUD = playerHUD;
        this.playerID = playerID;

        GameObject skin = Instantiate(skinObject.SkinPrefab, meshParent);
        
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
        {
            foreach (var element in coloredElements)
                element.color = skinObject.Color;

            ActivateCorrectColorServerRpc(skinObject.Index);
            mainAnimator = skin.GetComponent<Animator>();
            shaderManager = skin.GetComponentInChildren<PlayerShaderManager>();
        }
        else
        {
            foreach (var element in coloredElements)
                element.color = skinObject.Color;
            mainAnimator = skin.GetComponent<Animator>();
            shaderManager = skin.GetComponentInChildren<PlayerShaderManager>(); 
        }
        
        playerHUD.UpdateDamageText((int)damage);

        if (controllerRumbler != null)
        {
            this.controllerRumbler = controllerRumbler;
            isUsingGamepad = true;
        }
        playerStateHandler = GetComponent<PlayerStateHandler>();
        playerStateHandler.EnableDeath();
    }

    [ServerRpc(RequireOwnership = false)]
    void ActivateCorrectColorServerRpc(int index)
    {
        ActivateCorrectColorClientRpc(index);
    }
    
    [ClientRpc]
    void ActivateCorrectColorClientRpc(int index)
    {
        SkinSO skinSOToUse = null; 
        
        foreach (LobbyPlayerHandler.PlayerValues playerValues in LobbyPlayerHandler.Instance.playerValuesList)
        {
            if (playerValues.Skin.Index == index)
            {
                skinSOToUse = playerValues.Skin;
                break;
            }
        }

        if (skinSOToUse == null) return;
        
        foreach (Image image in coloredElements)
            image.color = skinSOToUse.Color;
    }
    #endregion
    
    #region Achievements
    private void HandleGroundRaycast()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        
        groundRaycastIsDetected = Physics.Raycast(rayOrigin, Vector3.down,
            out RaycastHit hit,
            groundCheckDistance,
            groundMask);

        if (!groundRaycastWasDetected && groundRaycastIsDetected)
            IncrementRegainGroundAchievement(hit);
        
        groundRaycastWasDetected = groundRaycastIsDetected;
    }
    
    void IncrementRegainGroundAchievement(RaycastHit hit)
    {
        if (isFirstGroundDetection)
        {
            isFirstGroundDetection = false;
            return;
        }
        
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)playerID 
            || !SteamIntegration.instance) return;
        
        SteamIntegration steamIntegration = SteamIntegration.instance;
        SteamIntegration.instance.IncrementIntSteamStat(steamIntegration.regainGroundStatID,
            1, 
            SteamIntegration.instance.StatThresholds[steamIntegration.regainGroundStatID], 
            steamIntegration.regainGroundAchievementID);
    }

    void IncrementDodgeBubbleAchievement()
    {
        Vector3 boxCenter = transform.position + boxCenterOffset;
        Collider[] hits = Physics.OverlapBox(boxCenter, boxHalfExtents, Quaternion.identity, bubbleLayer);

        foreach (var hit in hits)
        {
            if (hit == null || !hit.GetComponent<BasicBubble>() || hit.GetComponent<BasicBubble>() && hit.GetComponent<BasicBubble>().OwnerID == playerID) continue;
            
            if (!bubblesInside.Contains(hit))
                bubblesInside.Add(hit);
        }

        var exiting = bubblesInside.Where(b => !hits.Contains(b)).ToList();
        foreach (var b in exiting)
        {
            bubblesInside.Remove(b);
            if (b == null || !b.GetComponent<BasicBubble>()) return; 
            
            BasicBubble bubble = b.GetComponent<BasicBubble>();
            
            if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)playerID 
                || bubble.HasPopped
                || bubble.OwnerID == playerID
                || !isSprinting
                || !SteamIntegration.instance) return;
                
                SteamIntegration steamIntegration = SteamIntegration.instance;
                SteamIntegration.instance.IncrementIntSteamStat(steamIntegration.bubbleDodgeStatID, 
                    1, 
                    steamIntegration.StatThresholds[steamIntegration.bubbleDodgeStatID], 
                    steamIntegration.bubbleDodgeAchievementID);
        }
    }

    public void UnlockShotsHitInARowAchievement(bool hitAPlayer)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)playerID 
            || !SteamIntegration.instance) return;
        
        if (hitAPlayer)
        {
            // hit a player
            shotsHitInARowAmount++;
            if (shotsHitInARowAmount >= shotsHitInARowAmountNeeded)
            {
                SteamIntegration steamIntegration = SteamIntegration.instance;
                SteamIntegration.instance.UnlockAchievement(steamIntegration.shotsHitInARowAchievementID);
            }
        }
        else
        {
            if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay &&
                NetworkManager.Singleton.LocalClientId != (ulong)playerID) return;
            shotsHitInARowAmount = 0; 
        }
    }
    #endregion
}