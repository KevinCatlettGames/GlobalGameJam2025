using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GiantBubble : BasicBubble
{
    [SerializeField] private float extraOffset = 2f;
    [SerializeField] private float dmgMod = 3;
    [SerializeField] private float knbMod = .3f;
    [SerializeField] private float sizMod = .25f;
    [SerializeField] private float speedMod = 5f;
    [SerializeField] private Material angryMaterial;

    private bool isAngry = false;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);
        transform.position += direction * extraOffset;
    }
    protected override void BubbleMovement()
    {
        if (!sphereCollider.enabled) return;
        base.BubbleMovement();
    }
    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;
        if (!isAngry && other.CompareTag("Bubble"))
        {
            isAngry = true;
            damage *= dmgMod;
            knockback *= knbMod;
            size *= sizMod;
            speed *= speedMod;
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            List<Material> materials = new List<Material>();
            meshRenderer.GetMaterials(materials);
            materials[1] = angryMaterial;
            meshRenderer.SetMaterials(materials);
            transform.localScale = Vector3.one * size;
            return;
        }
        base.BubbleCollision(other);
    }
}