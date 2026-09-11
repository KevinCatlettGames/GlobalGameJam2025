using System.Linq;
using System.Collections; 
using System.Collections.Generic;
using UnityEngine;

public class UnlockPendingSkinsManager : MonoBehaviour
{
    [SerializeField] SkinButtonHandler[] skinButtonHandlers; 
    AchievementSaveSystem achievementSaveSystem;
    [SerializeField] float durationUntilNext = 1f;
    private void Awake()
    {
        achievementSaveSystem = AchievementSaveSystem.instance;
    }

    private void Start()
    {
        StartCoroutine(CheckPendingSkins());
    }

    IEnumerator CheckPendingSkins()
    {
        yield return new WaitForSeconds(0.1f);

        if (achievementSaveSystem && achievementSaveSystem.HasPendingUnlocks())
        {
            List<int> pendingUnlocks = achievementSaveSystem.ConsumePendingUnlocks();
            List<SkinButtonHandler> skinsToUnlock = new List<SkinButtonHandler>();

            foreach (SO_Achievement achSO in achievementSaveSystem.AchievementList)
            {
                if (!pendingUnlocks.Contains(achSO.AchievementID))
                    continue;

                foreach (SkinButtonHandler skinButtonHandler in skinButtonHandlers)
                {
                    if (skinButtonHandler.skinAchievementIndex == achSO.AchievementID)
                    {
                        skinsToUnlock.Add(skinButtonHandler);
                        break;
                    }
                }
            }
            skinsToUnlock = skinsToUnlock.OrderBy(handler => handler.skinButtonHandlerIndex).ToList();

            foreach (SkinButtonHandler skinButtonHandler in skinsToUnlock)
                skinButtonHandler.ActDisabled();

            yield return new WaitForSeconds(1f);
    
            foreach (SkinButtonHandler skinButtonHandler in skinsToUnlock)
            {
                skinButtonHandler.PerformUnlockAnimation();
                yield return new WaitForSeconds(durationUntilNext);
            }
        }
    }
}
