using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuFallingWizzos : MonoBehaviour
{
    [SerializeField] private List<Animator> wizzos = new List<Animator>();

    [SerializeField] private float minInterval = 3f;
    [SerializeField] private float maxInterval = 8f;

    [Range(0f, 1f)]
    [SerializeField] private float chainReactionChance = 0.25f;
    [SerializeField] private float chainReactionDelay = 0.6f;

    private readonly string[] animationTriggers = { "IdleBreak1", "IdleBreak2" };

    private List<int> wizzoQueue = new List<int>();
    private int[] lastAnimationIndex;

    private void Awake()
    {
        InitializeLastAnimationIndices();
    }

    private void OnEnable()
    {
        //PlayInitialWizzo();
        StartCoroutine(WizzoRoutine());
    }

    private void InitializeLastAnimationIndices()
    {
        lastAnimationIndex = new int[wizzos.Count];
        for (int i = 0; i < lastAnimationIndex.Length; i++)
        {
            lastAnimationIndex[i] = -1;
        }
    }

    private void PlayInitialWizzo()
    {
        if (wizzos == null || wizzos.Count == 0) return;

        if (lastAnimationIndex == null || lastAnimationIndex.Length != wizzos.Count)
        {
            InitializeLastAnimationIndices();
        }

        Animator firstWizzo = wizzos[0];
        if (firstWizzo != null)
        {
            firstWizzo.SetTrigger(animationTriggers[0]);
            lastAnimationIndex[0] = 0;
        }

        wizzoQueue.Clear();
        for (int i = 1; i < wizzos.Count; i++)
        {
            wizzoQueue.Add(i);
        }

        for (int i = 0; i < wizzoQueue.Count; i++)
        {
            int temp = wizzoQueue[i];
            int randomIndex = Random.Range(i, wizzoQueue.Count);
            wizzoQueue[i] = wizzoQueue[randomIndex];
            wizzoQueue[randomIndex] = temp;
        }
    }

    private IEnumerator WizzoRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);

            TriggerNextWizzo();

            if (Random.value < chainReactionChance)
            {
                yield return new WaitForSeconds(chainReactionDelay);
                TriggerNextWizzo();
            }
        }
    }

    private void TriggerNextWizzo()
    {
        if (wizzos == null || wizzos.Count == 0) return;

        if (wizzoQueue.Count == 0)
        {
            RefillAndShuffleQueue();
        }

        int nextWizzoIndex = wizzoQueue[0];
        wizzoQueue.RemoveAt(0);

        Animator targetWizzo = wizzos[nextWizzoIndex];

        if (targetWizzo != null)
        {
            int nextAnimIndex = (lastAnimationIndex[nextWizzoIndex] + 1) % animationTriggers.Length;

            if (lastAnimationIndex[nextWizzoIndex] == -1)
            {
                nextAnimIndex = Random.Range(0, animationTriggers.Length);
            }

            lastAnimationIndex[nextWizzoIndex] = nextAnimIndex;
            targetWizzo.SetTrigger(animationTriggers[nextAnimIndex]);
        }
    }

    private void RefillAndShuffleQueue()
    {
        wizzoQueue.Clear();

        for (int i = 0; i < wizzos.Count; i++)
        {
            wizzoQueue.Add(i);
        }

        for (int i = 0; i < wizzoQueue.Count; i++)
        {
            int temp = wizzoQueue[i];
            int randomIndex = Random.Range(i, wizzoQueue.Count);
            wizzoQueue[i] = wizzoQueue[randomIndex];
            wizzoQueue[randomIndex] = temp;
        }
    }
}