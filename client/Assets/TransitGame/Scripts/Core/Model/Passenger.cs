namespace TransitCore.Model
{
    public class Passenger
    {
        public int Id { get; }
        public StationShape Target { get; }

        public Passenger(int id, StationShape target)
        {
            Id = id;
            Target = target;
        }
    }
}
