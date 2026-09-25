using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CiociariaGuerraBot.Core
{
    public class GameEngine
    {
        public static void PlayTurn(MapRenderer renderer, IReadOnlyList<Municipality> municipalities, int extractedMunicipalityId, int turn, bool isDebug, string mapDocument)
        {
            Municipality extractedMunicipality = municipalities.First(c => c.Id == extractedMunicipalityId);
            Logger.Log($"Id estratto: {extractedMunicipality.Id} | {extractedMunicipality.Name}");

            Municipality attacker = Utilities.GetAttacker(municipalities, extractedMunicipality);
            Logger.Log($"Attaccante: {attacker.Id} | {attacker.Name}");

            Municipality? conquered = Utilities.GetConquered(municipalities, attacker);

            if (conquered == null)
            {
                Logger.Log("Nessun bersaglio disponibile per l'attaccante estratto, salto il turno.");
                return;
            }

            Utilities.ConquestHandler(renderer, turn, municipalities, attacker.Id, conquered.Id, isDebug, mapDocument);
        }
    }
}
