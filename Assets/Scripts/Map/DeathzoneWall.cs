using System.Collections;
using UnityEngine;

public class DeathzoneWall : MonoBehaviour
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
            if (isFloor) Invoke("StartDisable", 1f);
            GameManager.Instance.OnGameStarted += StartDisable;
        }
        else if (isFloor)
        {
            GetComponent<BoxCollider>().enabled = true;
        }
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
    private IEnumerator TemporarilyDisableCollider()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        col.enabled = false;
        yield return new WaitForSeconds(.1f);
        col.enabled = true;
    }
    private void OnDestroy()
    {
        if (!GameManager.Instance.PlayingLocal)
        {
            GameManager.Instance.OnGameStarted -= StartDisable;
        }
    }
}
