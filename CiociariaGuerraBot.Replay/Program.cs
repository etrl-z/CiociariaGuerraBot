using CiociariaGuerraBot.Core;
using System;
using System.Configuration;

namespace CiociariaGuerraBot.Replay
{
    public class Program
    {
        private static string? _svgMap;
        private static string? _outputFolder;
        private static bool _isDebug;
        private static int _gameSpeed;
        private static string? _dbPath;

        private static string? _gameToReplay;

        public static void Main(string[] args)
        {
            #region LOAD CONFIGURATIONS
            _svgMap = ConfigurationManager.AppSettings["SVG_Map"];
            if (string.IsNullOrWhiteSpace(_svgMap))
            {
                throw new FileNotFoundException("ERRORE: chiave 'SVG_Map' non configurata.");
            }

            var outputFolderConf = ConfigurationManager.AppSettings["OutputFolder"];
            if (string.IsNullOrWhiteSpace(outputFolderConf))
            {
                throw new FileNotFoundException("ERRORE: chiave 'OutputFolder' non configurata.");
            }

            _gameToReplay = ConfigurationManager.AppSettings["gameToReplay"];
            if (string.IsNullOrWhiteSpace(_gameToReplay))
            {
                throw new FileNotFoundException("ERRORE: chiave 'gameToReplay' non configurata.");
            }

            string timestamp = $"{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";
            _outputFolder = Path.GetFullPath(Path.Combine(outputFolderConf, timestamp));
            Directory.CreateDirectory(_outputFolder);

            Logger._logPath = Path.GetFullPath(Path.Combine(_outputFolder, $"Run_{timestamp}.txt"));

            _isDebug = Convert.ToBoolean(ConfigurationManager.AppSettings["isDebug"]);
            _gameSpeed = Convert.ToInt32(ConfigurationManager.AppSettings["gameSpeed"]);
            _dbPath = ConfigurationManager.AppSettings["dbPath"] ?? "";
            #endregion

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

            Logger.Log("START");
            Logger.Log($"Lista comuni caricata ({municipalities.Count} comuni).");

            IReadOnlyList<int> activeMunicipalities = Utilities.GetActiveMunicipalities(municipalities);

            int[] gameHistory = FirebaseClient.GetGameHistory(_gameToReplay).Result;

            int turn = 0;
            foreach (int extraction in gameHistory)
            {
                Logger.Log("---------------------------------------------------------------------------------------------------------------------------");

                turn++;
                Logger.Log($"TURNO {turn}");
                Logger.Log($"{activeMunicipalities.Count} Comuni in gara");

                Municipality extractedMunicipality = municipalities.First(c => c.Id == extraction);

                GameEngine.PlayTurn(renderer, municipalities, extractedMunicipality.Id, _dbPath, turn, _isDebug);

                activeMunicipalities = Utilities.GetActiveMunicipalities(municipalities);
                Logger.Log($"{activeMunicipalities.Count} {(activeMunicipalities.Count > 1 ? "Comuni rimanenti" : "Comune rimanente")}.");


                Thread.Sleep(_gameSpeed);
            }

            Logger.Log("---------------------------------------------------------------------------------------------------------------------------");

            Municipality? winner = municipalities.FirstOrDefault(c => c.Id == (activeMunicipalities.Count == 1 ? activeMunicipalities[0] : null));

            if (winner != null && winner.Name != null)
            {
                GameEngine.SetWinner(renderer, municipalities, winner, timestamp, null, _dbPath, ++turn, _isDebug);
            }
            else
            {
                Logger.Log("ERRORE: impossibile determinare il vincitore.");
            }
        }
    }
}