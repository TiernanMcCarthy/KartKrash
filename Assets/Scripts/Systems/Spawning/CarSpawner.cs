using Fusion;
using UnityEngine;

public class CarSpawner : SimulationBehaviour, IPlayerJoined
{
    public GameObject CarPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            Runner.Spawn(CarPrefab, new Vector3(0, 1, 0), Quaternion.identity);
        }
    }
}