using System;
using System.Collections.Generic;
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
    private const string PluginVersion = "1.2.0";
    
    // ReSharper disable memberCanBePrivate.Global
    public static ConfigEntry<string> PreferredResolution;
    public static ConfigEntry<int> PreferredFPSLimit;
    public static ConfigEntry<bool> PreferredDamageNumbers;
    public static ConfigEntry<bool> PreferredExpAndMoneyEffects;
    // ReSharper restore memberCanBePrivate.Global
    private static IEnumerable<string> ResolutionsList => Screen.resolutions.Select(ResolutionToString); // this looks bad but trust me it does not affect performance

    public void Awake()
    {
        Log.Init(Logger);

        var resolutionsString = ResolutionsList.Aggregate(string.Empty, (str, res) => str + $"\n{res}");

        #region Gameplay
        PreferredDamageNumbers = Config.Bind("Gameplay", "Preferred Damage Numbers", true);
        ModSettingsManager.AddOption(new CheckBoxOption(PreferredDamageNumbers));
        PreferredExpAndMoneyEffects = Config.Bind("Gameplay", "Preferred Exp and Money Effects", true);
        ModSettingsManager.AddOption(new CheckBoxOption(PreferredExpAndMoneyEffects));
        #endregion

        #region Video
        PreferredResolution = Config.Bind("Video", "Preferred Resolution", ResolutionToString(Screen.resolutions.Last()), $"Available resolutions: {resolutionsString}");
        ModSettingsManager.AddOption(new StringInputFieldOption(PreferredResolution));
        PreferredFPSLimit = Config.Bind("Video", "Preferred FPS Limit", Screen.resolutions.Last().refreshRate * 2, "Can be any positive number.");
        ModSettingsManager.AddOption(new IntFieldOption(PreferredFPSLimit, new IntFieldConfig { Min = 0 }));
        #endregion
        
        
        On.RoR2.ConVar.BaseConVar.AttemptSetString += (orig, self, value) =>
        {
            switch (self.name)
            {
                #region Gameplay
                case "enable_damage_numbers" when ZeroOneStringToBool(self.GetString()) == PreferredDamageNumbers.Value:
                    return;
                case "enable_damage_numbers":
                    self.SetString(BoolToZeroOneString(PreferredDamageNumbers.Value));
                    return;
                
                case "exp_and_money_effects" when ZeroOneStringToBool(self.GetString()) == PreferredExpAndMoneyEffects.Value:
                    return;
                case "exp_and_money_effects":
                    self.SetString(BoolToZeroOneString(PreferredExpAndMoneyEffects.Value));
                    return;
                #endregion

                #region Video
                case "resolution" when self.GetString() == PreferredResolution.Value:
                case "resolution" when !ResolutionsList.Contains(PreferredResolution.Value):
                    return;
                case "resolution":
                    self.SetString(PreferredResolution.Value);
                    return;
                
                case "fps_max" when Convert.ToInt32(self.GetString()) == PreferredFPSLimit.Value:
                    return;
                case "fps_max":
                    self.SetString(PreferredFPSLimit.Value.ToString());
                    return;
                #endregion
            }

            orig(self, value);
        };
    }

    private static bool ZeroOneStringToBool(string str)
    {
        return Convert.ToBoolean(Convert.ToInt32(str));
    }
    
    private static string BoolToZeroOneString(bool boolean)
    {
        return Convert.ToInt32(boolean).ToString();
    }

    private static string ResolutionToString(Resolution resolution)
    {
        return $"{resolution.width}x{resolution.height}x{resolution.refreshRate}"; // <--- he's not here anymore
    }

    private void FixedUpdate()
    {
        if(RoR2.Console.instance is null) return;

        var res = RoR2.Console.instance.FindConVar("resolution");
        res?.AttemptSetString(PreferredResolution.Value);
        var fpsMax = RoR2.Console.instance.FindConVar("fps_max");
        fpsMax?.AttemptSetString(PreferredFPSLimit.Value.ToString());
        var dmgNums = RoR2.Console.instance.FindConVar("enable_damage_numbers");
        dmgNums?.AttemptSetString(BoolToZeroOneString(PreferredDamageNumbers.Value));
        var capitalism = RoR2.Console.instance.FindConVar("exp_and_money_effects");
        capitalism?.AttemptSetString(BoolToZeroOneString(PreferredExpAndMoneyEffects.Value));
    }
}