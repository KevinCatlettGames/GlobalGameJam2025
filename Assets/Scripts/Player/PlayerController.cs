using FMODUnity;
using UnityEngine.Events;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Sound Events")]
    [SerializeField] private EventReference knockBackEvent;
    [SerializeField] private EventReference deathEvent;

    [Header("Spells")]
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
    [SerializeField] private float knockbackDecaySpeed = 5f; 
    [SerializeField] float rumbleDurationFactor = .01f;
    
    private ControllerRumbler controllerRumbler = null;
    private bool isUsingGamepad = false;
    private float mouseInputDeadzoneRadius = .4f;
    private float mouseInputVectorLimit = 5f;

    [Header("Effects")]
    [SerializeField] private GameObject dashStartEffect;
    [SerializeField] private GameObject spellSpawnEffect;
    [SerializeField] private ParticleSystem damageParticleSystem;
    [SerializeField] private float damageColorEffectDuration = .1f;

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
    
    #region Unity
    private void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
    }
    private void Update()
    {
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
            if(killCreditID != -1 && controller.isGrounded)
            {
                killCreditID = -1;
            }
        }

        if (!isDead) move += playerVelocity * Time.deltaTime;
        if (controller.enabled) controller.Move(move);


        if (!isDead && targetDirection != Vector3.zero)
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
            if (inputMagnitude > mouseInputVectorLimit)
            {
                movementInput *= mouseInputVectorLimit / inputMagnitude;
            }
        }
    }
    public void OnFirstSpell(InputAction.CallbackContext context)
    {
        if (context.performed && !isDead)
        {
            if (!isFirstSpellReady)
            {
                controllerRumbler?.Rumble(.15f, 1f, 5f);
                return;
            }
            mainAnimator.SetTrigger("SlapTrigger");
            Instantiate(spellSpawnEffect, transform.position, Quaternion.identity);
            float cooldown = firstSpell.CastSpell(playerID, transform.position, transform.forward, controller);
            isFirstSpellReady = false;
            firstSpellCoroutine = StartCoroutine(SpellCooldown(cooldown, 1));
            RuntimeManager.PlayOneShotAttached(firstSpell.GetSpellEventStruct(),gameObject);
        }
    }
    public void OnSecondSpell(InputAction.CallbackContext context)
    {
        if (context.performed && !isDead)
        {
            if (!isSecondSpellReady)
            {
                controllerRumbler?.Rumble(.15f, 1f, 5f);
                return;
            }
            mainAnimator.SetTrigger("SlapTrigger");
            Instantiate(spellSpawnEffect, transform.position, Quaternion.identity);
            float cooldown = secondSpell.CastSpell(playerID, transform.position, transform.forward, controller);
            isSecondSpellReady = false;
            secondSpellCoroutine = StartCoroutine(SpellCooldown(cooldown, 2));
            RuntimeManager.PlayOneShotAttached(secondSpell.GetSpellEventStruct(), gameObject);
        }
    }
    public void OnFistSpellEquip(InputAction.CallbackContext context)
    {
        if (itemToEquip != null && context.performed)
        {
            EquipSpell(1);
        }
    }
    public void OnSecondSpellEquip(InputAction.CallbackContext context)
    {
        if (itemToEquip != null && context.performed)
        {
            EquipSpell(2);
        }
    }
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!canSprint)
            {
                controllerRumbler?.Rumble(.15f, 1f, 5f);
                return;
            }
            StartCoroutine(SprintCoroutine());
            Instantiate(dashStartEffect, transform.position, transform.rotation);
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
        OnBeginSprint?.Invoke();
        yield return new WaitForSeconds(playerSprintDuration);
        playerSpeed = moveSpeed;
        moveSmoothTime = smoothTime;
        isSprinting = false;
        OnEndSprint?.Invoke();
        yield return new WaitForSeconds(sprintCooldown);
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
        }
        else if (!isInRange && item == itemToEquip)
        {
            itemToEquip = null;
        }
    }
    private void EquipSpell(int spellID)
    {
        switch (spellID)
        {
            case 1:
                firstSpell = itemToEquip.EquipSpell();
                itemToEquip = null;
                playerHUD.SetSpell(1, firstSpell.SpellIcon);
                break;
            case 2:
                secondSpell = itemToEquip.EquipSpell();
                itemToEquip = null;
                playerHUD.SetSpell(2, secondSpell.SpellIcon);
                break;
            default:
                Debug.Log("Spell Equip Error");
                break;
        }
        ResetSpell(spellID);
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
        GameManager.Instance.DeathReport(playerID, killCreditID);  
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
        }
        else
        {
            isSlippery = false;
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