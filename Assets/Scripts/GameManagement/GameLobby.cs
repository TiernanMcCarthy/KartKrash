using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using GameUI;

namespace LobbySystem
{
    public class GameLobby : NetworkBehaviour
    {


        private Dictionary<BasePlayer, NetworkObject> _spawnedCharacters = new Dictionary<BasePlayer, NetworkObject>();

        [SerializeField] private Transform playerLobbyTransform;

        [SerializeField] private PlayerIconInfo playerIconPrefab;

        private void CreatePlayerEntry(BasePlayer player)
        {
            PlayerIconInfo playerIcon = Instantiate(playerIconPrefab, playerLobbyTransform);
            playerIcon.Setup(player);
        }

        public void AddPlayer(BasePlayer player)
        {
            _spawnedCharacters.Add(player, player.GetNetworkObject());
        }

        public void RemovePlayer(BasePlayer player)
        {
            _spawnedCharacters.Remove(player);
        }

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
