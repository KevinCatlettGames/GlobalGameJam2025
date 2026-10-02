using System.Collections.Generic;
using UnityEngine;

public class DamageField : MonoBehaviour
{
    [SerializeField] private float damage = .1f;
    [SerializeField] private float intervall = .1f;
    private int playerID = -1;
    private List<PlayerController> playersInside = new List<PlayerController>();
    private float timer = 0f;

    public void SetID(int ID)
    {
        playerID = ID;
    }
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
                        playersInside[i].ApplyKnockbackLocal(-1, direction, 0, damage, false);
                    }
                    else
                    {
                        playersInside[i].ApplyKnockbackClientRpc(-1, direction, 0, damage, false);
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
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            if (player.PlayerID != playerID && !playersInside.Contains(player))
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
