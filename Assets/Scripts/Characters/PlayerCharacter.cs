using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{
    [Header("Player Properties")]
    [SerializeField] private float playerAcceleration;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float maxAccelerationForce;

    [Header("Jumping Properties")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpLength;
    [SerializeField] private float terminalJumpVelocity; //Max Jumping velocity

    [Header("Player Spring Settings")]
    //VerticalSpring
    [SerializeField] private float rideHeight;
    [SerializeField] private float rideSpringStrength;
    [SerializeField] private float rideDampnerForce;

    //Upright Force Spring Settings
    [SerializeField] private float uprightSpringStrength;
    [SerializeField] private float uprightDampnerForce;

    Quaternion startingRot;

    //Player's desired MoveDirection;
    Vector3 desiredDir;

    Vector3 m_UnitGoal; //targetDir?

    Vector3 m_GoalVel; //target Velocity




    private Rigidbody rig;

    UserInputs inputs;

    // Start is called before the first frame update
    void Start()
    {
        rig = GetComponent<Rigidbody>();

        desiredDir = transform.forward;

        inputs= new UserInputs();

        inputs.Enable();
    }


    private HitInformation RaycastFromBody(Transform centre)
    {
        RaycastHit hit;
        int layerMask = ~LayerMask.GetMask("PlayerTests");
        if (Physics.Raycast(centre.transform.position, centre.transform.up * -1, out hit, rideHeight, layerMask))
        {
            return new HitInformation(true, hit.point, hit.normal, hit.distance, hit.collider.gameObject,hit.rigidbody);
        }
        return new HitInformation(false);
    }
    public static Quaternion Multiply(Quaternion input, float scalar)
    {
        return new Quaternion(input.x * scalar, input.y * scalar, input.z * scalar, input.w * scalar);
    }
    Quaternion ShortestRotation(Quaternion target, Quaternion current)
    {
        if (Quaternion.Dot(target, current) < 0)
        {
            return target * Quaternion.Inverse(Multiply(current, -1));
        }
        else return target * Quaternion.Inverse(current);
    }

    private void ManageUprightForce(float elapsedTime)
    {
        Quaternion currentRotation=transform.rotation;

        Quaternion goalRotation = ShortestRotation(Quaternion.LookRotation(desiredDir,Vector3.up),currentRotation);//Maybe work something out with the ground and a slope?

        Vector3 rotAxis;
        float rotDegrees;

        goalRotation.ToAngleAxis(out rotDegrees, out rotAxis);
        rotAxis.Normalize();

        float rotRadians= rotDegrees*Mathf.Deg2Rad;

        rig.AddTorque((rotAxis * (rotRadians * uprightSpringStrength)) - (rig.angularVelocity * uprightDampnerForce));
    }

    private void ManageSpring()
    {
        HitInformation hitInfo = RaycastFromBody(transform);

        Vector3 downDir = Vector3.down;

        if (hitInfo.hit)
        {
            Vector3 velocity= rig.velocity;

            Vector3 rayDir= transform.TransformDirection(downDir);

            Vector3 otherVel = Vector3.zero;

            Rigidbody hitBody = hitInfo.hitRigid;


            if(hitBody != null)
            {
                otherVel = hitBody.velocity;
            }

            float rayDirVel=Vector3.Dot(rayDir,velocity);

            float otherDirVel= Vector3.Dot(rayDir,otherVel);

            float relVel = rayDirVel - otherDirVel;

            float x = hitInfo.hitDistance -rideHeight;

            float springForce = (x*rideSpringStrength) - (relVel*rideDampnerForce);

            Debug.DrawLine(transform.position, transform.position + (rayDir*-springForce).normalized*rideHeight, Color.yellow);

            rig.AddForce(rayDir * springForce);

            if(hitBody!=null)
            {
                hitBody.AddForceAtPosition(rayDir * -springForce, hitInfo.hitLocation);
            }
        }
    }

    void ManagePlayerDirection()
    {
        Vector2 playerInputs = new Vector2(inputs.Generic.Right.ReadValue<float>(), inputs.Generic.Forward.ReadValue<float>());

        if(playerInputs.magnitude==0) //no change needed
        {
            return;
        }


        // Calculate the camera's forward and right directions
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        // Adjust the forward direction to align with the camera's "up"
        Vector3 adjustedForward = Vector3.Cross(cameraRight, Vector3.up).normalized;

        // Adjust the right direction to ensure it's orthogonal to gravity
        Vector3 adjustedRight = Vector3.Cross(Vector3.up, adjustedForward).normalized;

        // Calculate the desired movement direction based on inputs
        Vector3 moveDirection = adjustedForward * playerInputs.y + adjustedRight * playerInputs.x;

        desiredDir = moveDirection;
        //desiredDir
    }

    private void ManageMovement()
    {
        Vector3 movementDir = desiredDir;

        if(movementDir.magnitude>1.0f)
        {
            movementDir.Normalize();
        }

        m_UnitGoal = movementDir;


        Vector3 groundVelocity = Vector3.zero; //Work out object beneath us and their velocity later

        Vector3 unitVel = m_GoalVel.normalized;

        float velDot= Vector3.Dot(m_UnitGoal, unitVel);

        float accel = playerAcceleration * 3; //Acceleration from dot factor needed

        float speedFactor = 1; //no clue what this is

        Vector3 goalVel = m_UnitGoal * maxSpeed * speedFactor;

        m_GoalVel = Vector3.MoveTowards(m_GoalVel, goalVel + groundVelocity, accel * Time.fixedDeltaTime);

        //Work out real force

        Vector3 neededAccel = (m_GoalVel-rig.velocity)/Time.fixedDeltaTime;

        //max accel*max acceleration force from dot (veldot) * maxAccelforceFactor
        float maxAccel = maxAccelerationForce * 3 * 1;

        neededAccel= Vector3.ClampMagnitude(neededAccel,maxAccel);

        Vector3 forceScale = new Vector3(1, 0, 1);
        rig.AddForce(Vector3.Scale(neededAccel * rig.mass, forceScale));

    }

    // Update is called once per frame
    void Update()
    {
        ManagePlayerDirection();
    }

    private void FixedUpdate()
    {
        ManageSpring();
        ManageUprightForce(Time.fixedDeltaTime);
        ManageMovement();
    }
}
