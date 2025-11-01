using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Entities are a base network object, entities and more complex behaviours
/// inherit from them to create more complex actions
/// Players should posess entities, when possessed entities should receive player
/// input
/// </summary>
public class Entity : NetworkBehaviour
{
    [SerializeField][Networked] protected BasePlayer owner { get; set; }
    protected NetworkObject networkObject;

    private float genericMoveSpeed;

    [SerializeField] private bool destroyOnPlayerLeave = false;
    
    

    /// <summary>
    /// Takes control and management of this object and assigns it to player that has selected it.
    /// </summary>
    public virtual void Possess(BasePlayer newOwner)
    {
        if (owner != null)
        {
            ClearOwner();
        }

        if(networkObject ==null)
        {
            networkObject=GetComponent<NetworkObject>();
        }

        networkObject.AssignInputAuthority(newOwner.GetPlayerRef());
        owner= newOwner;

        OnSelect(owner);
    }

    public virtual void ClearOwner()
    {
        networkObject.RemoveInputAuthority();

        BasePlayer ownerOld = owner.NonNetworkedReference();
        OnDeselect(owner);

        if (destroyOnPlayerLeave)
        {
            Runner.Despawn(networkObject);
        }
        //owner = null;
    }

    public virtual void OnDeselect(BasePlayer previousOwner)
    {

    }

    public virtual void OnSelect(BasePlayer newOwner)
    {

    }

    public override void Spawned()
    {
        networkObject = GetComponent<NetworkObject>();
    }

    //Generic Move To function, if possesed by an AI or something (who knows)
    public virtual bool MoveTo(Vector3 targetLocation)
    {

        Vector3 moveDir=targetLocation-transform.position;

        moveDir.Normalize();

        transform.forward = moveDir;

        transform.position += moveDir * genericMoveSpeed * Runner.DeltaTime;

        if(Vector3.Distance(transform.position, targetLocation)<0.1f)
        {
            return true;
        }

        return false;

    }
}
