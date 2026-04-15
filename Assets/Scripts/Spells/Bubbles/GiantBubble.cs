using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GiantBubble : BasicBubble
{
    [Header("Big Version")]
    [SerializeField] private float extraOffset = 2f;
    [SerializeField] private int knbDecreaseAngle = 45;
    [SerializeField] private float knbDecreaseIncrement = .25f;
    [Header("Small Version")]
    [SerializeField] private float dmgMod = 3;
    [SerializeField] private float knbMod = .3f;
    [SerializeField] private float sizMod = .25f;
    [SerializeField] private float speedMod = 5f;
    [SerializeField] private GameObject smallHitEffect;

    private bool isSmall = false;

    public override void InitialiseBubble(int ID, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dir, soundEvent, playerCollider);
        transform.position += direction * extraOffset;
    }
    protected override void BubbleMovement()
    {
        if (!IsServer) return;
        if (!hasInflated) return;
        base.BubbleMovement();
    }
    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;
        if (!isSmall && other.CompareTag("Bubble"))
        {
            isSmall = true;
            damage *= dmgMod;
            knockback *= knbMod;
            size *= sizMod;
            speed *= speedMod;
            transform.localScale = Vector3.one * size;
            hitEffect = smallHitEffect;
            spellType = SpellType.SmallerGiant;
            return;
        }
        if (!isSmall && other.CompareTag("Player"))
        {
            Vector3 v = other.transform.position - transform.position;
            float angle = Vector3.Angle(v, transform.forward);
            if (angle <= 5f)
            {
                knockback *= 1.2f;
            }
            else
            {
                int i = (int)angle / knbDecreaseAngle;
                knockback *= 1 - (knbDecreaseIncrement * i);
            }
        }
        
        base.BubbleCollision(other);
    }
}