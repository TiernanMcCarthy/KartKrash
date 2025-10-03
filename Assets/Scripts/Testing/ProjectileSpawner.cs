using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ProjectileSpawner : MonoBehaviour
{

    [SerializeField] private Rigidbody prefab;

    public float force = 500;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Rigidbody spawned= Instantiate(prefab);

            spawned.transform.position= Camera.main.transform.position;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            spawned.transform.forward = ray.direction;

            spawned.AddForce(spawned.transform.forward*force);
        }
    }
}
