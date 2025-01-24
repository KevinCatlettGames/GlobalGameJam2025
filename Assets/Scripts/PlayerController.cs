using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
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
    
    private void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    private void Update()
    {
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

        // Move the player
        controller.Move(move);

        // Smoothly rotate the player to face the movement direction
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // Gravity
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }
    
    public void ApplyKnockback(Vector3 direction, float force)
    {
        direction.y = 0; // Ignore vertical knockback (optional)
        knockbackVelocity = direction.normalized * force;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            Vector3 knockbackDirection = transform.position - other.transform.position;
            ApplyKnockback(knockbackDirection, 10f); // Adjust the force value as needed
        }
    }
}