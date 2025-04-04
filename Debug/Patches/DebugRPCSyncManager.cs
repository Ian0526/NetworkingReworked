using Photon.Pun;
using UnityEngine;
using System.IO;
using System.Text;
using System.Reflection;

public class DebugRPCSyncManager : MonoBehaviourPun
{
    private static readonly string logPath = Path.Combine(Application.persistentDataPath, "SyncedObjectDump_{0}.log");

    public static void RequestSyncedDump(PhysGrabObject target)
    {
        if (target == null) return;
        PhotonView pv = target.GetComponent<PhotonView>();
        if (pv == null || !pv.IsMine) return;

        pv.RPC(nameof(SyncedObjectDump), RpcTarget.All, pv.ViewID);
        Debug.Log($"[DebugRPC] Requested synced dump for ViewID {pv.ViewID}");
    }

    [PunRPC]
    public void SyncedObjectDump(int photonViewID)
    {
        PhotonView pv = PhotonView.Find(photonViewID);
        if (pv == null)
        {
            Debug.LogWarning($"[DebugRPC] Could not find object with ViewID {photonViewID}");
            return;
        }

        var obj = pv.GetComponent<PhysGrabObject>();
        var ptv = pv.GetComponent<PhotonTransformView>();

        var sb = new StringBuilder();
        sb.AppendLine($"[SyncedObjectDump] {Time.time:F2}s | Client: {PhotonNetwork.LocalPlayer.NickName} | ViewID: {photonViewID}");
        sb.AppendLine("PhysGrabObject:");
        sb.AppendLine(ReflectionUtils.DumpObject(obj, 0, 1));
        sb.AppendLine("PhotonTransformView:");
        sb.AppendLine(ptv != null ? ReflectionUtils.DumpObject(ptv, 0, 1) : "None");
        sb.AppendLine("PhotonView:");
        sb.AppendLine(ReflectionUtils.DumpObject(pv, 0, 1));

        string filename = string.Format(logPath, PhotonNetwork.LocalPlayer.ActorNumber);
        File.WriteAllText(filename, sb.ToString());

        Debug.Log($"[SyncedObjectDump] Written to {filename}");
    }
}
