using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Teleporter : NetworkBehaviour
{

    [SerializeField] private Transform teleportLocation;
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        NetworkedVehicle vehicle = other.attachedRigidbody.GetComponent<NetworkedVehicle>();

        if (vehicle != null)
        {
            vehicle.MoveToLocation(teleportLocation.position);
        }
    }
}
