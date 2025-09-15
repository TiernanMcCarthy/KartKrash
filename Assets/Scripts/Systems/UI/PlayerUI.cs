using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    
    
    public static PlayerUI instance;
    
    
    [SerializeField] TMP_Text carSpeedText;

    private NetworkedVehicle playerVehicle;
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    public void AssignVehicle(NetworkedVehicle playerVehicle)
    {
        this.playerVehicle = playerVehicle;
    }

    // Update is called once per frame
    void Update()
    {
        if (playerVehicle != null)
        {
            float speed = playerVehicle.GetCarSpeed();
            if (Mathf.Abs(speed) < 0.12f)
            {
                speed = 0.0f;
            }
            carSpeedText.text = string.Format("{0} KPH", speed.ToString("F1"));
        }
    }
}
