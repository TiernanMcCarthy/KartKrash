using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GameUI
{
    public class PlayerIconInfo : MonoBehaviour
    {
        [SerializeField] private BasePlayer basePlayer;

        [SerializeField] private TMP_Text playerName;

        [SerializeField] private TMP_Text playerScore;


        public void Setup(BasePlayer player)
        {
            basePlayer = player;
        }
        
        private void Update()
        {
            playerName.text = basePlayer.name;
            playerScore.text = basePlayer.score.ToString();
        }
    }
}
