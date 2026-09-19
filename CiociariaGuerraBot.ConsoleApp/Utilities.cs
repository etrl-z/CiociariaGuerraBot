using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CiociariaGuerraBot.ConsoleApp
{
    internal class Utilities
    {
        public static IReadOnlyList<int> GetActiveMunicipalities(IReadOnlyList<Municipality> municipalities) =>
            municipalities.Select(c => c.OwnerId ?? c.Id).Distinct().ToList();


        public static Municipality GetAttacker(IReadOnlyList<Municipality> municipalities, Municipality extractedMunicipality)
        {
            Municipality attacker = extractedMunicipality.OwnerId is int ownerId
                    ? municipalities.First(c => c.Id == ownerId)
                    : extractedMunicipality;

            return attacker;
        }

        public static Municipality GetConquered(IReadOnlyList<Municipality> municipalities, Municipality attacker)
        {
            Municipality? conquered = municipalities
                    .Where(c => c.Id != attacker.Id)
                    .Where(c => c.OwnerId != attacker.Id)
                    .OrderBy(c => attacker.DistanceFrom(c))
                    .FirstOrDefault();

            return conquered ?? new Municipality();
        }

        public static void ConquerHandler(MapRenderer renderer, int indexer, IReadOnlyList<Municipality> municipalities, int attackerId, int conqueredId)
        {
            Municipality? attacker = municipalities.FirstOrDefault(c => c.Id == attackerId);
            Municipality? conquered = municipalities.FirstOrDefault(c => c.Id == conqueredId);

            if (attacker == null || conquered == null)
                return;

            // SAVE OLD OWNER (cannot be null: it's either the real owner, or the municipality itself)
            Municipality oldOwner = conquered.OwnerId is int ownerId
                ? municipalities.First(c => c.Id == ownerId)
                : conquered;

            // CHANGE THE OWNER
            conquered.OwnerId = attacker.Id;

            GenerateText(municipalities, attacker, conquered, oldOwner);


            renderer.Render(municipalities, indexer, attacker.Id, conquered.Id, oldOwner.Id);


            // RECALCULATE CENTROIDS FOR BOTH NEW AND OLD OWNER
            RecalculateCentroid(attacker, municipalities);
            RecalculateCentroid(oldOwner, municipalities);

            GenerateReport(municipalities);

        }

        public static void GenerateText(IReadOnlyList<Municipality> municipalities, Municipality attacker, Municipality conquered, Municipality oldOwner)
        {
            Logger.Log($"[{DateTime.Now:dd/MM/yyyy - HH:mm:ss}] {attacker.Name} ha conquistato il territorio di {conquered.Name}", false);

            bool wasIndependent = oldOwner.Id == conquered.Id;
            Logger.Log(wasIndependent
                ? "."
                : $", precedentemente appartenente al Comune di {oldOwner.Name}.");

            // Total defeat can be declared wher NO other territory refers to the 'oldOwner' as its Owner
            bool hasTerritories = municipalities.Any(c => c.OwnerId == oldOwner.Id);
            if (!hasTerritories)
            {
                Logger.Log($"Il Comune di {oldOwner.Name} è stato completamente sconfitto.");
            }
        }

        public static void GenerateReport(IReadOnlyList<Municipality> municipalities)
        {
            foreach (Municipality m in municipalities)
            {
                Municipality? owner = municipalities.FirstOrDefault(x => x.Id == m.OwnerId);

                Logger.Log(
                    $"{m.Id,-2} | " +
                    $"{m.Name,-28} | " +
                    $"P: {owner?.Name,-28} | " +
                    $"X {m.OriginCentroid_X,8:F2} | " +
                    $"Y {m.OriginCentroid_Y,8:F2} | " +
                    $"X_t {m.TerritoryCentroid_X,8:F2} | " +
                    $"Y_t {m.TerritoryCentroid_Y,8:F2}"
                );
            }
        }

        static void RecalculateCentroid(Municipality owner, IReadOnlyList<Municipality> municipalities)
        {
            List<Municipality> territories = municipalities.Where(c => c.Id == owner.Id || c.OwnerId == owner.Id).ToList();

            if (territories.Count == 0)
            {
                owner.TerritoryCentroid_X = owner.OriginCentroid_X;
                owner.TerritoryCentroid_Y = owner.OriginCentroid_Y;
                return;
            }

            owner.TerritoryCentroid_X = territories.Average(c => c.OriginCentroid_X);
            owner.TerritoryCentroid_Y = territories.Average(c => c.OriginCentroid_Y);
        }
    }
}
