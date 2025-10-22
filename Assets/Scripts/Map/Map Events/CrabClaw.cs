using System.Collections;
using UnityEngine;

public class CrabClaw : MonoBehaviour
{
    [SerializeField] private bool isMapEventEnabled = true;
    [SerializeField] private CrabHuntingGrounds huntingGrounds;
    [Header("Time")]
    [SerializeField] private float startDelay = 8f;
    [SerializeField] private float restetTime = 5f;
    [SerializeField] private float huntingTime = 5f;
    [Header("Stats")]
    [SerializeField] private float damage = 35f;
    [SerializeField] private float knockback = 10f;
    [SerializeField] private float speed = 5f;


    private void Start()
    {
        if (isMapEventEnabled)
            StartHunting();
        else
            Destroy(gameObject);
    }
    private void StartHunting()
    {
        //StartCoroutine(HuntingCoroutine());
    }
    private void StopHunting()
    {

    }
    private IEnumerator HuntingCoroutine()
    {
        float timer = huntingTime;
        Vector3 target;
        Vector3 moveVector = Vector3.zero;
        while (timer > 0)
        {
            target = huntingGrounds.GetClosestTargetPosition(transform.position);
            Debug.Log("Huntin " + target);
            if (target != Vector3.zero)
            {
                moveVector = (target - transform.position) * speed * Time.deltaTime;
            }
            else
            {
                moveVector = Vector3.zero;
            }
            transform.position = transform.position + moveVector;
            yield return null;
        }
    }
    
    private void Update()
    {
        Vector3 target;
        Vector3 moveVector = Vector3.zero;
        target = huntingGrounds.GetClosestTargetPosition(transform.position);
        //Debug.Log("Huntin " + target);
        if (target != Vector3.zero)
        {
            moveVector = (target - transform.position) * speed * Time.deltaTime;
        }
        else
        {
            moveVector = Vector3.zero;
        }
        transform.position = transform.position + moveVector;
        //transform.position = Vector3.Lerp(transform.position, target, speed * Time.deltaTime);
    }
}
