using HarmonyLib;
using UnityEngine;

/// <summary>
/// Patches the RunManager class so that after Awake(),
/// we attach an OwnershipTransferManager (which updates host's ping).
/// </summary>
[HarmonyPatch(typeof(RunManager))]
[HarmonyPatch("Awake")]
public static class RunManagerPatch
{
    [HarmonyPostfix]
    public static void Postfix(RunManager __instance)
    {
        Debug.Log("[NoLag] RunManager.Awake() has finished. Doing custom logic...");

        // If not already present, add our manager
        if (!__instance.gameObject.GetComponent<OwnershipTransferManager>())
        {
            var ownership = __instance.gameObject.AddComponent<OwnershipTransferManager>();
            Object.DontDestroyOnLoad(ownership); // keep the manager across scenes
        }
    }
}
