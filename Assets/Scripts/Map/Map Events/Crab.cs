using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
public enum CrabState
{
    Searching,
    Hunting,
    Resting
}
public class Crab : MonoBehaviour
{
    [SerializeField] private Transform[] eyes = new Transform[2];
    [SerializeField] private CrabClaw claw;
    [SerializeField] private LurkingShadow shadow;
    [SerializeField] private float startDelay = 5f;
    [SerializeField] private float eyeRotationSpeed = 2f;
    [SerializeField] private float crabSearchingSpeed = 1f;
    [SerializeField] private float crabHuntingSpeed = 2f;

    [SerializeField] protected float maxRange = 20f;
    [SerializeField] private float restingTime = 3f;
    [SerializeField] private float huntingTime = 2f; 
    private GameObject currentTarget = null;
    private CrabState state = CrabState.Resting;
    private float rotationDirection = 1;
    private float timer = 100;
    private bool eventActive = false;

    private void Awake()
    {
        bool isMapEventActive = true;
        if (LobbyManager.instance)
            isMapEventActive = LobbyManager.instance.MapSettings[2].PlayWithMapEvent;

        if (!isMapEventActive)
        {
            Destroy(gameObject);
            return;
        }

        if (TransportSwitcher.Instance)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            GameManager.Instance.OnGameStarted += StartEvent;
            GameManager.Instance.OnGameEnded += StopEvent;
            Invoke(nameof(StartEvent), 7);
        }
        else
        {
            GameManager.Instance.OnGameStarted += StartEvent;
            GameManager.Instance.OnGameEnded += StopEvent;
            Invoke(nameof(StartEvent), 7);
        }
    }
    private void FixedUpdate()
    {
        switch (state)
        {
            case CrabState.Searching:
                Search();
                break;
            case CrabState.Hunting:
                Hunt();
                break;
            case CrabState.Resting:
                Rest();
                break;
        }
    }
    private void StartEvent()
    {
        timer = startDelay;
        eventActive = true;
    }
    private void StopEvent()
    {
        eventActive = false;
        timer = 100f;
    }
    private void Search()
    {
        transform.Rotate(Vector3.up, rotationDirection * crabSearchingSpeed * Time.fixedDeltaTime);
        if (currentTarget != null)
        {
            timer = huntingTime;
            state = CrabState.Hunting;
            shadow.LerpShadow(1, huntingTime);
        }
    }
    private void Hunt()
    {
        if (timer > 0)
        {
            if (maxRange < Vector3.Distance(currentTarget.transform.position, new Vector3(0, 1, 0)))
            {
                shadow.LerpShadow(0, .2f);
                ResetEyes();
                currentTarget = null;
                state = CrabState.Searching;
                return;
            }
            timer -= Time.fixedDeltaTime;
            if (Physics.Raycast(new Vector3(0, 1, 0), transform.forward, out RaycastHit hit, maxRange, LayerMask.GetMask("Player")))
            {
                    currentTarget = hit.transform.gameObject;
            }
            RotateCrab();
        }
        else
        {
            currentTarget = null;
            claw.Snap();
            ResetEyes();
            timer = restingTime;
            state = CrabState.Resting;
        }
    }
    private void Rest()
    {
        if (!eventActive)
            return;
        if (timer > 0)
        {
            timer -= Time.fixedDeltaTime;
        }
        else
        {
            //Wake up
            state = CrabState.Searching;
            rotationDirection *= -1f;
        }
    }
    private void RotateCrab()
    {
        Vector3 t = currentTarget.transform.position;
        Vector3 lookVector = t - transform.position;
        lookVector.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(lookVector, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * crabSearchingSpeed);
        for (int i = 0; i < eyes.Length; i++)
        {
            eyes[i].LookAt(new Vector3(t.x, eyes[i].position.y, t.z));
        }
    }
    private void ResetEyes()
    {
        //Quaternion targetRotation = Quaternion.LookRotation(new Vector3(0, 0, -1), Vector3.up);
        for (int i = 0; i < eyes.Length; i++)
        {
            eyes[i].rotation = transform.rotation;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (state == CrabState.Searching && other.CompareTag("Player"))
        {
            currentTarget = other.gameObject;
        }
    }
    private void OnDestroy()
    {
        if (TransportSwitcher.Instance)
        {
            if (NetworkManager.Singleton && !NetworkManager.Singleton.IsServer) return;
            GameManager.Instance.OnGameStarted -= StartEvent;
            GameManager.Instance.OnGameEnded -= StopEvent;
        }
        else
        {
            GameManager.Instance.OnGameStarted -= StartEvent;
            GameManager.Instance.OnGameEnded -= StopEvent;
        }
    }
}
