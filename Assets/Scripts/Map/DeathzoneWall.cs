using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class DeathzoneWall : NetworkBehaviour
{
    [SerializeField] private float cameraShakeTime = .2f;
    [SerializeField] private float cameraShakeIntensity = 1.0f;
    [SerializeField] GameObject blastZoneEffect;
    private Vector3 effectDirection;
    private Vector3 effectPosition;
    [SerializeField] private bool isFloor = false;
    [SerializeField] private float delayFloor = .75f;
    [SerializeField] private float yOffsetFloor = 5f;

    private void Start()
    {
        if (!GameManager.Instance.PlayingLocal)
        {
            DisableColServerRpc();
        }
        else if (isFloor)
        {
            GetComponent<BoxCollider>().enabled = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void DisableColServerRpc()
    {
        DisableColClientRpc();
    }

    [ClientRpc]
    void DisableColClientRpc()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        col.enabled = false;
    }
    
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
            CameraShaker.instance.ShakeCamera(cameraShakeTime, cameraShakeIntensity);
        }
    }

    private void SpawnEffect()
    {
        Instantiate(blastZoneEffect, effectPosition, Quaternion.identity);
    }
    private void StartDisable()
    {
        StartCoroutine(TemporarilyDisableCollider());
    }

    [ServerRpc(RequireOwnership = false)]
    public void EnableColServerRpc()
    {
       EnableColClientRpc();
    }

    [ClientRpc]
    void EnableColClientRpc()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        col.enabled = true;
    }
    
    private IEnumerator TemporarilyDisableCollider()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        col.enabled = false;
        yield return new WaitForSeconds(.1f);
        col.enabled = true;
    }
}
