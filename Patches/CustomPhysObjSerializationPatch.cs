using HarmonyLib;
using Photon.Pun;
using UnityEngine;

[HarmonyPatch(typeof(PhysGrabObject), "OnPhotonSerializeView")]
public static class CustomPhysGrabObjectSerializationPatch
{
    static bool Prefix(PhysGrabObject __instance, PhotonStream stream, PhotonMessageInfo info)
    {
        if (!SemiFunc.IsMultiplayer()) return true;
        var view = __instance.GetComponent<PhotonView>();

        var t = Traverse.Create(__instance);
        var tVelocity = t.Field("rbVelocity");
        var tAngularVelocity = t.Field("rbAngularVelocity");
        var tIsKinematic = t.Field("isKinematic");
        var tLastUpdateTime = t.Field("lastUpdateTime");

        var detector = __instance.GetComponent<PhysGrabObjectImpactDetector>();
        if (detector == null)
        {
            detector = __instance.GetComponent<PhysGrabObjectImpactDetector>();
            t.Field("impactDetector").SetValue(detector);
        }

        try
        {
            if (stream.IsWriting)
            {
                if (__instance.rb == null)
                    __instance.rb = __instance.GetComponent<Rigidbody>();

                stream.SendNext(tVelocity.GetValue<Vector3>());
                stream.SendNext(tAngularVelocity.GetValue<Vector3>());
                stream.SendNext(detector.isSliding);
                stream.SendNext(tIsKinematic.GetValue<bool>());
            }
            else
            {
                var velocity = (Vector3)stream.ReceiveNext();
                var angularVelocity = (Vector3)stream.ReceiveNext();
                var isSliding = (bool)stream.ReceiveNext();
                var isKinematic = (bool)stream.ReceiveNext();

                tVelocity.SetValue(velocity);
                tAngularVelocity.SetValue(angularVelocity);
                detector.isSliding = isSliding;
                tIsKinematic.SetValue(isKinematic);
                tLastUpdateTime.SetValue(Time.time);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[NetworkingReworked] SerializationPatch error: " + ex);
            return true;
        }

        return false;
    }
}
