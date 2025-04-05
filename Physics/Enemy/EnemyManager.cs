using Photon.Pun;
using UnityEngine;

public class EnemyManager : MonoBehaviourPunCallbacks
{
    public static EnemyManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private bool CanApplyImpact(Enemy enemy, PhysGrabObject physObj)
    {
        // Add cooldown, distance, or velocity checks here if needed
        return true;
    }
}