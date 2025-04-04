using BepInEx;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("net.ovchinikov.nwrework", "Network Rework", "1.0.0")]
public class OwnershipTransferPlugin : BaseUnityPlugin
{

    public static Harmony harmony;
    private void Awake()
    {
        harmony = new Harmony("net.ovchinikov.nwrework");
        harmony.PatchAll();
        Logger.LogInfo("[NetworkRework] Loaded and patched.");
    }
}
