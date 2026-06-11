using System.Numerics;
using NUnit.Framework;
using TransitCore.Model;
using TransitCore.Simulation;

namespace TransitGame.Tests
{
    public class CoreSimulationTests
    {
        static SimConfig QuietConfig() => new SimConfig
        {
            InitialStationCount = 0,
            StationSpawnInterval = 99999f,
            PassengerSpawnMin = 99999f,
            PassengerSpawnMax = 99999f,
            Seed = 42,
        };

        [Test]
        public void SingleLine_DeliversPassenger()
        {
            var engine = new SimulationEngine(QuietConfig());
            var a = engine.AddStationAt(StationShape.Circle, new Vector2(0, 0));
            var b = engine.AddStationAt(StationShape.Triangle, new Vector2(4, 0));
            Assert.IsTrue(engine.TryCreateLine(a.Id, b.Id, out _));

            // Circle station: only Triangle exists as a target -> deterministic.
            engine.SpawnPassenger(a.Id);
            Assert.AreEqual(1, a.Queue.Count);

            for (int i = 0; i < 1200 && engine.Score == 0; i++)
                engine.Tick(0.05f);

            Assert.AreEqual(1, engine.Score, "passenger should be delivered to the triangle station");
            Assert.AreEqual(0, a.Queue.Count);
        }

        [Test]
        public void Router_FindsTransferRoute()
        {
            var engine = new SimulationEngine(QuietConfig());
            var a = engine.AddStationAt(StationShape.Circle, new Vector2(0, 0));
            var b = engine.AddStationAt(StationShape.Triangle, new Vector2(4, 0));
            var c = engine.AddStationAt(StationShape.Square, new Vector2(8, 0));
            engine.TryCreateLine(a.Id, b.Id, out _);
            engine.TryCreateLine(b.Id, c.Id, out _);

            var step = engine.Router.NextStep(a.Id, StationShape.Square);
            Assert.IsNotNull(step, "route via transfer at B should exist");
            Assert.AreEqual(b.Id, step.Value.NextStationId);
        }

        [Test]
        public void Router_ReturnsNullWhenNoRoute()
        {
            var engine = new SimulationEngine(QuietConfig());
            var a = engine.AddStationAt(StationShape.Circle, new Vector2(0, 0));
            engine.AddStationAt(StationShape.Square, new Vector2(8, 0));

            Assert.IsNull(engine.Router.NextStep(a.Id, StationShape.Square));
        }

        [Test]
        public void Router_CacheInvalidatedOnNetworkChange()
        {
            var engine = new SimulationEngine(QuietConfig());
            var a = engine.AddStationAt(StationShape.Circle, new Vector2(0, 0));
            var b = engine.AddStationAt(StationShape.Square, new Vector2(4, 0));

            Assert.IsNull(engine.Router.NextStep(a.Id, StationShape.Square));
            engine.TryCreateLine(a.Id, b.Id, out _);
            Assert.IsNotNull(engine.Router.NextStep(a.Id, StationShape.Square));
        }

        [Test]
        public void TransferDelivery_AcrossTwoLines()
        {
            var engine = new SimulationEngine(QuietConfig());
            var a = engine.AddStationAt(StationShape.Circle, new Vector2(0, 0));
            var b = engine.AddStationAt(StationShape.Triangle, new Vector2(4, 0));
            var c = engine.AddStationAt(StationShape.Square, new Vector2(8, 0));
            engine.TryCreateLine(a.Id, b.Id, out _);
            engine.TryCreateLine(b.Id, c.Id, out _);

            a.Enqueue(new Passenger(999, StationShape.Square));

            for (int i = 0; i < 2400 && engine.Score == 0; i++)
                engine.Tick(0.05f);

            Assert.AreEqual(1, engine.Score, "passenger should reach the square station via transfer");
        }

        [Test]
        public void Overcrowding_TriggersGameOver()
        {
            var cfg = QuietConfig();
            cfg.StationQueueLimit = 2;
            cfg.OvercrowdGrace = 1f;
            var engine = new SimulationEngine(cfg);
            var a = engine.AddStationAt(StationShape.Circle, new Vector2(0, 0));
            for (int i = 0; i < 5; i++) a.Enqueue(new Passenger(i, StationShape.Triangle));

            bool fired = false;
            engine.GameOverTriggered += () => fired = true;
            for (int i = 0; i < 100; i++) engine.Tick(0.05f);

            Assert.IsTrue(engine.IsGameOver);
            Assert.IsTrue(fired);
        }
    }
}
