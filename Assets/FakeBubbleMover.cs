using UnityEngine;

public class FakeBubbleMover : MonoBehaviour
{
    [SerializeField] private float speed = 10f; // Set this to match your real bubble's speed
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private GameObject fizzleEffect;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Don't collide with players due to the layer matrix, but pop on walls/surfaces
        if (fizzleEffect != null)
        {
            Instantiate(fizzleEffect, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}