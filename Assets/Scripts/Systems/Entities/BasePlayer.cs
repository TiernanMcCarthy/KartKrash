using System.Collections;
using System.Collections.Generic;
using Fusion;
using LobbySystem;
using UnityEngine;

/// <summary>
/// Base Player exists as the minimum player in the simulation, it cannot move or do anything.
/// It exists to be able to interact with menus, possess entities and to manage networking
/// </summary>
public class BasePlayer : NetworkBehaviour
{

    public float score { get; private set; } = 0;
    [SerializeField] [Networked] private Entity possesedObject { get; set;}

    private PlayerRef playerRef;

    private NetworkObject networkObj;


    public override void Spawned()
    {
        networkObj = GetComponent<NetworkObject>();
        if(HasInputAuthority)
        {
            GameSpawnManager.instance.AssignLocalPlayer(this);
        }
        playerRef = networkObj.InputAuthority;
        GameSpawnManager.instance._lobby.AddPlayer(this);
    }

    public PlayerRef GetPlayerRef()
    {
        return playerRef;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        possesedObject.ClearOwner();
    }

    public NetworkObject GetNetworkObject()
    {
        return networkObj;
    }
    public void AssignPlayerRef(PlayerRef playerRef)
    {
        this.playerRef = playerRef;
    }

    public void PossessObject(Entity target)
    {
        if(possesedObject!=null)
        {
            possesedObject.ClearOwner();
        }

        possesedObject = target;
        possesedObject.Possess(this);
    }

    public BasePlayer NonNetworkedReference()
    {
        return this;
    }

    [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_SpawnPlayer(NetworkPrefabRef prefab, Vector3 pos)
    {
        var newEntity = Runner.Spawn(prefab, pos, Quaternion.identity, playerRef).GetComponent<Entity>();
        PossessObject(newEntity);

    }
}
