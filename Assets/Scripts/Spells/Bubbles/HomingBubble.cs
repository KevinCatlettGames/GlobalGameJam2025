using UnityEngine;

public class HomingBubble : BasicBubble
{
    private HomingTargeting homingTargeting;
    [Header("Homing")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float homingRadius = 5f;
    float timeAlive = 0;
    bool achUnlocked = false;
    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider, int assignedSpellID, bool fakeWithServerCaster)
    {
        base.InitialiseBubble(ID, dir, playerCollider, assignedSpellID, fakeWithServerCaster);

        homingTargeting = GetComponentInChildren<HomingTargeting>();
        if (homingTargeting != null)
        {
            homingTargeting.SetTargeting(homingRadius / size, playerCollider, ID);
        }
        else
        {
            Debug.LogWarning("HomingTargeting component not found on HomingBubble.");
        }
    }

    private void Update()
    {
        if (achUnlocked) return;
        if (timeAlive <= 2.4f)
        {
            timeAlive += Time.deltaTime;
        }
        else if (timeAlive >= 2.4f)
        {
            achUnlocked = true;
            if (AchievementSaveSystem.instance != null)
                AchievementSaveSystem.instance.UnlockAchievement(7);
        } 
    }

    protected override void BubbleMovement()
    {
        if (!hasInflated)
        {
            base.BubbleMovement();
            return;
        }

        Vector3 targetVector = homingTargeting.GetTargetVector();

        if (targetVector != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetVector);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            direction = transform.forward;
        }

        base.BubbleMovement();
    }
}