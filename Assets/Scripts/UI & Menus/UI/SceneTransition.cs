using System;
using UnityEngine;
using UnityEngine.Events;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition instance;
    public UnityEvent<bool> OnSceneTransition;
    public UnityEvent OnMenuTransition;
    public UnityEvent OnTransitionFinished;
    [SerializeField] private bool DoAutoSceneStartTransition = true;
    
    private void Awake()
    {
        if (instance == null)
            instance = this; 
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if(DoAutoSceneStartTransition)
            SceneStartTransition();
    }

    public void SceneStartTransition()
    {
        OnSceneTransition?.Invoke(true);
    }

    public void SceneEndTransition()
    {
        OnSceneTransition?.Invoke(false);
    }

    public void MenuTransition()
    {
        OnMenuTransition?.Invoke();
    }

    public void InvokeOnTransitionFinished()
    {
        OnTransitionFinished?.Invoke();
    }
}