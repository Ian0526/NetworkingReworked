using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// For some reason Photon doesn't let you access data that is definitely sent to you
// Like I get you should have no reason to ReadNext(), but 
public static class SerializeReading
{
    private static readonly string HarmonyID = "com.ovchinikov.jank";
    private static bool loadedOnce = false;

    [HarmonyPatch(typeof(RunManager), "Awake")]
    public static class PatchTrigger
    {
        static void Prefix()
        {
            FakeOwnershipData.ClearAll();
            if (PhotonNetwork.IsMasterClient || !SemiFunc.IsMultiplayer() || loadedOnce) return;
            ApplyPatch();
            loadedOnce = true;
        }
    }

    public static void ApplyPatch()
    {
        var target = AccessTools.Method(typeof(PhotonNetwork), "OnSerializeRead");
        var prefix = AccessTools.Method(typeof(SerializeReading), nameof(OnPhotonSerializeRead_Prefix));

        if (target == null || prefix == null)
        {
            return;
        }

        var harmony = new Harmony(HarmonyID);
        harmony.Patch(target, prefix: new HarmonyMethod(prefix));
    }

    static void OnPhotonSerializeRead_Prefix(object[] data, Player sender, int networkTime, short correctPrefix)
    {

        if (data == null || data.Length != 15)
        {
            return;
        }

        if (data[0] is int viewID)
        {

            for (int i = 0; i < data.Length; i++)
            {
                var element = data[i];
            }

            object[] transformChunk = new object[8];
            if (data.Length >= 15 &&
                data[7] is bool &&
                data[8] is bool &&
                data[9] is bool &&
                data[10] is Vector3 &&
                data[11] is Vector3 &&
                data[12] is Vector3 &&
                data[13] is Vector3 &&
                data[14] is Quaternion)
            {
                System.Array.Copy(data, 7, transformChunk, 0, 8);

                PhotonStreamCache.Store(viewID, transformChunk);
            }
        }
    }

    private static PhysGrabHinge isHinge(int pID)
    {
        PhotonView view = PhotonNetwork.GetPhotonView(pID);
        if (view == null) return null;

        var physGrab = view.gameObject.GetComponentInParent<PhysGrabObject>();
        if (physGrab == null) return null;

        var hinge = physGrab.GetComponent<PhysGrabHinge>();
        if (hinge != null) return hinge;
        return null;
    }
}
