using UnityEngine;
using Photon.Pun;
using System.Reflection;
using System.Collections;

public class PhysGrabObjSync : MonoBehaviourPun
{
    private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly FieldInfo ImpactForceField = typeof(PhysGrabObject).GetField("impactForce", Flags);
    private static readonly FieldInfo BreakForceField = typeof(PhysGrabObject).GetField("breakForce", Flags);
    private static readonly FieldInfo PrevVelocityField = typeof(PhysGrabObject).GetField("previousVelocity", Flags);
    private static readonly FieldInfo PrevAngularVelocityField = typeof(PhysGrabObject).GetField("previousAngularVelocity", Flags);
    private static readonly FieldInfo PrevVelocityRawField = typeof(PhysGrabObject).GetField("previousVelocityRaw", Flags);
    private static readonly FieldInfo ImpactDisabledTimerField = typeof(PhysGrabObject).GetField("impactDisabledTimer", Flags);

    [PunRPC]
    public void ApplyPhysicsSnapshot(Vector3 velocity, Vector3 angularVelocity, Vector3 position, Quaternion rotation)
    {
        var rb = GetComponent<Rigidbody>();
        rb.velocity = velocity;
        rb.angularVelocity = angularVelocity;
        rb.position = position;
        rb.rotation = rotation;
    }

    [PunRPC]
    public void ApplyImpactSync(float syncedImpact, float syncedBreak, Vector3 syncedPrevVel, Vector3 syncedPrevAngVel, Vector3 syncedPrevRawVel, float syncedImpactDisabledTimer)
    {
        var detector = GetComponent<PhysGrabObjectImpactDetector>();
        if (detector == null) return;

        ImpactForceField.SetValue(detector, syncedImpact);
        BreakForceField.SetValue(detector, syncedBreak);
        PrevVelocityField.SetValue(detector, syncedPrevVel);
        PrevAngularVelocityField.SetValue(detector, syncedPrevAngVel);
        PrevVelocityRawField.SetValue(detector, syncedPrevRawVel);
        ImpactDisabledTimerField.SetValue(detector, syncedImpactDisabledTimer);
    }
}
