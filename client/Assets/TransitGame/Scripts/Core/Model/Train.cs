using System.Collections.Generic;

namespace TransitCore.Model
{
    public class Train
    {
        public int Id;
        public int LineId;
        // Position = lerp(line.Stations[FromIndex], line.Stations[ToIndex], Progress)
        public int FromIndex;
        public int ToIndex;
        public float Progress;
        public int Direction = 1;
        public float DwellRemaining;
        public List<Passenger> Riders = new List<Passenger>();
    }
}
