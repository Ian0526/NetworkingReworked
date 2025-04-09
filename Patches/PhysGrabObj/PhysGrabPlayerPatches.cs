using HarmonyLib;
using NetworkingRework.Utils;
using Photon.Pun;
using UnityEngine;

[HarmonyPatch(typeof(PhysGrabObject), "GrabPlayerAddRPC")]
public static class GrabPlayerAddRPC_Patch
{
    [HarmonyPostfix]
    public static void Postfix(PhysGrabObject __instance, int photonViewID)
    {
        if (!SemiFunc.IsMultiplayer() || PhotonNetwork.IsMasterClient) return;

        if (BlockedItems.IsBlockedType(__instance))
        {
            return;
        }

        var fakeOwner = __instance.GetComponent<FakeOwnershipController>();
        if (fakeOwner == null)
        {
            fakeOwner = __instance.gameObject.AddComponent<FakeOwnershipController>();
        }

        fakeOwner.SimulateOwnership();

        if (__instance.TryGetComponent(out PhysGrabCart cart) &&
            __instance.TryGetComponent(out CartOwnershipFixer fixer))
        {
            foreach (var grabber in __instance.playerGrabbing)
            {
                if (grabber.isLocal)
                {
                    fixer.FixInitialPressTimerRPC(grabber.photonView.ViewID);
                    break;
                }
            }
        }
    }
}