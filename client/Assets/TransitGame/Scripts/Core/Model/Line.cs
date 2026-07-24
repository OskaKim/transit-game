using System.Collections.Generic;

namespace TransitCore.Model
{
    public class Line
    {
        public int Id { get; }
        public int ColorIndex { get; }
        // Ordered station ids. Trains shuttle back and forth along this list,
        // or keep circulating in one direction when IsLoop is true.
        public List<int> Stations { get; } = new List<int>();
        public bool IsLoop { get; set; }

        public Line(int id, int colorIndex)
        {
            Id = id;
            ColorIndex = colorIndex;
        }

        public bool Contains(int stationId) => Stations.Contains(stationId);

        public bool IsEndpoint(int stationId) =>
            Stations.Count > 0 && (Stations[0] == stationId || Stations[Stations.Count - 1] == stationId);
    }
}
