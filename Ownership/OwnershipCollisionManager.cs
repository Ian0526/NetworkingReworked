using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView), typeof(Rigidbody), typeof(PhysGrabObject))]
public class OwnershipCollisionMonitor : MonoBehaviour
{

    private float lastTransferTime = 0f;
    private const float transferCooldown = 0.5f;

    private PhotonView view;
    private PhysGrabObject grabObject;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
        grabObject = GetComponent<PhysGrabObject>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryTakeOwnership(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryTakeOwnership(collision.collider);
    }

    private void TryTakeOwnership(Collider other)
    {
        if (!view.IsMine || grabObject.playerGrabbing.Count <= 0)
            return;

        var otherView = other.GetComponentInParent<PhotonView>();
        var otherGrab = other.GetComponentInParent<PhysGrabObject>();

        if (otherView == null || otherGrab == null || otherView.IsMine)
            return;

        if (Time.time - lastTransferTime < transferCooldown)
            return;

        lastTransferTime = Time.time;

        if (otherView == null || otherGrab == null || otherView.IsMine)
        {
            return;
        }

        Photon.Realtime.Player bestOwner = null;
        int bestPing = int.MaxValue;

        if (otherGrab.playerGrabbing.Count > 0)
        {
            foreach (var grabber in otherGrab.playerGrabbing)
            {
                var grabberPlayer = grabber.photonView.Owner;
                int ping = OwnershipTransferManager.Instance.GetPing(grabberPlayer);
                if (ping < bestPing)
                {
                    bestPing = ping;
                    bestOwner = grabberPlayer;
                }
            }
        }
        else
        {
            bestOwner = view.Owner;
        }

        if (bestOwner != null && bestOwner.ActorNumber != otherView.OwnerActorNr)
        {
            OwnershipTakeoverHelper.SyncImpactIfAvailable(otherGrab.gameObject);
            otherView.TransferOwnership(bestOwner);
            Debug.Log($"[OwnershipCollisionMonitor] Transferred {otherView.name} to {bestOwner.NickName}");
        }
    }
}