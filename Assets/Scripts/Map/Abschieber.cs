using UnityEngine;
using System.Collections.Generic;

public class Abschieber : MonoBehaviour
{
    [SerializeField] private float damage = .1f;
    [SerializeField] private float knockback = .1f;
    [SerializeField] private float intervall = .1f;
    private List<PlayerController> playersInside = new List<PlayerController>();
    private float timer = 0f;

    private void Update()
    {
        if (timer >= intervall)
        {
            if (playersInside.Count > 0)
            {
                for (int i = 0; i < playersInside.Count; i++)
                {
                    Vector3 direction = playersInside[i].transform.position - transform.position;
                    direction.y = 0;
                    if (GameManager.Instance.PlayingLocal)
                    {
                        playersInside[i].ApplyKnockbackLocal(-2, direction, knockback, damage);
                    }
                    else
                    {
                        playersInside[i].ApplyKnockbackClientRpc(-2, direction, knockback, damage);
                    }
                }
                timer = 0;
            }
        }
        else
        {
            timer += Time.deltaTime;
        }
    }
    //private void OnTriggerStay(Collider other)
    //{
    //    if (other.CompareTag("Player"))
    //    {
    //        Vector3 direction = other.transform.position - transform.position;
    //        direction.y = 0;
    //        if (direction == Vector3.zero)
    //        {
    //            direction = other.transform.forward;
    //        }
    //        else
    //        {
    //            direction.Normalize();
    //        }
    //        PlayerController player = other.gameObject.GetComponent<PlayerController>();

    //        player.ApplyKnockbackLocal(-1, direction, knockback * Time.deltaTime, damage * Time.deltaTime);
    //    }
    //}
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            if (!playersInside.Contains(player))
                playersInside.Add(player); 
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            if (playersInside.Contains(player))
                playersInside.Remove(player);
        }
    }
}
