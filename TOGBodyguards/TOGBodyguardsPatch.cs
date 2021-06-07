using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TOGBodyguards
{
    public class TOGBodyguardsPatch : MonoBehaviour
    {
        private Harmony _harmony;

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
        }

        [HarmonyPatch(typeof(VRCharController))]
        [HarmonyPatch("EnableAvatar")]
        // ReSharper disable once UnusedType.Local
        private static class VRCharControllerEnableAvatarPatch
        {
            [HarmonyPostfix]
            private static void Postfix(VRCharController __instance)
            {
                var bodyguardSpawnerController =
                    __instance.config.gameObject.AddComponent<BodyguardSpawnerController>();
                var jsonInput = File.ReadAllText(Application.streamingAssetsPath +
                                                 "/Mods/TOGBodyguards/Settings.json");

                var data = JsonConvert.DeserializeObject<BodyguardSpawnerControllerData>(jsonInput);
                bodyguardSpawnerController.bodyguards = data.bodyguards;
            }
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
                    BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                                             | BindingFlags.Static;
                    GameConfigManager config =
                        __instance.GetType().GetField("config", bindFlags).GetValue(__instance) as GameConfigManager;
                    GameBrain brain =
                        __instance.GetType().GetField("brain", bindFlags).GetValue(__instance) as GameBrain;

                    if (!config.isCampaign && !config.isLobby && !config.isArena)
                    {
                        __instance.isSpawnBG = true;
                        var bodyguardSpawnerController = config.gameObject.GetComponent<BodyguardSpawnerController>();
                        //spawn body guard
                        for (int index = 0; index < __instance.BodyGuards.Count; ++index)
                        {
                            var bodyguardName = __instance.BodyGuards[index].name;
                            //has body guard name
                            if (!bodyguardSpawnerController.bodyguards.ContainsKey(bodyguardName))
                                continue;
                            if (!bodyguardSpawnerController.bodyguards[bodyguardName])
                                continue;
                            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(
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

                        __instance.GetType().GetField("brain", bindFlags).SetValue(__instance, brain);
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
                bool isStabbing,
                bool isOnFire,
                bool isHeadShot,
                WepDesc wp,
                Vector3 ImpactVel,
                bool isProj,
                bool isCata,
                bool isBeaten,
                bool isBeatLimb,
                bool isCut = false)
            {
                if (!__instance.config.isCampaign && !__instance.config.isLobby && !__instance.config.isArena &&
                    __instance.isBG)
                    dam = 0;
            }
        }
    }
}