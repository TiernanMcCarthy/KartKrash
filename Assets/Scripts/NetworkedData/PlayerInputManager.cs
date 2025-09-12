using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        drivingInput.steerTarget = carInput.Generic.Steer.ReadValue<float>();


        return drivingInput;
    }
    
}
