using Unity.Netcode;
using UnityEngine;

public class NetworkPSDestroy : NetworkBehaviour
{
    void Start()
    {
        if (!IsServer) return;
        
        if (TryGetComponent<ParticleSystem>(out var ps))
        {
            Invoke(nameof(DespawnObject), ps.main.duration);
        }
        else
        {
            Debug.LogWarning($"NetworkPSDestroy on {name} could not find ParticleSystem component!", this);
        }
    }

    void DespawnObject()
    {
        if (!IsServer) return;
        
        if (NetworkObject && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}