using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;

public class PlayerInputManager : MonoBehaviour
{

    DriveInputData drivingInput;

    CarInput carInput;

    
    // Start is called before the first frame update
    void Start()
    {
        carInput = new CarInput();

        carInput.Enable();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public DriveInputData GetDrivingInput()
    {
        drivingInput = new DriveInputData();

        drivingInput.steerTarget = carInput.Generic.Steer.ReadValue<Vector2>().x;
        
        drivingInput.accelerate = carInput.Generic.Accelerate.ReadValue<float>();
        
        drivingInput.brake= carInput.Generic.Brake.ReadValue<float>();
        

        
        
        return drivingInput;
    }
    
}
