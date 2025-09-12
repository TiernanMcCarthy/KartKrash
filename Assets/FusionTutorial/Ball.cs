using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;


public class Ball : NetworkBehaviour
{
    public void Init()
    {
        life = TickTimer.CreateFromSeconds(Runner, 5.0f);
    }


    [Networked] private TickTimer life { get; set; }
    public override void FixedUpdateNetwork()
    {
        if (life.Expired(Runner))
            Runner.Despawn(Object);
        else
        transform.position += 5 * transform.forward * Runner.DeltaTime;


    }


}

