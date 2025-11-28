using Fusion;
using UnityEngine;

public struct DriveInputData : INetworkInput
{
    public float xDir;
    public float yDir;
    
    public float accelerate;
    public float brake;
    public float steerTarget;


    

}