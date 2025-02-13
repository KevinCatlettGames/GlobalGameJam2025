using FMODUnity;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Sound Events")]
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

    public GameObject firstCoolDownCover;
    public GameObject secondCoolDownCover;
    public Image firstCoolDownImage;
    public Image secondCoolDownImage;
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

    public float firstCooldownDuration;
    public float secondCooldownDuration;
    private float firstCurrentCooldownTimer;
    private float secondCurrentCooldownTimer;

    public Animator mainAnimator;
    public GameObject[] characters; 
    
    #region Unity
    private void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();

        PlayerManager.Instance.OnPlayerWon += ResetOnNewGame;
    }

    private void UpdateCooldown(Image imageToUse, ref float currentCooldownTimer, float cooldownDuration)
    {
        /*Debug.Log("Updating Cooldown");*/
        if (currentCooldownTimer < cooldownDuration)
        {
            currentCooldownTimer += Time.deltaTime;

            // Normalize the value (map it to a 0-1 range)
            float normalizedValue = currentCooldownTimer / cooldownDuration;

            // Update the fill amount
            imageToUse.fillAmount = 1 - normalizedValue;
        }
    }

    private void Update()
    {
        if (inFirstCooldown && firstCoolDownCover != null)
        {
            UpdateCooldown(firstCoolDownCover.GetComponent<Image>(), ref firstCurrentCooldownTimer, firstCooldownDuration);
        }

        if (inSecondCooldown && secondCoolDownCover != null)
        {
            UpdateCooldown(secondCoolDownCover.GetComponent<Image>(), ref secondCurrentCooldownTimer, secondCooldownDuration);
        }

        if (damageText != null)
            damageText.text = damage.ToString();

        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        // Handle input movement
        targetDirection = new Vector3(movementInput.x, 0, movementInput.y);
        targetDirection = Vector3.ClampMagnitude(targetDirection, 1f);
        if (targetDirection.sqrMagnitude > 0)
        {
            mainAnimator.SetBool("IsWalking", true);
        }
        else
        {
            mainAnimator.SetBool("IsWalking", false);
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
    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }
    public void OnFirstSpell(InputAction.CallbackContext context)
    {
        if (isFirstSpellReady && context.performed)
        {
            mainAnimator.SetTrigger("SlapTrigger");
            Instantiate(spellSpawnEffect, transform.position, Quaternion.identity);
            float cooldown = firstSpell.CastSpell(transform.position, transform.forward);
            isFirstSpellReady = false;
            firstSpellCoroutine = StartCoroutine(SpellCooldown(cooldown, 1));
            RuntimeManager.PlayOneShotAttached(firstSpell.GetSpellEventStruct(),gameObject);
        }
    }
    public void OnSecondSpell(InputAction.CallbackContext context)
    {
        if (isSecondSpellReady && context.performed)
        {
            mainAnimator.SetTrigger("SlapTrigger");
            Instantiate(spellSpawnEffect, transform.position, Quaternion.identity);
            float cooldown = secondSpell.CastSpell(transform.position, transform.forward);
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
                Debug.Log("Starting first spell");
                inFirstCooldown = true;
                if (firstCoolDownCover != null)
                {
                    firstCooldownDuration = time;
                    firstCoolDownCover.GetComponent<Image>().fillAmount = 0;
                }
            }
            else
            {
                Debug.Log("Starting second spell");
                inSecondCooldown = true;
                if (secondCoolDownCover != null)
                {
                    secondCooldownDuration = time;
                    secondCoolDownCover.GetComponent<Image>().fillAmount = 0;
                }
            }

            yield return new WaitForSeconds(time);
            if (spellID == 1)
            {
                firstCurrentCooldownTimer = 0;
                inFirstCooldown = false;
            }
            else
            {
                secondCurrentCooldownTimer = 0;
                inSecondCooldown = false;
            }

            ResetSpell(spellID);
        }
    }

    public void ResetOnNewGame()
    {
        if (mainAnimator.gameObject.activeSelf)
        {
            mainAnimator.SetTrigger("VictoryTrigger"); // Trigger the victory animation
        }
        damage = 0;
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
                inFirstCooldown = false; 
                firstCoolDownImage.GetComponent<Image>().sprite = firstSpell.SpellIcon;
                firstCoolDownCover.GetComponent<Image>().fillAmount = 0;
                itemToEquip = null;
                break;
            case 2:
                secondSpell = itemToEquip.EquipSpell();
                inSecondCooldown = false; 
                secondCoolDownImage.GetComponent<Image>().sprite = secondSpell.SpellIcon;
                secondCoolDownCover.GetComponent<Image>().fillAmount = 0;
                itemToEquip = null;
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