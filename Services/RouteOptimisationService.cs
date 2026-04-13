using EcoCityWaste.Models;
using EcoCityWaste.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoCityWaste.Services
{
    /// <summary>
    /// Serviço de logística responsável por calcular a ordem ideal de recolha de resíduos.
    /// Utiliza um algoritmo baseado no vizinho mais próximo,
    /// priorizando contentores com níveis de enchimento críticos.
    /// </summary>
    public class RouteOptimisationService
    {
        /// <summary> Percentagem de enchimento (80%) a partir da qual um contentor é considerado prioritário. </summary>
        private const int HighFillThreshold = 80;

        /// <summary>
        /// Executa o algoritmo de otimização sobre uma lista de contentores.
        /// 1. Identifica o contentor mais cheio como ponto de partida.
        /// 2. Procura sucessivamente o contentor mais próximo do anterior.
        /// 3. Relega contentores sem coordenadas GPS para o final da lista para não quebrar o cálculo.
        /// </summary>
        /// <param name="containers">A coleção de contentores associados a uma rota.</param>
        /// <returns>Um DTO contendo a sequência otimizada de paragens e a distância total estimada.</returns>
        public OptimisedRouteDto Optimise(IEnumerable<Container> containers)
        {
            var all = containers.ToList();

            if (!all.Any())
                return new OptimisedRouteDto { Message = "Nenhum contentor fornecido para otimização." };

            // Separação de dados: Contentores sem coordenadas não podem entrar no cálculo de distância
            var withCoords = all.Where(c => c.Latitude != 0 || c.Longitude != 0).ToList();
            var withoutCoords = all.Where(c => c.Latitude == 0 && c.Longitude == 0).ToList();

            if (!withCoords.Any())
                return BuildResult(all, 0, "Nenhum contentor possui coordenadas válidas — a ordem original foi mantida.");

            // Ordenação inicial por prioridade: Nível crítico (>=80%) primeiro, seguido do nível absoluto
            var prioritised = withCoords
                .OrderByDescending(c => c.FillLevel >= HighFillThreshold ? 1 : 0)
                .ThenByDescending(c => c.FillLevel)
                .ToList();

            var ordered = new List<Container>();
            var remaining = new List<Container>(prioritised);

            // Seleção do ponto de partida (o mais crítico/cheio)
            var current = remaining[0];
            ordered.Add(current);
            remaining.RemoveAt(0);

            // Loop de vizinho mais próximo: enquanto houver paragens por visitar
            while (remaining.Any())
            {
                // Escolhe o contentor que está geograficamente mais perto da paragem atual
                var next = remaining
                    .OrderBy(c => HaversineKm(current.Latitude, current.Longitude,
                                              c.Latitude, c.Longitude))
                    .First();

                ordered.Add(next);
                remaining.Remove(next);
                current = next;
            }

            // Adiciona contentores "invísiveis" ao mapa no final da rota
            ordered.AddRange(withoutCoords);

            // Cálculo da métrica de eficiência da rota
            double totalKm = CalculateTotalDistance(ordered);

            string msg = withoutCoords.Any()
                ? $"Rota optimizada. Nota: {withoutCoords.Count} contentor(es) sem coordenadas foram movidos para o final."
                : "Rota optimizada com sucesso usando o algoritmo de vizinho mais próximo.";

            return BuildResult(ordered, totalKm, msg);
        }

        /// <summary>
        /// Método auxiliar para construir o objeto de resposta (DTO) final com as paragens numeradas.
        /// </summary>
        private static OptimisedRouteDto BuildResult(IEnumerable<Container> ordered, double distKm, string message)
        {
            var stops = ordered.Select((c, i) => new OptimisedStopDto
            {
                ContainerId = c.Id,
                Code = c.Code,
                Location = c.Location,
                FillLevel = c.FillLevel,
                Latitude = c.Latitude,
                Longitude = c.Longitude,
                PickupOrder = i + 1 // Define a ordem de paragem (1ª, 2ª, 3ª...)
            }).ToList();

            return new OptimisedRouteDto
            {
                Stops = stops,
                EstimatedDistanceKm = Math.Round(distKm, 2),
                Message = message
            };
        }

        /// <summary>
        /// Percorre a lista final de contentores e soma as distâncias entre cada par consecutivo.
        /// </summary>
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

        /// <summary>
        /// Implementação da fórmula para determinar a distância em quilómetros entre dois pontos 
        /// na superfície de uma esfera (Terra) dadas as suas latitudes e longitudes.
        /// </summary>
        public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0; // Raio médio da Terra em KM
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        /// <summary> Converte graus decimais para radianos. </summary>
        private static double ToRad(double deg) => deg * Math.PI / 180.0;
    }
}