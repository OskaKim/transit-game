using System.Collections.Generic;
using System.Numerics;

namespace TransitCore.Model
{
    public class Station
    {
        public int Id { get; }
        public StationShape Shape { get; }
        public Vector2 Position { get; }
        public List<Passenger> Queue { get; } = new List<Passenger>();
        // Incremented on every queue mutation so views can rebuild only when needed.
        public int QueueVersion { get; private set; }
        public float OvercrowdTimer { get; set; }

        public Station(int id, StationShape shape, Vector2 position)
        {
            Id = id;
            Shape = shape;
            Position = position;
        }

        public void Enqueue(Passenger passenger)
        {
            Queue.Add(passenger);
            QueueVersion++;
        }

        public bool Remove(Passenger passenger)
        {
            if (!Queue.Remove(passenger)) return false;
            QueueVersion++;
            return true;
        }
    }
}
