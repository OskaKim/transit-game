using TransitCore.Model;

namespace TransitCore.Simulation
{
    public class GameRules
    {
        readonly SimConfig _cfg;

        public GameRules(SimConfig cfg)
        {
            _cfg = cfg;
        }

        /// <summary>Advances overcrowd timers. Returns true when game over triggers.</summary>
        public bool Tick(float dt, TransitNetwork network)
        {
            bool gameOver = false;
            foreach (var s in network.Stations.Values)
            {
                if (s.Queue.Count > _cfg.StationQueueLimit)
                {
                    s.OvercrowdTimer += dt;
                    if (s.OvercrowdTimer >= _cfg.OvercrowdGrace) gameOver = true;
                }
                else
                {
                    // Recover twice as fast as it builds up.
                    s.OvercrowdTimer = System.Math.Max(0f, s.OvercrowdTimer - dt * 2f);
                }
            }
            return gameOver;
        }
    }
}
