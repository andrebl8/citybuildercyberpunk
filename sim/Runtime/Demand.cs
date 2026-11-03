using System;
using System.Globalization;

namespace NeonCity.Sim
{
    /// <summary>Residential / Commercial / Industrial demand, each in (-1, 1).</summary>
    public readonly struct RciDemand
    {
        public readonly double Residential;
        public readonly double Commercial;
        public readonly double Industrial;

        public RciDemand(double residential, double commercial, double industrial)
        {
            Residential = residential;
            Commercial = commercial;
            Industrial = industrial;
        }

        public double For(ZoneType zone) => zone switch
        {
            ZoneType.Residential => Residential,
            ZoneType.Commercial => Commercial,
            ZoneType.Industrial => Industrial,
            _ => 0,
        };

        public override string ToString() => $"R {Fmt(Residential)} C {Fmt(Commercial)} I {Fmt(Industrial)}";

        private static string Fmt(double v)
        {
            double r = Math.Round(v, 2);
            string s = Math.Abs(r).ToString("0.00", CultureInfo.InvariantCulture);
            return r > 0 ? "+" + s : r < 0 ? "-" + s : " " + s;
        }

        /// <summary>
        /// Residents want jobs; shops want customers; industry wants workers and exports.
        /// Raw gaps are squashed with x / (|x| + k) so they saturate smoothly at ±1.
        /// </summary>
        internal static RciDemand Compute(SimConfig cfg, int population, int commercialJobs, int industrialJobs)
        {
            double jobs = commercialJobs + industrialJobs;
            double workers = population * cfg.WorkforceRatio;

            double r = jobs + cfg.BaseResidentialDemand - workers;
            double c = population * cfg.CommercialJobsPerResident + cfg.BaseCommercialDemand - commercialJobs;
            double i = population * cfg.IndustrialJobsPerResident + cfg.BaseIndustrialDemand - industrialJobs;

            return new RciDemand(Squash(r, cfg.DemandSoftness), Squash(c, cfg.DemandSoftness), Squash(i, cfg.DemandSoftness));
        }

        private static double Squash(double x, double k) => x / (Math.Abs(x) + k);
    }
}
