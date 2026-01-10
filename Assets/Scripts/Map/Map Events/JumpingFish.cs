using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class JumpingFish : MonoBehaviour
{
    private SplineAnimate splineAnimate;
    private bool isJumping = false;
    [SerializeField] private GameObject fish;
    [SerializeField] private ParticleSystem jumpVFX;
    void Start()
    {
        splineAnimate = fish.GetComponent<SplineAnimate>();
    }

    public void Jump(Transform startPos)
    {
        if (isJumping) return;
        transform.position = startPos.position;
        transform.rotation = startPos.rotation;
        jumpVFX.Play();
        StartCoroutine(JumpCoroutine());
    }
    private IEnumerator JumpCoroutine()
    {
        isJumping = true;
        fish.SetActive(true);
        splineAnimate.Restart(true);
        yield return new WaitForSeconds(splineAnimate.Duration);
        fish.SetActive(false);
        isJumping = false;
    }
}
