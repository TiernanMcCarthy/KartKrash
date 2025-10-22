using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{

    public CameraBehaviour activeCamera;

    private CinemachineBrain _cinemachineBrain;
    // Start is called before the first frame update
    void Start()
    {
        if (_cinemachineBrain == null)
        {
            _cinemachineBrain=FindObjectOfType<CinemachineBrain>();
        }
        
        ArrangeNewCamera();
    }

    private void ArrangeNewCamera()
    {
        DeselectCamera(activeCamera);

        if (_cinemachineBrain != null)
        {
            activeCamera = _cinemachineBrain.ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CameraBehaviour>();

            if (activeCamera != null)
            {
                SelectCamera(activeCamera);
            }
        }
    }

    public void SelectCamera(CameraBehaviour cam)
    {
        if (cam != null)
        {
            cam.OnActivated();
            cam.ManageMouse();
        }
    }

    public void DeselectCamera(CameraBehaviour cam)
    {
        if (cam != null)
        {
            cam.OnDeactivated();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (activeCamera == null)
        {
            ArrangeNewCamera();
        }
        if (activeCamera.gameObject != _cinemachineBrain.ActiveVirtualCamera.VirtualCameraGameObject)
        {
            ArrangeNewCamera();
        }
    }
}
