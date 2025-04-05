using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class OwnershipGraceManager : MonoBehaviourPun
{
    public float networkGraceDuration = 0.35f;
    public float grabGracePeriod = 0.35f;

    private float graceEndTime = -1f;
    private float lastGrabTime = -1f;

    [PunRPC]
    public void StartNetworkGracePeriod(float duration)
    {
        graceEndTime = Time.time + duration;
    }

    public void MarkGrabbed()
    {
        lastGrabTime = Time.time;
    }

    public bool IsInNetworkGracePeriod()
    {
        return Time.time < graceEndTime;
    }

    public bool IsInGrabGracePeriod()
    {
        return Time.time - lastGrabTime <= grabGracePeriod;
    }
}
