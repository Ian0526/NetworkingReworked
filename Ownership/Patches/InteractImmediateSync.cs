using HarmonyLib;
using NetworkingRework.Utils;
using Photon.Pun;
using UnityEngine;

// for some reason, grabbed, localGrabbed, and playerGrabbers was not updating in time
// so we gotta make this occur in the proper order
[HarmonyPatch(typeof(PhysGrabObject), "GrabPlayerAddRPC")]
public static class GrabAddHook
{
    static void Prefix(PhysGrabObject __instance, int photonViewID)
    {
        if (PhotonNetwork.IsMasterClient || !SemiFunc.IsMultiplayer()) return;
        var view = __instance.GetComponent<PhotonView>();
        if (view == null) return;

        if (BlockedItems.IsBlockedType(__instance))
        {
            return;
        }

        FakeOwnershipData.AddGrabber(view);
        FakeOwnershipData.SetNetworkGrabbed(view);

        if (PhotonNetwork.LocalPlayer.ActorNumber == PhotonNetwork.GetPhotonView(photonViewID).OwnerActorNr)
        {
            // auto added with mono
            FakeOwnershipData.SetLocallyGrabbed(view, true);
            FakeOwnershipData.SimulateOwnership(view);
        }
    }
}

[HarmonyPatch(typeof(PhysGrabObject), "GrabPlayerRemoveRPC")]
public static class GrabReleaseHook
{
    static void Prefix(PhysGrabObject __instance, int photonViewID)
    {
        if (PhotonNetwork.IsMasterClient || !SemiFunc.IsMultiplayer()) return;
        var view = __instance.GetComponent<PhotonView>();
        if (view == null) return;

        if (BlockedItems.IsBlockedType(__instance))
        {
            return;
        }

        FakeOwnershipData.RemoveGrabber(view);
        FakeOwnershipData.RemoveItemFromCart(view);

        if (PhotonNetwork.LocalPlayer.ActorNumber == PhotonNetwork.GetPhotonView(photonViewID).OwnerActorNr)
        {
            // auto added with mono
            FakeOwnershipData.SetRecentlyThrown(view);
            FakeOwnershipData.SetLocallyGrabbed(view, false);

            var ownershipController = __instance.GetComponent<FakeOwnershipController>();
            if (ownershipController != null)
            {
                //ownershipController.HardSyncFromThrow();
            }
        }
    }
}
