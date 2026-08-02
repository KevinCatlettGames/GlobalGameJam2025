using Cinemachine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TargetGroupManager : MonoBehaviour
{

    public static TargetGroupManager Instance;
    private List<Transform> targetGroup = new List<Transform> ();

    [SerializeField] private CinemachineVirtualCamera virtualCamera;

    [Header("Zoom Sizes")] [SerializeField]
    private bool isZooming = true;
    [SerializeField] private float zoomSpeed = .3f;
    [SerializeField] private float maxSize = 21f;
    [SerializeField] private float minSize = 16f;
    [SerializeField] private float defaultSize = 19.75f;

    [Header("Zoom Thresholds")]
    [SerializeField] private float zoomInThreshold = 5f;
    [SerializeField] private float zoomOutThreshold = 15f;
    [SerializeField] private float minBound = 2.5f;
    [SerializeField] private float maxBound = 20f;

    [Header("Pan")]
    [SerializeField] private bool isPanning = true;
    [SerializeField] private Vector3 defaultPosition;
    [SerializeField] private float panSpeed = 1f;
    [SerializeField] private float maxPanDistance = 5f;
    [SerializeField] private float panThreshold = 2f;
    [SerializeField] private float panStrengh = .5f;

    private Vector3 targetPosition = Vector3.zero;
    private float targetSize = 0f;
    private float minStep = 0f;
    private float maxStep = 0f;
    private Vector3 velocity = Vector3.zero;

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

        defaultPosition = transform.position;
        minStep = (defaultSize - minSize) / (zoomInThreshold - minBound);
        maxStep = (maxSize - defaultSize) / (maxBound - zoomOutThreshold);
    }

    private void Start()
    {
        //GameManager.Instance.OnGameStarted += ResetZoom;
    }

    private void LateUpdate()
    {
        Bounds bounds = GetBounds();
        if (isZooming)
            Zoom(bounds);
        if (isPanning)
            Pan(bounds);
    }
    private void Pan(Bounds bounds)
    {
        Vector3 followPosition = bounds.center;
        followPosition.y = 0f;
        if (followPosition.magnitude > panThreshold)
        {
            followPosition *= panStrengh;
            Vector3.ClampMagnitude(followPosition, maxPanDistance);
        }
        else
        {
            followPosition = Vector3.zero;
        }
        targetPosition = followPosition + defaultPosition;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, panSpeed);
    }
    private void Zoom(Bounds bounds)
    {
        float boundsWidth = bounds.extents.x;
        if (boundsWidth == 0)
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

    private Bounds GetBounds()
    {
        Bounds bounds = new Bounds();
        if (targetGroup.Count > 0)
        {
            foreach (Transform t in targetGroup)
            {
                bounds.Encapsulate(t.transform.position);
            }
        }
        else
        {
            bounds.Encapsulate(Vector3.zero);
        }
        return bounds;
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
        //GameManager.Instance.OnGameStarted -= ResetZoom;
    }
}
