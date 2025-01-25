using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodingBubble : BasicBubble
{
    private List<PlayerController> effectedPlayers = new List<PlayerController>();
    private List<BasicBubble> effectedBubbles = new List<BasicBubble>();

    protected override void Pop()
    {
        StopCoroutine(rangeCoroutine);
        Explode();
        base.Pop();
    }
    private void Explode()
    {
        if (effectedPlayers.Count > 0)
        {
            foreach (PlayerController player in effectedPlayers)
            {
                if (player != null) 
                { 
                    Vector3 kockbackDirection = player.transform.position - transform.position;
                    player.ApplyKnockback(kockbackDirection, knockback, damage);
                }
            }
        }
        if (effectedBubbles.Count > 0)
        {
            foreach (BasicBubble bubble in effectedBubbles)
            {
                if (bubble != null)
                {
                    bubble.BubbleCollision(this.gameObject);
                }
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            effectedPlayers.Add(other.GetComponent<PlayerController>());
        }
        else if (other.CompareTag("Bubble"))
        {
            effectedBubbles.Add(other.GetComponent<BasicBubble>());
        }
    }
    private void OnTriggerExit(Collider other) 
    {
        if (other.CompareTag("Player"))
        {
            effectedPlayers.Remove(other.GetComponent<PlayerController>());
        }
        else if (other.CompareTag("Bubble"))
        {
            effectedBubbles.Remove(other.GetComponent<BasicBubble>());
        }
    }

}
