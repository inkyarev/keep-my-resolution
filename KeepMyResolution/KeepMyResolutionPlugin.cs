using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.Options;
using UnityEngine;

namespace KeepMyResolution;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class KeepMyResolutionPlugin : BaseUnityPlugin
{
    private const string PluginGUID = PluginAuthor + "." + PluginName;
    private const string PluginAuthor = "InkyaRev";
    private const string PluginName = "KeepMyResolution";
    private const string PluginVersion = "1.0.0";
    
    // ReSharper disable once memberCanBePrivate.Global
    public static ConfigEntry<string> PreferredResolution;
    private static IEnumerable<string> ResolutionsList => Screen.resolutions.Select(ResolutionToString); // this looks bad but trust me it does not affect performance

    public void Awake()
    {
        Log.Init(Logger);

        var resolutionsString = ResolutionsList.Aggregate(string.Empty, (str, res) => str + $"\n{res}");

        PreferredResolution = Config.Bind("Settings", "Preferred Resolution", ResolutionToString(Screen.resolutions.Last()), $"Available resolutions: {resolutionsString}");
        ModSettingsManager.AddOption(new StringInputFieldOption(PreferredResolution));
        
        
        On.RoR2.ConVar.BaseConVar.AttemptSetString += (orig, self, value) =>
        {
            if (self.name == "resolution")
            {
                if(self.GetString() == PreferredResolution.Value) return;
                
                if(!ResolutionsList.Contains(PreferredResolution.Value)) return;
                
                self.SetString(PreferredResolution.Value);
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
    }
}