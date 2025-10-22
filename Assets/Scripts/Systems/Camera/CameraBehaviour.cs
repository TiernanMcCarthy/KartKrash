using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraBehaviour : MonoBehaviour
{

    [SerializeField] private bool lockMouse = false;
    [SerializeField] private bool hideMouse = false;
    public CinemachineVirtualCamera  attachedCamera;

    public virtual void ManageMouse()
    {
        Cursor.lockState = lockMouse ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !hideMouse;
    }
    public virtual void OnActivated()
    {

    }

    public virtual void OnDeactivated()
    {
        
    }
    // Start is called before the first frame update
    void Start()
    {
        attachedCamera=GetComponent<CinemachineVirtualCamera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (hideMouse && Keyboard.current.gKey.wasPressedThisFrame)
        {
            Destroy(gameObject);
        }
    }
}
