using Photon.Pun;
using UnityEngine;
using System.Reflection;

public static class PhotonTransformViewUtils
{
    public static void SyncFields(
        PhotonTransformView ptv,
        Vector3 position,
        Quaternion rotation,
        Vector3 direction = default,
        double sentServerTime = -1)
    {
        if (ptv == null) return;

        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        float lag = sentServerTime > 0
            ? Mathf.Abs((float)(PhotonNetwork.Time - sentServerTime))
            : 0f;

        Vector3 predictedPos = position + direction * lag;
        float distance = Vector3.Distance(ptv.transform.position, predictedPos);
        float angle = Quaternion.Angle(ptv.transform.rotation, rotation);

        ptv.GetType().GetField("m_NetworkPosition", flags)?.SetValue(ptv, predictedPos);
        ptv.GetType().GetField("m_StoredPosition", flags)?.SetValue(ptv, predictedPos);
        ptv.GetType().GetField("smoothedPosition", flags)?.SetValue(ptv, predictedPos);
        ptv.GetType().GetField("receivedPosition", flags)?.SetValue(ptv, predictedPos);
        ptv.GetType().GetField("m_Direction", flags)?.SetValue(ptv, direction);
        ptv.GetType().GetField("m_Distance", flags)?.SetValue(ptv, distance);

        ptv.GetType().GetField("m_NetworkRotation", flags)?.SetValue(ptv, rotation);
        ptv.GetType().GetField("smoothedRotation", flags)?.SetValue(ptv, rotation);
        ptv.GetType().GetField("receivedRotation", flags)?.SetValue(ptv, rotation);
        ptv.GetType().GetField("m_Angle", flags)?.SetValue(ptv, angle);

        ptv.GetType().GetField("m_firstTake", flags)?.SetValue(ptv, true);
    }
}
