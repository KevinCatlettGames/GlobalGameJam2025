using UnityEngine;

public class WallFormation : MonoBehaviour
{
    private RisingWall[] walls;
    private bool isActive = false;
    private void Awake()
    {
        walls = GetComponentsInChildren<RisingWall>(); 
    }
    public void RiseFormation()
    {
        if (isActive)
            return;
        isActive = true;
        for (int i = 0; i < walls.Length; i++)
        {
            walls[i].gameObject.SetActive(true);
            walls[i].Rise();
        }
    }
    public void SinkFormation()
    {
        if (!isActive)
            return;
        isActive = false;
        for (int i = 0; i < walls.Length; i++)
        {
            RisingWall wall = walls[i];
            if (wall.gameObject.activeSelf && wall.IsActive)
            {
                walls[i].Sink(false);
            }
        }
    }
}
