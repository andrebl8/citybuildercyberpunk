using System.Collections.Generic;

namespace NeonCity.Sim
{
    public readonly struct PowerStats
    {
        public readonly int Capacity;
        public readonly int Demand;
        public readonly int UnpoweredZones;

        public PowerStats(int capacity, int demand, int unpoweredZones)
        {
            Capacity = capacity;
            Demand = demand;
            UnpoweredZones = unpoweredZones;
        }

        public override string ToString() => $"{Demand}/{Capacity} MW, {UnpoweredZones} unpowered";
    }

    /// <summary>
    /// Power flows from plants through any non-empty cell (roads, zones, plants) to its 4 neighbours.
    /// Each connected network shares the capacity of its plants; when demand exceeds it,
    /// cells nearest a plant (BFS order) are served first and the rest brown out.
    /// </summary>
    internal static class PowerGrid
    {
        public static int Consumption(SimConfig cfg, in Cell c) =>
            c.Kind == TileKind.Zoned ? cfg.PowerUsePerZoneCell + c.Density * cfg.PowerUsePerDensity : 0;

        /// <summary>Writes the new powered flag per cell into <paramref name="powered"/>.</summary>
        public static PowerStats Compute(SimConfig cfg, Cell[] cells, int width, int height, bool[] powered)
        {
            int n = cells.Length;
            var component = new int[n];
            for (int i = 0; i < n; i++) component[i] = -1;
            for (int i = 0; i < n; i++) powered[i] = false;

            var members = new List<int>();
            var plants = new List<int>();
            var queue = new Queue<int>();
            int totalCapacity = 0, totalDemand = 0;
            int componentId = 0;

            for (int start = 0; start < n; start++)
            {
                if (cells[start].Kind != TileKind.PowerPlant || component[start] != -1) continue;

                // Pass 1: collect the whole network this plant belongs to.
                members.Clear();
                plants.Clear();
                component[start] = componentId;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int i = queue.Dequeue();
                    members.Add(i);
                    if (cells[i].Kind == TileKind.PowerPlant) plants.Add(i);
                    foreach (int nb in Neighbours(i, width, height))
                    {
                        if (component[nb] != -1 || cells[nb].Kind == TileKind.Empty) continue;
                        component[nb] = componentId;
                        queue.Enqueue(nb);
                    }
                }

                // Pass 2: serve the network outward from all its plants until capacity runs out.
                int remaining = plants.Count * cfg.PowerPlantCapacity;
                totalCapacity += remaining;
                var visited = new HashSet<int>(plants);
                foreach (int p in plants) queue.Enqueue(p);
                while (queue.Count > 0)
                {
                    int i = queue.Dequeue();
                    int use = Consumption(cfg, cells[i]);
                    totalDemand += use;
                    if (use <= remaining)
                    {
                        remaining -= use;
                        powered[i] = true;
                    }

                    foreach (int nb in Neighbours(i, width, height))
                    {
                        if (component[nb] != componentId || !visited.Add(nb)) continue;
                        queue.Enqueue(nb);
                    }
                }

                componentId++;
            }

            int unpoweredZones = 0;
            for (int i = 0; i < n; i++)
            {
                if (cells[i].Kind != TileKind.Zoned) continue;
                if (!powered[i]) unpoweredZones++;
                if (component[i] == -1) totalDemand += Consumption(cfg, cells[i]);
            }

            return new PowerStats(totalCapacity, totalDemand, unpoweredZones);
        }

        private static IEnumerable<int> Neighbours(int i, int width, int height)
        {
            int x = i % width, y = i / width;
            if (x > 0) yield return i - 1;
            if (x < width - 1) yield return i + 1;
            if (y > 0) yield return i - width;
            if (y < height - 1) yield return i + width;
        }
    }
}
