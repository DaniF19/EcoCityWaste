using EcoCityWaste.Models;
using EcoCityWaste.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoCityWaste.Services
{
    /// <summary>
    /// Greedy Nearest-Neighbour route optimiser.
    /// Prioritises containers with high fill levels (≥ 80 %) and then minimises
    /// total travel distance using a nearest-neighbour heuristic.
    /// </summary>
    public class RouteOptimisationService
    {
        private const int HighFillThreshold = 80;

        /// <summary>
        /// Returns an ordered list of stops for the supplied containers.
        /// Containers without valid coordinates (0,0) are appended at the end.
        /// </summary>
        public OptimisedRouteDto Optimise(IEnumerable<Container> containers)
        {
            var all = containers.ToList();

            if (!all.Any())
                return new OptimisedRouteDto { Message = "Nenhum contentor fornecido." };

            // Separate containers with and without GPS coordinates
            var withCoords = all.Where(c => c.Latitude != 0 || c.Longitude != 0).ToList();
            var withoutCoords = all.Where(c => c.Latitude == 0 && c.Longitude == 0).ToList();

            if (!withCoords.Any())
                return BuildResult(all, 0, "Nenhum contentor tem coordenadas válidas — ordem mantida.");

            // ── Greedy nearest-neighbour starting from the highest-priority container ──
            // Priority score: fill level drives the start; then we build the tour greedily.
            var prioritised = withCoords
                .OrderByDescending(c => c.FillLevel >= HighFillThreshold ? 1 : 0)
                .ThenByDescending(c => c.FillLevel)
                .ToList();

            var ordered = new List<Container>();
            var remaining = new List<Container>(prioritised);

            // Start at the highest-priority container
            var current = remaining[0];
            ordered.Add(current);
            remaining.RemoveAt(0);

            while (remaining.Any())
            {
                var next = remaining
                    .OrderBy(c => HaversineKm(current.Latitude, current.Longitude,
                                              c.Latitude, c.Longitude))
                    .First();
                ordered.Add(next);
                remaining.Remove(next);
                current = next;
            }

            // Append containers without coordinates at the end (original order)
            ordered.AddRange(withoutCoords);

            double totalKm = CalculateTotalDistance(ordered);
            string msg = withoutCoords.Any()
                ? $"Rota optimizada. {withoutCoords.Count} contentor(es) sem coordenadas adicionados no final."
                : "Rota optimizada com sucesso.";

            return BuildResult(ordered, totalKm, msg);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static OptimisedRouteDto BuildResult(
            IEnumerable<Container> ordered, double distKm, string message)
        {
            var stops = ordered.Select((c, i) => new OptimisedStopDto
            {
                ContainerId = c.Id,
                Code = c.Code,
                Location = c.Location,
                FillLevel = c.FillLevel,
                Latitude = c.Latitude,
                Longitude = c.Longitude,
                PickupOrder = i + 1
            }).ToList();

            return new OptimisedRouteDto
            {
                Stops = stops,
                EstimatedDistanceKm = Math.Round(distKm, 2),
                Message = message
            };
        }

        private static double CalculateTotalDistance(IReadOnlyList<Container> containers)
        {
            double total = 0;
            for (int i = 0; i < containers.Count - 1; i++)
            {
                total += HaversineKm(
                    containers[i].Latitude, containers[i].Longitude,
                    containers[i + 1].Latitude, containers[i + 1].Longitude);
            }
            return total;
        }

        /// <summary>Haversine formula — distance in km between two GPS points.</summary>
        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;
    }
}
