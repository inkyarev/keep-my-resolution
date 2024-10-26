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
    // ReSharper restore memberCanBePrivate.Global
    private static IEnumerable<string> ResolutionsList => Screen.resolutions.Select(ResolutionToString); // this looks bad but trust me it does not affect performance

    public void Awake()
    {
        Log.Init(Logger);

        var resolutionsString = ResolutionsList.Aggregate(string.Empty, (str, res) => str + $"\n{res}");

        PreferredResolution = Config.Bind("Video", "Preferred Resolution", ResolutionToString(Screen.resolutions.Last()), $"Available resolutions: {resolutionsString}");
        ModSettingsManager.AddOption(new StringInputFieldOption(PreferredResolution));
        PreferredFPSLimit = Config.Bind("Video", "Preferred FPS Limit", Screen.resolutions.Last().refreshRate, "Can be any positive number.");
        ModSettingsManager.AddOption(new IntFieldOption(PreferredFPSLimit, new IntFieldConfig { Min = 0 }));
        
        On.RoR2.ConVar.BaseConVar.AttemptSetString += (orig, self, value) =>
        {
            switch (self.name)
            {
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
            }

            orig(self, value);
        };
    }

    private static string ResolutionToString(Resolution resolution)
    {
        return $"{resolution.width}x{resolution.height}x{resolution.refreshRate}".Trim();
    }

    private void FixedUpdate()
    {
        if(RoR2.Console.instance is null) return;

        var res = RoR2.Console.instance.FindConVar("resolution");
        res?.AttemptSetString(PreferredResolution.Value);
        var fpsMax = RoR2.Console.instance.FindConVar("fps_max");
        fpsMax?.AttemptSetString(PreferredFPSLimit.Value.ToString());
    }
}