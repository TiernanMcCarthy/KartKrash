using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    //Temporary UI
    [SerializeField]private TMP_Text playerSpeed; 
    
    [SerializeField]public Rigidbody playerRigidbody;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        playerSpeed.text = string.Format("Speed:{0}", playerRigidbody.velocity.magnitude.ToString("F2"));
    }
}
