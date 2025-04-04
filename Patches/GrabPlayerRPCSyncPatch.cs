using HarmonyLib;
using Photon.Pun;
using UnityEngine;

[HarmonyPatch(typeof(PhysGrabObject), "GrabPlayerAddRPC")]
public static class GrabPlayerAddRPC_Patch
{
    [HarmonyPostfix]
    public static void Postfix(PhysGrabObject __instance, int photonViewID)
    {
        var helper = __instance.GetComponent<OwnershipTakeoverHelper>();
        if (helper != null)
        {
            helper.MarkGrabbed();
            __instance.GetComponent<PhotonView>().RPC("StartNetworkGracePeriod", RpcTarget.All, helper.networkGraceDuration);
            helper.BeginMonitoring();
        }

        // === Fix cart grab timing sync ===
        if (__instance.TryGetComponent(out PhysGrabCart cart) &&
            __instance.TryGetComponent(out CartOwnershipFixer fixer))
        {
            foreach (var grabber in __instance.playerGrabbing)
            {
                if (grabber.isLocal)
                {
                    fixer.photonView.RPC("FixInitialPressTimerRPC", RpcTarget.Others, grabber.photonView.ViewID);
                    Debug.Log($"[CartOwnershipFixer] Sent FixInitialPressTimerRPC to ViewID {grabber.photonView.ViewID} via fallback patch.");
                    break;
                }
            }
        }
    }
}

[HarmonyPatch(typeof(PhysGrabObject), "GrabPlayerRemoveRPC")]
public static class GrabPlayerRemoveRPC_Patch
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
