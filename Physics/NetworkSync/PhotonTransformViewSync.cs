using UnityEngine;
using Photon.Pun;
using System.Reflection;
using NoLag.Helper.Utility;

[RequireComponent(typeof(PhotonView))]
public class PhotonViewTransformSync : MonoBehaviourPun
{
    private Rigidbody rb;
    private bool hasRigidbody;

    private static readonly BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        hasRigidbody = rb != null;
    }

    [PunRPC]
    public void ApplyTransformSync(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
    {
        if (hasRigidbody)
        {
            rb.position = position;
            rb.rotation = rotation;
            rb.velocity = velocity;
            rb.angularVelocity = angularVelocity;
        }
        else
        {
            transform.position = position;
            transform.rotation = rotation;
        }
    }

    public void ApplyInitialSyncSnapshot(PhotonView view, Rigidbody rb)
    {
        if (view == null || rb == null) return;

        transform.position = rb.position;
        transform.rotation = rb.rotation;

        Vector3 direction = rb.velocity * Time.fixedDeltaTime;
        SyncSnapshotToPhotonTransformView(direction);
        Debug.Log("[PhotonViewTransformSync] Initial transform snapshot applied.");
    }

    public void SyncToOthers()
    {
        if (!photonView.IsMine) return;

        if (hasRigidbody)
        {
            photonView.RPC("ApplyTransformSync", RpcTarget.Others,
                rb.position,
                rb.rotation,
                rb.velocity,
                rb.angularVelocity
            );
        }
        else
        {
            photonView.RPC("ApplyTransformSync", RpcTarget.Others,
                transform.position,
                transform.rotation,
                Vector3.zero,
                Vector3.zero
            );
        }
    }

    public void SyncSnapshotToPhotonTransformView(Vector3 direction = default, double sentServerTime = -1)
    {
        PhotonTransformView ptv = GetComponent<PhotonTransformView>();
        if (ptv == null) return;

        float lag = 0f;
        if (sentServerTime > 0)
        {
            lag = Mathf.Abs((float)(PhotonNetwork.Time - sentServerTime));
        }

        Vector3 predictedPos = transform.position + direction * lag;
        float distance = Vector3.Distance(ptv.transform.position, predictedPos);
        float angle = Quaternion.Angle(ptv.transform.rotation, transform.rotation);

        ptv.GetType().GetField("m_NetworkPosition", flags)?.SetValue(ptv, predictedPos);
        ptv.GetType().GetField("m_StoredPosition", flags)?.SetValue(ptv, predictedPos);
        ptv.GetType().GetField("smoothedPosition", flags)?.SetValue(ptv, predictedPos);
        ptv.GetType().GetField("receivedPosition", flags)?.SetValue(ptv, predictedPos);
        ptv.GetType().GetField("m_Direction", flags)?.SetValue(ptv, direction);
        ptv.GetType().GetField("m_Distance", flags)?.SetValue(ptv, distance);

        ptv.GetType().GetField("m_NetworkRotation", flags)?.SetValue(ptv, transform.rotation);
        ptv.GetType().GetField("smoothedRotation", flags)?.SetValue(ptv, transform.rotation);
        ptv.GetType().GetField("receivedRotation", flags)?.SetValue(ptv, transform.rotation);
        ptv.GetType().GetField("m_Angle", flags)?.SetValue(ptv, angle);
        ptv.GetType().GetField("m_firstTake", flags)?.SetValue(ptv, true);
    }
}