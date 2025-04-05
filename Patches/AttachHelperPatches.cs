using HarmonyLib;
using UnityEngine;
using Photon.Pun;
using NoLag.Physics;

[HarmonyPatch]
public static class AttachHelperPatches
{
    [HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "Start")]
    [HarmonyPostfix]
    public static void AttachImpactSync(PhysGrabObjectImpactDetector __instance)
    {
        if (!__instance.TryGetComponent(out ImpactSyncHandler _))
        {
            __instance.gameObject.AddComponent<ImpactSyncHandler>();
        }
    }

    [HarmonyPatch(typeof(PhysGrabHinge), "Awake")]
    [HarmonyPostfix]
    public static void AttachHingeRPC(PhysGrabHinge __instance)
    {
        if (!__instance.TryGetComponent(out HingeRPCs _))
        {
            __instance.gameObject.AddComponent<HingeRPCs>();
        }
    }
}
