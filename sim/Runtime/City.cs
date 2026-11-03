using System;
using System.Collections.Generic;

namespace NeonCity.Sim
{
    public readonly struct MonthReport
    {
        public readonly int Month;
        public readonly long Income;
        public readonly long Expenses;
        public readonly long Money;

        public MonthReport(int month, long income, long expenses, long money)
        {
            Month = month;
            Income = income;
            Expenses = expenses;
            Money = money;
        }

        public long Net => Income - Expenses;

        public override string ToString() => $"Month {Month}: +{Income} -{Expenses} = {Money}";
    }

    /// <summary>
    /// The whole simulation state and its rules. Engine-agnostic: the Unity side calls
    /// <see cref="Execute"/> for player actions and <see cref="Step"/> on a fixed timer,
    /// and listens to the events to update visuals and UI.
    /// </summary>
    public sealed class City
    {
        private readonly Cell[] _cells;
        private readonly bool[] _poweredScratch;
        private readonly SplitMix64 _rng;
        private readonly List<LoggedCommand> _log = new List<LoggedCommand>();
        private readonly List<CellEdit> _editScratch = new List<CellEdit>();
        private readonly int _growthStride;

        private bool _accessDirty = true;
        private int _resDensity, _comDensity, _indDensity;
        private int _roadCount, _plantCount;
        private int _monthsInDebt;

        public City(SimConfig config, ulong seed)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            if (config.Width <= 0 || config.Height <= 0) throw new ArgumentException("Map size must be positive.");
            if (config.TicksPerMonth <= 0) throw new ArgumentException("TicksPerMonth must be positive.");

            Seed = seed;
            Width = config.Width;
            Height = config.Height;
            _cells = new Cell[Width * Height];
            _poweredScratch = new bool[_cells.Length];
            _rng = new SplitMix64(seed);
            _growthStride = PickStride(_cells.Length);
            Money = config.StartingMoney;
            RecomputeDemand();
        }

        // ---------------------------------------------------------------- events

        /// <summary>A cell's kind, zone, density, power or road access changed.</summary>
        public event Action<GridPos>? CellChanged;
        public event Action<long>? MoneyChanged;
        public event Action<MonthReport>? MonthEnded;
        public event Action? WentBankrupt;

        // ---------------------------------------------------------------- state

        public SimConfig Config { get; }
        public ulong Seed { get; }
        public int Width { get; }
        public int Height { get; }

        /// <summary>Completed ticks since the city was founded.</summary>
        public int Tick { get; private set; }
        public int Month => Tick / Config.TicksPerMonth;
        public long Money { get; private set; }
        public bool IsBankrupt { get; private set; }
        public int MonthsInDebt => _monthsInDebt;

        public RciDemand Demand { get; private set; }
        public PowerStats Power { get; private set; }

        public int Population => _resDensity * Config.ResidentsPerDensity;
        public int CommercialJobs => _comDensity * Config.CommercialJobsPerDensity;
        public int IndustrialJobs => _indDensity * Config.IndustrialJobsPerDensity;
        public int Jobs => CommercialJobs + IndustrialJobs;
        public int RoadCount => _roadCount;
        public int PowerPlantCount => _plantCount;

        /// <summary>Every successful command with the tick it ran at. Seed + log = full save.</summary>
        public IReadOnlyList<LoggedCommand> CommandLog => _log;

        public Cell GetCell(int x, int y) => _cells[Index(x, y)];
        public Cell GetCell(GridPos p) => _cells[Index(p.X, p.Y)];

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool InBounds(GridPos p) => InBounds(p.X, p.Y);
        public bool InBounds(GridRect r) => InBounds(r.MinX, r.MinY) && InBounds(r.MaxX, r.MaxY);

        internal int Index(int x, int y) => y * Width + x;

        // ---------------------------------------------------------------- commands

        /// <summary>Dry run: what would this command cost, and would it succeed?</summary>
        public CommandResult Preview(CityCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (IsBankrupt) return new CommandResult(CommandError.Bankrupt, 0, 0);

            _editScratch.Clear();
            var error = command.Plan(this, _editScratch, out long cost);
            if (error == CommandError.None && cost > Money) error = CommandError.InsufficientFunds;
            return new CommandResult(error, cost, _editScratch.Count);
        }

        /// <summary>Validate and apply a player action. All edits apply, or none.</summary>
        public CommandResult Execute(CityCommand command)
        {
            var preview = Preview(command);
            if (!preview.Success) return preview;

            foreach (var edit in _editScratch) SetCell(edit.Index, edit.NewCell);
            _editScratch.Clear();

            if (preview.Cost != 0) AddMoney(-preview.Cost);
            _log.Add(new LoggedCommand(Tick, command));

            // Refresh derived state now so the view never shows a stale tick.
            RefreshRoadAccess();
            RecomputePower();
            return preview;
        }

        // ---------------------------------------------------------------- simulation

        /// <summary>Advance one tick: power → growth/decay → (month end) economy.</summary>
        public void Step()
        {
            if (IsBankrupt) return;

            if (_accessDirty) RefreshRoadAccess();
            RecomputePower();
            GrowAndDecay();

            Tick++;
            if (Tick % Config.TicksPerMonth == 0) SettleMonth();
        }

        public void Step(int ticks)
        {
            for (int i = 0; i < ticks; i++) Step();
        }

        private void GrowAndDecay()
        {
            var cfg = Config;
            int n = _cells.Length;
            int start = _rng.NextInt(n);

            // Visit every cell once in a scrambled order so growth isn't biased to one corner.
            for (int k = 0; k < n; k++)
            {
                int i = (int)((start + (long)k * _growthStride) % n);
                var cell = _cells[i];
                if (cell.Kind != TileKind.Zoned) continue;

                double demand = Demand.For(cell.Zone);
                bool serviced = cell.Powered && cell.RoadAccess;

                if (serviced && demand > cfg.GrowthThreshold && cell.Density < cfg.MaxDensity)
                {
                    if (_rng.NextDouble() < cfg.GrowthChance * demand)
                    {
                        cell.Density++;
                        SetCell(i, cell);
                    }
                }
                else if (cell.Density > 0 && (!serviced || demand < -cfg.GrowthThreshold))
                {
                    double chance = serviced ? cfg.DecayChance * -demand : cfg.DecayChance;
                    if (_rng.NextDouble() < chance)
                    {
                        cell.Density--;
                        SetCell(i, cell);
                    }
                }
            }
        }

        private void SettleMonth()
        {
            var cfg = Config;
            long income = (long)Math.Round(Population * cfg.TaxPerResidentPerMonth + Jobs * cfg.TaxPerJobPerMonth);
            long expenses = _roadCount * cfg.RoadUpkeepPerMonth + _plantCount * cfg.PowerPlantUpkeepPerMonth;
            AddMoney(income - expenses);

            _monthsInDebt = Money < 0 ? _monthsInDebt + 1 : 0;
            MonthEnded?.Invoke(new MonthReport(Month, income, expenses, Money));

            if (_monthsInDebt >= cfg.MonthsInDebtBeforeBankruptcy)
            {
                IsBankrupt = true;
                WentBankrupt?.Invoke();
            }
        }

        // ---------------------------------------------------------------- derived state

        private void RecomputePower()
        {
            Power = PowerGrid.Compute(Config, _cells, Width, Height, _poweredScratch);
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i].Powered == _poweredScratch[i]) continue;
                _cells[i].Powered = _poweredScratch[i];
                RaiseCellChanged(i);
            }
        }

        private void RefreshRoadAccess()
        {
            int r = Config.RoadAccessRadius;
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int i = Index(x, y);
                bool access = _cells[i].Kind == TileKind.Zoned && HasRoadWithin(x, y, r);
                if (_cells[i].RoadAccess == access) continue;
                _cells[i].RoadAccess = access;
                RaiseCellChanged(i);
            }

            _accessDirty = false;
        }

        private bool HasRoadWithin(int cx, int cy, int r)
        {
            int minX = Math.Max(0, cx - r), maxX = Math.Min(Width - 1, cx + r);
            int minY = Math.Max(0, cy - r), maxY = Math.Min(Height - 1, cy + r);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                if (_cells[Index(x, y)].Kind == TileKind.Road) return true;
            return false;
        }

        private void RecomputeDemand() =>
            Demand = RciDemand.Compute(Config, Population, CommercialJobs, IndustrialJobs);

        // ---------------------------------------------------------------- mutation

        private void SetCell(int i, Cell next)
        {
            var prev = _cells[i];
            Count(prev, -1);
            Count(next, +1);
            if (prev.Kind != next.Kind) _accessDirty = true;

            _cells[i] = next;
            RecomputeDemand();
            RaiseCellChanged(i);
        }

        private void Count(in Cell c, int sign)
        {
            switch (c.Kind)
            {
                case TileKind.Road: _roadCount += sign; break;
                case TileKind.PowerPlant: _plantCount += sign; break;
                case TileKind.Zoned:
                    int d = c.Density * sign;
                    if (c.Zone == ZoneType.Residential) _resDensity += d;
                    else if (c.Zone == ZoneType.Commercial) _comDensity += d;
                    else if (c.Zone == ZoneType.Industrial) _indDensity += d;
                    break;
            }
        }

        private void AddMoney(long delta)
        {
            if (delta == 0) return;
            Money += delta;
            MoneyChanged?.Invoke(Money);
        }

        private void RaiseCellChanged(int i) => CellChanged?.Invoke(new GridPos(i % Width, i / Width));

        // ---------------------------------------------------------------- diagnostics

        /// <summary>FNV-1a hash of the full sim state. Equal hashes ⇒ identical cities (for replay tests).</summary>
        public ulong ComputeStateHash()
        {
            const ulong prime = 1099511628211UL;
            ulong h = 14695981039346656037UL;
            unchecked
            {
                void Mix(ulong v)
                {
                    for (int b = 0; b < 8; b++)
                    {
                        h ^= (v >> (b * 8)) & 0xFF;
                        h *= prime;
                    }
                }

                foreach (var c in _cells)
                    Mix((ulong)c.Kind | ((ulong)c.Zone << 8) | ((ulong)c.Density << 16) |
                        ((c.Powered ? 1UL : 0) << 24) | ((c.RoadAccess ? 1UL : 0) << 25));
                Mix((ulong)Money);
                Mix((ulong)Tick);
                Mix(_rng.State);
                Mix((ulong)_monthsInDebt);
            }

            return h;
        }

        private static int PickStride(int n)
        {
            // Large-ish odd stride that is coprime with n, so (start + k*stride) % n visits every cell.
            for (int s = n / 2 + 1; s < n + 64; s++)
                if (Gcd(s, n) == 1) return s % n == 0 ? 1 : s % n;
            return 1;
        }

        private static int Gcd(int a, int b)
        {
            while (b != 0) (a, b) = (b, a % b);
            return a;
        }
    }
}
