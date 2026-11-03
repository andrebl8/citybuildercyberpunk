using System;
using System.Collections.Generic;

namespace NeonCity.Sim
{
    public enum CommandError
    {
        None = 0,
        OutOfBounds,
        Blocked,
        InsufficientFunds,
        NothingToDo,
        Bankrupt,
        InvalidArgument,
    }

    public readonly struct CommandResult
    {
        public readonly CommandError Error;
        public readonly long Cost;
        public readonly int CellsChanged;

        public CommandResult(CommandError error, long cost, int cellsChanged)
        {
            Error = error;
            Cost = cost;
            CellsChanged = cellsChanged;
        }

        public bool Success => Error == CommandError.None;

        public override string ToString() => Success ? $"OK cost={Cost} cells={CellsChanged}" : Error.ToString();
    }

    internal readonly struct CellEdit
    {
        public readonly int Index;
        public readonly Cell NewCell;

        public CellEdit(int index, Cell newCell)
        {
            Index = index;
            NewCell = newCell;
        }
    }

    /// <summary>
    /// A player action. Commands are immutable data so they can be logged and replayed.
    /// Each command plans its edits against the current city; <see cref="City.Execute"/>
    /// then checks funds and applies all edits or none.
    /// </summary>
    public abstract class CityCommand
    {
        internal abstract CommandError Plan(City city, List<CellEdit> edits, out long cost);
    }

    /// <summary>Zone a rectangle. Skips cells that can't be zoned (roads, plants, built lots).</summary>
    public sealed class ZoneCommand : CityCommand
    {
        public GridRect Area { get; }
        public ZoneType Zone { get; }

        public ZoneCommand(GridRect area, ZoneType zone)
        {
            Area = area;
            Zone = zone;
        }

        internal override CommandError Plan(City city, List<CellEdit> edits, out long cost)
        {
            cost = 0;
            if (Zone == ZoneType.None) return CommandError.InvalidArgument;
            if (!city.InBounds(Area)) return CommandError.OutOfBounds;

            for (int y = Area.MinY; y <= Area.MaxY; y++)
            for (int x = Area.MinX; x <= Area.MaxX; x++)
            {
                var cell = city.GetCell(x, y);
                bool emptyLot = cell.Kind == TileKind.Empty ||
                                (cell.Kind == TileKind.Zoned && cell.Density == 0 && cell.Zone != Zone);
                if (!emptyLot) continue;
                edits.Add(new CellEdit(city.Index(x, y), Cell.Zoned(Zone)));
                cost += city.Config.CostZonePerCell;
            }

            return edits.Count == 0 ? CommandError.NothingToDo : CommandError.None;
        }
    }

    /// <summary>
    /// Build road along a path (e.g. from <see cref="RoadPath.LShape"/>). Existing road is
    /// skipped; empty zoned lots are converted; any built lot or plant blocks the whole command.
    /// </summary>
    public sealed class BuildRoadCommand : CityCommand
    {
        private readonly GridPos[] _path;

        public IReadOnlyList<GridPos> Path => _path;

        public BuildRoadCommand(IEnumerable<GridPos> path)
        {
            _path = new List<GridPos>(path ?? throw new ArgumentNullException(nameof(path))).ToArray();
        }

        internal override CommandError Plan(City city, List<CellEdit> edits, out long cost)
        {
            cost = 0;
            if (_path.Length == 0) return CommandError.InvalidArgument;

            var seen = new HashSet<int>();
            foreach (var p in _path)
            {
                if (!city.InBounds(p)) return CommandError.OutOfBounds;
                int idx = city.Index(p.X, p.Y);
                if (!seen.Add(idx)) continue;

                var cell = city.GetCell(p);
                switch (cell.Kind)
                {
                    case TileKind.Road:
                        continue;
                    case TileKind.PowerPlant:
                        return CommandError.Blocked;
                    case TileKind.Zoned when cell.Density > 0:
                        return CommandError.Blocked;
                }

                edits.Add(new CellEdit(idx, Cell.Road()));
                cost += city.Config.CostRoadPerCell;
            }

            return edits.Count == 0 ? CommandError.NothingToDo : CommandError.None;
        }
    }

    /// <summary>Place a 1x1 power plant on an empty cell.</summary>
    public sealed class PlacePowerPlantCommand : CityCommand
    {
        public GridPos Pos { get; }

        public PlacePowerPlantCommand(GridPos pos) => Pos = pos;

        internal override CommandError Plan(City city, List<CellEdit> edits, out long cost)
        {
            cost = 0;
            if (!city.InBounds(Pos)) return CommandError.OutOfBounds;
            if (city.GetCell(Pos).Kind != TileKind.Empty) return CommandError.Blocked;

            edits.Add(new CellEdit(city.Index(Pos.X, Pos.Y), Cell.Plant()));
            cost = city.Config.CostPowerPlant;
            return CommandError.None;
        }
    }

    /// <summary>Clear everything in a rectangle back to empty ground.</summary>
    public sealed class BulldozeCommand : CityCommand
    {
        public GridRect Area { get; }

        public BulldozeCommand(GridRect area) => Area = area;

        internal override CommandError Plan(City city, List<CellEdit> edits, out long cost)
        {
            cost = 0;
            if (!city.InBounds(Area)) return CommandError.OutOfBounds;

            for (int y = Area.MinY; y <= Area.MaxY; y++)
            for (int x = Area.MinX; x <= Area.MaxX; x++)
            {
                if (city.GetCell(x, y).Kind == TileKind.Empty) continue;
                edits.Add(new CellEdit(city.Index(x, y), Cell.Empty));
                cost += city.Config.CostBulldozePerCell;
            }

            return edits.Count == 0 ? CommandError.NothingToDo : CommandError.None;
        }
    }

    public static class RoadPath
    {
        /// <summary>
        /// Drag-to-build path: horizontal from <paramref name="from"/> then vertical to <paramref name="to"/>.
        /// </summary>
        public static List<GridPos> LShape(GridPos from, GridPos to, bool horizontalFirst = true)
        {
            var path = new List<GridPos>();
            var corner = horizontalFirst ? new GridPos(to.X, from.Y) : new GridPos(from.X, to.Y);
            AddLine(path, from, corner);
            AddLine(path, corner, to);
            return path;
        }

        private static void AddLine(List<GridPos> path, GridPos a, GridPos b)
        {
            int dx = Math.Sign(b.X - a.X), dy = Math.Sign(b.Y - a.Y);
            var p = a;
            while (true)
            {
                if (path.Count == 0 || path[path.Count - 1] != p) path.Add(p);
                if (p == b) break;
                p = new GridPos(p.X + dx, p.Y + dy);
            }
        }
    }
}
