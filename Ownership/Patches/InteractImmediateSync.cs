using HarmonyLib;
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

        if (__instance.GetComponentInParent<Enemy>() != null ||
            __instance.GetComponentInParent<EnemyRigidbody>() != null ||
            __instance.GetComponent<Enemy>() != null ||
            __instance.GetComponent<EnemyRigidbody>() != null ||
            __instance.GetComponent<PhysGrabHinge>() != null)
        {
            return;
        }

        FakeOwnershipData.AddGrabber(view);

        if (PhotonNetwork.LocalPlayer.ActorNumber == PhotonNetwork.GetPhotonView(photonViewID).OwnerActorNr)
        {
            // auto added with mono
            FakeOwnershipData.AddItemToCart(view);
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

        if (__instance.GetComponentInParent<Enemy>() != null ||
            __instance.GetComponentInParent<EnemyRigidbody>() != null ||
            __instance.GetComponent<Enemy>() != null ||
            __instance.GetComponent<EnemyRigidbody>() != null ||
            __instance.GetComponent<PhysGrabHinge>() != null)
        {
            return;
        }

        FakeOwnershipData.RemoveGrabber(view);

        if (PhotonNetwork.LocalPlayer.ActorNumber == PhotonNetwork.GetPhotonView(photonViewID).OwnerActorNr)
        {
            // auto added with mono
            FakeOwnershipData.RemoveItemFromCart(view);
            FakeOwnershipData.SetRecentlyThrown(view);
            FakeOwnershipData.SetLocallyGrabbed(view, false);
        }
    }
}
