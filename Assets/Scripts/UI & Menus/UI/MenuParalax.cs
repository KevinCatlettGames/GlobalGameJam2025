using UnityEngine;

public class MenuParalax : MonoBehaviour
{
    [SerializeField] private float offsetMultiplier = 1f;
    [SerializeField] private float smoothTime = .3f;

    private Vector2 startPos;
    private Vector3 velocity;
    void Start()
    {
        startPos = transform.position;
    }


    void Update()
    {
        Vector2 offset = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        transform.position = Vector3.SmoothDamp(transform.position, startPos + (offset * offsetMultiplier), ref velocity, smoothTime);
    }
}
