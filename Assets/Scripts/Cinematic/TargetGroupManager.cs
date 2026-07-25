using Cinemachine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TargetGroupManager : MonoBehaviour
{

    public static TargetGroupManager Instance;
    private List<Transform> targetGroup = new List<Transform> ();

    [SerializeField] private float zoomSpeed = .3f;
    [Header("Zoom Sizes")]
    [SerializeField] private float maxSize = 21f;
    [SerializeField] private float minSize = 16f;
    [SerializeField] private float defaultSize = 19.75f;


    [Header("Zoom Thresholds")]
    [SerializeField] private float zoomInThreshold = 5f;
    [SerializeField] private float zoomOutThreshold = 15f;

    [SerializeField] private float minBound = 2.5f;
    [SerializeField] private float maxBound = 20f;

    [SerializeField] private CinemachineVirtualCamera virtualCamera;

    private float targetSize = 0f;
    private float minStep = 0f;
    private float maxStep = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }


        minStep = (defaultSize - minSize) / (zoomInThreshold - minBound);
        maxStep = (maxSize - defaultSize) / (maxBound - zoomOutThreshold);
    }

    private void Start()
    {
        GameManager.Instance.OnGameStarted += ResetZoom;
    }

    private void LateUpdate()
    {
        float boundsWidth = GetBoundsWidth();
        if (boundsWidth == -1)
        {
            targetSize = defaultSize;
        }
        else if (boundsWidth > zoomOutThreshold)
        {
            if (boundsWidth > maxBound)
                boundsWidth = maxBound;
            targetSize = defaultSize + (boundsWidth - zoomOutThreshold) * maxStep;
        }
        else if (boundsWidth < zoomInThreshold)
        {
            if (boundsWidth < minBound)
                boundsWidth = minBound;
            targetSize = minSize + (boundsWidth - minBound) * minStep;
        }
        else
        {
           targetSize = defaultSize;
        }

        virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(virtualCamera.m_Lens.OrthographicSize, targetSize, Time.deltaTime * zoomSpeed);
    }

    public void ResetZoom()
    {
        virtualCamera.m_Lens.OrthographicSize = defaultSize;
        targetSize = defaultSize;
    }

    private float GetBoundsWidth()
    {
        if (targetGroup.Count <= 1)
            return -1;
        Bounds bounds = new Bounds();
        foreach (Transform t in targetGroup)
        {
            bounds.Encapsulate(t.transform.position);
        }
        return bounds.extents.x;
    }

    public void AddToGroup(Transform t)
    {
        if (!targetGroup.Contains(t))
            targetGroup.Add(t);
    }

    public void RemoveFromGroup(Transform t)
    {
        if (targetGroup.Contains(t))
            targetGroup.Remove(t);
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGameStarted -= ResetZoom;
    }
}
