using System.Collections;
using UnityEngine;

public class IceFloor : MonoBehaviour
{
    [SerializeField] private float riseDuration = 1;
    [SerializeField] private float sinkDuration = 5;
    [SerializeField] private float sinkDistance = 5;
    [SerializeField] private float stayTime = 3;
    // Animator
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material crackedMaterial;
    private Material material;
    bool isCracked = false;
    bool isActive = true;
    Vector3 start;
    Vector3 end;

    private void Start()
    {
        start = transform.position;
        end = start;
        end.y -= sinkDistance;
        material = meshRenderer.material;
    }
    public void Crack()
    {
        if (!isActive)
            return;
        if (!isCracked)
        {
            isCracked = true;
            meshRenderer.material = crackedMaterial;
        }
        else
        {
            Sink();
        }
    }
    private void Sink()
    {
        if (!isActive)
            return;
        isActive = false;
        StartCoroutine(SinkCoroutine());
    }
    private IEnumerator SinkCoroutine()
    {
        float rate = 1 / sinkDuration;
        float progress = 0;
        while (progress < 1)
        {
            progress += rate * Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, progress);
            yield return null;
        }
        yield return new WaitForSeconds(stayTime);
        isCracked = false;
        rate = 1 / riseDuration;
        progress = 0;
        meshRenderer.material = material;
        while (progress < 1)
        {
            progress += rate * Time.deltaTime;
            transform.position = Vector3.Lerp(end, start, progress);
            yield return null;
        }
        yield return new WaitForSeconds(.1f);
        isActive = true;
    }
    public void ResetFloorPiece()
    {
        StopAllCoroutines();
        isActive = true;
        isCracked = false;
        transform.position = start;
        meshRenderer.material = material;
    }
}
