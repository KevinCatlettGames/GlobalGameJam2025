using UnityEngine;

public class DeathzoneWall : MonoBehaviour
{
    [SerializeField] GameObject blastZoneEffect;
    private Vector3 effectDirection;
    private Vector3 effectPosition;
    [SerializeField] private bool isFloor = false;
    [SerializeField] private float delayFloor = .75f;
    [SerializeField] private float yOffsetFloor = 5f;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            Debug.Log("Death by Wall");
            effectPosition = other.transform.position;
            if (isFloor)
            {
                effectPosition -= Vector3.up * yOffsetFloor;
                Invoke("SpawnEffect", delayFloor);
            }
            else
            {
                effectDirection = other.GetComponent<CharacterController>().velocity * -1f;
                Instantiate(blastZoneEffect, effectPosition, Quaternion.LookRotation(effectDirection));
            }
            CameraShaker.instance.ShakeCamera(.3f, 20f);
        }
    }

    private void SpawnEffect()
    {
        Instantiate(blastZoneEffect, effectPosition, Quaternion.identity);
    }
}
