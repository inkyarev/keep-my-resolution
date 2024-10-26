using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
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
    private const string PluginVersion = "1.2.1";
    
    // ReSharper disable memberCanBePrivate.Global
    public static ConfigEntry<string> PreferredResolution;
    public static ConfigEntry<int> PreferredFPSLimit;
    public static ConfigEntry<bool> PreferredDamageNumbers;
    public static ConfigEntry<bool> PreferredExpAndMoneyEffects;
    // ReSharper restore memberCanBePrivate.Global

    private static readonly IEnumerable<string> Cfg = File.ReadAllLines(Path.Combine(Paths.GameRootPath, Paths.ProcessName + "_Data", "Config", "config.cfg"));
    private static IEnumerable<string> ResolutionsList => Screen.resolutions.Select(res => res.ToString()); // this looks bad but trust me it does not affect performance

    public void Awake()
    {
        Log.Init(Logger);

        var resolutionsString = ResolutionsList.Aggregate(string.Empty, (str, res) => str + $"\n{res}");

        #region Gameplay config
        PreferredDamageNumbers = Config.Bind("Gameplay", "Preferred Damage Numbers", FindValueInCfg("enable_damage_numbers").ToBool());
        ModSettingsManager.AddOption(new CheckBoxOption(PreferredDamageNumbers));
        PreferredExpAndMoneyEffects = Config.Bind("Gameplay", "Preferred Exp and Money Effects", FindValueInCfg("exp_and_money_effects").ToBool());
        ModSettingsManager.AddOption(new CheckBoxOption(PreferredExpAndMoneyEffects));
        #endregion

        #region Video config
        PreferredResolution = Config.Bind("Video", "Preferred Resolution", FindValueInCfg("resolution"), $"Available resolutions: {resolutionsString}");
        ModSettingsManager.AddOption(new StringInputFieldOption(PreferredResolution));
        PreferredFPSLimit = Config.Bind("Video", "Preferred FPS Limit", FindValueInCfg("fps_max").ToInt32(), "Can be any positive number.");
        ModSettingsManager.AddOption(new IntFieldOption(PreferredFPSLimit, new IntFieldConfig { Min = 0 }));
        #endregion
        
        
        On.RoR2.ConVar.BaseConVar.AttemptSetString += (orig, self, value) =>
        {
            switch (self.name)
            {
                #region Gameplay
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

                #region Video
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

        var res = RoR2.Console.instance.FindConVar("resolution");
        res?.AttemptSetString(PreferredResolution.Value);
        var fpsMax = RoR2.Console.instance.FindConVar("fps_max");
        fpsMax?.AttemptSetString(PreferredFPSLimit.Value.ToString());
        var dmgNums = RoR2.Console.instance.FindConVar("enable_damage_numbers");
        dmgNums?.AttemptSetString(PreferredDamageNumbers.Value.ToCfgString());
        var capitalism = RoR2.Console.instance.FindConVar("exp_and_money_effects");
        capitalism?.AttemptSetString(PreferredExpAndMoneyEffects.Value.ToCfgString());
    }

    private static string FindValueInCfg(string varName)
    {
        return Cfg.Where(line => line.StartsWith(varName))
            .Select(str => str.Replace($"{varName} ", string.Empty).TrimEnd(';'))
            .First();
    }
}