namespace NeonCity.Sim
{
    /// <summary>
    /// All tuning numbers in one place. Public fields so a Unity ScriptableObject
    /// can wrap or copy it for live tuning. Defaults are a first guess, not balanced.
    /// </summary>
    public sealed class SimConfig
    {
        // --- Map ---
        public int Width = 64;
        public int Height = 64;

        // --- Time ---
        /// <summary>Sim ticks per in-game month. Economy settles at each month end.</summary>
        public int TicksPerMonth = 30;

        // --- Money ---
        public long StartingMoney = 20000;
        public long CostZonePerCell = 10;
        public long CostRoadPerCell = 10;
        public long CostPowerPlant = 3000;
        public long CostBulldozePerCell = 1;
        public long RoadUpkeepPerMonth = 1;
        public long PowerPlantUpkeepPerMonth = 100;
        public double TaxPerResidentPerMonth = 0.5;
        public double TaxPerJobPerMonth = 0.5;
        /// <summary>Consecutive month-ends with money below zero before bankruptcy.</summary>
        public int MonthsInDebtBeforeBankruptcy = 6;

        // --- Power ---
        public int PowerPlantCapacity = 400;
        public int PowerUsePerZoneCell = 1;
        public int PowerUsePerDensity = 2;

        // --- Zones ---
        public int MaxDensity = 3;
        /// <summary>Chebyshev distance (in cells) within which a road gives access.</summary>
        public int RoadAccessRadius = 2;
        public int ResidentsPerDensity = 20;
        public int CommercialJobsPerDensity = 10;
        public int IndustrialJobsPerDensity = 15;

        // --- RCI demand ---
        /// <summary>Share of residents who need a job.</summary>
        public double WorkforceRatio = 0.45;
        /// <summary>People who want to move in regardless of jobs (bootstraps an empty city).</summary>
        public double BaseResidentialDemand = 100;
        public double BaseCommercialDemand = 20;
        /// <summary>External demand for exports (bootstraps industry).</summary>
        public double BaseIndustrialDemand = 60;
        public double CommercialJobsPerResident = 0.20;
        public double IndustrialJobsPerResident = 0.25;
        /// <summary>Softness k in demand = raw / (|raw| + k). Higher = gentler curve.</summary>
        public double DemandSoftness = 200;

        // --- Growth ---
        /// <summary>|demand| must exceed this before a zone grows or shrinks from demand.</summary>
        public double GrowthThreshold = 0.02;
        /// <summary>Per-tick growth chance for a serviced cell at demand 1.0.</summary>
        public double GrowthChance = 0.08;
        /// <summary>Per-tick decay chance for an unserviced built cell (or at demand -1.0).</summary>
        public double DecayChance = 0.05;
    }
}
