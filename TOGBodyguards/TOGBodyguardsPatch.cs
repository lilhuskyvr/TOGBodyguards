using System;
using System.Reflection;
using HarmonyLib;
using MLSpace;
using TOGModFramework;
using UnityEngine;
using Valve.Newtonsoft.Json;
using Object = UnityEngine.Object;

namespace TOGBodyguards
{
    public class TOGBodyguardsPatch : MonoBehaviour
    {
        private Harmony _harmony;
        public string modName = "TOGBodyguards";

        public void Inject()
        {
            try
            {
                _harmony = new Harmony("Bodyguards");
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                Debug.Log("Body Guards Loaded");
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }

            EventManager.OnPlayerLoaded += EventManagerOnOnPlayerLoaded;
        }

        private void EventManagerOnOnPlayerLoaded()
        {
            var bodyguardSpawnerController =
                ConfigManager.local.gameObject.AddComponent<BodyguardSpawnerController>();

            var jsonInput = FileManager.GetModJsonData(modName);

            var data = JsonConvert.DeserializeObject<BodyguardSpawnerControllerData>(jsonInput);
            bodyguardSpawnerController.bodyguards = data.bodyguards;
            bodyguardSpawnerController.isInvincible = data.isInvincible;
        }

        [HarmonyPatch(typeof(NPCSpawner))]
        [HarmonyPatch("Update")]
        // ReSharper disable once UnusedType.Local
        private static class NPCSpawnerUpdatePatch
        {
            [HarmonyPostfix]
            private static void Postfix(NPCSpawner __instance)
            {
                if (!__instance.isSpawnBG)
                {
                    GameConfigManager config = ConfigManager.local;
                    GameBrain brain = ConfigManager.local.brain;


                    if (!config.isCampaign && !config.isLobby && !config.isArena)
                    {
                        __instance.isSpawnBG = true;
                        var bodyguardSpawnerController =
                            ConfigManager.local.gameObject.GetComponent<BodyguardSpawnerController>();

                        for (int index = 0; index < __instance.BodyGuards.Count; ++index)
                        {
                            var bodyguardName = __instance.BodyGuards[index].name;
                            //has body guard name
                            if (!bodyguardSpawnerController.bodyguards.ContainsKey(bodyguardName))
                                continue;
                            if (!bodyguardSpawnerController.bodyguards[bodyguardName])
                                continue;
                            GameObject gameObject = Instantiate(
                                __instance.BodyGuards[index],
                                config.mc.Player.transform.position + 2 * config.mc.Player.transform.forward,
                                config.mc.Player.transform.rotation);

                            gameObject.GetComponent<Stats>().isBgFemale =
                                bodyguardSpawnerController.femaleBodyguardNames.Contains(bodyguardName);
                            gameObject.GetComponent<Stats>().setBodyGuardInit(config, brain);
                            gameObject.GetComponent<Stats>().isBG = true;
                            gameObject.SetActive(true);
                            brain.T1.Add(gameObject.GetComponent<Stats>());
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Stats))]
        [HarmonyPatch("SetDamage")]
        // ReSharper disable once UnusedType.Local
        private static class StatsSetDamagePatch
        {
            [HarmonyPrefix]
            private static void Prefix(Stats __instance, ref int dam,
                float WepDam,
                Transform Limb,
                Stats enemy,
                Vector3 HitVel,
                ref bool isStabbing,
                ref bool isOnFire,
                ref bool isHeadShot,
                WepDesc wp,
                Vector3 ImpactVel,
                bool isProj,
                bool isCata,
                bool isBeaten,
                bool isBeatLimb,
                bool isCut = false)
            {
                if (__instance.config.gameObject.GetComponent<BodyguardSpawnerController>().isInvincible)
                {
                    if (!__instance.config.isCampaign && !__instance.config.isLobby && !__instance.config.isArena &&
                        __instance.isBG)
                    {
                        dam = 0;
                        isStabbing = false;
                        isHeadShot = false;
                        isOnFire = false;
                        __instance.isGrabbed = false;
                        Limb.GetComponent<BodyColliderScript>().critical = false;
                    }
                }
            }
        }
    }
}