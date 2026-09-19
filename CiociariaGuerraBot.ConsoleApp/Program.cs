using System.Configuration;

namespace CiociariaGuerraBot.ConsoleApp
{
    public class Program
    {
        private static string? _outputFolder;
        private static string? _svgMap;

        public static void Main(string[] args)
        {
            // --- LOAD CONFIGURATIONS ---
            var outputFolderConf = ConfigurationManager.AppSettings["OutputFolder"];
            if (string.IsNullOrWhiteSpace(outputFolderConf))
            {
                throw new FileNotFoundException("ERRORE: chiave 'OutputFolder' non configurata.");
            }

            var timestamp = $"{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";

            _outputFolder = outputFolderConf + $"\\{timestamp}";
            Directory.CreateDirectory(_outputFolder);

            Logger._logPath = Path.Combine(_outputFolder, $"Run_{timestamp}.txt");

            _svgMap = ConfigurationManager.AppSettings["SVG_Map"];
            if (string.IsNullOrWhiteSpace(_svgMap))
            {
                Logger.Log("ERRORE: chiave 'SVG_Map' non configurata.");
                return;
            }

            Logger.Log("START");

            MapRenderer renderer;

            try
            {
                renderer = new MapRenderer(_svgMap, _outputFolder);
            }
            catch (Exception ex)
            {
                Logger.Log($"ERRORE durante il caricamento della mappa '{_svgMap}': {ex.Message}");
                return;
            }

            IReadOnlyList<Municipality> municipalities = renderer.Municipalities;
            if (municipalities.Count == 0)
            {
                Logger.Log("ERRORE: nessun comune caricato dal file mappa.");
                return;
            }

            Logger.Log($"Lista comuni caricata ({municipalities.Count} comuni).");

            int indexer = 0;

            IReadOnlyList<int> activeMunicipalities = Utilities.GetActiveMunicipalities(municipalities);

            while (activeMunicipalities.Count > 1)
            {
                Logger.Log("---------------------------------------------------------------------------------------------------------------------------");
                indexer++;
                Logger.Log($"TURNO {indexer}");
                Logger.Log($"{activeMunicipalities.Count} Comuni in gara");

                Municipality extractedMunicipality = municipalities[Random.Shared.Next(municipalities.Count)];
                Logger.Log($"Id estratto: {extractedMunicipality.Id} | {extractedMunicipality.Name}");

                Municipality attacker = Utilities.GetAttacker(municipalities, extractedMunicipality);
                Logger.Log($"Attaccante: {attacker.Id} | {attacker.Name}");

                Municipality? conquered = Utilities.GetConquered(municipalities, attacker);
                if (conquered == null)
                {
                    Logger.Log("Nessun bersaglio disponibile per l'attaccante estratto, salto il turno.");
                    continue;
                }

                Utilities.ConquerHandler(renderer, indexer, municipalities, attacker.Id, conquered.Id);

                activeMunicipalities = Utilities.GetActiveMunicipalities(municipalities);
                Logger.Log($"{activeMunicipalities.Count} {(activeMunicipalities.Count > 1 ? "Comuni rimanenti" : "Comune rimanente")}.");
            }

            Logger.Log("---------------------------------------------------------------------------------------------------------------------------");

            Municipality? winner = municipalities.FirstOrDefault(c => c.Id == (activeMunicipalities.Count == 1 ? activeMunicipalities.First() : null));

            if (winner != null)
            {
                renderer.Render(municipalities, ++indexer, winner.Id);

                Logger.Log($"{winner.Name} ha interamente conquistato la Ciociaria.");
                Logger.Log($"Tutti i territori sono stati unificati e formano ora il Comune di {winner.Name}.");
            }
            else
            {
                Logger.Log("ERRORE: impossibile determinare il vincitore.");
            }


            // GENERATE GIF
            // ----------------------------------------------------------------------------------------------------

            //GifMaker.CreateGif(_outputFolder);

            // ----------------------------------------------------------------------------------------------------


            Console.WriteLine("Premi un tasto per uscire...");
            Console.ReadKey();
        }
    }
}