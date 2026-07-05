using Unity.Netcode;
using UnityEngine;

public class NetworkAnimatorProxy : NetworkBehaviour
{
    public Animator parentAnimator;
    public Animator childAnimator;

    private void Awake()
    {
        parentAnimator = GetComponent<Animator>();
    }

    public void RegisterChildAnimator(Animator realChildAnimator)
    {
        childAnimator = realChildAnimator;
    }

    public void SetAnimFloat(string parameterName, float value)
    {
        if (parentAnimator != null) parentAnimator.SetFloat(parameterName, value);
        if (childAnimator != null) childAnimator.SetFloat(parameterName, value);
    }

    public void SetAnimInt(string parameterName, int value)
    {
        if (parentAnimator != null) parentAnimator.SetInteger(parameterName, value);
        if (childAnimator != null) childAnimator.SetInteger(parameterName, value);
    }

    public void SetAnimBool(string parameterName, bool value)
    {
        if (parentAnimator != null) parentAnimator.SetBool(parameterName, value);
        if (childAnimator != null) childAnimator.SetBool(parameterName, value);
    }

    public void SetAnimTrigger(string parameterName)
    {
        if (parentAnimator != null) parentAnimator.SetTrigger(parameterName);

        if ((IsLocalPlayer || IsServer) && childAnimator != null)
        {
            childAnimator.SetTrigger(parameterName);
        }
    }

    private void Update()
    {
        if (!IsOwner && childAnimator != null && parentAnimator != null)
        {
            var stateInfo = parentAnimator.GetCurrentAnimatorStateInfo(0);

            if (childAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash != stateInfo.fullPathHash)
            {
                childAnimator.Play(stateInfo.fullPathHash, 0, stateInfo.normalizedTime);
            }
        }
    }
}