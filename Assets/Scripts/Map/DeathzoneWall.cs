using UnityEngine;

public class DeathzoneWall : MonoBehaviour
{
    [SerializeField] GameObject blastZoneEffect;
    private Vector3 effectDirection;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            Debug.Log("Death by Wall");
            Vector3 pos = other.transform.position;
            effectDirection = other.GetComponent<CharacterController>().velocity * -1f;
            Instantiate(blastZoneEffect, pos, Quaternion.LookRotation(effectDirection));
            CameraShaker.instance.ShakeCamera(.3f, 20f);
        }
    }
}
