using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

[RequireComponent(typeof(PhotonView), typeof(Rigidbody), typeof(PhysGrabObject))]
public class OwnershipCollisionMonitor : MonoBehaviour
{

    private float lastTransferTime = 0f;
    private const float transferCooldown = 0.5f;

    private PhotonView view;
    private PhysGrabObject grabObject;
    private bool initialized = false;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
        grabObject = GetComponent<PhysGrabObject>();
        initialized = true;
    }

    private void Update()
    {
        PredictiveOwnershipCheck();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!initialized || view == null || grabObject == null) return;
        TryTakeOwnership(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!initialized || view == null || grabObject == null) return;
        TryTakeOwnership(collision.collider);
    }

    private void TryTakeOwnership(Collider other)
    {
        if (!view.IsMine || grabObject.playerGrabbing.Count <= 0)
            return;

        var otherView = other.GetComponentInParent<PhotonView>();
        var otherGrab = other.GetComponentInParent<PhysGrabObject>();
        if (otherGrab == null) return;

        // genuinely don't know if this is parent or non-parent
        if (otherGrab.GetComponentInParent<Enemy>() != null || otherGrab.GetComponentInParent<EnemyRigidbody>() != null || otherGrab.GetComponent<Enemy>() != null || otherGrab.GetComponent<EnemyRigidbody>() != null)
        {
            Debug.Log("Prevented ownership of enemy");
            return;
        }

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
            var takeoverHelper = otherGrab.GetComponent<OwnershipTakeoverHelper>();
            if (takeoverHelper == null)
            {
                Debug.Log("[PredictiveOwnership] Takeover object wasn't found.");
            }
            otherView.RPC("SyncThisFrame", RpcTarget.All);
            otherView.TransferOwnership(bestOwner);
            Debug.Log("[OwnershipStabilizer] Synced and returned to host.");

            takeoverHelper.BeginMonitoring();
            Debug.Log($"[OwnershipCollisionMonitor] Transferred {otherView.name} to {bestOwner.NickName}");
        }
    }

    private void PredictiveOwnershipCheck()
    {
        if (view == null || !view.IsMine || grabObject == null) return;

        Rigidbody rb = grabObject.rb;
        if (rb == null || rb.velocity.sqrMagnitude < 0.01f) return;

        float predictionTime = 0.2f;
        float speed = rb.velocity.magnitude;
        float predictionDistance = speed * predictionTime;
        Vector3 direction = rb.velocity.normalized;
        Vector3 forwardPoint = transform.position + direction * predictionDistance;

        int fullLayerMask = ~0;
        Collider[] allColliders = grabObject.GetComponentsInChildren<Collider>();

        HashSet<PhysGrabObject> targetsToTransfer = new HashSet<PhysGrabObject>();

        foreach (var col in allColliders)
        {
            if (col == null) continue;

            Vector3 fromPoint = col.ClosestPoint(forwardPoint);
            if (Physics.SphereCast(fromPoint, 0.5f, direction, out RaycastHit hit, predictionDistance + 0.2f, fullLayerMask, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.gameObject == gameObject) continue;

                Transform current = hit.collider.transform;
                PhysGrabObject targetGrab = null;

                // Walk up until we find a PhysGrabObject or PhysGrabHinge
                while (current != null)
                {
                    targetGrab = current.GetComponent<PhysGrabObject>();
                    if (targetGrab != null) break;

                    var hinge = current.GetComponent<PhysGrabHinge>();
                    if (hinge != null)
                    {
                        targetGrab = hinge.GetComponent<PhysGrabObject>();
                        break;
                    }

                    current = current.parent;
                }

                if (targetGrab == null || targetsToTransfer.Contains(targetGrab)) continue;

                // Skip enemies
                if (targetGrab.GetComponentInParent<Enemy>() != null ||
                    targetGrab.GetComponentInParent<EnemyRigidbody>() != null) continue;

                targetsToTransfer.Add(targetGrab);
            }
        }

        foreach (var targetGrab in targetsToTransfer)
        {
            PhotonView targetView = targetGrab.GetComponent<PhotonView>();
            if (targetView == null || targetView.IsMine) continue;

            if (Time.time - lastTransferTime < transferCooldown) continue;
            lastTransferTime = Time.time;

            Photon.Realtime.Player bestOwner = null;
            int bestPing = int.MaxValue;

            if (targetGrab.playerGrabbing.Count > 0)
            {
                foreach (var grabber in targetGrab.playerGrabbing)
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

            if (bestOwner != null && bestOwner.ActorNumber != targetView.OwnerActorNr)
            {
                var takeoverHelper = targetGrab.GetComponent<OwnershipTakeoverHelper>();
                if(takeoverHelper == null)
                {
                    Debug.Log("[PredictiveOwnership] Takeover object wasn't found.");
                    continue;
                }
                targetView.RPC("SyncThisFrame", RpcTarget.All);
                targetView.TransferOwnership(bestOwner);
                Debug.Log("[OwnershipStabilizer] Synced and returned to host.");

                takeoverHelper.MarkGrabbed();
                takeoverHelper.BeginMonitoring();
            }
        }
    }
}