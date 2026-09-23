using Google.Cloud.Firestore;

namespace CiociariaGuerraBot.ConsoleApp
{
    internal class FirebaseClient
    {
        private static string projectId = "base64-image-visualizer";
        private static string? credentialsPath;

        public static async Task LoadImage(string imagePath)
        {
            credentialsPath = Path.Combine(
                AppContext.BaseDirectory,
                "firebaseConfiguration.json"
            );

            Environment.SetEnvironmentVariable(
                "GOOGLE_APPLICATION_CREDENTIALS",
                credentialsPath
            );

            byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
            string base64 = Convert.ToBase64String(imageBytes);

            FirestoreDb db = await FirestoreDb.CreateAsync(projectId);

            DocumentReference doc = db
                .Collection("maps")
                .Document("current");

            Dictionary<string, object> data = new()
            {
                { "imageBase64", base64 },
                { "format", "jpg" },
                { "fileName", Path.GetFileName(imagePath) },
                { "timestamp", Timestamp.GetCurrentTimestamp() }
            };

            await doc.SetAsync(data);

            Logger.Log("Immagine caricata su Firestore.");
            Logger.Log($"Dimensione originale: {imageBytes.Length:N0} byte");
            Logger.Log($"Dimensione Base64: {base64.Length:N0} caratteri");

        }

        public static async Task LoadVictory(string documentPath, int winnerId, int turn)
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

            DocumentReference doc = db
                .Collection("history")
                .Document(documentPath);

            Dictionary<string, object> data = new()
            {
                { "winner", winnerId },
                { "duration", turn },
                { "timestamp", Timestamp.GetCurrentTimestamp() }
            };

            await doc.SetAsync(data);

            Logger.Log("Vittoria registrata su Firestore.");
        }
    }
}
