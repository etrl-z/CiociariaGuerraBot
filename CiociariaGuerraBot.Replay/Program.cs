using CiociariaGuerraBot.Core;
using System;
using System.Configuration;

namespace CiociariaGuerraBot.Replay
{
    public class Program
    {
        private static string? _gameToReplay;

        private static string? _outputFolder;
        private static string? _svgMap;

        private static bool _isDebug;
        private static int _gameSpeed;
        private static string? _mapDocument;

        public static void Main(string[] args)
        {
            #region LOAD CONFIGURATIONS
            var outputFolderConf = ConfigurationManager.AppSettings["OutputFolder"];
            if (string.IsNullOrWhiteSpace(outputFolderConf))
            {
                throw new FileNotFoundException("ERRORE: chiave 'OutputFolder' non configurata.");
            }

            string timestamp = $"{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";

            _outputFolder = Path.GetFullPath(Path.Combine(outputFolderConf, timestamp));
            Directory.CreateDirectory(_outputFolder);

            Logger._logPath = Path.GetFullPath(Path.Combine(_outputFolder, $"Run_{timestamp}.txt"));

            _isDebug = Convert.ToBoolean(ConfigurationManager.AppSettings["isDebug"]);
            _gameSpeed = Convert.ToInt32(ConfigurationManager.AppSettings["gameSpeed"]);
            _mapDocument = ConfigurationManager.AppSettings["mapDocument"] ?? "replay";

            _gameToReplay = ConfigurationManager.AppSettings["gameToReplay"];
            if (string.IsNullOrWhiteSpace(_gameToReplay))
            {
                Logger.Log("ERRORE: chiave 'SVG_Map' non configurata.");
                return;
            }

            _svgMap = ConfigurationManager.AppSettings["SVG_Map"];
            if (string.IsNullOrWhiteSpace(_svgMap))
            {
                Logger.Log("ERRORE: chiave 'SVG_Map' non configurata.");
                return;
            }
            #endregion

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

            int[] gameHistory = FirebaseClient.GetGameHistory(_gameToReplay).Result;

            for (int i = 0; i < gameHistory.Length; i++)
            {
                Logger.Log("---------------------------------------------------------------------------------------------------------------------------");

                indexer++;
                Logger.Log($"TURNO {indexer}");
                Logger.Log($"{activeMunicipalities.Count} Comuni in gara");

                Municipality extractedMunicipality = municipalities.First(c => c.Id == gameHistory[indexer - 1]);

                GameEngine.PlayTurn(renderer, municipalities, extractedMunicipality.Id, indexer, _isDebug, _mapDocument);

                activeMunicipalities = Utilities.GetActiveMunicipalities(municipalities);
                Logger.Log($"{activeMunicipalities.Count} {(activeMunicipalities.Count > 1 ? "Comuni rimanenti" : "Comune rimanente")}.");


                Thread.Sleep(_gameSpeed);
            }

            Logger.Log("---------------------------------------------------------------------------------------------------------------------------");

            Municipality? winner = municipalities.FirstOrDefault(c => c.Id == (activeMunicipalities.Count == 1 ? activeMunicipalities.First() : null));

            if (winner != null && winner.Name != null)
            {
                indexer++;
                Utilities.ConquestHandler(renderer, indexer, municipalities, winner.Id, null, _isDebug, _mapDocument);

                Logger.Log($"{winner.Name} ha interamente conquistato la Ciociaria.");
                Logger.Log($"Tutti i territori sono stati unificati e formano ora il Comune di {winner.Name}.");
            }
            else
            {
                Logger.Log("ERRORE: impossibile determinare il vincitore.");
            }
        }
    }
}