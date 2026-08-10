using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    #region Audio

    [Header("Sound Events")] 
    [SerializeField] private EventReference knockBackEvent;
    [SerializeField] private EventReference tickDamageEvent;
    [SerializeField] private EventReference vulnerableDamageEvent;
    [SerializeField] string knockBackEventIntensityParam;
    [SerializeField] int knockBackEventMaxIntensity = 100; 
    [SerializeField] private EventReference dashEvent;

    #endregion

    #region Visuals & Effects

    public GameObject childAnimatorObject; 

    [Header("Visuals")] 
    [SerializeField] private Image[] coloredElements;
    [SerializeField] private GameObject canvas;
    [SerializeField] private Transform meshParent;
    [SerializeField] private PlayerStatusIndicator statusIndicator;

    [Header("Effects")] 
    [SerializeField] private GameObject dashStartEffect;
    [SerializeField] private ParticleSystem splashEffect;
    [SerializeField] private ParticleSystem wetEffect;
    [SerializeField] private ParticleSystem vulnerableEffect;
    [SerializeField] private ParticleSystem vulnerableHitEffect;
    [SerializeField] private ParticleSystem trail;
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
    private List<BasicBubble> activeLocalFakes = new List<BasicBubble>();
    private int localSpellCounter = 0;

    #endregion

    #region Damage

    [Header("Damage")] 
    [SerializeField] private float damageModifier = 0.05f;
    [SerializeField] private float rumbleDurationFactor = 0.01f;
    [SerializeField] private float knockbackDecaySpeed = 5f;
    private float damage = 0;
    public float Damage { get { return damage; } }
    
    private int killCreditID = -1;
    
    private bool isDead = false;
    private bool canBeBoneFished = true;
    private DmgGenerator damageGenerator;

    #endregion

    #region Status
    [Header("Status")]
    [SerializeField] private float vulnerableFactor = 1.5f;
    [SerializeField] private float slipperyModifier = 1.5f;
    [SerializeField] private float slowFactor = .4f;
    [SerializeField] private GameObject dashDisabledUI;
    private bool isVulnerable = false;
    public bool IsVulnerable {  get { return isVulnerable; } }
    private int slowCounter = 0;
    private bool isSlowed = false;
    private bool wasSlowedWhenLastHit = false;
    public bool WasSlowedWhenLastHit { get { return wasSlowedWhenLastHit; } }
    private int slipperyCounter = 0;
    private bool isSlippery = false; 
    private Coroutine vulnerableRoutine = null;
    private float vulnerableTimer = 0f;
    private bool isStunned = false;
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
    private bool inputEnabled = true;

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
    private SkinSO currentSkinSO;
    public SkinSO CurrentSkinSO
    {
        get { return currentSkinSO; }
    }

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
    private int pickedUpSpellsNeeded = 10;
    
    #endregion

    #region Initialization

    public void InitializeLocal()
    {
        PlayerManager.Instance?.AddPlayerLocal(GetComponent<PlayerInput>());
        EnableInput();

        controller = GetComponent<CharacterController>();
        GameManager.Instance.OnGameStarted += ResetPlayerController;
        initialized = true;
    }

    [ClientRpc]
    public void InitializeClientRpc()
    {
        if (IsOwner)
        {
            var netObj = GetComponent<NetworkObject>();
            EnableInput();
        }

        controller = GetComponent<CharacterController>();
        PlayerManager.Instance.OnPlayerJoined(GetComponent<PlayerInput>());
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
        damageGenerator = GetComponent<DmgGenerator>();
        shaderManager?.SetStatusIndicator(statusIndicator);
    }
    protected void Update()
    {
        if (!inputEnabled || !initialized || isDead) return;
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
        if (isStunned)
        {
            controller.Move(Vector3.zero);
            return;
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
            if (wasSlowedWhenLastHit)
                wasSlowedWhenLastHit = false;
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

    public void Teleport(Vector3 destination, Quaternion rotation)
    {
        if (trail != null)
            trail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        knockbackVelocity = Vector3.zero;
        playerVelocity = Vector3.zero;

        if (controller != null)
            controller.enabled = false;

        transform.position = destination;
        transform.rotation = rotation;

        if (controller != null)
            controller.enabled = true;

        if (!GameManager.Instance.PlayingLocal && TryGetComponent<Unity.Netcode.Components.NetworkTransform>(out var netTransform))
        {
            if (netTransform.CanCommitToTransform)
            {
                netTransform.Teleport(destination, rotation, transform.lossyScale);
            }
        }

        if (trail != null && !isDead)
        {
            trail.Play();
        }
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

    public void OnGameContinue(InputAction.CallbackContext context)
    {
        if (!context.canceled) return;
 
        if (ScoreManager.Instance.ScoresResolved && GameManager.Instance.IsReadyToRestart && !WinScreenManager.Instance)
        {
            if (!MapRotationSystem.Instance.CheckForMapSwitch(GameManager.Instance.FinishedRoundCount))
            {
                if(GameManager.Instance.PlayingLocal)
                    GameManager.Instance.RestartGame();
                else
                {
                    if(IsServer)
                        GameManager.Instance.RestartGameServerRpc();
                }
            }
        }
    }

    #endregion

    #region Spell System

    public void OnFirstSpell(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused || !context.performed || isDead || isStunned || !inputEnabled) return;
        if (!isFirstSpellReady || firstSpell == null)
        {
            controllerRumbler?.Rumble(.15f, 1f, 5f);
            playerHUD.AnimateSpellIcon(1);
            return;
        }

        CastSpell(true);
    }

    public void OnSecondSpell(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused || !context.performed || isDead || isStunned|| !inputEnabled) return;
        if (!isSecondSpellReady || secondSpell == null)
        {
            controllerRumbler?.Rumble(.15f, 1f, 5f);
            playerHUD.AnimateSpellIcon(2);
            return;
        }

        CastSpell(false);
    }

    public void OnUltCharge(InputAction.CallbackContext context)
    {
        return;
        if (GameManager.IsGamePaused || !context.performed || isDead || isStunned) return;
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
        {
            CastSpellLocal(isFirstSpell);
        }
        else
        {
            int assignedSpellID = ((int)NetworkManager.Singleton.LocalClientId * 10000) + (localSpellCounter + 1);
            if(!IsServer)
            {
                CastSpellLocal(isFirstSpell);
            }
            CastSpellServerRpc(isFirstSpell, assignedSpellID, playerID);
            if(IsServer && spell.FakeWithServerCaster)
            {
                CastSpellOnServerCastClientRpc(isFirstSpell, playerID, transform.position, transform.forward, isUltCharged, NetworkManager.Singleton.LocalClientId, assignedSpellID);
            }
        }

        if (isFirstSpell) isFirstSpellReady = false;
        else isSecondSpellReady = false;

        if (GameManager.Instance.PlayingLocal)
        {
            mainAnimator.SetTrigger("SlapTrigger");
            RuntimeManager.PlayOneShotAttached(spell.SpellVoiceEvent, gameObject);
        }

        if (!GameManager.Instance.PlayingLocal)
        {
            GetComponent<NetworkAnimatorProxy>().SetAnimTrigger("SlapTrigger");
            if (spell != null)
                RuntimeManager.PlayOneShotAttached(spell.SpellVoiceEvent, gameObject);
            SlapAnimServerRpc(isFirstSpell);
        }
    }

    [ClientRpc]
    private void CastSpellOnServerCastClientRpc(bool isFirstSpell, int playerID, Vector3 pos, Vector3 dir, bool isUltCharged, ulong clientID, int assignedID)
    {
        SO_Spell spell = isFirstSpell ? firstSpell : secondSpell;
        Collider casterCollider = null;
        foreach(PlayerController controller in GameManager.Instance.Players)
            if(controller != null)
                if(controller.PlayerID == playerID)
                    casterCollider = controller.GetComponent<Collider>();

        spell.CastSpell(playerID, pos, dir, casterCollider, isUltCharged, clientID, assignedID);
    }

    private void CastSpellLocal(bool isFirstSpell)
    {
        SO_Spell spell = isFirstSpell ? firstSpell : secondSpell;

        localSpellCounter++;
        int assignedID = ((int)NetworkManager.Singleton.LocalClientId * 10000) + localSpellCounter;
        if (spell is SO_Rapid)
            localSpellCounter++;

        float cooldown = spell.CastSpell(playerID, transform.position, transform.forward, controller, isUltCharged, NetworkManager.Singleton.LocalClientId, assignedID);

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
        {
            usedSpell.Add(spell);
            //Debug.Log("Added to used spells");
        }

        if (AchievementSaveSystem.instance)
        {
            if (usedSpell.Count >= ItemSpawner.Instance.SpawnableItems.Length)
                AchievementSaveSystem.instance.UnlockAchievement(28);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CastSpellServerRpc(bool isFirstSpell, int assignedSpellID, int casterPlayerID, ServerRpcParams rpcParams = default)
    {
        SO_Spell spell = isFirstSpell ? firstSpell : secondSpell;
        float cooldown = spell.CastSpell(casterPlayerID, transform.position, transform.forward, GameManager.Instance.Players[casterPlayerID].controller, isUltCharged, rpcParams.Receive.SenderClientId, assignedSpellID);
        CastSpellClientRpc(isFirstSpell, rpcParams.Receive.SenderClientId, cooldown);
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
        {
            usedSpell.Add(spell);
            Debug.Log("Added to used spells");
        }

        if (AchievementSaveSystem.instance)
        {
            if (usedSpell.Count >= ItemSpawner.Instance.SpawnableItems.Length)
                AchievementSaveSystem.instance.UnlockAchievement(28);
        }
    }

    [ClientRpc]
    private void CastSpellClientRpc(bool isFirstSpell, ulong senderClientId, float cooldown)
    {
        if (NetworkManager.Singleton.LocalClientId == senderClientId)
        {
            return;
        }

        SO_Spell spell = isFirstSpell ? firstSpell : secondSpell;

        if (isUltCharged)
        {
            currentUltCharge = 0;
            isUltCharged = false;
            playerHUD.ChargeUlt(false);
            playerHUD.SetUltSlider(0);
        }

        if (isFirstSpell)
            firstSpellCoroutine = StartCoroutine(SpellCooldown(cooldown, 1));
        else
            secondSpellCoroutine = StartCoroutine(SpellCooldown(cooldown, 2));

        if (!usedSpell.Contains(spell))
        {
            usedSpell.Add(spell);
            Debug.Log("Added to used spells");
        }

        if (AchievementSaveSystem.instance)
        {
            if (usedSpell.Count >= ItemSpawner.Instance.SpawnableItems.Length)
                AchievementSaveSystem.instance.UnlockAchievement(28);
        }
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

    private void EquipSpellLocal(int spellSlotID)
    {
        if (itemsToEquip == null || itemsToEquip.Count == 0) return;

        for (int i = itemsToEquip.Count - 1; i >= 0; i--)
        {
            Item item = itemsToEquip[i];
            if (item == null || !item.gameObject.activeSelf) itemsToEquip.RemoveAt(i);           
        }
        if (itemsToEquip.Count == 0) return;

        SO_Spell spell = FindSpellByIndex(itemsToEquip[0].EquipSpell());
        UpdateEquippedSpell(spellSlotID, spell);
        itemsToEquip.RemoveAt(0);
        
        pickedUpSpellsAmount++;
        if (pickedUpSpellsAmount >= pickedUpSpellsNeeded)
        {
            if (SteamIntegration.instance)
                SteamIntegration.instance.UnlockAchievement(15);
        }
    }

    private void UpdateEquippedSpell(int spellSlotID, SO_Spell spell)
    {
        if (spellSlotID == 1)
        {
            firstSpell = spell;
            playerHUD.SetSpell(1, firstSpell.SpellIcon, firstSpell.UsedSpellIcon);
        }
        else
        {
            secondSpell = spell;
            playerHUD.SetSpell(2, secondSpell.SpellIcon, secondSpell.UsedSpellIcon);
        }

        ResetSpell(spellSlotID);
    }

    [ServerRpc]
    private void EquipSpellServerRpc(int spellSlotID)
    {
        if (itemsToEquip == null || itemsToEquip.Count == 0) return;

        for (int i = itemsToEquip.Count - 1; i >= 0; i--)
        {
            Item item = itemsToEquip[i];
            if (item == null || !item.gameObject.activeSelf) itemsToEquip.RemoveAt(i);
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
            return;
        }

        UpdateEquippedSpell(spellSlotID, equippedSpell);

        if ((ulong)playerID == NetworkManager.Singleton.LocalClientId)
        {
            pickedUpSpellsAmount++;
            if (pickedUpSpellsAmount >= pickedUpSpellsNeeded)
            {
                if (SteamIntegration.instance)
                    SteamIntegration.instance.UnlockAchievement(15);
            }
        }
    }

    #endregion

    #region Sprinting

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (GameManager.IsGamePaused || !context.performed || isDead || isStunned || !inputEnabled) return;

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
            mainAnimator.Play("Dash", 0, 0);
        }
        else
        {
            Instantiate(dashStartEffect, transform.position, transform.rotation);
            RuntimeManager.PlayOneShotAttached(dashEvent, gameObject);
            SpawnDashEffectServerRpc();
            DashAnimServerRpc();
        }
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
        if (IsOwner) return;
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
        if (GameManager.IsGamePaused || !context.performed || isDead || isStunned || !inputEnabled) return;

        Vector2 value = context.ReadValue<Vector2>();
        if (GameManager.Instance.PlayingLocal)
            TriggerEmote(value);
        else
            EmoteAnimServerRpc(value);
    }

    [ServerRpc(RequireOwnership = false)]
    private void EmoteAnimServerRpc(Vector2 value)
    {
        EmoteAnimClientRpc(value);
    }

    [ClientRpc]
    private void EmoteAnimClientRpc(Vector2 value)
    {
        switch (value.x, value.y)
        {
            case (0, 1): GetComponent<NetworkAnimatorProxy>().SetAnimPlay("UP_Emote", 0, 0); break;   // EmoteUp
            case (1, 0): GetComponent<NetworkAnimatorProxy>().SetAnimPlay("RIGHT_Emote", 0, 0); break;  // EmoteRight
            case (0, -1): GetComponent<NetworkAnimatorProxy>().SetAnimPlay("DOWN_Emote", 0, 0); break; // EmoteDown
            case (-1, 0): GetComponent<NetworkAnimatorProxy>().SetAnimPlay("LEFT_Emote", 0, 0); break; // EmoteLeft
        }
    }

    private void TriggerEmote(Vector2 value)
    {
        switch (value.x, value.y)
        {
            case (0, 1): mainAnimator.Play("UP_Emote", 0, 0); break;   // EmoteUp
            case (1, 0): mainAnimator.Play("RIGHT_Emote", 0, 0); break;  // EmoteRight
            case (0, -1): mainAnimator.Play("DOWN_Emote", 0, 0); break; // EmoteDown
            case (-1, 0): mainAnimator.Play("LEFT_Emote", 0, 0); break; // EmoteLeft
        }
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

    [ServerRpc]
    public void ApplyImpulseServerRpc(Vector3 direction, float force)
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

    public void ApplyKnockbackLocal(int ID, Vector3 direction, float force, float dmg)
    {
        if (isDead) return;

        if (isVulnerable)
        {
            if (vulnerableHitEffect)
                vulnerableHitEffect.Play();
            dmg *= vulnerableFactor;
            force *= vulnerableFactor;

            EventInstance fmodEvent = RuntimeManager.CreateInstance(vulnerableDamageEvent);
            RuntimeManager.AttachInstanceToGameObject(fmodEvent, transform, GetComponent<Rigidbody>());
            fmodEvent.start();
            fmodEvent.release();

            StopVulnerable();
        }

        if (isSlippery)
        {
            force *= slipperyModifier;
            splashEffect.Play();
        }

        if (isSlowed)
            wasSlowedWhenLastHit = true;

        direction.y = 0;
        // Fixed knockback for -2 ID
        float mul = (ID == -2) ? 1 : (1 + (damage * damageModifier));
        Vector3 knockback = mul * force * direction.normalized;

        if (knockback.sqrMagnitude >= knockbackVelocity.sqrMagnitude)
            killCreditID = ID;

        knockbackVelocity += knockback;
        damage += dmg;

        if (dmg > 0)
        {
            damageGenerator?.SpawnDamagePopup((int)dmg);
            playerHUD.UpdateDamageText((int)damage);
            damageParticleSystem.Play();
            damagedEffect.UpdateParticleSystem(damage);
            GainUltCharge(dmg, false);

            mainAnimator.SetTrigger("Flinch");

            if (force != 0)
            {
                EventInstance fmodEvent = RuntimeManager.CreateInstance(knockBackEvent);
                RuntimeManager.AttachInstanceToGameObject(fmodEvent, transform, GetComponent<Rigidbody>());
                float normalized = Mathf.InverseLerp(0f, knockBackEventMaxIntensity, knockback.magnitude);
                float knockBackEventValue = Mathf.Clamp(normalized * 2f, 0f, 2f);
                int knockBackEventInt = Mathf.RoundToInt(knockBackEventValue);
                fmodEvent.setParameterByName(knockBackEventIntensityParam, knockBackEventInt);
                fmodEvent.start();
                fmodEvent.release();
            }
            else
            {
                RuntimeManager.PlayOneShotAttached(tickDamageEvent, gameObject);
            }

            shaderManager?.DamageEffect(damageColorEffectDuration);

            float knbMagnitude = knockbackVelocity.magnitude;
            float duration = knbMagnitude * rumbleDurationFactor;
            controllerRumbler?.Rumble(duration, force, dmg);
        }
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
            if (vulnerableHitEffect)
                vulnerableHitEffect.Play();
            dmg *= vulnerableFactor;
            force *= vulnerableFactor;

            EventInstance fmodEvent = RuntimeManager.CreateInstance(vulnerableDamageEvent);
            RuntimeManager.AttachInstanceToGameObject(fmodEvent, transform, GetComponent<Rigidbody>());
            fmodEvent.start();
            fmodEvent.release();

            StopVulnerable();
        }

        if (isSlippery)
        {
            force *= slipperyModifier;
            splashEffect.Play();
        }

        if (isSlowed)
            wasSlowedWhenLastHit = true;

        direction.y = 0;
        // Fixed knockback for -2 ID
        float mul = (ID == -2) ? 1 : (1 + (damage * damageModifier));
        Vector3 knockback = mul * force * direction.normalized;

        if (knockback.sqrMagnitude >= knockbackVelocity.sqrMagnitude)
            killCreditID = ID;

        knockbackVelocity += knockback;
        damage += dmg;

        if (dmg > 0)
        {
            // Spawns exactly once per client network broadcast
            damageGenerator?.SpawnDamagePopup((int)dmg);
            playerHUD.UpdateDamageText((int)damage);
            damagedEffect.UpdateParticleSystem(damage);
            damageParticleSystem.Play();
            GainUltCharge(dmg, false);

            // Trigger animation safely through your proxy split gates
            GetComponent<NetworkAnimatorProxy>().SetAnimTrigger("Flinch");

            // 🟢 FIX: Move the audio & shader logic directly here. No more FlinchServerRpc!
            if (force != 0)
            {
                EventInstance fmodEvent = RuntimeManager.CreateInstance(knockBackEvent);
                RuntimeManager.AttachInstanceToGameObject(fmodEvent, transform, GetComponent<Rigidbody>());
                float normalized = Mathf.InverseLerp(0f, knockBackEventMaxIntensity, force);
                float knockBackEventValue = Mathf.Clamp(normalized * 2f, 0f, 2f);
                int knockBackEventInt = Mathf.RoundToInt(knockBackEventValue);
                fmodEvent.setParameterByName(knockBackEventIntensityParam, knockBackEventInt);
                fmodEvent.start();
                fmodEvent.release();
            }
            else
            {
                RuntimeManager.PlayOneShotAttached(tickDamageEvent, gameObject);
            }

            shaderManager?.DamageEffect(damageColorEffectDuration);

            float knbMagnitude = knockbackVelocity.magnitude;
            float duration = knbMagnitude * rumbleDurationFactor;
            controllerRumbler?.Rumble(duration, force, dmg);
        }
    }

    [ClientRpc]
    public void DieClientRpc() => Die();
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        controller.enabled = false;
        damagedEffect.UpdateParticleSystem(-1);
        trail.Stop();

        if (GameManager.Instance.PlayingLocal)
        {
            mainAnimator.SetBool("IsDead", true);
        }
        else
        {
            DeadAnimServerRpc(true);
        }

        if (playerID == killCreditID)
        {
            GameManager.Instance.UnlockDieFromOwnExplosionAchievement(playerID);
            killCreditID = -1;
        }

        if (GameManager.Instance.PlayingLocal)
        {
            GameManager.Instance.DeathReportLocal(playerID, killCreditID);
            DisableUIElementsLocal();
        }
        else
        {
            if(IsOwner)
                GameManager.Instance.DeathReportServerRpc(playerID, killCreditID);

            DisableUIElementsServerRpc();
        }

        playerHUD.DisplayDeath();
    }
    private void DisableUIElementsLocal()
    {
        canvas.SetActive(false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DisableUIElementsServerRpc()
    {
        DisableUIElementsClientRpc();
    }

    [ClientRpc]
    private void DisableUIElementsClientRpc()
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
        if (canBeBoneFished && hit.gameObject.CompareTag("BoneFish"))
        {
            canBeBoneFished = false;
            float dmg = hit.gameObject.GetComponent<BoneFish>().BoneHit();
            if (knockbackVelocity.magnitude < 3f)
            {
                Vector3 v = transform.position - hit.point;
                if (GameManager.Instance.PlayingLocal)
                {
                    ApplyKnockbackLocal(-1, v, 1, dmg);
                }
                else
                {
                    ApplyKnockbackServerRpc(-1, v, 1, dmg);
                }
            }
            else
            {
                ReflectKnockback(hit.normal);
            }
            StartCoroutine(BoneFishCoroutine());
        }
    }
    private IEnumerator BoneFishCoroutine()
    {
        yield return new WaitForSeconds(.25f);
        canBeBoneFished = true;
    }
    public void ReflectKnockback(Vector3 reflectNormal)
    {
        //Effects and Animation go here
        knockbackVelocity = Vector3.Reflect(knockbackVelocity, reflectNormal);
        knockbackVelocity.y = 0;
        knockbackVelocity *= 1.1f;
    }

    public void SetDoomed(bool isDoomed)
    {
        shaderManager?.SetShaderState(ShaderState.doomed, isDoomed);
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
   
        shaderManager?.SetShaderState(ShaderState.wet, isSlippery);  
 
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

        shaderManager?.SetShaderState(ShaderState.inked, isSlowed);  
        dashDisabledUI?.SetActive(isSlowed);
    }
    public void StartVulnerable(float time)
    {
        if (vulnerableRoutine == null)
        {
            vulnerableRoutine = StartCoroutine(VulnerableCoroutine(time));
            if (vulnerableEffect)
                vulnerableEffect.Play();
        }
        else if (time > vulnerableTimer)
        {
            vulnerableTimer = time;
        }
    }
    private IEnumerator VulnerableCoroutine(float duration)
    {
        vulnerableTimer = duration;
        isVulnerable = true;
        shaderManager?.SetShaderState(ShaderState.sauced, true);
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
        shaderManager?.SetShaderState(ShaderState.sauced, false);
        if (vulnerableEffect)
            vulnerableEffect.Stop();
    }
    public void Stun(float duration)
    {
        if (isStunned || duration <= 0)
            return;

        if (GameManager.Instance.PlayingLocal)
            StartCoroutine(StunCoroutine(duration));
        else
            StunServerRpc(duration);

    }

    [ServerRpc]
    private void StunServerRpc(float duration)
    {
        StunClientRpc(duration);
    }

    [ClientRpc]
    private void StunClientRpc(float duration)
    {
        NetcodeStunCoroutine(duration);
    }

    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        mainAnimator.SetBool("HitStun", true);
        controllerRumbler?.Rumble(duration, 1f, 5f);
        yield return new WaitForSeconds(duration);
        mainAnimator.SetBool("HitStun", false);
        isStunned = false;
    }

    private IEnumerator NetcodeStunCoroutine(float duration)
    {
        isStunned = true;
        GetComponent<NetworkAnimatorProxy>().SetAnimBool("HitStun", true);
        controllerRumbler?.Rumble(duration, 1f, 5f);
        yield return new WaitForSeconds(duration);
        GetComponent<NetworkAnimatorProxy>().SetAnimBool("HitStun", false);
        isStunned = false;
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

    public void ResetPlayerController()
    {
        damage = 0;
        damagedEffect.UpdateParticleSystem(-1);
        killCreditID = -1;
        currentUltCharge = 0;
        playerHUD.SetUltSlider(0);
        isUltCharged = false;
        slipperyCounter = 0;
        isSlippery = false;
        slowCounter = 0;
        isSlowed = false;
        dashDisabledUI?.SetActive(false);
        isVulnerable = false;
        if (vulnerableRoutine != null)
            StopCoroutine(vulnerableRoutine);
        canBeBoneFished = true;
        isStunned = false;

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

        movementInput = Vector2.zero;
        knockbackVelocity = Vector3.zero;
        controller.enabled = true;

        pickedUpSpellsAmount = 0; 
        usedSpell.Clear();
        isFirstGroundDetection = true;
        shotsHitInARowAmount = 0; 
        
        playerStateHandler.ResetPlayer();
        if (trail != null)
            trail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        isDead = false;
        StartCoroutine(EntranceCoroutine(0));
    }

    public void StartEntrence(float remainingDelay)
    {
        StartCoroutine(EntranceCoroutine(remainingDelay));
    }

    private IEnumerator EntranceCoroutine(float remainingDelay)
    {
        inputEnabled = false;
        canvas.SetActive(false);
        if (GameManager.Instance.PlayingLocal)
            mainAnimator.Play("Entrance", 0, 0);
        else
            PlayAnimServerRpc("Entrance", 0, 0);
        float animationTime = 1.06f; //Duration of entrance animation
        yield return new WaitForSeconds(0.4f); //Time when player hits the ground
        canvas.SetActive(true);
        yield return new WaitForSeconds(animationTime - 0.4f);
        if (remainingDelay > 0)
        {
            yield return new WaitForSeconds(remainingDelay - animationTime);
        }
        inputEnabled = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetHudServerRpc() => ResetHudClientRpc();

    [ClientRpc]
    private void ResetHudClientRpc() => playerHUD.ResetHUD();

    public void SetUpPlayer(int playerID, PlayerHUD playerHUD, ControllerRumbler controllerRumbler, SkinSO skinObject, bool dropInJoin)
    {
        currentSkinSO = skinObject;
        this.playerHUD = playerHUD;
        this.playerID = playerID;

        childAnimatorObject = Instantiate(skinObject.SkinPrefab, meshParent);
        
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
        {
            if(LobbyManager.instance && LobbyManager.instance.SelectedGameMode == GameManager.GameModeType.Team)
            {
                if (LobbyPlayerValues.Instance.playerValuesList[playerID].TeamIndex == 1)
                {
                    foreach (var element in coloredElements)
                        element.color = LobbyManager.instance.TeamColors[0];
                }
                else if (LobbyPlayerValues.Instance.playerValuesList[playerID].TeamIndex == 2)
                {
                    foreach (var element in coloredElements)
                        element.color = LobbyManager.instance.TeamColors[1];
                }
            }
            else 
            {
                foreach (var element in coloredElements)
                    element.color = skinObject.Color;
            }       

            ActivateCorrectColorServerRpc(skinObject.Index);

            mainAnimator = GetComponent<Animator>();
            SetupProxyAnimatorClientRpc();
        }
        else
        {
            if (LobbyManager.instance && LobbyManager.instance.SelectedGameMode == GameManager.GameModeType.Team)
            {
                if (LobbyPlayerValues.Instance.playerValuesList[playerID].TeamIndex == 1)
                {
                    foreach (var element in coloredElements)
                        element.color = LobbyManager.instance.TeamColors[0];

                    ScoreManager.Instance.teamModeScorePanels[0].SetTeamPortraits(skinObject.HeadSprites[0]);
                }
                else if (LobbyPlayerValues.Instance.playerValuesList[playerID].TeamIndex == 2)
                {
                    foreach (var element in coloredElements)
                        element.color = LobbyManager.instance.TeamColors[1];

                    ScoreManager.Instance.teamModeScorePanels[1].SetTeamPortraits(skinObject.HeadSprites[0]);
                }
            }
            else
            {
                foreach (var element in coloredElements)
                    element.color = skinObject.Color;
            }

            mainAnimator = childAnimatorObject.GetComponent<Animator>();
            shaderManager = childAnimatorObject.GetComponentInChildren<PlayerShaderManager>();
            shaderManager?.SetStatusIndicator(statusIndicator);
            
        }
        
        playerHUD.UpdateDamageText((int)damage);

        if (controllerRumbler != null)
        {
            this.controllerRumbler = controllerRumbler;
            isUsingGamepad = true;
        }
        playerStateHandler = GetComponent<PlayerStateHandler>();
        playerStateHandler.EnableDeath();
        if (dropInJoin)
        {
            StartCoroutine(EntranceCoroutine(0));
        }
        else
        {
            inputEnabled = false;
            canvas.SetActive(false);
        }
    }

    [ClientRpc]
    private void SetupProxyAnimatorClientRpc()
    {
        GetComponent<NetworkAnimatorProxy>().RegisterChildAnimator(childAnimatorObject.GetComponent<Animator>());
        shaderManager = childAnimatorObject.GetComponentInChildren<PlayerShaderManager>();
        shaderManager?.SetStatusIndicator(statusIndicator);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivateCorrectColorServerRpc(int index)
    {
        ActivateCorrectColorClientRpc(index);
    }
    
    [ClientRpc]
    private void ActivateCorrectColorClientRpc(int index)
    {
        SkinSO skinSOToUse = null; 
        
        foreach (LobbyPlayerValues.PlayerValues playerValues in LobbyPlayerValues.Instance.playerValuesList)
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

    private void IncrementRegainGroundAchievement(RaycastHit hit)
    {
        if (isFirstGroundDetection)
        {
            isFirstGroundDetection = false;
            return;
        }
        
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)playerID 
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5) return;
        
        AchievementSaveSystem achSaveSystem = AchievementSaveSystem.instance;
        achSaveSystem.IncrementStat(25, 1);
    }

    private void IncrementDodgeBubbleAchievement()
    {
        bubblesInside.RemoveWhere(b => b == null);
        Vector3 boxCenter = transform.position + boxCenterOffset;
        Collider[] hits = Physics.OverlapBox(boxCenter, boxHalfExtents, Quaternion.identity, bubbleLayer);

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            if (hit.TryGetComponent<BasicBubble>(out var bubble))
            {
                if (bubble.OwnerID.Value == playerID) continue;

                if (!bubblesInside.Contains(hit))
                    bubblesInside.Add(hit);
            }
        }

        var exiting = bubblesInside.Where(b => !hits.Contains(b)).ToList();

        foreach (var b in exiting)
        {
            bubblesInside.Remove(b);

            if (b == null) continue;

            if (!b.TryGetComponent<BasicBubble>(out var bubble)) continue;

            bool isLocalPlayer = NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == (ulong)playerID;
            if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && !isLocalPlayer || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) continue;

            if (bubble.HasPopped || !isSprinting) continue;
        
            if (AchievementSaveSystem.instance != null)
                AchievementSaveSystem.instance.IncrementStat(17, 1);
        }
    }

    #endregion

    #region RPC Animations

    [ServerRpc(RequireOwnership = false)]
    private void WalkingAnimServerRpc(Vector3 direction)
    {
        WalkingAnimClientRpc(direction);
    }

    [ClientRpc]
    private void WalkingAnimClientRpc(Vector3 direction)
    {
        GetComponent<NetworkAnimatorProxy>().SetAnimBool("IsWalking", direction.sqrMagnitude > 0.01f);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SlapAnimServerRpc(bool isFirstSpell)
    {
        SlapAnimClientRpc(isFirstSpell);
    }

    [ClientRpc]
    private void SlapAnimClientRpc(bool isFirstSpell)
    {
        if (IsOwner) return;
        GetComponent<NetworkAnimatorProxy>().SetAnimTrigger("SlapTrigger");
        SO_Spell spell = isFirstSpell ? firstSpell : secondSpell;
        if (spell != null)
            RuntimeManager.PlayOneShotAttached(spell.SpellVoiceEvent, gameObject);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DashAnimServerRpc()
    {
        DashAnimClientRpc();
    }

    [ClientRpc]
    private void DashAnimClientRpc()
    {
        if (IsOwner) return;
        GetComponent<NetworkAnimatorProxy>().SetAnimTrigger("Dash");
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeadAnimServerRpc(bool activationState)
    {
        DeadAnimClientRpc(activationState);
    }

    [ClientRpc]
    private void DeadAnimClientRpc(bool activationState)
    {
        GetComponent<NetworkAnimatorProxy>().SetAnimBool("IsDead", activationState);
    }

    [ServerRpc(RequireOwnership = false)]
    private void VictoryAnimServerRpc(bool activationState)
    {
        VictoryAnimClientRpc(activationState);
    }

    [ClientRpc]
    private void VictoryAnimClientRpc(bool activationState)
    {
        GetComponent<NetworkAnimatorProxy>().SetAnimBool("Victory", activationState);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayAnimServerRpc(string animName, int layer, float normalizedTime)
    {
        PlayAnimClientRpc(animName, layer, normalizedTime);
    }

    [ClientRpc]
    private void PlayAnimClientRpc(string animName, int layer, float normalizedTime)
    {
        GetComponent<NetworkAnimatorProxy>().SetAnimPlay(animName, layer, normalizedTime);
    }
    #endregion
}