using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class JumpingFish : MonoBehaviour
{
    private SplineAnimate splineAnimate;
    private bool isJumping = false;
    void Start()
    {
        splineAnimate = GetComponent<SplineAnimate>();
    }

    public void Jump(Transform startPos)
    {
        transform.position = startPos.position;
        transform.rotation = startPos.rotation;
    }
   // private IEnumerator JumpCoroutine()
   // {
   //
   // }
}
