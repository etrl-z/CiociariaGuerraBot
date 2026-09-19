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

        public static Municipality? GetConquered(IReadOnlyList<Municipality> municipalities, Municipality attacker)
        {
            Municipality? conquered = municipalities
                    .Where(c => c.Id != attacker.Id)
                    .Where(c => c.OwnerId != attacker.Id)
                    .OrderBy(c => attacker.DistanceFrom(c))
                    .FirstOrDefault();

            return conquered;
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

            // [Obsolete]
            // CHANGE THE OWNER FOR EVERY TERRITORY HANDLED BY THE OLD ONE
            //foreach (Municipality t in municipalities.Where(t => t.OwnerId == conquered.Id && t.Id != conquered.Id))
            //    t.OwnerId = attacker.Id;

            conquered.OwnerId = attacker.Id;

            GenerateText(municipalities, attacker, conquered, oldOwner);


            renderer.Render(municipalities, indexer, attacker.Id, conquered.Id, oldOwner.Id);


            // RECALCULATE CENTROIDS FOR BOTH NEW AND OLD OWNER
            RecalculateCentroid(attacker, municipalities);
            RecalculateCentroid(conquered, municipalities);
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
            bool hasTerritories = municipalities.Any(c => c.Id != oldOwner.Id && c.OwnerId == oldOwner.Id);
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

        static void RecalculateCentroid(Municipality entity, IReadOnlyList<Municipality> municipalities)
        {
            List<Municipality> territories = municipalities.Where(c => c.OwnerId == entity.Id).ToList();

            if (territories.Count == 0)
            {
                entity.TerritoryCentroid_X = entity.OriginCentroid_X;
                entity.TerritoryCentroid_Y = entity.OriginCentroid_Y;
                return;
            }

            entity.TerritoryCentroid_X = territories.Average(c => c.OriginCentroid_X);
            entity.TerritoryCentroid_Y = territories.Average(c => c.OriginCentroid_Y);
        }
    }
}
