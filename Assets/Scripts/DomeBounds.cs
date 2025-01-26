using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DomeBounds : MonoBehaviour
{
    public Animator deadAnimation;

    private void Start()
    {
        deadAnimation = GetComponent<Animator>();
    }
    private void OnTriggerExit(Collider other)
    {
        SlipBubble slipBubble;
        if (other.TryGetComponent<SlipBubble>(out slipBubble))
        {
            slipBubble.BubbleCollision(this.gameObject);
            return;
        }
        if (other.CompareTag("Player"))
        {
            // Get all Animator components in the player's children
            Animator[] childAnimators = other.GetComponentsInChildren<Animator>();

            foreach (Animator animator in childAnimators)
            {
                // Trigger the death animation for each animator
                animator.SetBool("IsDead", true);
            }
        }

    }
}
