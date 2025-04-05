using HarmonyLib;
using Photon.Pun;
using System.Reflection;
using UnityEngine;

// this entire class is disgusting
// after my refactor, i'm not sure if this is needed anymore, but it works and i don't want to remove
// to see if it doesn't
namespace NoLag
{
    [HarmonyPatch(typeof(PhotonTransformView))]
    public static class PhotonTransformViewPatch
    {
        private static FieldInfo rbField = typeof(PhotonTransformView).GetField("rb", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo m_NetworkPosition = typeof(PhotonTransformView).GetField("m_NetworkPosition", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo m_NetworkRotation = typeof(PhotonTransformView).GetField("m_NetworkRotation", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo m_Distance = typeof(PhotonTransformView).GetField("m_Distance", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo m_Angle = typeof(PhotonTransformView).GetField("m_Angle", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo teleportField = typeof(PhotonTransformView).GetField("teleport", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo smoothedPositionField = typeof(PhotonTransformView).GetField("smoothedPosition", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo smoothedRotationField = typeof(PhotonTransformView).GetField("smoothedRotation", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo isSleepingField = typeof(PhotonTransformView).GetField("isSleeping", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo receivedVelocityField = typeof(PhotonTransformView).GetField("receivedVelocity", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo receivedAngularVelocityField = typeof(PhotonTransformView).GetField("receivedAngularVelocity", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo kinematicClientForcedTimerField = typeof(PhotonTransformView).GetField("kinematicClientForcedTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo kinematicClientForcedField = typeof(PhotonTransformView).GetField("kinematicClientForced", BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool Prefix_Update(PhotonTransformView __instance)
        {
            var view = __instance.GetComponent<PhotonView>();
            var rb = (Rigidbody)rbField.GetValue(__instance);
            if (rb == null || view == null) return false;

            var netPos = (Vector3)m_NetworkPosition.GetValue(__instance);
            var netRot = (Quaternion)m_NetworkRotation.GetValue(__instance);
            var dist = (float)m_Distance.GetValue(__instance);
            var ang = (float)m_Angle.GetValue(__instance);
            var teleport = (bool)teleportField.GetValue(__instance);
            var isSleeping = (bool)isSleepingField.GetValue(__instance);
            var smoothedPos = (Vector3)smoothedPositionField.GetValue(__instance);
            var smoothedRot = (Quaternion)smoothedRotationField.GetValue(__instance);
            var velocity = (Vector3)receivedVelocityField.GetValue(__instance);
            var angularVelocity = (Vector3)receivedAngularVelocityField.GetValue(__instance);
            var kinematicTimer = (float)kinematicClientForcedTimerField.GetValue(__instance);
            var kinematicForced = (bool)kinematicClientForcedField.GetValue(__instance);

            if (!rb.IsSleeping())
            {
                Debug.DrawLine(__instance.transform.position, __instance.transform.position + Vector3.up * 5f, Color.red);
            }

            if (!view.IsMine)
            {
                if (isSleeping)
                {
                    if (!rb.isKinematic)
                        rb.isKinematic = true;

                    if (rb.position != netPos || rb.rotation != netRot)
                    {
                        rb.position = netPos;
                        rb.rotation = netRot;
                    }
                    return false;
                }

                rb.interpolation = RigidbodyInterpolation.Interpolate;
                if (rb.isKinematic)
                {
                    smoothedPos = Vector3.MoveTowards(smoothedPos, netPos, dist * Time.deltaTime * PhotonNetwork.SerializationRate * 0.9f);
                    smoothedRot = Quaternion.RotateTowards(smoothedRot, netRot, ang * Time.deltaTime * PhotonNetwork.SerializationRate * 0.9f);
                }
                else
                {
                    float posLerp = Vector3.Distance(rb.position, netPos);
                    smoothedPos = Vector3.MoveTowards(rb.position, netPos, posLerp * Time.deltaTime * PhotonNetwork.SerializationRate);
                    smoothedRot = Quaternion.RotateTowards(rb.rotation, netRot, Quaternion.Angle(rb.rotation, netRot) * Time.deltaTime * PhotonNetwork.SerializationRate);
                }

                rb.MovePosition(smoothedPos);
                rb.MoveRotation(smoothedRot);

                smoothedPositionField.SetValue(__instance, smoothedPos);
                smoothedRotationField.SetValue(__instance, smoothedRot);

                if (!teleport)
                {
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    if (!rb.isKinematic)
                    {
                        rb.velocity = velocity;
                        rb.angularVelocity = angularVelocity;
                    }
                }
                else
                {
                    rb.position = netPos;
                    rb.rotation = netRot;
                    smoothedPositionField.SetValue(__instance, netPos);
                    smoothedRotationField.SetValue(__instance, netRot);
                    teleportField.SetValue(__instance, false);
                }
            }

            if (kinematicTimer > 0f)
            {
                kinematicTimer -= Time.deltaTime;
                if (kinematicTimer <= 0f)
                    kinematicForced = false;

                kinematicClientForcedTimerField.SetValue(__instance, kinematicTimer);
                kinematicClientForcedField.SetValue(__instance, kinematicForced);
            }

            return false;
        }
    }


    [HarmonyPatch(typeof(PhotonTransformView))]
    public static class PhotonTransformViewSerializationPatch
    {
        private static readonly BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly FieldInfo rbField = typeof(PhotonTransformView).GetField("rb", instanceFlags);
        private static readonly FieldInfo m_DirectionField = typeof(PhotonTransformView).GetField("m_Direction", instanceFlags);
        private static readonly FieldInfo m_StoredPositionField = typeof(PhotonTransformView).GetField("m_StoredPosition", instanceFlags);
        private static readonly FieldInfo isKinematicField = typeof(PhotonTransformView).GetField("isKinematic", instanceFlags);
        private static readonly FieldInfo teleportField = typeof(PhotonTransformView).GetField("teleport", instanceFlags);
        private static readonly FieldInfo kinematicClientForcedField = typeof(PhotonTransformView).GetField("kinematicClientForced", instanceFlags);
        private static readonly FieldInfo prevRotationField = typeof(PhotonTransformView).GetField("prevRotation", instanceFlags);
        private static readonly FieldInfo prevPositionField = typeof(PhotonTransformView).GetField("prevPosition", instanceFlags);
        private static readonly FieldInfo receivedPositionField = typeof(PhotonTransformView).GetField("receivedPosition", instanceFlags);
        private static readonly FieldInfo receivedRotationField = typeof(PhotonTransformView).GetField("receivedRotation", instanceFlags);
        private static readonly FieldInfo extrapolateTimerField = typeof(PhotonTransformView).GetField("extrapolateTimer", instanceFlags);
        private static readonly FieldInfo isSleepingField = typeof(PhotonTransformView).GetField("isSleeping", instanceFlags);
        private static readonly FieldInfo m_NetworkPositionField = typeof(PhotonTransformView).GetField("m_NetworkPosition", instanceFlags);
        private static readonly FieldInfo m_NetworkRotationField = typeof(PhotonTransformView).GetField("m_NetworkRotation", instanceFlags);
        private static readonly FieldInfo m_firstTakeField = typeof(PhotonTransformView).GetField("m_firstTake", instanceFlags);
        private static readonly FieldInfo m_DistanceField = typeof(PhotonTransformView).GetField("m_Distance", instanceFlags);
        private static readonly FieldInfo m_AngleField = typeof(PhotonTransformView).GetField("m_Angle", instanceFlags);
        private static readonly FieldInfo smoothedRotationField = typeof(PhotonTransformView).GetField("smoothedRotation", instanceFlags);

        [HarmonyPrefix]
        [HarmonyPatch("OnPhotonSerializeView")]
        public static bool Prefix_OnPhotonSerializeView(PhotonTransformView __instance, PhotonStream stream, PhotonMessageInfo info)
        {
            Rigidbody rb = (Rigidbody)rbField.GetValue(__instance);
            Transform transform = __instance.transform;

            if (stream.IsWriting)
            {
                bool sleeping = rb.IsSleeping();
                stream.SendNext(sleeping);

                bool teleport = (bool)teleportField.GetValue(__instance);
                stream.SendNext(teleport);
                teleportField.SetValue(__instance, false);

                bool isKinematic = rb.isKinematic;
                if ((bool)kinematicClientForcedField.GetValue(__instance))
                {
                    isKinematic = true;
                }
                stream.SendNext(isKinematic);
                stream.SendNext(rb.velocity);
                stream.SendNext(rb.angularVelocity);

                Vector3 storedPos = (Vector3)m_StoredPositionField.GetValue(__instance);
                Vector3 direction = transform.position - storedPos;
                m_DirectionField.SetValue(__instance, direction);
                m_StoredPositionField.SetValue(__instance, transform.position);

                stream.SendNext(transform.position);
                stream.SendNext(direction);
                stream.SendNext(transform.rotation);
            }
            else
            {
                prevRotationField.SetValue(__instance, receivedRotationField.GetValue(__instance));
                prevPositionField.SetValue(__instance, receivedPositionField.GetValue(__instance));

                extrapolateTimerField.SetValue(__instance, PhotonNetwork.SerializationRate * Time.deltaTime);
                bool sleeping = (bool)stream.ReceiveNext();
                isSleepingField.SetValue(__instance, sleeping);

                if (rb == null)
                {
                    rb = __instance.GetComponent<Rigidbody>();
                    rbField.SetValue(__instance, rb);
                }

                if (sleeping && !rb.IsSleeping() && !rb.isKinematic)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.Sleep();
                }
                else
                {
                    rb.WakeUp();
                }

                teleportField.SetValue(__instance, (bool)stream.ReceiveNext());
                isKinematicField.SetValue(__instance, (bool)stream.ReceiveNext());
                rb.isKinematic = (bool)isKinematicField.GetValue(__instance);

                Vector3 receivedVel = (Vector3)stream.ReceiveNext();
                Vector3 receivedAngVel = (Vector3)stream.ReceiveNext();
                Vector3 netPos = (Vector3)stream.ReceiveNext();
                m_NetworkPositionField.SetValue(__instance, netPos);
                receivedPositionField.SetValue(__instance, netPos);

                Vector3 dir = (Vector3)stream.ReceiveNext();
                m_DirectionField.SetValue(__instance, dir);

                bool firstTake = (bool)m_firstTakeField.GetValue(__instance);
                if (firstTake)
                {
                    rb.position = netPos;
                    transform.position = netPos;
                    m_DistanceField.SetValue(__instance, 0f);
                }
                else
                {
                    float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
                    netPos += dir * lag;
                    m_NetworkPositionField.SetValue(__instance, netPos);
                    float dist = Vector3.Distance(transform.position, netPos);
                    m_DistanceField.SetValue(__instance, dist);

                    if (dist > 5f && !(bool)teleportField.GetValue(__instance))
                    {
                        teleportField.SetValue(__instance, true);
                    }
                }

                Quaternion netRot = (Quaternion)stream.ReceiveNext();
                m_NetworkRotationField.SetValue(__instance, netRot);
                receivedRotationField.SetValue(__instance, netRot);

                if (firstTake)
                {
                    m_AngleField.SetValue(__instance, 0f);
                    rb.rotation = netRot;
                    transform.rotation = netRot;
                    smoothedRotationField.SetValue(__instance, netRot);
                    m_firstTakeField.SetValue(__instance, false);
                }
                else
                {
                    float angle = Quaternion.Angle(transform.rotation, netRot);
                    m_AngleField.SetValue(__instance, angle);
                }
            }

            return false; // Skip original
        }
    }
}
