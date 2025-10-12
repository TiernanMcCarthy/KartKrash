using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class PlayerCharacter : MonoBehaviour
{
    [Header("Player Properties")]
    [SerializeField] private float playerAcceleration;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float maxAccelerationForce;
    [SerializeField] private AnimationCurve accelerationFromDot;
    [SerializeField] private AnimationCurve maxAccelerationFromDot;

    [Header("Jumping Properties")] 
    [SerializeField] private bool canJump = false;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float maxJumpTime = 0.3f;
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpLength;
    [SerializeField] private float terminalJumpVelocity; //Max Jumping velocity

    public bool _isJumping = false;
    private float _jumpTime;
    
    
    float lastGroundedTime;

    [Header("Player Spring Settings")]
    //VerticalSpring
    [SerializeField] private float rideHeight;
    [SerializeField] private float rideSpringStrength;
    [SerializeField] private float rideDampnerForce;

    //Upright Force Spring Settings
    [SerializeField] private float uprightSpringStrength;
    [SerializeField] private float uprightDampnerForce;


    [SerializeField] private bool isGrounded = false;
    
    public TMPro.TMP_Text speed;

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

    private void ManageUprightForce()
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

    private Vector3 groundVelocity = Vector3.zero;
    
    private void ManageSpring()
    {
        HitInformation hitInfo = RaycastFromBody(transform);

        //Lets think about different Gravity Directions later :)
        Vector3 downDir = Vector3.down;

        //Manage Coyote Time
        if (hitInfo.hit == false)
        {
            if (Time.time - lastGroundedTime > coyoteTime)
            {
                canJump = false;
            }
        }
        else
        {
            canJump = true;
        }
        
        
        //Jump checks later
        isGrounded = hitInfo.hit;
        
        
        if (hitInfo.hit)
        {
            
            //Used to calculate if the user can jump
            lastGroundedTime=Time.time;
            
            //Velocity comparisons for managing spring force
            Vector3 velocity= rig.velocity;

            Vector3 rayDir= transform.TransformDirection(downDir);

            Vector3 otherVel = Vector3.zero;
            
            
            
            hitObject = hitInfo.hitRigid;


            if(hitObject != null) //Store relative velocity of platforms for later use
            {
                groundVelocity = hitObject.GetPointVelocity(hitInfo.hitLocation);
                otherVel = groundVelocity;
            }
            else
            {
                groundVelocity = Vector3.zero;
            }

            //store dot product of velocities compared to player upright direction
            float rayDirVel=Vector3.Dot(rayDir,velocity);

            float otherDirVel= Vector3.Dot(rayDir,otherVel);
            
            
            //store results to work out spring strength required to float the player
            float relVel = rayDirVel - otherDirVel;

            float x = hitInfo.hitDistance -rideHeight;

            float springForce = (x*rideSpringStrength) - (relVel*rideDampnerForce);

            //Debug.DrawLine(transform.position, transform.position + (rayDir*-springForce).normalized*rideHeight, Color.yellow);

            //Float player
            if (!_isJumping)
            {
                rig.AddForce(rayDir * springForce);

                if (hitObject != null) //Add opposite spring force to object to simulate standing on it
                {
                    hitObject.AddForceAtPosition(rayDir * -springForce, hitInfo.hitLocation);
                }
            }
        }
        else
        {
            groundVelocity=Vector3.zero;
        }
    }

    private Vector2 playerInputs;
    void ManagePlayerDirection()
    {
        playerInputs = new Vector2(inputs.Generic.Right.ReadValue<float>(), inputs.Generic.Forward.ReadValue<float>());

        if (playerInputs.sqrMagnitude == 0)
            return;

        Transform cam = Camera.main.transform;

        // Project the camera forward/right onto the horizontal plane (Y=0)
        Vector3 cameraForward = cam.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 cameraRight = cam.right;
        cameraRight.y = 0;
        cameraRight.Normalize();

        // collect user input
        Vector3 moveDirection = (cameraForward * playerInputs.y + cameraRight * playerInputs.x).normalized;

        desiredDir = moveDirection;
    }

    private void ManageMovement()
    {
        if (hitObject != null)
        {
            rig.AddForce(rig.velocity*Time.fixedDeltaTime,ForceMode.Impulse);
        }
        
        if (playerInputs.magnitude == 0) return;
        
        Vector3 movementDir = desiredDir;
        m_UnitGoal = movementDir;
        
        Vector3 unitVel = rig.velocity.sqrMagnitude > 0.001f ? rig.velocity.normalized : Vector3.zero;

        float velDot = Vector3.Dot(m_UnitGoal, unitVel);
        
        float accelDot = Mathf.Max(accelerationFromDot.Evaluate(velDot), 0.4f);
        float maxAccelDot = Mathf.Max(maxAccelerationFromDot.Evaluate(velDot), 0.4f);

        float accel = playerAcceleration * accelDot;
        float maxAccel = maxAccelerationForce * maxAccelDot;

        float targetSpeed = maxSpeed * Mathf.Clamp01((playerInputs.magnitude));
        
        Vector3 goalVel = m_UnitGoal * targetSpeed;
        m_GoalVel = Vector3.MoveTowards(m_GoalVel, goalVel, accel * Time.fixedDeltaTime);

        Vector3 neededAccel = (m_GoalVel - rig.velocity) / Time.fixedDeltaTime;
        neededAccel = Vector3.ClampMagnitude(neededAccel, maxAccel);

        Vector3 forceScale = new Vector3(1, 0, 1);
        rig.AddForce(Vector3.Scale(neededAccel * rig.mass, forceScale));
    }

    void ManageFriction()
    {
        float yVelocity = rig.velocity.y;
        // generic Drag
        if (!isGrounded)
        {
            
            rig.velocity *= 0.99f;
            
            rig.velocity= new Vector3(rig.velocity.x,yVelocity,rig.velocity.z);
            
            return;
        }

        
        Vector3 relativeVel = rig.velocity - groundVelocity;

        yVelocity = rig.velocity.y;
        
        // blend velocity towards ground velocity direction 
        rig.velocity = Vector3.Lerp(rig.velocity, groundVelocity, 0.1f);
    
        // if there is no player input add lots of friction;
        if (playerInputs.magnitude == 0)
        {
            relativeVel *= 0.95f;
            rig.velocity = groundVelocity + relativeVel;
        }
        
        rig.velocity= new Vector3(rig.velocity.x,yVelocity,rig.velocity.z);
    }

    IEnumerator JumpCoroutine()
    {
        

        yield return null;
    }

    private void CalculateJumpForce()
    {
        if (_isJumping)
        {
            rig.AddForce(Vector3.up * jumpForce, ForceMode.Force);
        }
    }
    void ManageJump()
    {
        if (_isJumping)
        {
            if (Time.time - _jumpTime > jumpLength || !inputs.Generic.PrimaryAction.IsPressed())
            {
                _isJumping = false;
            }

        }
        else
        {
            if (inputs.Generic.PrimaryAction.IsPressed())
            {
                if (canJump)
                {
                    _jumpTime = Time.time;
                    _isJumping=true;
                }
            }
        }
        
        CalculateJumpForce();

      
    }
    
    // Update is called once per frame
    void Update()
    {
        ManagePlayerDirection();
        speed.text = rig.velocity.magnitude.ToString();
    }

    Rigidbody hitObject=null;
    private void FixedUpdate()
    {
        hitObject = null;
        ManageFriction();
        ManageSpring();
        ManageUprightForce();
        ManageMovement();
        ManageJump();
    }
}
