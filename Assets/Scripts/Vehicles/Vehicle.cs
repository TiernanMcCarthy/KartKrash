using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;
using Cinemachine;

public class Vehicle : NetworkBehaviour
{

    [Header("Driving Atributes")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float brakeForce;
    [SerializeField] private float accelerationTorque;
    [SerializeField] private AnimationCurve accelerationCurve;





    [Header("Transforms")]
    [SerializeField] private List<Wheel> wheels;

    [Header("Components")]
    [SerializeField] private Rigidbody rb;


    [Header("Player Driver Atributes")]
    [SerializeField] private float steerSpeed = 15; //Rate at which the steering will change towards the target



    CarInput carInput;


    //Local Car Values

    [Networked] private float steerAmount { get; set; } = 0;


    private void Start()
    {

    }

    public override void Spawned()
    {
        carInput = new CarInput();

        carInput.Enable();


        if (Object.HasStateAuthority)
        {
            Camera.main.GetComponent<CinemachineFreeLook>().Follow = transform;
            Camera.main.GetComponent<CinemachineFreeLook>().LookAt = transform;
        }
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

    void ManageSuspension()
    {

        for(int i = 0; i < wheels.Count; i++)
        {
            Wheel currentWheel = wheels[i];
            HitInformation info=RayCastFromWheel(currentWheel);



            if(info.hit==false)
            {
                continue;
            }

            //Get World Space Direction of our wheel
            Vector3 springDir = currentWheel.transform.up;

            //World Space velocity of the tyre in question
            Vector3 tyreWorldVel= rb.GetPointVelocity(currentWheel.transform.position);

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
        if (Object.HasStateAuthority)
        {
            float targetSteer = carInput.Generic.Steer.ReadValue<float>();

            float rotateAmount = steerSpeed;

            if (steerAmount != targetSteer)
            {
                if (targetSteer < steerAmount)
                {
                    rotateAmount *= -1;
                }

                steerAmount += rotateAmount * Time.deltaTime;


                if (rotateAmount < 0 && steerAmount < targetSteer)
                {
                    steerAmount = targetSteer;
                }
                else if (rotateAmount > 0 && steerAmount > targetSteer)
                {
                    steerAmount = targetSteer;
                }


            }
        }

        for ( int i = 0;i < wheels.Count;i++)
        {
            Wheel currentWheel= wheels[i];



            if (currentWheel.IsSteered())
            {
                currentWheel.transform.localRotation = Quaternion.Euler(0, steerAmount*currentWheel.GetMaxRotation(), 0);
            }

            

            HitInformation info = RayCastFromWheel(currentWheel);

            if(info.hit==false)
            {
                continue;
            }

            //World space direction of the spring force
            Vector3 steeringDir = currentWheel.GetRight();

            //World space velocity of the suspension
            Vector3 tyreWorldVel = rb.GetPointVelocity(currentWheel.transform.position);

            //Calculate what is the tyre's velocity in the steering dir
            //Creates a unit vector of the magnitude of tyreworldvel projected onto steering dir
            float steeringVel= Vector3.Dot(steeringDir, tyreWorldVel);

            //change in velocity that the tyre is lookign to cause = -steeringVel*gripfactor
            //0 is no grip 1 is 100%
            float desiredVelChange = -steeringVel * currentWheel.GetGripFactor();

            //turn change in velocity into an acceleration to apply to the vehicle in this one timestep
            float desiredAccel = desiredVelChange / Time.fixedDeltaTime;

            //Force = Mass * Acceleration, mass of tyre and apply as force

            rb.AddForceAtPosition(steeringDir*currentWheel.GetWheelMass()*desiredAccel, currentWheel.transform.position);

        }
    }

    public override void FixedUpdateNetwork()
    {
        ManageSuspension();
        ManageSteering();

        if (Keyboard.current.spaceKey.isPressed && Object.HasInputAuthority)
        {
            rb.AddForce(transform.forward * 5000 * Time.fixedDeltaTime);
            GUI.TextField(new Rect(new Vector2(Screen.width / 2, Screen.height / 2), new Vector2(150, 150)), "SPEEED");
        }
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


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.TransformPoint(rb.centerOfMass), 0.2f);
    }

}
