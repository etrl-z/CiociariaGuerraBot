using Google.Cloud.Firestore;

namespace CiociariaGuerraBot.ConsoleApp
{
    internal class FirebaseClient
    {
        public static async Task Load(string imagePath)
        {
            string projectId = "base64-image-visualizer";

            string credentialsPath = Path.Combine(
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
    }
}
