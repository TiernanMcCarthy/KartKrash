using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;
using Cinemachine;

//Container for RaycastHits
class HitInformation
{
    public bool hit;

    public Vector3 hitLocation;

    public Vector3 hitNormal;

    public float hitDistance;

    public GameObject hitObject;

    public HitInformation(bool hit, Vector3 hitLocation, Vector3 hitNormal, float hitDistance, GameObject hitObject)
    {
        this.hit = hit;
        this.hitLocation = hitLocation;
        this.hitNormal = hitNormal;
        this.hitDistance = hitDistance;
        this.hitObject = hitObject;
    }

    public HitInformation(bool hit)
    {
        hit = false;
    }
}


public class NetworkedVehicle : NetworkBehaviour
{

    [Header("Driving Atributes")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float brakeForce;
    [SerializeField] private float accelerationTorque;
    [SerializeField] private float engineSlowDownForce = 0.1f;
    [SerializeField] private float frictionForce;
    [SerializeField] private AnimationCurve frictionCurve;
    [SerializeField] private AnimationCurve accelerationCurve;
    [SerializeField] private AnimationCurve reverseAccelerationCurve;
    
    [SerializeField] private AnimationCurve speedSteerEffect;
    

    [Header("Transforms")]
    [SerializeField] private List<Wheel> wheels;

    [SerializeField] private Transform carBody;

    [Header("Components")]
    [SerializeField] private Rigidbody rb;


    [Header("Player Driver Atributes")]
    [SerializeField] private float steerSpeed = 15; //Rate at which the steering will change towards the target



    CarInput carInput;


    //Local Car Values

    [Networked] private float steerAmount { get; set; } = 0;
    
    private float targetSteer = 0;

    private float accelerationTarget = 0;


    private void Start()
    {

    }

    void OnDestroy()
    {
        carInput.Disable();
    }

    public override void Spawned()
    {
        carInput = new CarInput();

        carInput.Enable();

        Runner.SetIsSimulated(Object, true);
        if (Object.HasInputAuthority)
        {
            Camera.main.GetComponent<CinemachineFreeLook>().Follow = carBody.transform;
            Camera.main.GetComponent<CinemachineFreeLook>().LookAt = carBody.transform;

            PlayerUI.instance.AssignVehicle(this);
        }
    }

    public float GetCarSpeed()
    {
        return rb.velocity.magnitude * 3.6f;
    }
    private float GetSpeedToMaxSpeed()
    {
        // get forward speed of the car in its direction of driving
        float carSpeed=Vector3.Dot(transform.forward,rb.velocity);
                
        //normalise speed;
        float normalisedSpeed= Mathf.Clamp01(Mathf.Abs(carSpeed)/maxSpeed);
        
        return normalisedSpeed;
    }

    
    private HitInformation RayCastFromWheel(Wheel wTransform)
    {
        RaycastHit hit;
        int layerMask = ~LayerMask.GetMask("Vehicle");
        if (Physics.Raycast(wTransform.transform.position, wTransform.transform.up * -1, out hit, wTransform.GetWheelSize(), layerMask))
         {

            return new HitInformation(true, hit.point, hit.normal, hit.distance, hit.collider.gameObject);
         }
        return new HitInformation(false);
    }
    
    /* Messing Around with Spherecast, not stable and not what I want, work on later
    private HitInformation RayCastFromWheel(Wheel wTransform)
    {
        RaycastHit hit;
        int layerMask = ~LayerMask.GetMask("Vehicle");

        // sphere radius (wheel radius)
        float wheelRadius = wTransform.GetWheelSize() * 0.5f;

        // maxDistance should allow the SPHERE CENTER to reach the ground when suspension is fully extended
        // suspension rest distance is how far from the wheel origin to ground when at rest (you set this at Start in Wheel)
        float suspensionRest = wTransform.GetSuspensionRestDistance();

        // sphereCast distance is how far we let the center travel
        float sphereCastDistance = suspensionRest + wheelRadius;

        Vector3 wheelCenter = wTransform.transform.position - wTransform.transform.up * wheelRadius;

        Ray rayTemplate = new Ray(wheelCenter, -wTransform.transform.up);

        if (Physics.SphereCast(rayTemplate, wheelRadius, out hit, suspensionRest + wheelRadius, layerMask))
        {
            float centerDistance = hit.distance + wheelRadius;
            return new HitInformation(true, hit.point, hit.normal, centerDistance, hit.collider.gameObject);
        }

        return new HitInformation(false);
    }*/

    void ManageSuspension()
    {

        for (int i = 0; i < wheels.Count; i++)
        {
            Wheel currentWheel = wheels[i];
            HitInformation info = RayCastFromWheel(currentWheel);



            if (info.hit == false)
            {
                continue;
            }

            //Get World Space Direction of our wheel
            Vector3 springDir = currentWheel.transform.up;

            //World Space velocity of the tyre in question
            Vector3 tyreWorldVel = rb.GetPointVelocity(currentWheel.transform.position);

            //Calculate offset from Raycast
            float offset = currentWheel.GetSuspensionRestDistance() - info.hitDistance;


            //Calculate velocity along spring direction
            // spring dir is a unit vector so magnitutde of tire worldVel
            // is returned as a projection of spring dir
            float vel = Vector3.Dot(springDir, tyreWorldVel);


            //Calculate the magnitude of the spring force with dampening effect
            float force = (offset * currentWheel.GetSpringStrength()) - (vel * currentWheel.GetDampening());


            rb.AddForceAtPosition(springDir * force, currentWheel.transform.position);


        }

    }

    void ManageSteering()
    {

            float rotateAmount = steerSpeed;

            if (steerAmount != targetSteer)
            {
                if (targetSteer < steerAmount)
                {
                    rotateAmount *= -1;
                }

                steerAmount += rotateAmount*speedSteerEffect.Evaluate(GetSpeedToMaxSpeed()) * Runner.DeltaTime;


                if (rotateAmount < 0 && steerAmount < targetSteer)
                {
                    steerAmount = targetSteer;
                }
                else if (rotateAmount > 0 && steerAmount > targetSteer)
                {
                    steerAmount = targetSteer;
                }


            }
        

        for (int i = 0; i < wheels.Count; i++)
        {
            Wheel currentWheel = wheels[i];



            if (currentWheel.IsSteered())
            {
                currentWheel.transform.localRotation = Quaternion.Euler(0, steerAmount * currentWheel.GetMaxRotation(), 0);
            }



            HitInformation info = RayCastFromWheel(currentWheel);

            if (info.hit == false)
            {
                continue;
            }

            //World space direction of the spring force
            Vector3 steeringDir = currentWheel.GetRight();

            //World space velocity of the suspension
            Vector3 tyreWorldVel = rb.GetPointVelocity(currentWheel.transform.position);

            //Calculate what is the tyre's velocity in the steering dir
            //Creates a unit vector of the magnitude of tyreworldvel projected onto steering dir
            float steeringVel = Vector3.Dot(steeringDir, tyreWorldVel);

            
            //Calculate percentage velocity is in Tyre.right direction
            float steeringPercentage= Mathf.Abs(Vector3.Dot(currentWheel.GetRight(),rb.GetPointVelocity(currentWheel.transform.position)));
            
            //change in velocity that the tyre is lookign to cause = -steeringVel*gripfactor
            //0 is no grip 1 is 100%
            float desiredVelChange = -steeringVel * currentWheel.GetGripFactor(steeringPercentage)*currentWheel.GetGripStrength();

            //Modify velocity change by steering relative to max speed
            desiredVelChange *= speedSteerEffect.Evaluate(GetSpeedToMaxSpeed());
            
            //turn change in velocity into an acceleration to apply to the vehicle in this one timestep
            float desiredAccel = desiredVelChange / Runner.DeltaTime;

            //Force = Mass * Acceleration, mass of tyre and apply as force

            rb.AddForceAtPosition(steeringDir * currentWheel.GetWheelMass() * desiredAccel, currentWheel.transform.position);

        }
    }

    private void ManageDrive()
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            //Manage current wheel
            Wheel currentWheel = wheels[i];

            Vector3 accelDir = currentWheel.transform.forward;

            //How close the pedal is to the metal :)
            float accelValue = accelerationTarget;

            //Adjust the function if the tyre isn't driven, give a small deacceleration
            if (!currentWheel.IsDriven())
            {
                continue;
            }

            
            HitInformation info = RayCastFromWheel(currentWheel);

            if (info.hit == false)
            {
                continue;
            }
            // get forward speed of the car in its direction of driving

            float carSpeed=Vector3.Dot(transform.forward,rb.velocity);
                
            //normalise speed;
            float normalisedSpeed= Mathf.Clamp01(Mathf.Abs(carSpeed)/maxSpeed);
            
            
            //Forward Drive Mode;
            if (accelValue > 0.0f)
            {
                
                // get available torque
                
                float torque = accelerationCurve.Evaluate(normalisedSpeed)*accelValue;
                
                if (carSpeed >= -0.3f) //car is going forwards
                {
                    rb.AddForceAtPosition(accelDir*torque*accelerationTorque, currentWheel.transform.position);
                }
                else if(carSpeed < -0.3f) //car is reversing
                {
                    rb.AddForceAtPosition(accelDir*brakeForce*accelValue, currentWheel.transform.position);
                }
               
            }
            else if(accelValue < 0.0f ) //Reverse mode
            {
                
                // get available torque

                if (carSpeed > 0.3f) //car is going forwards
                {
                    rb.AddForceAtPosition(accelDir*brakeForce*accelValue, currentWheel.transform.position);
                }
                else if(carSpeed <= 0.3f) //car is reversing
                {
                    float torque = reverseAccelerationCurve.Evaluate(normalisedSpeed)*accelValue;
                    rb.AddForceAtPosition(accelDir*torque*accelerationTorque, currentWheel.transform.position);
                }

            }
            else //Engine Slowdown
            {

                float torque = accelerationTorque * engineSlowDownForce;
                
                if (carSpeed < -0.3f) //If the car is going backwards
                {
                    accelDir *= -1;
                    rb.AddForceAtPosition(accelDir*torque, currentWheel.transform.position);
                }
                else if (carSpeed > 0.3f) //forwards
                {
                    rb.AddForceAtPosition(accelDir*-torque, currentWheel.transform.position);
                }

            }

        }
    }

    private void ManageFriction()
    {
        //Detect which wheels are grounded,
        //work out opposite direction to car travel, 
        //split max or force to 0 speed between the number of wheels that are on the ground.
        
        List<int> wheelsOnFloor = new List<int>();
        for (int i = 0; i < wheels.Count; i++)
        {
            Wheel currentWheel = wheels[i];
            
            HitInformation info = RayCastFromWheel(currentWheel);

            if (info.hit)
            {
                wheelsOnFloor.Add(i);
            }
        }
        
        // get forward speed of the car in its direction of driving

        float carSpeed=Vector3.Dot(transform.forward,rb.velocity);

        float frictionAtCarSpeed = -frictionForce * frictionCurve.Evaluate(GetSpeedToMaxSpeed());
        if (Mathf.Abs(carSpeed) < frictionAtCarSpeed)
        {
            frictionAtCarSpeed = Mathf.Abs(carSpeed);
        }

        int dir = 1;
        if (carSpeed < 0)
        {
            dir = -1;
        }

        if (wheelsOnFloor.Count > 0)
        {
            frictionAtCarSpeed /= wheelsOnFloor.Count;

            frictionAtCarSpeed *= dir;
        }

        for (int w = 0; w < wheelsOnFloor.Count; w++)
        {
            Wheel currentWheel = wheels[wheelsOnFloor[w]];
            
            rb.AddForceAtPosition(currentWheel.transform.forward*frictionAtCarSpeed,currentWheel.transform.position);
        }
        
        //Generic Car Friction
        if (wheelsOnFloor.Count > 0)
        {
            rb.velocity *= 0.9995f;
        }
        
    }

    public override void FixedUpdateNetwork()
    {

        if (GetInput(out DriveInputData data))
        {
            targetSteer=data.steerTarget;
            accelerationTarget = data.accelerate+data.brake;
        }

        //rb.AddForce(transform.forward * 15000 * accelerationTarget* Runner.DeltaTime);
        
        
        ManageSuspension();
        ManageSteering();
        ManageDrive();
        
        ManageFriction();

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {

    }

    private void Update()
    {

    }

    public void MoveToLocation(Vector3 location)
    {
        if (HasStateAuthority)
        {
            transform.position = location;
        }
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.TransformPoint(rb.centerOfMass), 0.2f);
    }

}
