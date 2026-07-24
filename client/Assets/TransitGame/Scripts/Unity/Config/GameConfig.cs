using TransitCore.Simulation;
using UnityEngine;

namespace TransitGame
{
    [CreateAssetMenu(menuName = "TransitGame/GameConfig", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Stations")]
        public int initialStationCount = 3;
        public float stationSpawnInterval = 20f;
        public float stationSpawnIntervalMin = 8f;
        [Range(0.5f, 1f)] public float stationSpawnDecay = 0.92f;
        public float minStationDistance = 2.2f;

        [Header("Passengers")]
        public float passengerSpawnMin = 3f;
        public float passengerSpawnMax = 6f;
        public int stationQueueLimit = 6;
        public float overcrowdGrace = 8f;
        public int transferPenalty = 2;

        [Header("Trains & Lines")]
        public float trainSpeed = 2f;
        public int trainCapacity = 6;
        public int maxLines = 3;
        public float dwellTime = 0.6f;

        [Header("World")]
        public float worldWidth = 14f;
        public float worldHeight = 8f;

        [Header("Random")]
        public bool useRandomSeed = true;
        public int seed = 12345;

        public SimConfig ToSimConfig() => new SimConfig
        {
            InitialStationCount = initialStationCount,
            StationSpawnInterval = stationSpawnInterval,
            StationSpawnIntervalMin = stationSpawnIntervalMin,
            StationSpawnDecay = stationSpawnDecay,
            MinStationDistance = minStationDistance,
            PassengerSpawnMin = passengerSpawnMin,
            PassengerSpawnMax = passengerSpawnMax,
            StationQueueLimit = stationQueueLimit,
            OvercrowdGrace = overcrowdGrace,
            TransferPenalty = transferPenalty,
            TrainSpeed = trainSpeed,
            TrainCapacity = trainCapacity,
            MaxLines = maxLines,
            DwellTime = dwellTime,
            WorldWidth = worldWidth,
            WorldHeight = worldHeight,
            Seed = seed,
        };
    }
}
