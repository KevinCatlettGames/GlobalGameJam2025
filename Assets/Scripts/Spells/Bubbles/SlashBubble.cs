using System.Collections;
using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public class SlashBubble : BasicBubble
{
	[Header("SpecialStats")] 
	[SerializeField] private GameObject slasherL;
	[SerializeField] private GameObject slasherR;
	[SerializeField] private Transform spinner;
	
    public override void InitialiseBubble(int ID, Vector3 dir, EventReference soundEvent, Collider playerCollider){
		base.InitialiseBubble(ID, dir, soundEvent, playerCollider);
		canMiss = false;
		StartCoroutine(StartSlashers());
    }

    private IEnumerator StartSlashers()
    {
	    while (currentSize < size)
	    {
		    currentSize += inflationSpeed * Time.deltaTime;
		    if (currentSize > size) currentSize = size;

		    slasherL.transform.localScale = Vector3.one * currentSize;
		    slasherR.transform.localScale = Vector3.one * currentSize;
		    yield return null;
	    }

	    slasherL.GetComponentInChildren<Slasher>().SetInflated(playerCollider, OwnerID);
	    slasherR.GetComponentInChildren<Slasher>().SetInflated(playerCollider, OwnerID);
	    hasInflated = true;
    }
	protected override void BubbleMovement()
	{
		transform.position = playerCollider.transform.position;
	}

	protected override IEnumerator BubbleRangeLimit()
	{
		while (!hasInflated)
			yield return null;
		
		float roatation = 0f;
		float angle = 0f;
		while (roatation < range)
		{
			angle = speed * Time.deltaTime;
			transform.Rotate(Vector3.up * (angle));
			roatation += angle;
			yield return null;
		}
		Pop();
	}

	public void SlasherHit(Vector3 slasherDir, GameObject other)
	{
		direction = slasherDir;
		if (other.CompareTag("Bubble") && other.TryGetComponent<BasicBubble>(out BasicBubble bubble))
		{
			if (bubble.OwnerID != OwnerID)
			{
				bubble.BubbleCollision(gameObject);
			}
		}
		BubbleCollision(other);
	}
}
