using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class JumpingFish : MonoBehaviour
{
    private SplineAnimate splineAnimate;
    private bool isJumping = false;
    [SerializeField] private GameObject fish;
    [SerializeField] private ParticleSystem jumpVFX;
    [SerializeField] private ParticleSystem announceVFX;
    [SerializeField] private ParticleSystem dipVFX;
    [SerializeField] private float vfxDelay = .5f;
    void Start()
    {
        splineAnimate = fish.GetComponent<SplineAnimate>();
    }

    public void Jump(Transform startPos)
    {
        if (isJumping) return;
        transform.position = startPos.position;
        transform.rotation = startPos.rotation;
        announceVFX.Play();
        StartCoroutine(JumpCoroutine());
    }
    private IEnumerator JumpCoroutine()
    {
        yield return new WaitForSeconds(vfxDelay);
        jumpVFX.Play();
        isJumping = true;
        fish.SetActive(true);
        splineAnimate.Restart(true);
        yield return new WaitForSeconds(splineAnimate.Duration - .2f);
        dipVFX?.Play();
        yield return new WaitForSeconds(.2f);
        fish.SetActive(false);
        isJumping = false;
    }
}
