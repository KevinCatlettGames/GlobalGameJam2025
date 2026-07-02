using Cinemachine;
using UnityEngine;

public class TargetGroupManager : MonoBehaviour
{
    private CinemachineTargetGroup targetGroup;

    public static TargetGroupManager Instance;

    [SerializeField] private float weight = 1f;
    [SerializeField] private float radius = .5f;

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

        targetGroup = GetComponent<CinemachineTargetGroup>();
    }

    public void AddToGroup(Transform t)
    {
        if (targetGroup.FindMember(t) != -1)
            return;

        targetGroup.AddMember(t, weight, radius);
    }

    public void RemoveFromGroup(Transform t)
    {
        if (targetGroup.FindMember(t) == -1)
            return;

        targetGroup.RemoveMember(t);
    }
}
