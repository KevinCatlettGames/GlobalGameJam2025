using UnityEngine;
using TMPro;

public class FPSDisplay : MonoBehaviour
{
    public TextMeshProUGUI fpsText;

    private float timer;
    private int frames;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        frames++;
        timer += Time.deltaTime;

        if (timer >= 0.5f) // updates twice per second
        {
            float fps = frames / timer;
            fpsText.text = Mathf.RoundToInt(fps) + " FPS";

            frames = 0;
            timer = 0f;
        }
    }
}