using UnityEngine;

public class DummyController : PlayerController
{
    [Header("Dummy")]
    [SerializeField] private Vector3[] resetPositions;
    [SerializeField] private float minResetDistance = 1f;
    [SerializeField] private SkinSO skin;
    [SerializeField] private PlayerHUD dummyPlayerHUD;
    [SerializeField] private float resetDelay = 3f;
    private float moveTimer = 0;
    private int positionIndex = 0;

    private void Start()
    {
        SetUpPlayer(5, dummyPlayerHUD, null, skin, true);
        controller = GetComponent<CharacterController>();
        SetSpells(0, 0);
        isUsingGamepad = true;
        base.Start();
        initialized = true;
        dummyPlayerHUD.gameObject.SetActive(true);
        dummyPlayerHUD.InitialisePlayerHUD(skin);
        TargetGroupManager.Instance.AddToGroup(transform);
    }

    private void Update()
    {
        if (resetPositions.Length > 1)
            PatrolMovement();
        else
            StationaryMovement();

        base.Update();
    }

    private void PatrolMovement()
    {
        if (Vector3.Distance(resetPositions[positionIndex], transform.position) < minResetDistance)
        {
            if (moveTimer > 0)
            {
                moveTimer -= Time.deltaTime;
            }
            else if (moveTimer <= 0)
            {
                positionIndex++;
                if (positionIndex >= resetPositions.Length)
                    positionIndex = 0;
                moveTimer = resetDelay;
            }
        }

        if (Vector3.Distance(resetPositions[positionIndex], transform.position) > minResetDistance)
        {
            Vector3 v = resetPositions[positionIndex] - transform.position;
            v.y = 0f;
            v.Normalize();
            movementInput = new Vector2(v.x, v.z);
        }
        else
        {
            movementInput = Vector2.zero;
        }
    }

    private void StationaryMovement()
    {
        if (knockbackVelocity.sqrMagnitude > .5f)
        {
            moveTimer = resetDelay;
        }
        else if (moveTimer > 0)
        {
            moveTimer -= Time.deltaTime;
        }

        if (Vector3.Distance(resetPositions[positionIndex], transform.position) > minResetDistance && moveTimer <= 0)
        {
            Vector3 v = resetPositions[positionIndex] - transform.position;
            v.y = 0f;
            v.Normalize();
            movementInput = new Vector2(v.x, v.z);
        }
        else
        {
            movementInput = Vector2.zero;
        }
    }
}
