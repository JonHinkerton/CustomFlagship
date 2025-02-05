using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;
using Kingmaker.PubSubSystem.Core.Interfaces;
using Kingmaker.PubSubSystem.Core;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats.Base;
using UnityEngine;
using Kingmaker.ResourceLinks;
using Warhammer.SpaceCombat.Blueprints.Progression;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities.Base;
using Kingmaker.EntitySystem.Persistence;
using Newtonsoft.Json;
using Kingmaker.BundlesLoading;
using Kingmaker.UI.DollRoom;
using Kingmaker.View;
using System.IO;
using System.Text.Json;
using Kingmaker.Designers.EventConditionActionSystem.Actions;

namespace CustomFlagship
{
    public static class Main
    {
        internal static Harmony HarmonyInstance;
        internal static UnityModManager.ModEntry.ModLogger log;

        private static PerSaveSettings cachedPerSave = null;

        private static List<string[]> ships = new List<string[]> {
            new string[] {"Original", "923ea8656e3946c38b13038c1d9e7307"},
            new string[] {"Sword Frigate", "923ea8656e3946c38b13038c1d9e7307"},
            new string[] {"Viper Destroyer", "711f6477b8d4405cb09cf82f4f7e322d"},
            new string[] {"Cobra Destroyer", "f9e619867a7c4a67b574d346d10cffa1"},
            new string[] {"Claymore Corvette", "82bad175995d4f23baf884a9caf77dea"},
            new string[] {"Imperial Cruiser", "3b6609d444a34dfe83a56028b86abe90"},
            new string[] {"Imperial Cruiser 2", "c41bae2a60d24abd99d0663c439f37e1"},
            new string[] {"Imperial Transport", "f29445dd202d4d98b9bc2ca0e4ce6192"},
            new string[] {"Chaos Frigate", "2074b0cda9de41f2a446483f98605215"},
            new string[] {"Chaos Cruiser", "ddf557a81bca41f79c04b848ac4406bb" }
            //new string[] { "Firestorm", "2d9014f545b143418cb42cbf64dfd74f" },
            //new string[] { "Falchion", "4e4481bae463473ebc4c3f7b10de9403" }
        };

        static string[] shipNames = { ships[0][0], ships[1][0], ships[2][0], ships[3][0], ships[4][0], ships[5][0], ships[6][0], ships[7][0], ships[8][0], ships[9][0] }; //, ships[8][0], ships[9][0] };
        static string[] sizes = { "XXLarge", "XLarge", "Large", "Normal", "Small", "XSmall", "XXSmall" };

        static int shipPick = 0;
        static int sizePick = 0;

        public static BaseUnitEntity Descriptor(this BaseUnitEntity entity) => entity;

        public class PerSaveSettings : EntityPart
        {
            public const string ID = "CustomFlagship.PerSaveSettings";
            [JsonProperty]
            public string savedShip = "0";
            [JsonProperty]
            public string SavedSize = "1";
        }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            log = modEntry.Logger;

            modEntry.OnGUI = OnGUI;
            HarmonyInstance = new Harmony(modEntry.Info.Id);
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            return true;
        }

        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            bool IsInGame = Game.Instance.Player?.Party.Any() ?? false;

            if (!IsInGame)
            {
                ReloadPerSaveSettings();
                InitChange();
            }

            ships[0][1] = Game.Instance.Player.AllStarships.FirstOrDefault()?.Descriptor().Blueprint.AssetGuid;

            using (new GUILayout.VerticalScope())
            {
                GUILayout.Space(10);
            }
            GUILayout.Label("To use this mod, pick a ship and a size. Save. Reload. Carry on.", GUILayout.ExpandWidth(false));
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Space(10);
            }

            GUILayout.Label("Pick a ship", GUILayout.ExpandWidth(false));

            int selectedShip = shipPick;
            shipPick = GUILayout.SelectionGrid(selectedShip, shipNames, 3);
            if (selectedShip != shipPick)
            {
                //log.Log("selectedShip: " + selectedShip + " shipPick: " + shipPick + ".");
                selectedShip = shipPick;
                cachedPerSave.savedShip = shipPick.ToString();
                SavePerSaveSettings();
                InitChange();
            }

            using (new GUILayout.VerticalScope())
            {
                GUILayout.Space(10);
            }

            GUILayout.Label("Pick a size", GUILayout.ExpandWidth(false));

            int selectedSize = sizePick;
            sizePick = GUILayout.SelectionGrid(selectedSize, sizes, 3);
            if (selectedSize != sizePick)
            {
                cachedPerSave.SavedSize = sizePick.ToString();
                selectedSize = sizePick;
                SavePerSaveSettings();
                InitChange();
            }

        }

        public static void InitChange()
        {
            ships[0][1] = Game.Instance.Player.AllStarships.FirstOrDefault()?.Descriptor().Blueprint.AssetGuid;
            ReloadPerSaveSettings();
            log.Log("savedShip: " + shipPick + " savedSize: " + sizePick + ".");

            BaseUnitEntity thisShip = Game.Instance.Player.AllStarships.FirstOrDefault()?.Descriptor();
            log.Log("bue: " + thisShip.Name + " shipPickBP:" + ships[shipPick][1].ToString() + " sizePick: " + sizes[sizePick].ToString());

            var blueprintId = ships[shipPick][1];
            BlueprintUnit shipBlueprint = ResourcesLibrary.TryGetBlueprint<BlueprintUnit>(blueprintId);
            if (shipBlueprint == null)
            {
                return;
            }

            thisShip.Blueprint.Prefab = shipBlueprint.Prefab;

            // Scale mapping for each size
            var sizeScales = new Dictionary<string, Vector3>
            {
                { "XXLarge", new Vector3(2.3f, 2.3f, 1.9f) },
                { "XLarge", new Vector3(2.0f, 2.0f, 1.6f) },
                { "Large", new Vector3(1.6f, 1.6f, 1.3f) },
                { "Normal", new Vector3(1.0f, 1.0f, 1.0f) },
                { "Small", new Vector3(0.6f, 0.6f, 0.8f) },
                { "XSmall", new Vector3(0.3f, 0.3f, 0.5f) },
                { "XXSmall", new Vector3(0.1f, 0.1f, 0.3f) }
            };

            if (sizeScales.TryGetValue(sizes[sizePick], out var scale))
            {
                thisShip.View.gameObject.transform.localScale = scale;
            }
            
        }

        public static void ReloadPerSaveSettings()
        {
            var player = Game.Instance?.Player;
            if (player == null || Game.Instance.SaveManager.CurrentState == SaveManager.State.Loading) return;
            if (Game.Instance.State.InGameSettings.List.TryGetValue(PerSaveSettings.ID, out var obj) && obj is string json)
            {
                try
                {
                    cachedPerSave = JsonConvert.DeserializeObject<PerSaveSettings>(json);
                    int.TryParse(cachedPerSave.savedShip.ToString(), out shipPick);
                    int.TryParse(cachedPerSave.SavedSize.ToString(), out sizePick);
                }
                catch (Exception e)
                {
                    log.Log(e.ToString());
                }
            }
            if (cachedPerSave == null)
            {
                cachedPerSave = new PerSaveSettings();
                SavePerSaveSettings();
            }
        }

        public static void SavePerSaveSettings()
        {
            var player = Game.Instance?.Player;
            if (player == null) return;
            if (cachedPerSave == null) ReloadPerSaveSettings();
            var json = JsonConvert.SerializeObject(cachedPerSave);
            Game.Instance.State.InGameSettings.List[PerSaveSettings.ID] = json;
        }

        [HarmonyPatch]
        static class Patches
        {
            [HarmonyPatch(typeof(BundlesLoadService), nameof(BundlesLoadService.GetBundleNameForAsset))]
            [HarmonyPrefix]
            static bool GetBundleNameForAsset_Prefix(string assetId, ref string __result, BundlesLoadService __instance)
            {
                try
                {
                    InitChange();
                }
                catch (Exception e)
                {
                    
                }
                return true;
            }

            [HarmonyPatch(typeof(ShipDollRoom), nameof(ShipDollRoom.CreateSimpleAvatar))]
            static class IgnoreBrokenShips
            {
                static bool Prefix(ShipDollRoom __instance)
                {
                    //return false;
                    BaseUnitEntity ship = Game.Instance.Player.AllStarships.FirstOrDefault()?.Descriptor();
                    UnitEntityView unitEntityView = ship.View;
                    log.Log("uev: " + unitEntityView);
                    if (unitEntityView == null)
                    {
                        log.Log("create view");
                        unitEntityView = ship.CreateView();
                        log.Log("attach view");
                        ship.AttachView(unitEntityView);
                    }
                    log.Log("uev: " + unitEntityView);
                    var gcic = unitEntityView.GetComponentInChildren<StarshipView>();
                    log.Log("gcic: " + gcic);
                    bool hasbr = false;
                    try
                    {
                        var brthis = gcic.BaseRenderer.gameObject;
                        hasbr = true;
                        log.Log("brthis: " + brthis);
                    }
                    catch (Exception e) { }

                    if (!hasbr)
                    {
                        //Replace gcic with root ship
                        log.Log("return false");
                        return false;
                    }
                    var br = gcic.BaseRenderer;
                    log.Log("in null: " + br);
                    GameObject original = br.gameObject;
                    log.Log("original: " + original);
                    GameObject m_SimpleAvatar = null;
                    Transform m_TargetPlaceholder = null;
                    m_SimpleAvatar = UnityEngine.Object.Instantiate(original, m_TargetPlaceholder, worldPositionStays: false);
                    log.Log("m_SimpleAvatar: " + m_SimpleAvatar);
                    m_SimpleAvatar.transform.localPosition = Vector3.zero;
                    m_SimpleAvatar.transform.localRotation = Quaternion.identity;
                    //m_SimpleAvatar.transform.localScale = unitEntityView.transform.localScale;
                    log.Log("transformed");

                    log.Log("returning");
                    return true;
                }
            }

            //[HarmonyPatch(typeof(ShipDollRoom), nameof(ShipDollRoom.SetupShip))]
            //[HarmonyPrefix]
            //static bool SetupShip_Prefix(BaseUnitEntity ship)
            //{
            //    //log.Log("passed ship prefab: " + ship.Blueprint.Prefab.AssetId);
            //    //BaseUnitEntity thisship = Game.Instance.Player.AllStarships.FirstOrDefault().Descriptor();
            //    //ship = thisship;

            //    //log.Log("player ship prefab: " + thisship.Blueprint.Prefab.AssetId);
            //    //log.Log("converted ship prefab: " + thisship.Blueprint.Prefab.AssetId);
            //    //BlueprintUnit bpu = ResourcesLibrary.TryGetBlueprint<BlueprintUnit>("923ea8656e3946c38b13038c1d9e7307");
            //    //log.Log("bpu prefab: " + bpu.Prefab.AssetId);
            //    //UnitViewLink uvl = bpu.Prefab;
            //    //ship.Blueprint.Prefab = uvl;
            //    //log.Log("final ship prefab: " + ship.Blueprint.Prefab.AssetId);
            //    return true;

            //}

        }

    }
}

