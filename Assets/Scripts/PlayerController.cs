using FMODUnity;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Sound Events")]
    [SerializeField] private EventReference deathEvent;
    [SerializeField] private EventReference knockBackEvent;

    [Header("Spells")]
    [SerializeField] private SO_Spell baseSpell;
    [SerializeField] private SO_Spell firstSpell;
    [SerializeField] private SO_Spell secondSpell;
    [SerializeField] private GameObject spellSpawnEffect; 
    
    private bool isFirstSpellReady = true;
    private bool isSecondSpellReady = true;
    private Coroutine firstSpellCoroutine;
    private Coroutine secondSpellCoroutine;
    private Item itemToEquip;
    private bool inFirstCooldown = false; 
    private bool inSecondCooldown = false;
    private bool isSlippery = false;

    public GameObject firstCooldownSlider;
    public GameObject secondCooldownSlider;
    public TextMeshProUGUI damageText;
    
    private float damage = 0;
    [Header("Knockback Modifiers")]
    [SerializeField] float damageModifier = .05f;
    [SerializeField] float slipperyModifier = 1.5f;

    [Header("Player Stats")]
    #region Player Physics
    [SerializeField]
    private float playerSpeed = 2.0f;
    [SerializeField]
    private float gravityValue = -9.81f;
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
    [SerializeField]
    private float rotationSpeed = 10f; // Adjust for smoother rotation
    [SerializeField]
    private float moveSmoothTime = 0.1f; // Smoothing duration
    private Vector3 moveVelocity = Vector3.zero;
    #endregion

    #region Knockback
    [SerializeField]
    private float knockbackDecaySpeed = 5f; // Speed at which knockback decays
    private Vector3 knockbackVelocity = Vector3.zero; // Current knockback force
    #endregion
    
    #region Unity
    private void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();

        PlayerManager.Instance.OnPlayerWon += ResetOnNewGame;
    }
    
    private void Update()
    {
        if (inFirstCooldown && firstCooldownSlider != null)
            firstCooldownSlider.GetComponent<Slider>().value += Time.deltaTime;
        
        if (inSecondCooldown && secondCooldownSlider != null)
            secondCooldownSlider.GetComponent<Slider>().value += Time.deltaTime;
        
        if(damageText != null)
            damageText.text = damage.ToString();
        
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        // Handle input movement
        targetDirection = new Vector3(movementInput.x, 0, movementInput.y);
        targetDirection = Vector3.ClampMagnitude(targetDirection, 1f);

        // Smoothly interpolate movement direction
        smoothMoveDirection = Vector3.SmoothDamp(smoothMoveDirection, targetDirection, ref moveVelocity, moveSmoothTime);
        Vector3 move = smoothMoveDirection * (playerSpeed * Time.deltaTime);

        // Apply knockback if it exists
        if (knockbackVelocity.magnitude > 0.1f)
        {
            move += knockbackVelocity * Time.deltaTime; // Add knockback to movement
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecaySpeed * Time.deltaTime); // Decay knockback over time
        }

        if(controller.enabled) 
            controller.Move(move);

        // Smoothly rotate the player to face the movement direction
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // Gravity
        playerVelocity.y += gravityValue * Time.deltaTime;
        
        if(controller.enabled)
            controller.Move(playerVelocity * Time.deltaTime);
    }
    #endregion
    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }
    public void OnFirstSpell(InputAction.CallbackContext context)
    {
        if (isFirstSpellReady && context.performed)
        {
            Instantiate(spellSpawnEffect, transform.position, Quaternion.identity);
            float cooldown = firstSpell.CastSpell(transform.position, transform.forward);
            isFirstSpellReady = false;
            firstSpellCoroutine = StartCoroutine(SpellCooldown(cooldown, 1));
        }
    }
    public void OnSecondSpell(InputAction.CallbackContext context)
    {
        if (isSecondSpellReady && context.performed)
        {
            Instantiate(spellSpawnEffect, transform.position, Quaternion.identity);
            float cooldown = secondSpell.CastSpell(transform.position, transform.forward);
            isSecondSpellReady = false;
            secondSpellCoroutine = StartCoroutine(SpellCooldown(cooldown, 2));
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
    public void ApplyKnockback(Vector3 direction, float force, float dmg)
    {
        damage += dmg;
        if (isSlippery) force *= slipperyModifier;
        direction.y = 0; // Ignore vertical knockback (optional)
        knockbackVelocity += direction.normalized * (force * (1 + (damage * damageModifier)));
        RuntimeManager.PlayOneShotAttached(knockBackEvent, gameObject);
    }
    private IEnumerator SpellCooldown(float time, int spellID)
    {
        {
            if (spellID == 1)
            {
                inFirstCooldown = true;
                if (firstCooldownSlider != null)
                {
                    firstCooldownSlider.GetComponent<Slider>().maxValue = time;
                    firstCooldownSlider.GetComponent<Slider>().value = 0;
                }
            }
            else
            {
                inSecondCooldown = true;
                if (secondCooldownSlider != null)
                {
                    secondCooldownSlider.GetComponent<Slider>().maxValue = time;
                    secondCooldownSlider.GetComponent<Slider>().value = 0;
                }
            }

            yield return new WaitForSeconds(time);
            if (spellID == 1)
                inFirstCooldown = false;
            else
                inSecondCooldown = false; 

            ResetSpell(spellID);
        }
    }

    public void ResetOnNewGame()
    {
        firstSpell = baseSpell;
        ResetSpell(1);
        secondSpell = baseSpell;
        ResetSpell(2);
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
    private void EquipSpell(int spellID)
    {
        switch (spellID)
        {
            case 1:
                firstSpell = itemToEquip.EquipSpell();
                itemToEquip = null;
                break;
            case 2:
                secondSpell = itemToEquip.EquipSpell();
                itemToEquip= null;
                break;
            default:
                Debug.Log("Spell Equip Error");
                break;
        }
        ResetSpell(spellID);
    }
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
}