using HarmonyLib;
using Photon.Pun;

[HarmonyPatch]
public static class GrabAddRemovePatches
{
    [HarmonyPatch(typeof(PhysGrabObject), "GrabPlayerAddRPC")]
    [HarmonyPostfix]
    public static void OnGrabPlayerAdd(PhysGrabObject __instance, int photonViewID)
    {
        var stabilizer = __instance.GetComponent<OwnershipTakeoverHelper>();
        if (stabilizer != null)
        {
            stabilizer.MarkGrabbed();
            stabilizer.GetComponent<PhotonView>().RPC("StartNetworkGracePeriod", RpcTarget.All, stabilizer.networkGraceDuration);
            stabilizer.BeginMonitoring();
        }
    }

    [HarmonyPatch(typeof(PhysGrabObject), "GrabPlayerRemoveRPC")]
    [HarmonyPostfix]
    public static void OnGrabPlayerRemove(PhysGrabObject __instance, int photonViewID)
    {
        var stabilizer = __instance.GetComponent<OwnershipTakeoverHelper>();
        if (stabilizer != null)
        {
            stabilizer.BeginMonitoring();
        }
    }
}
