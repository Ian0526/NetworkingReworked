using HarmonyLib;
using Photon.Pun;
using UnityEngine;

[HarmonyPatch(typeof(PhysGrabObject), "OnPhotonSerializeView")]
public static class PhysGrabObjectSerializationPatch
{
    static bool Prefix(PhysGrabObject __instance, PhotonStream stream, PhotonMessageInfo info)
    {
        PhotonView pv = __instance.GetComponent<PhotonView>();

        var tRbVelocity = Traverse.Create(__instance).Field("rbVelocity");
        var tRbAngularVelocity = Traverse.Create(__instance).Field("rbAngularVelocity");
        var tIsKinematic = Traverse.Create(__instance).Field("isKinematic");
        var tLastUpdateTime = Traverse.Create(__instance).Field("lastUpdateTime");

        var impactDetector = __instance.GetComponent<PhysGrabObjectImpactDetector>();
        if (impactDetector == null)
        {
            impactDetector = __instance.GetComponent<PhysGrabObjectImpactDetector>();
            Traverse.Create(__instance).Field("impactDetector").SetValue(impactDetector);
        }

        try
        {
            if (stream.IsWriting)
            {
                if (__instance.rb == null)
                    __instance.rb = __instance.GetComponent<Rigidbody>();

                stream.SendNext(tRbVelocity.GetValue<Vector3>());
                stream.SendNext(tRbAngularVelocity.GetValue<Vector3>());
                stream.SendNext(impactDetector.isSliding);
                stream.SendNext(tIsKinematic.GetValue<bool>());
            }
            else
            {
                Vector3 velocity = (Vector3)stream.ReceiveNext();
                Vector3 angularVelocity = (Vector3)stream.ReceiveNext();
                bool isSliding = (bool)stream.ReceiveNext();
                bool isKinematic = (bool)stream.ReceiveNext();

                tRbVelocity.SetValue(velocity);
                tRbAngularVelocity.SetValue(angularVelocity);
                impactDetector.isSliding = isSliding;
                tIsKinematic.SetValue(isKinematic);
                tLastUpdateTime.SetValue(Time.time);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[NetworkingReworked] SerializeView exception: " + ex);
            return true;
        }

        return false;
    }
}
