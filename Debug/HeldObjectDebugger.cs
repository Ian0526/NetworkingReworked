using UnityEngine;
using Photon.Pun;
using System.IO;
using System.Text;
using UnityEngine.InputSystem;

public class HeldObjectDebugger : MonoBehaviourPun
{
    private static readonly string path = Path.Combine(Application.persistentDataPath, "HeldObjectDump.log");

    void Update()
    {
        if (!photonView.IsMine) return;

        if (Keyboard.current.f8Key.wasPressedThisFrame)
        {
            foreach (var grabber in FindObjectsOfType<PhysGrabber>())
            {
                if (grabber.photonView.IsMine && grabber.GetComponent<PhysGrabObject>() != null)
                {
                    var grabbed = grabber.GetComponent<PhysGrabObject>();
                    photonView.RPC("RemoteDumpRequest", RpcTarget.All, grabbed.GetComponent<PhotonView>().ViewID);
                    break;
                }
            }
        }
    }

    [PunRPC]
    public void RemoteDumpRequest(int targetViewId)
    {
        var pv = PhotonView.Find(targetViewId);
        if (pv == null)
        {
            Debug.LogWarning("[HeldObjectDump] Could not find PhotonView with ID " + targetViewId);
            return;
        }

        var grabbed = pv.GetComponent<PhysGrabObject>();
        if (grabbed == null)
        {
            Debug.LogWarning("[HeldObjectDump] Found view but no PhysGrabObject on it");
            return;
        }

        WriteDump(grabbed);
    }

    private void WriteDump(PhysGrabObject grabbed)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[HeldObjectDump] {Time.time:F2}s — Dump for {grabbed.name} on {PhotonNetwork.NickName} ({PhotonNetwork.LocalPlayer.ActorNumber})");

        sb.AppendLine("PhysGrabObject:");
        sb.AppendLine(ReflectionUtils.DumpObject(grabbed, 0, 1));

        var ptv = grabbed.GetComponent<PhotonTransformView>();
        sb.AppendLine("PhotonTransformView:");
        sb.AppendLine(ptv ? ReflectionUtils.DumpObject(ptv, 0, 1) : "None");

        var pv = grabbed.GetComponent<PhotonView>();
        sb.AppendLine("PhotonView:");
        sb.AppendLine(pv ? ReflectionUtils.DumpObject(pv, 0, 1) : "None");

        string fullPath = path.Replace(".log", $"_Client{PhotonNetwork.LocalPlayer.ActorNumber}.log");
        File.WriteAllText(fullPath, sb.ToString());
        Debug.Log($"[HeldObjectDump] Written to {fullPath}");
    }
}