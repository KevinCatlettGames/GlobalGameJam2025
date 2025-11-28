using UnityEngine;

public class Crab : MonoBehaviour
{
    [SerializeField] private Transform[] eyes = new Transform[2];
    [SerializeField] private CrabClaw[] claws = new CrabClaw[2];
    [SerializeField] private Transform crabBody;
    [SerializeField] private float eyeRotationSpeed = 2;
    [SerializeField] private float crabRotationSpeed = 1;
    private int activeClawIndex = -1;

    private void FixedUpdate()
    {
        int activeClaws = 0;
        activeClawIndex = -1;
        for (int q = 0; q < 2; q++)
        {
            if (claws[q].Status == CrabClawStatus.hunting)
            {
                activeClaws++;
                activeClawIndex = q;
            }
        }

        switch (activeClaws)
        {
            default:
                RotateCrab(-1);
                break;
            case 1:
                RotateCrab(activeClawIndex);
                //RotateEye(0, activeClawIndex);
                //RotateEye(1, activeClawIndex);
                break;
            case 2:
                RotateCrab();
                //RotateEye(0, 0);
                //RotateEye(1, 1);
                break;
        }
    }

    private void RotateCrab(int clawIndex)
    {
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(0, 0, -1), Vector3.up);
        if (clawIndex != -1)
        {
            Vector3 lookVector = claws[clawIndex].transform.position - crabBody.position;
            lookVector.y = 0;
            targetRotation = Quaternion.LookRotation(lookVector, Vector3.up);
        }
        crabBody.rotation = Quaternion.Lerp(crabBody.rotation, targetRotation, Time.fixedDeltaTime * crabRotationSpeed);
    }
    private void RotateCrab()
    {
        Vector3 lookVector = claws[0].transform.position - claws[1].transform.position;
        lookVector.y = 0;
        lookVector *= 0.5f;
        lookVector += claws[1].transform.position;
        lookVector -= crabBody.position;
        lookVector.y = 0;
        Debug.DrawRay(transform.position, lookVector, Color.blue);
        Quaternion targetRotation = Quaternion.LookRotation(lookVector, Vector3.up);
        crabBody.rotation = Quaternion.Lerp(crabBody.rotation, targetRotation, Time.fixedDeltaTime * crabRotationSpeed);
    }
    private void RotateEye(int eyeIndex, int clawIndex)
    {
        Vector3 lookVector = claws[clawIndex].transform.position - eyes[eyeIndex].position;
        lookVector.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(lookVector, Vector3.up);
        eyes[eyeIndex].rotation = Quaternion.Lerp(eyes[eyeIndex].rotation, targetRotation, Time.fixedDeltaTime * eyeRotationSpeed);
    }
}
