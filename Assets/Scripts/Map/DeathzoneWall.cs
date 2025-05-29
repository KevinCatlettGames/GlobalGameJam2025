using UnityEngine;

public class DeathzoneWall : MonoBehaviour
{
    [SerializeField] GameObject blastZoneEffect;
    private Vector3 effectDirection;
    [SerializeField] private bool isFloor = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            Debug.Log("Death by Wall");
            Vector3 pos = other.transform.position;
            if (isFloor)
            {
                Instantiate(blastZoneEffect, pos, Quaternion.identity);
            }
            else
            {
                effectDirection = other.GetComponent<CharacterController>().velocity * -1f;
                Instantiate(blastZoneEffect, pos, Quaternion.LookRotation(effectDirection));
            }
            CameraShaker.instance.ShakeCamera(.3f, 20f);
        }
    }
}
