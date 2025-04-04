using Photon.Pun;
using UnityEngine;

public static class OwnershipUtils
{
    public static void SyncAndTransferOwnership(GameObject target, Photon.Realtime.Player newOwner, bool syncImpact = true, bool syncTransform = true)
    {
        if (target == null || newOwner == null) return;

        PhotonView view = target.GetComponent<PhotonView>();
        Rigidbody rb = target.GetComponent<Rigidbody>();
        PhysGrabObject grab = target.GetComponent<PhysGrabObject>();

        if (view == null || rb == null || grab == null) return;

        if (syncImpact)
        {
            var detector = target.GetComponent<PhysGrabObjectImpactDetector>();
            var syncer = target.GetComponent<ImpactSyncHandler>();
            if (syncer != null && detector != null)
            {
                syncer.CaptureFrom(detector);
                syncer.SyncToOthers();
                Debug.Log("[OwnershipUtils] Synced impact data");
            }
        }

        if (syncTransform)
        {
            var ptv = target.GetComponent<PhotonTransformView>();
            if (ptv != null)
            {
                Vector3 velocity = rb.velocity;
                Vector3 direction = velocity * Time.fixedDeltaTime;
                OwnershipTakeoverHelper.SyncImpactIfAvailable(target);
                target.GetComponent<PhotonView>()?.RPC("SyncThisFrame", RpcTarget.All);
                Debug.Log("[OwnershipUtils] Synced transform view");
            }
        }

        if (view.OwnerActorNr != newOwner.ActorNumber)
        {
            view.TransferOwnership(newOwner);
            Debug.Log("[OwnershipUtils] Ownership transferred to: " + newOwner.NickName);
        }
    }
}
