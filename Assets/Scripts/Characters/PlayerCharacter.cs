using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class PlayerCharacter : Entity
{
    [Header("Player Properties")]
    [SerializeField] private float playerAcceleration;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float maxAccelerationForce;
    [SerializeField] private AnimationCurve accelerationFromDot;
    [SerializeField] private AnimationCurve maxAccelerationFromDot;
    [SerializeField] private AnimationCurve playerAccelerationCurve;

    [Header("Jumping Properties")] 
    [SerializeField] private bool canJump = false;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpInterval = 0.2f;
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

    [SerializeField] private AnimationCurve slopeGripFactor;
    
    // Smoothly apply resistance between these angles                
    float resistStartAngle = 20f; // begin resisting                 
    float resistFullAngle  = 40f; // completely block uphill motion  
    float uphillResistance = 2f;  // scaling factor                  


    [SerializeField] private bool isGrounded = false;
    [SerializeField] private bool canStand = false;

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

        _jumpTime = Time.time;
        lastGroundedTime = Time.time;

        //Init Slope Factors
        if (slopeGripFactor.length > 0)
        {
            resistStartAngle = slopeGripFactor.keys[0].value;

            resistFullAngle = slopeGripFactor.keys[slopeGripFactor.length - 1].value;
        }
    }
    
    private float GetGroundAngleRelativeToGravity()
    {
        RaycastHit hit;
        float slopeAngle = 0;
        if(Physics.Raycast(transform.position, transform.up * -1, out hit, rideHeight * 1.2f,~0, QueryTriggerInteraction.Ignore))
        {
            slopeAngle = Vector3.Angle(hit.normal, -Physics.gravity);
        }
        return slopeAngle;
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
    
    private Vector3 groundNormal = Vector3.zero;
    
                                     
    private float gripRatio;
    
    private void ManageSpring()
    {
        HitInformation hitInfo = RaycastFromBody(transform);

        groundNormal = hitInfo.hitNormal;
        //Lets think about different Gravity Directions later :)
        Vector3 downDir = Vector3.down;

         canStand = GetGroundAngleRelativeToGravity() < 40;

         gripRatio = slopeGripFactor.Evaluate(GetGroundAngleRelativeToGravity());
         
        //Manage Coyote Time
        if (hitInfo.hit == false)
        {
            if (Time.time - lastGroundedTime > coyoteTime || _isJumping)
            {
                canJump = false;
            }
        }
        else
        {
            if (Time.time - _jumpTime > jumpInterval)
            {
                canJump = true;
            }
        }
        

        //Jump checks later
        isGrounded = hitInfo.hit;

        if (canStand == false)
        {
            return;
        }
        
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
                rig.AddForce(rayDir * springForce*gripRatio);

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

    #region Old Movement
    //Old Movement
    /*
    private void ManageMovement()
    {
        if (hitObject != null)
        {
            rig.AddForce(rig.velocity*Time.fixedDeltaTime,ForceMode.Impulse);
        }
        
        if (playerInputs.magnitude == 0) return;
        
       // Vector3 movementDir = desiredDir;
        Vector3 movementDir = Vector3.ProjectOnPlane(desiredDir, groundNormal).normalized;
        
        
        // Calculate slope steepness
        float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);

        float modifiedAcceleration = playerAcceleration;
        // Reduce uphill movement on steep slopes
        if (slopeAngle > 0f)
        {
            // How much the player is trying to move uphill (dot between movement direction and up)
            float uphillFactor = Vector3.Dot(movementDir, Vector3.up);

            if (uphillFactor > 0f) // moving up the slope
            {
                // Compute slope steepness factor (e.g. 0–1 between easy & impossible)
                float steepnessRatio = Mathf.InverseLerp(20f, 40f, slopeAngle);
            
                // If slope too steep, scale down the uphill movement
                float uphillResistance = Mathf.Lerp(1f, 0f, steepnessRatio);

                // Reduce uphill force
                movementDir = Vector3.Lerp(movementDir, Vector3.ProjectOnPlane(movementDir, Vector3.up), steepnessRatio);
                modifiedAcceleration *= uphillResistance;
            }
        }
        
        
        
        
        m_UnitGoal = movementDir;

        Vector3 unitVel = rig.velocity.sqrMagnitude > 0.001f ? rig.velocity.normalized : Vector3.zero;

        float velDot = Vector3.Dot(m_UnitGoal, unitVel);

        float accelDot = Mathf.Max(accelerationFromDot.Evaluate(velDot), 0.4f);
        float maxAccelDot = Mathf.Max(maxAccelerationFromDot.Evaluate(velDot), 0.4f);

        float accel = modifiedAcceleration * accelDot;
        float maxAccel = maxAccelerationForce * maxAccelDot;

        float targetSpeed = maxSpeed * Mathf.Clamp01((playerInputs.magnitude));

        Vector3 goalVel = m_UnitGoal * targetSpeed;
        m_GoalVel = Vector3.MoveTowards(m_GoalVel, goalVel, accel * Time.fixedDeltaTime);

        Vector3 neededAccel = (m_GoalVel - rig.velocity) / Time.fixedDeltaTime;
        neededAccel = Vector3.ClampMagnitude(neededAccel, maxAccel);

        Vector3 forceScale = new Vector3(1, 0, 1);
        rig.AddForce(Vector3.Scale(neededAccel * rig.mass, forceScale));
    }*/
             #endregion

    private void ManageMovement()
    {
        
        // [MODIFIERS FOR PLAYER MOVEMENT & PLAYER IMPARTING VELOCITY] 
        //Air Movement modifiers
        float airTimeModifier = 1;

        if (!isGrounded)
        {
            airTimeModifier = 0.5f;
        }
        
        //Add ground velocity to player
        if (hitObject != null)
        {

            //fixed delta time before
            rig.AddForce(rig.velocity * Runner.DeltaTime, ForceMode.Impulse);
        }
        
        //If the player isn't moving, we don't need to calculate any new forces
        if (playerInputs.magnitude == 0) return;
        
        
        // [CALCULATING PLAYER VELOCITY MOVEMENT & DIRECTION]  

        // Project movement dir along a plane so that we can travel over slopes more efficently
        Vector3 movementDir = Vector3.ProjectOnPlane(desiredDir, groundNormal).normalized;
        m_UnitGoal = movementDir;



        // Calculate velocity for directions
        Vector3 unitVel = rig.velocity.sqrMagnitude > 0.3f ? rig.velocity.normalized : rig.transform.forward*0.3f;
        float velDot = Vector3.Dot(m_UnitGoal, unitVel);
                                  
        //Get potential acceleration change from current velocity
        float accelDot = Mathf.Max(accelerationFromDot.Evaluate(velDot), 0.4f);
        float maxAccelDot = Mathf.Max(maxAccelerationFromDot.Evaluate(velDot), 0.4f);

        float tempAcceleration = playerAcceleration;
        
        tempAcceleration*=Mathf.Clamp01(playerAccelerationCurve.Evaluate(rig.velocity.magnitude/maxSpeed)*2);


        float accel = tempAcceleration * accelDot;
        
        
        accel *= airTimeModifier;
        
        float maxAccel = maxAccelerationForce * maxAccelDot;
        
        //Calculate goal velocity for our player
        float targetSpeed = maxSpeed * Mathf.Clamp01(playerInputs.magnitude);
        
        

        Vector3 goalVel = m_UnitGoal * targetSpeed;
        m_GoalVel = Vector3.MoveTowards(m_GoalVel, goalVel, accel * Runner.DeltaTime);
        
        
        //calculate desired acceleration and clamp that to the max acceleration
        Vector3 neededAccel = (m_GoalVel - rig.velocity) / Runner.DeltaTime;
        neededAccel = Vector3.ClampMagnitude(neededAccel, maxAccel);

        
        // [UPHILL MODIFIERS FOR PLAYER MOVEMENT]
        
        // Compute uphill direction (the direction up the slope surface)
        Vector3 uphillDir = Vector3.ProjectOnPlane(Vector3.up, groundNormal);
        if (uphillDir.sqrMagnitude > 0.0001f)
            uphillDir.Normalize();
        else
            uphillDir = Vector3.zero;

        float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);






        if (slopeAngle > resistStartAngle && uphillDir != Vector3.zero)
        {
            // how much of the acceleration points uphill
            Vector3 uphillAccel = Vector3.Project(neededAccel, uphillDir);
            float uphillDot = Vector3.Dot(uphillAccel.normalized, uphillDir);

            if (uphillAccel.magnitude > 0f && uphillDot > 0f)
            {
                // Compute how steep the slope is (0–1)
                float steepnessRatio = Mathf.InverseLerp(resistStartAngle, resistFullAngle, slopeAngle);

                // Reduce uphill acceleration only
                uphillResistance = Mathf.Lerp(1f, 0f, steepnessRatio);


                Vector3 reducedUphill = uphillAccel * uphillResistance;

                reducedUphill *= gripRatio;
                
                Vector3 otherAccel = neededAccel - uphillAccel;

                neededAccel = otherAccel + reducedUphill;
                // Ensure total accel doesn't exceed allowed cap
                neededAccel = Vector3.ClampMagnitude(neededAccel, maxAccel);
            }
        }

    // --- Apply force ---
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

    public float jumpSlowDown = 0.1f;
    IEnumerator JumpSlowDown()
    {
        float targetVelocity = 0;
        float velocityY = rig.velocity.y;

        float t = 0;

        while (velocityY > 1f)
        {
            velocityY=rig.velocity.y;
            velocityY = Mathf.Lerp(velocityY, targetVelocity, t);
            t += jumpSlowDown*Runner.DeltaTime;
            
            rig.velocity = new Vector3(rig.velocity.x, velocityY, rig.velocity.z);
            yield return new WaitForEndOfFrame();
        }



        yield return null;
    }

    private void CalculateJumpForce()
    {
        if (_isJumping)
        {
            rig.AddForce(Vector3.up * jumpForce*(1-(Time.time-_jumpTime)/jumpLength));
            
        }
    }
    void ManageJump()
    {
        if (_isJumping)
        {
            if (Time.time - _jumpTime > jumpLength || !inputs.Generic.PrimaryAction.IsPressed())
            {
                if (!inputs.Generic.PrimaryAction.IsPressed() & _isJumping)
                {
                    StartCoroutine(JumpSlowDown());
                }
                _isJumping = false;


                //StartCoroutine(JumpSlowDown());
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
                    rig.velocity = new Vector3(rig.velocity.x, 0, rig.velocity.z);
                    rig.AddForce(Vector3.up * jumpForce*1.3f, ForceMode.Impulse);

                }
            }
        }
        
        CalculateJumpForce();

      
    }
    
    // Update is called once per frame
    void Update()
    {
        ManagePlayerDirection();

    }

    Rigidbody hitObject=null;
    
    public override void FixedUpdateNetwork()
    {
        hitObject = null;
        ManageFriction();
        ManageSpring();
        ManageUprightForce();
        ManageMovement();
        ManageJump();
    }
}