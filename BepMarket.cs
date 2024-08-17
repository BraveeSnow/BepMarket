using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BepMarket;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class BepMarket : BaseUnityPlugin
{
    private const string PLUGIN_GUID = "BraveeSnow.BepMarket";
    private const string PLUGIN_NAME = "BepMarket";
    private const string PLUGIN_VERSION = "1.0";

    private static readonly Harmony harmony = new Harmony(PLUGIN_GUID);
    internal static new ManualLogSource Logger;

    private GameObject mainObject;

    private void Start()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"UH OH, GAME IS SUPERBEPPED!!!");

        // harmony stuff
        harmony.PatchAll(typeof(GameEvents));

        Logger.LogInfo("Patched methods:");
        IEnumerable<MethodBase> methodsPatched = harmony.GetPatchedMethods();

        if (methodsPatched.Count() == 0)
        {
            Logger.LogWarning("No methods were patched!");
        }

        foreach (MethodBase method in methodsPatched)
        {
            Logger.LogInfo($"-> {method.ReflectedType}::{method.Name}");
        }

        // load modded components
        mainObject = new GameObject();
        mainObject.AddComponent<SuperGUI>();
        DontDestroyOnLoad(mainObject);
    }

    private void OnDestroy()
    {
        // clean harmony
        harmony.UnpatchSelf();

        // clean up any external resources here
        Destroy(mainObject);
    }
}
