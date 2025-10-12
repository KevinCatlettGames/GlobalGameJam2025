using UnityEngine;

public class CrabClaw : MonoBehaviour
{
    [SerializeField] private bool isMapEventEnabled = true;
    [SerializeField] private float startDelay = 8f;
    [SerializeField] private CrabHuntingGrounds huntingGrounds;
    [Header("Stats")]
    [SerializeField] private float damage = 35f;
    [SerializeField] private float knockback = 10f;
    [SerializeField] private float speed = 5f;


    private void Start()
    {
        if (isMapEventEnabled)
            Invoke(nameof(StartHunting), startDelay);
        else
            Destroy(gameObject);
    }
    private void StartHunting()
    {
        
    }
    private void Update()
    {
        
    }
}
