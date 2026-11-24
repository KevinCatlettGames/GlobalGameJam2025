using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private static int maxSunkenFloors = 3;
    private static List<IceFloor> sunkenFloors = new List<IceFloor>();
    private Coroutine sinkCoroutine;
    private Material material;
    bool isCracked = false;
    bool isActive = false;
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
        sinkCoroutine = StartCoroutine(SinkCoroutine());
        if (sunkenFloors.Count >= maxSunkenFloors)
        {
            sunkenFloors[0].Rise();
        }
        sunkenFloors.Add(this);
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
        Rise();
    }
    public void Rise()
    {
        StopCoroutine(sinkCoroutine);
        sunkenFloors.Remove(this);
        StartCoroutine(RiseCoroutine());
    }
    private IEnumerator RiseCoroutine()
    {
        isCracked = false;
        float rate = 1 / riseDuration;
        float progress = 0;
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
        isCracked = false;
        isActive = false;
        transform.position = start;
        meshRenderer.material = material;
    }
    public static void ClearSunkenList()
    {
        sunkenFloors.Clear();
    }
    public void ActivateIcePice()
    {
        isActive = true;
    }
}
