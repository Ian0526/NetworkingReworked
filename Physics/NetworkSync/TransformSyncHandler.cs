using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class TransformSyncHandler : MonoBehaviourPun
{
    private Rigidbody rb;
    private bool hasRigidbody;

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

    public void SyncToOthers()
    {
        if (!photonView.IsMine) return;

        if (hasRigidbody)
        {
            photonView.RPC("ApplyTransformSync", RpcTarget.Others, rb.position, rb.rotation, rb.velocity, rb.angularVelocity);
        }
        else
        {
            photonView.RPC("ApplyTransformSync", RpcTarget.Others, transform.position, transform.rotation, Vector3.zero, Vector3.zero);
        }
    }
}
