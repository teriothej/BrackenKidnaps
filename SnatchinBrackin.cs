﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SnatchinBracken.Patches;
using SnatchinBracken.Patches.data;
using RuntimeNetcodeRPCValidator;
using SnatchingBracken.Patches.network;
using GameNetcodeStuff;
using SnatchingBracken;
using SnatchingBracken.Patches.dungeon;
using System;
using System.Linq;

namespace SnatchinBracken
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("NicholaScott.BepInEx.RuntimeNetcodeRPCValidator", BepInDependency.DependencyFlags.HardDependency)]
    public class SnatchinBrackenBase : BaseUnityPlugin
    {
        private const string modGUID = "Ovchinikov.SnatchinBracken.Main";
        private const string modName = "SnatchinBracken";
        private const string modVersion = "1.3.9";

        private static SnatchinBrackenBase _instance;
        public static SnatchinBrackenBase Instance
        {
            get { return _instance; }
        }

        private readonly Harmony harmony = new Harmony(modGUID);

        private static SnatchinBrackenBase instance;

        private NetcodeValidator netcodeValidator;

        internal ManualLogSource mls;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }

            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(this);
                return;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            mls.LogInfo("Enabling SnatchinBracken");

            InitializeConfigValues();

            harmony.PatchAll(typeof(SnatchinBrackenBase));
            harmony.PatchAll(typeof(BrackenAIPatch));
            harmony.PatchAll(typeof(EnemyAIPatch));
            harmony.PatchAll(typeof(TeleporterPatch));
            harmony.PatchAll(typeof(LandminePatch));
            harmony.PatchAll(typeof(TurretPatch));
            harmony.PatchAll(typeof(PlayerPatch));
            harmony.PatchAll(typeof(DungeonGenPatch));
            harmony.PatchAll(typeof(StartOfRound));

            netcodeValidator = new NetcodeValidator(modGUID);
            netcodeValidator.PatchAll();
            netcodeValidator.BindToPreExistingObjectByBehaviour<FlowermanBinding, PlayerControllerB>();

            mls.LogInfo("Finished Enabling SnatchinBracken");
        }

        private void InitializeConfigValues()
        {
            mls.LogInfo("Parsing SnatchinBracken config");

            bool isLethalConfigAvailable = AppDomain.CurrentDomain.GetAssemblies()
                .Any(assembly => assembly.GetName().Name.Equals("LethalConfig"));

            if (isLethalConfigAvailable)
            {
                mls.LogInfo("Found LethalConfigAPI, let's use that.");
                LethalConfigAPIHook.InitializeConfig();
            }
            else
            {
                mls.LogInfo("LethalConfigAPI not found, using built-in BepInEx config stuff.");
                // Should players drop items on grab
                ConfigEntry<bool> dropItemsOption = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Drop Items on Snatch", true, "Should players drop their items when a Bracken grabs them?");
                SharedData.Instance.DropItems = dropItemsOption.Value;
                dropItemsOption.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        SharedData.Instance.DropItems = dropItemsOption.Value;
                    }
                };

                // Should players be ignored from Turrets
                ConfigEntry<bool> turretOption = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Ignore Turrets on Snatch", true, "Should players be ignored by turrets when dragged?");
                SharedData.Instance.IgnoreTurrets = turretOption.Value;
                turretOption.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        SharedData.Instance.IgnoreTurrets = turretOption.Value;
                    }
                };

                // Should Brackens behave more naturally, meaning faster, more chaotic deaths
                ConfigEntry<bool> chaoticOption = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Brackens Behave More Naturally", false, "If enabled, Brackens will perform kills at unpredictable times after an initial drop. Otherwise, the Bracken either must be in distance of the favorite location, or hit the time limit.");
                SharedData.Instance.ChaoticTendencies = chaoticOption.Value;
                chaoticOption.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        SharedData.Instance.ChaoticTendencies = chaoticOption.Value;
                    }
                };

                // Should people be able to teleported if they're being dragged?
                ConfigEntry<bool> allowDraggedTps = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Allow teleports to save dragged players", true, "Should players be able to be saved through teleportation?");
                SharedData.Instance.AllowTeleports = allowDraggedTps.Value;
                allowDraggedTps.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        SharedData.Instance.AllowTeleports = allowDraggedTps.Value;
                    }
                };

                // Players
                ConfigEntry<bool> monstersIgnorePlayersOption = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Enemies Ignore Dragged Players", true, "Should players be ignored by other monsters while being dragged?");
                SharedData.Instance.monstersIgnorePlayers = monstersIgnorePlayersOption.Value;
                monstersIgnorePlayersOption.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        SharedData.Instance.monstersIgnorePlayers = monstersIgnorePlayersOption.Value;
                    }
                };

                ConfigEntry<bool> stuckForceKillOption = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Stuck Force Kill", false, "If enabled, Brackens will force kill when stuck at the same spot for at least 5 seconds.");
                SharedData.Instance.StuckForceKill = stuckForceKillOption.Value;

                stuckForceKillOption.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        SharedData.Instance.StuckForceKill = stuckForceKillOption.Value;
                    }
                };

                ConfigEntry<bool> brackenRoomOption = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Force Set Favorite Location To Bracken Room", true, "If enabled, Brackens' favorite locations will be set to the Bracken room. The room sometimes doesn't spawn, so please don't be alarmed if they don't take you there if this is enabled.");
                SharedData.Instance.BrackenRoom = brackenRoomOption.Value;

                brackenRoomOption.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        SharedData.Instance.BrackenRoom = brackenRoomOption.Value;
                    }
                };

                // Should players ignore Landmines
                ConfigEntry<bool> mineOption = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Ignore Mines on Snatch", true, "Should players ignore Landmines while being dragged?");
                SharedData.Instance.IgnoreMines = mineOption.Value;
                mineOption.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        SharedData.Instance.IgnoreMines = mineOption.Value;
                    }
                };

                // Slider for seconds until Bracken automatically kills when grabbed
                ConfigEntry<int> instaKillTimeEntry = ((BaseUnityPlugin)this).Config.Bind<int>("SnatchinBracken Settings", "Chance for Insta Kill", 0, "Percent chance for insta kill, 0 to disable.");
                SharedData.Instance.PercentChanceForInsta = instaKillTimeEntry.Value;
                instaKillTimeEntry.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        SharedData.Instance.PercentChanceForInsta = instaKillTimeEntry.Value;
                    }
                };

                // Slider for seconds until Bracken automatically kills when grabbed
                ConfigEntry<int> brackenKillTimeEntry = ((BaseUnityPlugin)this).Config.Bind<int>("SnatchinBracken Settings", "Seconds Until Auto Kill", 15, "Time in seconds until Bracken automatically kills when grabbed. Range: 1-60 seconds.");
                SharedData.Instance.KillAtTime = brackenKillTimeEntry.Value;
                brackenKillTimeEntry.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        SharedData.Instance.KillAtTime = brackenKillTimeEntry.Value;
                    }
                };

                // Slider for seconds until Bracken can try to attack another person after dropping/being hit
                ConfigEntry<int> brackenNextAttemptEntry = ((BaseUnityPlugin)this).Config.Bind<int>("SnatchinBracken Settings", "Seconds Until Next Attempt", 5, "Time in seconds until Bracken is allowed to take another victim.");
                SharedData.Instance.SecondsBeforeNextAttempt = brackenNextAttemptEntry.Value;
                brackenNextAttemptEntry.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        SharedData.Instance.SecondsBeforeNextAttempt = brackenNextAttemptEntry.Value;
                    }
                };

                // Should Brackens deal damage over time instead of abruptly killing them after they reach a spot?
                ConfigEntry<bool> doDamageOnIntervalEntry = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Do Gradual Damage", false, "Should players be hurt gradually while being dragged?");
                SharedData.Instance.DoDamageOnInterval = doDamageOnIntervalEntry.Value;
                doDamageOnIntervalEntry.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        SharedData.Instance.DoDamageOnInterval = doDamageOnIntervalEntry.Value;
                    }
                };

                // Time required for above entry
                ConfigEntry<int> damageDealtProgressively = ((BaseUnityPlugin)this).Config.Bind<int>("SnatchinBracken Settings", "Damage Dealt At Interval", 5, "This only applies if you have \"Do Gradual Damage\" enabled. While dragged, every second this configured amount of damage will be dealt to the player.");
                SharedData.Instance.DamageDealtAtInterval = damageDealtProgressively.Value;
                damageDealtProgressively.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        SharedData.Instance.DamageDealtAtInterval = damageDealtProgressively.Value;
                    }
                };

                // Slider for seconds until Bracken can try to attack another person after dropping/being hit
                ConfigEntry<int> distanceAutoKillerEntry = ((BaseUnityPlugin)this).Config.Bind<int>("SnatchinBracken Settings", "Distance For Kill", 1, "How far should the Bracken be from its favorite spot to initiate a kill?");
                SharedData.Instance.DistanceFromFavorite = distanceAutoKillerEntry.Value;
                distanceAutoKillerEntry.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        SharedData.Instance.DistanceFromFavorite = distanceAutoKillerEntry.Value;
                    }
                };

                // Should the Bracken instakill if the player is alone
                ConfigEntry<bool> instaKillOption = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Instakill When Alone", false, "Should players be instantly killed if they're alone?");
                SharedData.Instance.InstantKillIfAlone = instaKillOption.Value;
                instaKillOption.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        SharedData.Instance.InstantKillIfAlone = instaKillOption.Value;
                    }
                };
            }

            mls.LogInfo("Config finished parsing");
        }
    }
}