using Google.Cloud.Firestore;

namespace CiociariaGuerraBot.Core
{
    public class FirebaseClient
    {
        private static string projectId = "base64-image-visualizer";
        private static string? credentialsPath;

        public static async Task LoadImage(string imagePath, string mapDocument = "current")
        {
            byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
            string base64 = Convert.ToBase64String(imageBytes);

            FirestoreDb db = GetDB().Result;

            DocumentReference doc = db
                .Collection("maps")
                .Document(mapDocument);

            Dictionary<string, object> data = new()
            {
                { "imageBase64", base64 },
                { "format", "jpg" },
                { "fileName", Path.GetFileName(imagePath) },
                { "timestamp", Timestamp.GetCurrentTimestamp() }
            };

            await doc.SetAsync(data);
        }

        public static async Task LoadVictory(string documentPath, int winnerId, string winnerName, int turn, int[] gameHistory)
        {
            FirestoreDb db = GetDB().Result;

            DocumentReference doc = db
                .Collection("history")
                .Document(documentPath);

            Dictionary<string, object> data = new()
            {
                { "winner", winnerId },
                { "winnerName", winnerName },
                { "duration", turn },
                { "gameHistory", gameHistory },
                { "timestamp", Timestamp.GetCurrentTimestamp() }
            };

            await doc.SetAsync(data);

            Logger.Log("Vittoria registrata su Firestore.");
        }

        public static async Task<int[]> GetGameHistory(string documentPath)
        {
            FirestoreDb db = GetDB().Result;

            DocumentReference doc = db
                .Collection("history")
                .Document(documentPath);

            DocumentSnapshot snapshot = await doc.GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                throw new FileNotFoundException(
                    $"Partita '{documentPath}' non trovata su Firestore."
                );
            }

            int[] gameHistory = snapshot.GetValue<int[]>("gameHistory");

            return gameHistory;
        }

        private static async Task<FirestoreDb> GetDB()
        {
            credentialsPath = Path.Combine(
                AppContext.BaseDirectory,
                "firebaseConfiguration.json"
            );

            Environment.SetEnvironmentVariable(
                "GOOGLE_APPLICATION_CREDENTIALS",
                credentialsPath
            );

            FirestoreDb db = await FirestoreDb.CreateAsync(projectId);

            return db;
        }
    }
}
