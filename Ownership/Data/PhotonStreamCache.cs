using System.Collections.Generic;
using UnityEngine;

public static class PhotonStreamCache
{
    public struct StreamState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public float timestamp;
    }

    private static readonly Dictionary<int, StreamState> cache = new Dictionary<int, StreamState>();

    public static void Update(int viewID, Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)
    {
        cache[viewID] = new StreamState
        {
            position = pos,
            rotation = rot,
            velocity = vel,
            angularVelocity = angVel,
            timestamp = Time.time
        };
    }

    public static void Update(int viewID, StreamState state)
    {
        state.timestamp = Time.time;
        cache[viewID] = state;
    }


    public static bool TryGet(int viewID, out StreamState state) => cache.TryGetValue(viewID, out state);

    public static void Clear() => cache.Clear();


    public static void Store(int viewID, object[] data)
    {
        if (data.Length < 8) return;

        // Format 1: PhysGrabObject + PhotonTransformView
        // Layout: [0–7] = bool, bool, bool, Vector3, Vector3, Vector3, Vector3, Quaternion
        bool isPhysGrabLayout =
            data[0] is bool &&
            data[1] is bool &&
            data[2] is bool &&
            data[3] is Vector3 &&
            data[4] is Vector3 &&
            data[5] is Vector3 &&
            data[6] is Vector3 &&
            data[7] is Quaternion;

        if (isPhysGrabLayout)
        {
            var velocity = (Vector3)data[3];
            var angularVelocity = (Vector3)data[4];
            var position = (Vector3)data[5];
            var direction = (Vector3)data[6]; // Unused
            var rotation = (Quaternion)data[7];

            Update(viewID, position, rotation, velocity, angularVelocity);
            return;
        }

        // Format 2: Door / PhysGrabHinge style
        // Layout: [0–7] = Vector3, Vector3, Vector3, Quaternion, Vector3, Vector3, bool, bool
        bool isDoorLayout =
            data[0] is Vector3 &&
            data[1] is Vector3 &&
            data[2] is Vector3 &&
            data[3] is Quaternion &&
            data[4] is Vector3 &&
            data[5] is Vector3 &&
            data[6] is bool &&
            data[7] is bool;

        if (isDoorLayout)
        {
            var velocity = (Vector3)data[0];
            var angularVelocity = (Vector3)data[1];
            var position = (Vector3)data[2];
            var rotation = (Quaternion)data[3];
            // Ignoring data[4–5] hinge points and data[6–7] status flags

            Update(viewID, position, rotation, velocity, angularVelocity);
            return;
        }

        // Debug unknown format
        // Debug.LogWarning($"[PhotonStreamCache] ViewID {viewID} has unknown stream layout: {DumpTypeList(data)}");
    }
}
