using Unity.Netcode;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class NetworkAnimatorProxy : NetworkBehaviour
{
    public Animator parentAnimator;
    public Animator childAnimator; // The real one spawned at runtime

    private void Awake()
    {
        parentAnimator = GetComponent<Animator>();
    }

    // Call this via your skin-spawning logic when the child is ready
    public void RegisterChildAnimator(Animator realChildAnimator)
    {
        childAnimator = realChildAnimator;
    }

    // Instead of calling mainAnimator.SetFloat(), call this custom method!
    public void SetAnimFloat(string parameterName, float value)
    {
        // 1. Update the parent so OwnerNetworkAnimator detects the change and syncs it
        if (parentAnimator != null) parentAnimator.SetFloat(parameterName, value);

        // 2. Update the local child skin so it actually plays the visual movement
        if (childAnimator != null) childAnimator.SetFloat(parameterName, value);
    }

    public void SetAnimInt(string parameterName, int value)
    {
        // 1. Update the parent so OwnerNetworkAnimator detects the change and syncs it
        if (parentAnimator != null) parentAnimator.SetInteger(parameterName, value);

        // 2. Update the local child skin so it actually plays the visual movement
        if (childAnimator != null) childAnimator.SetInteger(parameterName, value);
    }

    public void SetAnimTrigger(string parameterName)
    {
        // 1. Update the parent so OwnerNetworkAnimator detects the change and syncs it
        if (parentAnimator != null) parentAnimator.SetTrigger(parameterName);

        // 2. Update the local child skin so it actually plays the visual movement
        if (childAnimator != null) childAnimator.SetTrigger(parameterName);
    }

    public void SetAnimBool(string parameterName, bool value)
    {
        // 1. Update the parent so OwnerNetworkAnimator detects the change and syncs it
        if (parentAnimator != null) parentAnimator.SetBool(parameterName, value);

        // 2. Update the local child skin so it actually plays the visual movement
        if (childAnimator != null) childAnimator.SetBool(parameterName, value);
    }

    private void Update()
    {
        // If your transitions rely on crossfades or trigger states, 
        // you can also map the current state of the parent to the child here:
        if (childAnimator != null && parentAnimator != null)
        {
            // Syncs the actual state playing on the network dummy down to the child visual
            var stateInfo = parentAnimator.GetCurrentAnimatorStateInfo(0);
            childAnimator.Play(stateInfo.fullPathHash, 0, stateInfo.normalizedTime);
        }
    }
}