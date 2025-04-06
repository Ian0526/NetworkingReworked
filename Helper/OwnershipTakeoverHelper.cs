using System.Collections;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(PhotonView), typeof(PhysGrabObject))]
public class OwnershipTakeoverHelper : MonoBehaviourPun
{
    public float syncCheckInterval = 0.15f;
    public float stableTimeRequired = 0.5f;
    public float networkGraceDuration = 0.35f;
    public float grabGracePeriod = 0.35f;

    private Rigidbody rb;
    private PhotonView view;
    private PhysGrabObject grabObject;

    private float stillTime;
    private bool isMonitoring;
    private float graceEndTime = -1f;
    private float lastGrabTime = -1f;
    private int stableGrabberCount = -1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        view = GetComponent<PhotonView>();
        grabObject = GetComponent<PhysGrabObject>();
        view.OwnershipTransfer = OwnershipOption.Takeover;
    }

    [PunRPC]
    public void StartNetworkGracePeriod(float duration)
    {
        graceEndTime = Time.time + duration;
    }

    public bool IsInNetworkGracePeriod() => Time.time < graceEndTime;

    public void MarkGrabbed() => lastGrabTime = Time.time;

    public bool IsInGrabGracePeriod() => Time.time - lastGrabTime <= grabGracePeriod;

    public void BeginMonitoring()
    {
        if (isMonitoring || rb == null || view == null) return;

        isMonitoring = true;
        stillTime = 0f;
        stableGrabberCount = grabObject.playerGrabbing.Count;

        StartCoroutine(MonitorStability());
    }

    private void LateUpdate()
    {
        if (!SemiFunc.IsMultiplayer()) return;
        if (rb == null || view == null) return;
        if (GetComponentInParent<PlayerTumble>() == null)
        {
            rb.isKinematic = !view.IsMine;
        }
    }

    private IEnumerator MonitorStability()
    {
        bool ranOnce = false;

        var hinge = grabObject.GetComponent<PhysGrabHinge>();
        var joint = hinge ? hinge.GetComponent<HingeJoint>() : null;

        float lastJointRot = joint ? joint.angle : -123f;
        Vector3 lastPosition = transform.position;
        bool hadGrabbers = grabObject.playerGrabbing.Count > 0;

        // GetComponentInParent will return the actual instance of the parent if it's the inputted type
        if (GetComponentInParent<Enemy>() != null || GetComponentInParent<EnemyRigidbody>() != null || GetComponent<Enemy>() != null || GetComponent<EnemyRigidbody>() != null)
        {
            var view = GetComponent<PhotonView>();
            if (view != null && !PhotonNetwork.IsMasterClient)
            {
                yield break;
            }
        }

        if (!view.IsMine)
        {
            isMonitoring = false;
            yield break;
        }

        if (hadGrabbers) TransferToBestGrabber();

        while (isMonitoring)
        {
            if (ranOnce) yield return new WaitForSeconds(syncCheckInterval);
            ranOnce = true;

            if (grabObject.playerGrabbing.Count != stableGrabberCount)
            {
                stillTime = 0f;
                stableGrabberCount = grabObject.playerGrabbing.Count;
                continue;
            }

            bool isStill = rb.velocity.sqrMagnitude < 0.005f &&
                           rb.angularVelocity.sqrMagnitude < 0.01f &&
                           Vector3.Distance(transform.position, lastPosition) <= 0.01f;

            if (isStill && joint != null)
            {
                float delta = Mathf.Abs(joint.angle - lastJointRot);
                if (delta > 0.01f) isStill = false;
                lastJointRot = joint.angle;
            }

            if (isStill)
            {
                stillTime += syncCheckInterval;
                if (stillTime >= stableTimeRequired)
                {
                    if (grabObject.playerGrabbing.Count > 0)
                    {
                        TransferToBestGrabber();
                    }
                    else if (view.Owner != PhotonNetwork.MasterClient && view.IsMine)
                    {
                        SyncImpactData();
                        view.RPC("SyncThisFrame", RpcTarget.All);
                        view.TransferOwnership(PhotonNetwork.MasterClient);
                        Debug.Log("[OwnershipStabilizer] Synced and returned to host.");
                    }

                    isMonitoring = false;
                    yield break;
                }
            }
            else
            {
                stillTime = 0f;
                lastPosition = transform.position;
            }
        }
    }

    public void TransferToBestGrabber()
    {
        if (grabObject.playerGrabbing.Count == 0) return;

        var best = grabObject.playerGrabbing[0];
        int bestPing = OwnershipTransferManager.Instance.GetPing(best.photonView.Owner);

        for (int i = 1; i < grabObject.playerGrabbing.Count; i++)
        {
            var g = grabObject.playerGrabbing[i];
            int ping = OwnershipTransferManager.Instance.GetPing(g.photonView.Owner);
            if (ping < bestPing)
            {
                best = g;
                bestPing = ping;
            }
        }

        if (view.OwnerActorNr != best.photonView.OwnerActorNr)
        {
            SyncImpactData();
            view.RPC("SyncThisFrame", RpcTarget.All);
            view.TransferOwnership(best.photonView.OwnerActorNr);
            Debug.Log($"[OwnershipStabilizer] Ownership transferred to: {best.name}");
        }
    }

    public void SyncImpactData()
    {
        var detector = GetComponent<PhysGrabObjectImpactDetector>();
        var syncer = GetComponent<ImpactSyncHandler>();
        if (syncer != null && detector != null)
        {
            syncer.CaptureFrom(detector);
            syncer.SyncToOthers();
            Debug.Log("[OwnershipStabilizer] Synced impact data.");
        }
    }

    [PunRPC]
    public void SyncThisFrame()
    {
        var ptv = GetComponent<PhotonTransformView>();
        var rb = GetComponent<Rigidbody>();

        if (ptv == null || rb == null) return;

        var pos = transform.position;
        var rot = transform.rotation;
        var dir = rb.velocity * Time.fixedDeltaTime;

        PhotonTransformViewUtils.SyncFields(ptv, pos, rot, dir);
        Debug.Log("[OwnershipFix] Synced PhotonTransformView.");
    }

    public static void SyncImpactIfAvailable(GameObject obj)
    {
        var detector = obj.GetComponent<PhysGrabObjectImpactDetector>();
        var syncer = obj.GetComponent<ImpactSyncHandler>();
        if (syncer != null && detector != null)
        {
            syncer.CaptureFrom(detector);
            syncer.SyncToOthers();
            Debug.Log("[OwnershipUtils] Synced impact data.");
        }
    }
}
