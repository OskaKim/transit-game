namespace TransitCore.Simulation
{
    public class SimConfig
    {
        public int InitialStationCount = 3;
        public float StationSpawnInterval = 20f;
        public float StationSpawnIntervalMin = 8f;
        public float StationSpawnDecay = 0.92f;
        public float MinStationDistance = 2.2f;

        public float PassengerSpawnMin = 3f;
        public float PassengerSpawnMax = 6f;
        public int StationQueueLimit = 6;
        public float OvercrowdGrace = 8f;
        public int TransferPenalty = 2;

        public float TrainSpeed = 2f;
        public int TrainCapacity = 6;
        public int MaxLines = 3;
        public float DwellTime = 0.6f;

        public float WorldWidth = 14f;
        public float WorldHeight = 8f;
        public int Seed = 12345;
    }
}
