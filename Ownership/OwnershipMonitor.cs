using Photon.Pun;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PhotonView), typeof(Rigidbody), typeof(PhysGrabObject))]
public class OwnershipMonitor : MonoBehaviourPun
{
    public float syncCheckInterval = 0.15f;
    public float stableTimeRequired = 0.5f;

    private Rigidbody rb;
    private PhotonView view;
    private PhysGrabObject grabObject;

    private float stillTime;
    private bool isMonitoring;
    private bool isCart;
    private int stableGrabberCount = -1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        view = GetComponent<PhotonView>();
        grabObject = GetComponent<PhysGrabObject>();
        isCart = GetComponent<PhysGrabCart>() != null;
        view.OwnershipTransfer = OwnershipOption.Takeover;
    }

    public void BeginMonitoring()
    {
        if (isMonitoring || rb == null || view == null) return;

        isMonitoring = true;
        stillTime = 0f;
        stableGrabberCount = grabObject?.playerGrabbing?.Count ?? -1;

        StartCoroutine(MonitorStability());
    }

    private IEnumerator MonitorStability()
    {
        bool ranOnce = false;

        var hinge = grabObject.GetComponent<PhysGrabHinge>();
        var joint = hinge ? hinge.GetComponent<HingeJoint>() : null;
        var takeOverHelper = grabObject.GetComponent<OwnershipTakeoverHelper>();

        float lastJointRot = joint ? joint.angle : -123f;
        Vector3 lastPosition = transform.position;
        bool hadGrabbers = grabObject.playerGrabbing.Count > 0;

        if (isCart)
        {
            syncCheckInterval = 0.25f;
            stableTimeRequired = 1.0f;
        }

        if (!view.IsMine)
        {
            isMonitoring = false;
            yield break;
        }

        if (hadGrabbers)
        {
            takeOverHelper.TransferToBestGrabber();
        }

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
                           rb.angularVelocity.sqrMagnitude < 0.01f;

            if (Vector3.Distance(transform.position, lastPosition) > 0.01f)
                isStill = false;

            if (isStill && joint != null && Mathf.Abs(joint.angle - lastJointRot) > 0.01f)
                isStill = false;

            lastJointRot = joint?.angle ?? lastJointRot;

            if (isStill)
            {
                stillTime += syncCheckInterval;
                if (stillTime >= stableTimeRequired)
                {
                    if (grabObject.playerGrabbing.Count > 0)
                    {
                        takeOverHelper.TransferToBestGrabber();
                    }
                    else if (view.Owner != PhotonNetwork.MasterClient && view.IsMine)
                    {
                        takeOverHelper.SyncImpactData();
                        view.RPC("SyncThisFrame", RpcTarget.All);
                        view.TransferOwnership(PhotonNetwork.MasterClient);
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

    private IEnumerator MonitorStabilityAfterCollision()
    {
        bool ranOnce = false;

        var hinge = grabObject.GetComponent<PhysGrabHinge>();
        var joint = hinge ? hinge.GetComponent<HingeJoint>() : null;
        var takeOverHelper = grabObject.GetComponent<OwnershipTakeoverHelper>();

        float lastJointRot = joint ? joint.angle : -123f;
        Vector3 lastPosition = transform.position;

        if (isCart)
        {
            syncCheckInterval = 0.25f;
            stableTimeRequired = 1.0f;
        }

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
                           rb.angularVelocity.sqrMagnitude < 0.01f;

            if (Vector3.Distance(transform.position, lastPosition) > 0.01f)
                isStill = false;

            if (isStill && joint != null && Mathf.Abs(joint.angle - lastJointRot) > 0.01f)
                isStill = false;

            lastJointRot = joint?.angle ?? lastJointRot;

            if (isStill)
            {
                stillTime += syncCheckInterval;
                if (stillTime >= stableTimeRequired)
                {
                    if (grabObject.playerGrabbing.Count > 0)
                    {
                        takeOverHelper.TransferToBestGrabber();
                    }
                    else if (view.Owner != PhotonNetwork.MasterClient && view.IsMine)
                    {
                        takeOverHelper.SyncImpactData();
                        view.RPC("SyncThisFrame", RpcTarget.All);
                        view.TransferOwnership(PhotonNetwork.MasterClient);
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
}
