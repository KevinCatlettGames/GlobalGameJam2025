using UnityEngine;

public class RandomSpriteSelector : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;
    private SpriteRenderer spriteRenderer;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        int i = Random.Range(0, sprites.Length);
        spriteRenderer.sprite = sprites[i];
    }
}
