using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wheel : MonoBehaviour
{

    [Header("Wheel Function")]
    [SerializeField] private bool isDriven;
    [SerializeField] private bool isSteerable;
    [SerializeField] private bool invertRight;


    [Header("Drive Attributes")]

    [SerializeField] private float wheelSize = 1;

    //Spring Strength manages the force that a spring needs to reach its resting point (Force = (offset x strength))

    //Offset of 0 is the resting point, + - is directions from resting point
    [SerializeField] private float springStrength;

    //Dampner force slows down motion of the spring so it rests
    [SerializeField] private float dampnerForce;

    [SerializeField] private float tractionForce;


    [Header("Steering Attributes")]
    [SerializeField] private float maxSteerAngle = 30.0f;
    [SerializeField] private float gripFactor = 0.3f;
    [SerializeField] private float wheelMass = 5;


    public AnimationCurve gripFactorCurve;




    private float suspensionRestDistance;

    private void Start()
    {
        suspensionRestDistance = transform.localPosition.y;
    }

    public float GetWheelMass()
    {
        return wheelMass;
    }

    public Vector3 GetRight()
    {
        if(invertRight)
        {
            return transform.right * -1;
        }
        return transform.right;
    }

    public float GetGripFactor()
    {
        return gripFactor;
    }

    public float GetMaxRotation()
    {
        return maxSteerAngle;
    }
    public float GetSpringStrength()
    {
        return springStrength;
    }
    public float GetDampening()
    { return dampnerForce; }

    public float GetSuspensionRestDistance()
    {
        return suspensionRestDistance;
    }
    public float GetWheelSize()
    {
        return wheelSize;
    }

    public bool IsSteered()
    {
        return isSteerable;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, (transform.position+transform.up *wheelSize*-1));
    }

}
