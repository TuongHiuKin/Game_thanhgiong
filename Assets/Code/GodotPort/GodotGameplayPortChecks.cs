using System;
using System.Collections;
using UnityEngine;

// Explicit Play-mode checks; never runs automatically or changes a saved scene.
public static class GodotGameplayPortChecks
{
    public static IEnumerator CheckHealth(ThanhGiongCampaignController campaign, Action<bool, string> completed)
    {
        if (campaign == null || campaign.CurrentChapter != ThanhGiongCampaignController.Chapter.Battle) {
            completed?.Invoke(false, "Health check requires a loaded Battle map."); yield break;
        }
        campaign.RestartBattle();
        ThanhGiongEnemy[] enemies = campaign.battleRoot != null
            ? campaign.battleRoot.GetComponentsInChildren<ThanhGiongEnemy>(true) : new ThanhGiongEnemy[0];
        bool[] enabled = new bool[enemies.Length];
        object[] identities = new object[enemies.Length];
        for (int i = 0; i < enemies.Length; i++) {
            enabled[i] = enemies[i].enabled; identities[i] = enemies[i];
            enemies[i].StopAllCoroutines(); enemies[i].enabled = false;
            if (enemies[i].Body != null) {
                enemies[i].Body.linearVelocity = Vector3.zero;
                enemies[i].Body.angularVelocity = Vector3.zero;
                enemies[i].Body.isKinematic = true;
            }
        }
        bool pass = Mathf.Approximately(campaign.Health, campaign.maxHealth);
        pass &= campaign.DamagePlayer(8);
        pass &= Mathf.Approximately(campaign.Health, campaign.maxHealth - 8);
        pass &= !campaign.DamagePlayer(8); // Same-frame multi-enemy hits share the hurt window.
        pass &= Mathf.Approximately(campaign.Health, campaign.maxHealth - 8);
        Time.timeScale = 0;
        pass &= !campaign.DamagePlayer(35);
        Time.timeScale = 1;
        yield return new WaitForSeconds(campaign.hurtInvulnerabilitySeconds + .05f);
        pass &= campaign.DamagePlayer(35);
        pass &= Mathf.Approximately(campaign.Health, campaign.maxHealth - 43);
        yield return new WaitForSeconds(campaign.hurtInvulnerabilitySeconds + .05f);
        pass &= campaign.DamagePlayer(campaign.maxHealth * 2);
        pass &= campaign.IsDead && !campaign.IsBattleActive;
        pass &= !campaign.GetComponent<MountedHorseController>().enabled;
        pass &= !campaign.DamagePlayer(8);
        campaign.RestartBattle();
        pass &= !campaign.IsDead && campaign.IsBattleActive && campaign.Kills == 0;
        pass &= Mathf.Approximately(campaign.Health, campaign.maxHealth);
        pass &= campaign.CurrentWeapon == ThanhGiongCampaignController.Weapon.IronSword;
        pass &= campaign.GetComponent<MountedHorseController>().enabled;
        for (int i = 0; i < enemies.Length; i++) {
            pass &= ReferenceEquals(identities[i], enemies[i]);
            pass &= enemies[i].gameObject.activeInHierarchy && Mathf.Approximately(enemies[i].HealthRatio, 1);
            enemies[i].enabled = enabled[i];
        }
        completed?.Invoke(pass, pass ? "Health damage, invulnerability, pause, death lock, and same-actor restart PASS." : "Health parity check FAILED.");
    }
}
