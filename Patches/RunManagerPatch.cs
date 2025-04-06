using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(RunManager))]
[HarmonyPatch("Awake")]
public static class RunManagerPatch
{
    [HarmonyPostfix]
    public static void Postfix(RunManager __instance)
    {
        Debug.Log("[NetworkingReworked] RunManager.Awake() has finished. Doing custom logic...");

        if (!__instance.gameObject.GetComponent<OwnershipTransferManager>())
        {
            var ownership = __instance.gameObject.AddComponent<OwnershipTransferManager>();
            Object.DontDestroyOnLoad(ownership); 
        }
    }
}
