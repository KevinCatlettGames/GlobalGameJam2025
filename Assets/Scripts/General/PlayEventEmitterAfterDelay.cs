using UnityEngine;
using FMODUnity;

public class PlayEventEmitterAfterDelay : MonoBehaviour
{

    [SerializeField] float delay;
    [SerializeField] StudioEventEmitter emitter;
    void Start()
    {
        Invoke(nameof(Emit), delay);
    }

    void Emit()
    {
        emitter.Play();
    }
}