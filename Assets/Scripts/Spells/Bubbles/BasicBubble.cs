using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using FMODUnity;
using FMOD;

public class BasicBubble : MonoBehaviour
{
    protected float damage = 1.0f;
    protected float knockback = 1.0f;
    protected float speed = 1.0f;
    protected float range = 1.0f;
    protected float size = 1.0f;
    protected Vector3 direction = Vector3.zero;
    protected Coroutine rangeCoroutine;
    protected bool hasPopped = false;
    protected float inflationSpeed = 8f;
    protected SphereCollider sphereCollider;
    protected float currentSize = 0.01f;
    
    [SerializeField] protected GameObject popEffect; 
    
    public virtual void InitialiseBubble(float dmg, float knb, float spd, float rng, float siz, Vector3 dir, EventReference soundEvent)
    {
        damage = dmg;
        knockback = knb;
        speed = spd;
        range = rng;
        size = siz;
        direction = dir;
        rangeCoroutine = StartCoroutine(BubbleRangeLimit());
        RuntimeManager.PlayOneShotAttached(soundEvent, gameObject);
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null) 
        {
            UnityEngine.Debug.Log("Inflate");
            sphereCollider.enabled = false;
            StartCoroutine(Inflate());
        }
    }

    private void FixedUpdate()
    {
        BubbleMovement();
    }

    protected virtual void BubbleMovement()
    {
        transform.position += direction * speed * Time.fixedDeltaTime;
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
        if(hasPopped) return;
        hasPopped = true;
        StopCoroutine(rangeCoroutine);
        //play sound
        
        //pop effect
        GameObject effect = Instantiate(popEffect, transform.position, Quaternion.identity);
        BubbleEffect bubbleEffect = effect.GetComponent<BubbleEffect>();
        bubbleEffect?.Initialise(size);

        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Reflector reflector;
        if (collision.gameObject.TryGetComponent<Reflector>(out reflector))
        {
            if (reflector.GetIsReflecting())
            {
                Reflect(collision.GetContact(0).normal);
                return;
            }
        }
        BubbleCollision(collision.gameObject);
    }

    public virtual void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.ApplyKnockback(direction, knockback, damage);
        }
        Pop();
    }

    protected IEnumerator Inflate()
    {
        Vector3 currentScale = Vector3.one;
        while (currentSize < size) 
        {
            currentSize += inflationSpeed * Time.deltaTime;
            transform.localScale = Vector3.one * currentSize;
            if (currentSize > size) currentSize = size;
            transform.localScale = currentScale * currentSize;
            yield return new WaitForEndOfFrame();
        }
        sphereCollider.enabled = true;
    }
    private void Reflect(Vector3 normal)
    {
        direction = Vector3.Reflect(direction, normal);
    }
}
