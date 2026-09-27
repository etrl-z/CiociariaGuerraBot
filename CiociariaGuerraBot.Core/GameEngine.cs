using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CiociariaGuerraBot.Core
{
    public class GameEngine
    {
        public static void PlayTurn(MapRenderer renderer, IReadOnlyList<Municipality> municipalities, int extractedMunicipalityId, string dbPath, int turn, bool isDebug)
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

            TurnOutcome outcome = Utilities.ConquestHandler(municipalities, attacker, conquered);

            // RENDER IMAGE AND RETURN JPG PATH
            string outputJpg = renderer.Render(municipalities, outcome.Attacker?.Id, outcome.Conquered?.Id, outcome.OldOwner?.Id, turn, isDebug);

            if (!String.IsNullOrEmpty(outputJpg))
            {
                // LOAD BASE64 ON FIREBASE
                try
                {
                    FirebaseClient.LoadImage(outputJpg, dbPath).Wait();
                }
                catch (Exception e)
                {
                    Logger.Log("ERROR: Errore nel caricamento su Firestore. | " + e.Message);
                }
            }
        }

        public static void SetWinner(MapRenderer renderer, IReadOnlyList<Municipality> municipalities, Municipality winner, string timestamp, List<int>? gameHistory, string dbPath, int turn, bool isDebug)
        {
            // RENDER IMAGE AND RETURN JPG PATH
            string outputJpg = renderer.Render(municipalities, winner.Id, null, null, turn, isDebug);

            if (!String.IsNullOrEmpty(outputJpg))
            {
                // LOAD BASE64 ON FIREBASE
                try
                {
                    FirebaseClient.LoadImage(outputJpg, dbPath).Wait();
                }
                catch (Exception e)
                {
                    Logger.Log("ERROR: Errore nel caricamento su Firestore. | " + e.Message);
                }
            }

            Logger.Log($"{winner.Name} ha interamente conquistato la Ciociaria.");
            Logger.Log($"Tutti i territori sono stati unificati e formano ora il Comune di {winner.Name}.");

            if (gameHistory != null)
                FirebaseClient.LoadVictory(timestamp, winner.Id, winner.Name, turn, [.. gameHistory]).Wait();
        }

        public class TurnOutcome()
        {
            public Municipality? Attacker { get; set; }
            public Municipality? Conquered { get; set; }
            public Municipality? OldOwner { get; set; }
        }
    }
}
