using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using JetBrains.Annotations;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using UnityEngine;

namespace KeepMySettings;

[BepInDependency("com.rune580.riskofoptions")]
[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class KeepMySettingsPlugin : BaseUnityPlugin
{
    private const string PluginGUID = PluginAuthor + "." + PluginName;
    private const string PluginAuthor = "InkyaRev";
    private const string PluginName = "KeepMySettings";
    private const string PluginVersion = "1.4.0";
    
    // ReSharper disable memberCanBePrivate.Global
    public static ConfigEntry<bool> GameplayModuleEnabled;
    public static ConfigEntry<bool> PreferredDamageNumbers;
    public static ConfigEntry<bool> PreferredExpAndMoneyEffects;

    public static ConfigEntry<bool> AudioModuleEnabled;
    public static ConfigEntry<float> PreferredMasterVolume;
    public static ConfigEntry<float> PreferredSFXVolume;
    public static ConfigEntry<float> PreferredMusicVolume;
    
    public static ConfigEntry<bool> VideoModuleEnabled;
    public static ConfigEntry<string> PreferredResolution;
    public static ConfigEntry<int> PreferredFPSLimit;
    // ReSharper restore memberCanBePrivate.Global

    private static readonly IEnumerable<string> Cfg = File.ReadAllLines(Path.Combine(Paths.GameRootPath, Paths.ProcessName + "_Data", "Config", "config.cfg"));
    private static IEnumerable<string> ResolutionsList => Screen.resolutions.Select(res => res.ToCfgString()); // this looks bad but trust me it does not affect performance

    public void Awake()
    {
        Log.Init(Logger);

        var resolutionsString = ResolutionsList.Aggregate(string.Empty, (str, res) => str + $"\n{res}");

        #region Gameplay config
        GameplayModuleEnabled = Config.Bind("Gameplay", "Enabled", false, "Enables gameplay module.");
        ModSettingsManager.AddOption(new CheckBoxOption(GameplayModuleEnabled));
        var dmgNumsCfg = FindValueInCfg("enable_damage_numbers") ?? "1";
        PreferredDamageNumbers = Config.Bind("Gameplay", "Preferred Damage Numbers", dmgNumsCfg.ToBool());
        ModSettingsManager.AddOption(new CheckBoxOption(PreferredDamageNumbers));
        var capitalismCfg = FindValueInCfg("exp_and_money_effects") ?? "1";
        PreferredExpAndMoneyEffects = Config.Bind("Gameplay", "Preferred Exp and Money Effects", capitalismCfg.ToBool());
        ModSettingsManager.AddOption(new CheckBoxOption(PreferredExpAndMoneyEffects));
        #endregion

        #region Audio config
        AudioModuleEnabled = Config.Bind("Audio", "Enabled", false, "Enables audio module.");
        ModSettingsManager.AddOption(new CheckBoxOption(AudioModuleEnabled));
        var masterCfg = FindValueInCfg("volume_master") ?? "100";
        PreferredMasterVolume = Config.Bind("Audio", "Preferred Master Volume", masterCfg.ToSingle());
        ModSettingsManager.AddOption(new SliderOption(PreferredMasterVolume, new SliderConfig { FormatString = "{0:N0}%" }));
        var sfxCfg = FindValueInCfg("volume_sfx") ?? "100";
        PreferredSFXVolume = Config.Bind("Audio", "Preferred SFX Volume", sfxCfg.ToSingle());
        ModSettingsManager.AddOption(new SliderOption(PreferredSFXVolume, new SliderConfig { FormatString = "{0:N0}%" }));
        var musicCfg = FindValueInCfg("parent_volume_music") ?? "100";
        PreferredMusicVolume = Config.Bind("Audio", "Preferred Music Volume", musicCfg.ToSingle());
        ModSettingsManager.AddOption(new SliderOption(PreferredMusicVolume, new SliderConfig { FormatString = "{0:N0}%" }));
        #endregion

        #region Video config
        VideoModuleEnabled = Config.Bind("Video", "Enabled", false, "Enables video module.");
        ModSettingsManager.AddOption(new CheckBoxOption(VideoModuleEnabled));
        var resCfg = FindValueInCfg("resolution") ?? Screen.currentResolution.ToCfgString();
        PreferredResolution = Config.Bind("Video", "Preferred Resolution", resCfg, $"Available resolutions: {resolutionsString}");
        ModSettingsManager.AddOption(new StringInputFieldOption(PreferredResolution));
        var fpsMaxCfg = FindValueInCfg("fps_max") ?? (Screen.currentResolution.refreshRate * 2).ToString();
        PreferredFPSLimit = Config.Bind("Video", "Preferred FPS Limit", fpsMaxCfg.ToInt32(), "Can be any positive number.");
        ModSettingsManager.AddOption(new IntFieldOption(PreferredFPSLimit, new IntFieldConfig { Min = 0 }));
        #endregion
        
        
        On.RoR2.ConVar.BaseConVar.AttemptSetString += (orig, self, value) =>
        {
            switch (self.name)
            {
                #region Gameplay
                case "enable_damage_numbers" when !GameplayModuleEnabled.Value:
                case "exp_and_money_effects" when !GameplayModuleEnabled.Value:
                    break;
                
                case "enable_damage_numbers" when self.GetString().ToBool() == PreferredDamageNumbers.Value:
                    return;
                case "enable_damage_numbers":
                    self.SetString(PreferredDamageNumbers.Value.ToCfgString());
                    return;
                
                case "exp_and_money_effects" when self.GetString().ToBool() == PreferredExpAndMoneyEffects.Value:
                    return;
                case "exp_and_money_effects":
                    self.SetString(PreferredExpAndMoneyEffects.Value.ToCfgString());
                    return;
                #endregion

                #region Audio
                case "volume_master" when !AudioModuleEnabled.Value:
                case "volume_sfx" when !AudioModuleEnabled.Value:
                case "parent_volume_music" when !AudioModuleEnabled.Value:
                    break;
                
                case "volume_master" when Mathf.Approximately(self.GetString().ToSingle(), PreferredMasterVolume.Value):
                    return;
                case "volume_master":
                    self.SetString(PreferredMasterVolume.Value.ToString(CultureInfo.InvariantCulture));
                    return;
                
                case "volume_sfx" when Mathf.Approximately(self.GetString().ToSingle(), PreferredSFXVolume.Value):
                    return;
                case "volume_sfx":
                    self.SetString(PreferredSFXVolume.Value.ToString(CultureInfo.InvariantCulture));
                    return;
                
                case "parent_volume_music" when Mathf.Approximately(self.GetString().ToSingle(), PreferredMusicVolume.Value):
                    return;
                case "parent_volume_music":
                    self.SetString(PreferredMusicVolume.Value.ToString(CultureInfo.InvariantCulture));
                    return;
                #endregion

                #region Video
                case "resolution" when !VideoModuleEnabled.Value:
                case "fps_max" when !VideoModuleEnabled.Value:
                    break;
                
                case "resolution" when self.GetString() == PreferredResolution.Value:
                case "resolution" when !ResolutionsList.Contains(PreferredResolution.Value):
                    return;
                case "resolution":
                    self.SetString(PreferredResolution.Value);
                    return;
                
                case "fps_max" when self.GetString().ToInt32() == PreferredFPSLimit.Value:
                    return;
                case "fps_max":
                    self.SetString(PreferredFPSLimit.Value.ToString());
                    return;
                #endregion
            }

            orig(self, value);
        };
    }

    private void FixedUpdate()
    {
        if(RoR2.Console.instance is null) return;

        #region Gameplay
        if (GameplayModuleEnabled.Value)
        {
            var dmgNums = RoR2.Console.instance.FindConVar("enable_damage_numbers");
            dmgNums?.AttemptSetString(PreferredDamageNumbers.Value.ToCfgString());
        
            var capitalism = RoR2.Console.instance.FindConVar("exp_and_money_effects");
            capitalism?.AttemptSetString(PreferredExpAndMoneyEffects.Value.ToCfgString());
        }
        #endregion

        #region Audio
        if (AudioModuleEnabled.Value)
        {
            var master = RoR2.Console.instance.FindConVar("volume_master");
            master?.AttemptSetString(PreferredResolution.Value);
        
            var sfx = RoR2.Console.instance.FindConVar("volume_sfx");
            sfx?.AttemptSetString(PreferredResolution.Value);
        
            var music = RoR2.Console.instance.FindConVar("parent_volume_music");
            music?.AttemptSetString(PreferredResolution.Value);
        }
        #endregion
        
        #region Video
        if (VideoModuleEnabled.Value)
        {
            var res = RoR2.Console.instance.FindConVar("resolution");
            res?.AttemptSetString(PreferredResolution.Value);
        
            var fpsMax = RoR2.Console.instance.FindConVar("fps_max");
            fpsMax?.AttemptSetString(PreferredFPSLimit.Value.ToString());
        }
        #endregion
    }

    [CanBeNull]
    private static string FindValueInCfg(string varName)
    {
        var varNameWithSpace = $"{varName} ";
        return Cfg.Where(line => line.StartsWith(varNameWithSpace))
            .Select(str => str.Replace(varNameWithSpace, string.Empty).TrimEnd(';'))
            .FirstOrDefault();
    }
}