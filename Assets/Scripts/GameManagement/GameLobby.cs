using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using GameUI;

namespace LobbySystem
{
    public class GameLobby : NetworkBehaviour
    {
        private Dictionary<BasePlayer, NetworkObject> _spawnedCharacters {get; set;}

        [SerializeField] private Transform playerLobbyTransform;

        [SerializeField] private PlayerIconInfo playerIconPrefab;


        public void OnLocalPlayerConnected()
        {
            BasePlayer[] players=FindObjectsOfType<BasePlayer>();

            for (int i = 0; i < players.Length; i++)
            {
                _spawnedCharacters.Add(players[i], players[i].GetNetworkObject());
                CreatePlayerEntry(players[i]);
            }
        }

        private void CreatePlayerEntry(BasePlayer player)
        {
            PlayerIconInfo playerIcon = Instantiate(playerIconPrefab, playerLobbyTransform);
            playerIcon.Setup(player);
        }

        public void AddPlayer(BasePlayer player)
        {
            if (player.HasInputAuthority)
            {
                OnLocalPlayerConnected();
            }
            else
            {
                _spawnedCharacters.Add(player, player.GetNetworkObject());
                CreatePlayerEntry(player);
            }
        }

        public void RemovePlayer(BasePlayer player)
        {
            _spawnedCharacters.Remove(player);
        }

        // Start is called before the first frame update
        void Start()
        {
            _spawnedCharacters = new Dictionary<BasePlayer, NetworkObject>();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
