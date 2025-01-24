using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BasicBubble : MonoBehaviour
{
    private Rigidbody rigidBody;
    private float damage = 1.0f;
    private float kockback = 1.0f;
    private float speed = 1.0f;
    private float range = 1.0f;
    private float size = 1.0f;
    private Vector3 direction = Vector3.zero;
    private Coroutine rangeCoroutine;

    void Start()
    {
        
    }

    public virtual void InitialiseBubble(float dmg, float knb, float spd, float rng, float siz, Vector3 dir)
    {
        damage = dmg;
        kockback = knb;
        speed = spd;
        range = rng;
        size = siz;
        direction = dir;
        rigidBody = GetComponent<Rigidbody>();
        transform.localScale *= size;
        rangeCoroutine = StartCoroutine(BubbleRangeLimit());
        rigidBody.velocity = direction * speed;
    }
    private IEnumerator BubbleRangeLimit()
    {
        float killTime = 0;
        killTime = range / speed;
        yield return new WaitForSeconds(killTime);
        Pop();
    }

    protected virtual void Pop()
    {
        StopCoroutine(rangeCoroutine);
        //play sound
        //pop effect
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        BubbleCollision(collision.gameObject);
    }

    public void BubbleCollision(GameObject other)
    {
        if (other.CompareTag("Player"))
        {
            //knockbackplayer
        }
        Pop();
    }

}
