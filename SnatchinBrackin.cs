﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SnatchinBracken.Patches;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using SnatchinBracken.Patches.data;
using SnatchingBracken.Patches.test;

namespace SnatchinBracken
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class SnatchinBrackenBase : BaseUnityPlugin
    {
        private const string modGUID = "Ovchinikov.SnatchingBracken.Main";
        private const string modName = "SnatchingBracken";
        private const string modVersion = "1.1.2";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static SnatchinBrackenBase instance;

        internal ManualLogSource mls;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
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
            harmony.PatchAll(typeof(TestCommands));

            mls.LogInfo("Finished Enabling SnatchinBracken");
        }

        private void InitializeConfigValues()
        {
            mls.LogInfo("Parsing SnatchinBracken config");
            LethalConfigManager.SetModDescription("A mod that alters the behavior of the Bracken. The Bracken pulls players into a new spot before performing a kill.");

            // Should players drop items on grab
            ConfigEntry<bool> dropItemsOption = ((BaseUnityPlugin) this).Config.Bind<bool>("SnatchinBracken Settings", "Drop Items on Snatch", true, "Should players drop their items when a Bracken grabs them.");
            BoolCheckBoxConfigItem dropItemsVal = new BoolCheckBoxConfigItem(dropItemsOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem) dropItemsVal);
            SharedData.Instance.DropItems = dropItemsOption.Value;
            dropItemsOption.SettingChanged += delegate
            {
                SharedData.Instance.DropItems = dropItemsOption.Value;
            };

            // Should players be ignored from Turrets
            ConfigEntry<bool> turretOption = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Ignore Turrets on Snatch", true, "Should players be able to be targeted by turrets while being dragged.");
            BoolCheckBoxConfigItem turretVal = new BoolCheckBoxConfigItem(turretOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem) turretVal);
            SharedData.Instance.IgnoreTurrets = turretOption.Value;
            turretOption.SettingChanged += delegate
            {
                SharedData.Instance.IgnoreTurrets = turretOption.Value;
            };

            // Should players ignore Landmines
            ConfigEntry<bool> mineOption = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Ignore Mines on Snatch", true, "Should players ignore Landmines while being dragged.");
            BoolCheckBoxConfigItem mineVal = new BoolCheckBoxConfigItem(mineOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)mineVal);
            SharedData.Instance.IgnoreMines = mineOption.Value;
            mineOption.SettingChanged += delegate
            {
                SharedData.Instance.IgnoreMines = mineOption.Value;
            };

            // Slider for seconds until Bracken automatically kills when grabbed
            ConfigEntry<int> brackenKillTimeEntry = ((BaseUnityPlugin)this).Config.Bind<int>("SnatchinBracken Settings", "Seconds Until Auto Kill", 15, "Time in seconds until Bracken automatically kills when grabbed. Range: 1-60 seconds.");
            IntSliderOptions brackenKillTimeOptions = new IntSliderOptions
            {
                RequiresRestart = false,
                Min = 1,
                Max = 60
            };
            IntSliderConfigItem brackenKillTimeSlider = new IntSliderConfigItem(brackenKillTimeEntry, brackenKillTimeOptions);
            LethalConfigManager.AddConfigItem((BaseConfigItem) brackenKillTimeSlider);
            SharedData.Instance.KillAtTime = brackenKillTimeEntry.Value;
            brackenKillTimeEntry.SettingChanged += delegate
            {
                SharedData.Instance.KillAtTime = brackenKillTimeEntry.Value;
            };

            // Slider for seconds until Bracken automatically kills when grabbed
            ConfigEntry<int> brackenNextAttemptEntry = ((BaseUnityPlugin)this).Config.Bind<int>("SnatchinBracken Settings", "Seconds Until Next Attempt", 5, "Time in seconds until Bracken is allowed to take another victim.");
            IntSliderOptions brackenNextAttemptOptions = new IntSliderOptions
            {
                RequiresRestart = false,
                Min = 1,
                Max = 60
            };
            IntSliderConfigItem brackenNextAttemptySlider = new IntSliderConfigItem(brackenNextAttemptEntry, brackenNextAttemptOptions);
            LethalConfigManager.AddConfigItem((BaseConfigItem)brackenNextAttemptySlider);
            SharedData.Instance.SecondsBeforeNextAttempt = brackenNextAttemptEntry.Value;
            brackenNextAttemptEntry.SettingChanged += delegate
            {
                SharedData.Instance.SecondsBeforeNextAttempt = brackenNextAttemptEntry.Value;
            };

            mls.LogInfo("Config finished parsing");
        }
    }
}
