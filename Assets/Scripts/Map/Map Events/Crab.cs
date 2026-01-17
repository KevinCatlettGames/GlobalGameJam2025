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
    [SerializeField] private CrabHuntingGrounds huntingGrounds;
    [SerializeField] private Transform crabBody;
    [SerializeField] private float startDelay = 5f;
    [SerializeField] private float eyeRotationSpeed = 2;
    [SerializeField] private float crabRotationSpeed = 1;
    [SerializeField] private float restingTime = 3f;
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
        switch(state)
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
        crabBody.Rotate(Vector3.up, rotationDirection * crabRotationSpeed * Time.fixedDeltaTime);
        if (Physics.Raycast(new Vector3(0, 1, 0), crabBody.forward, out RaycastHit hit, 20f, LayerMask.GetMask("Player")))
        {
            Vector3 v = hit.point;
            v.y = 0;
            claw.transform.position = v;
            claw.Target = huntingGrounds.GetClosestTargetPosition(v);
            claw.StartHunting();
            state = CrabState.Hunting;
        }
    }
    private void Hunt()
    {
        if (claw.Status != CrabClawStatus.hunting)
        {
            timer = restingTime;
            state = CrabState.Resting;
            ResetEyes();
        }
        else
        {
            RotateCrab();
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
        Vector3 t = claw.Target;
        Vector3 lookVector = t - transform.position;
        lookVector.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(lookVector, Vector3.up);
        crabBody.rotation = Quaternion.Lerp(crabBody.rotation, targetRotation, Time.fixedDeltaTime * crabRotationSpeed);
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
            eyes[i].rotation = crabBody.rotation;
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
