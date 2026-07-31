using UnityEngine;

public class DummyController : PlayerController
{
    [Header("Dummy")]
    [SerializeField] private Vector3 resetPosition;
    [SerializeField] private float minResetDistance = 1f;
    [SerializeField] private SkinSO skin;
    [SerializeField] private PlayerHUD dummyPlayerHUD;
    [SerializeField] private float resetDelay = 3f;
    private float moveTimer = 0;

    private void Start()
    {
        SetUpPlayer(1, dummyPlayerHUD, null, skin);
        controller = GetComponent<CharacterController>();
        SetSpells(0, 0);
        isUsingGamepad = true;
        base.Start();
        initialized = true;
        dummyPlayerHUD.gameObject.SetActive(true);
        dummyPlayerHUD.InitialisePlayerHUD(skin);
    }

    private void Update()
    {
        if (knockbackVelocity.sqrMagnitude > .5f)
        {
            moveTimer = resetDelay;
        }
        else if(moveTimer > 0) 
        {
            moveTimer -= Time.deltaTime;
        }
        if (Vector3.Distance(resetPosition, transform.position) > minResetDistance && moveTimer <= 0)
        {
            Vector3 v = resetPosition - transform.position;
            v.y = 0f;
            v.Normalize();
            movementInput = new Vector2(v.x, v.z);
        }
        else
        {
            movementInput = Vector2.zero;
        }
        base.Update();
    }

}
