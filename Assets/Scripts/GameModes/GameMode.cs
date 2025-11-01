using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;



namespace GameModes
{
    //Template

    class SpawnSystem
    {
    }


    /// <summary>
    /// GameModes manage the game state, how players respawn, scoring, victory conditions e.t.c
    /// </summary>
    public class GameMode : NetworkBehaviour
    {
        
        [SerializeField] private SpawnSystem spawningSystem;
        
        
        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}
