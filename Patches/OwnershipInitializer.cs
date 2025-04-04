using HarmonyLib;
using NoLag.Physics;
using Photon.Pun;
using UnityEngine;

[HarmonyPatch(typeof(PhysGrabObject), "Start")]
public static class OwnershipInitializer
{
    [HarmonyPostfix]
    public static void Postfix(PhysGrabObject __instance)
    {
        PhotonNetwork.LogLevel = PunLogLevel.Full;
        if (__instance.GetComponent<OwnershipTakeoverHelper>() == null)
        {
            __instance.gameObject.AddComponent<OwnershipCollisionMonitor>();
            __instance.gameObject.AddComponent<OwnershipTakeoverHelper>();
        }
    }
}

[HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "Start")]
public static class ImpactSyncInitializer
{
    [HarmonyPostfix]
    public static void Postfix(PhysGrabObjectImpactDetector __instance)
    {
        if (__instance.GetComponent<ImpactSyncHandler>() == null)
        {
            __instance.gameObject.AddComponent<ImpactSyncHandler>();
        }
    }
}

[HarmonyPatch(typeof(PhysGrabHinge), "Awake")]
public static class HingeRPCInitializer
{
    [HarmonyPostfix]
    public static void Postfix(PhysGrabHinge __instance)
    {
        if (__instance.GetComponent<HingeRPCs>() == null)
        {
            __instance.gameObject.AddComponent<HingeRPCs>();
        }
    }
}
