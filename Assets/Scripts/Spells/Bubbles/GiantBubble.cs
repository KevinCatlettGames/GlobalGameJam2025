using FMODUnity;
using System.Collections;
using UnityEngine;

public class GiantBubble : BasicBubble
{
    private int stages = 0;
    [SerializeField] private float idleTimer = .5f;
    [SerializeField] private float[] damageStages;
    [SerializeField] private float[] knockbackStages;
    [SerializeField] private float[] speedStages;
    [SerializeField] private float[] sizeStages;
    private float timer = 0f;
    private int currentStage = -1;
    private bool hasShield = true;
    private bool isGrowing = false;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);
        stages = damageStages.Length;
        timer = idleTimer;
        size = sizeStages[0];
    }
    private void Update()
    {
        if (!IsServer) return;

        if (sphereCollider.enabled && currentStage < stages && hasShield && !isGrowing)
        {
            timer += Time.deltaTime;
            if (timer >= idleTimer)
            {
                timer = 0f;
                currentStage++;
                StartCoroutine(SetStage());
            }
        }

    }
    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || !IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                if (GameManager.Instance.playingLocal)
                    player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
                else
                    player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);
            }
            Pop();
        }
        else if (other.CompareTag("Bubble"))
        {
            DamageBubble();
        }
        else
        {
            Pop();
        }
    }

    private void DamageBubble()
    {
        if(hasShield)
        {
            hasShield = false;
            return;
        }
        Pop();
    }
    private IEnumerator SetStage()
    {
        size = sizeStages[currentStage];
        isGrowing = true;
        while (currentSize < size)
        {
            currentSize += inflationSpeed * Time.deltaTime;
            if (currentSize > size) currentSize = size;

            transform.localScale = Vector3.one * currentSize;
            yield return null;
        }

        damage = damageStages[currentStage];
        knockback = knockbackStages[currentStage];
        speed = speedStages[currentStage];
        isGrowing = false;
    }
}