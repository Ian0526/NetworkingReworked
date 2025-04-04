using HarmonyLib;
using Photon.Pun;
using UnityEngine;

[HarmonyPatch(typeof(PhysGrabObject), "GrabPlayerAddRPC")]
public static class GrabBeginPatch
{
    [HarmonyPostfix]
    public static void Postfix(PhysGrabObject __instance, int photonViewID)
    {
        var helper = __instance.GetComponent<OwnershipTakeoverHelper>();
        if (helper != null)
        {
            helper.MarkGrabbed();
            helper.GetComponent<PhotonView>()?.RPC("StartNetworkGracePeriod", RpcTarget.All, helper.networkGraceDuration);
            helper.BeginMonitoring();
        }
    }
}

[HarmonyPatch(typeof(PhysGrabObject), "GrabPlayerRemoveRPC")]
public static class GrabEndPatch
{
    [HarmonyPostfix]
    public static void Postfix(PhysGrabObject __instance, int photonViewID)
    {
        var helper = __instance.GetComponent<OwnershipTakeoverHelper>();
        if (helper != null)
        {
            helper.BeginMonitoring();
        }
    }
}
