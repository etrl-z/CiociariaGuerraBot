using ImageMagick;

namespace CiociariaGuerraBot.ConsoleApp
{
    internal class GifMaker
    {
        public static void ConvertAllSvgToJpg(string cartella)
        {
            foreach (string fileSvg in Directory.GetFiles(cartella, "*.svg"))
            {
                string fileJpg = Path.Combine(
                    cartella,
                    Path.GetFileNameWithoutExtension(fileSvg) + ".jpg"
                );

                ConvertSvgToJpg(fileSvg, fileJpg);

                Console.WriteLine($"Convertito: {Path.GetFileName(fileSvg)}");
            }
        }

        public static void ConvertSvgToJpg(string fileSvg, string fileJpg)
        {
            var settings = new MagickReadSettings
            {
                BackgroundColor = MagickColors.White
            };

            using var image = new MagickImage();

            image.Read(fileSvg, settings);

            image.Format = MagickFormat.Jpeg;
            image.Quality = 95;

            image.Write(fileJpg);
        }

        public static void CreateGif(string cartella)
        {
            string fileName = $"Run_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.gif";
            string fileGif = Path.Combine(cartella, fileName);

            var files = Directory
                .GetFiles(cartella, "*.jpg")
                .Where(f => !Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f =>
                {
                    string nome = Path.GetFileNameWithoutExtension(f);
                    string numero = new string(nome.Where(char.IsDigit).ToArray());
                    return int.TryParse(numero, out int n) ? n : int.MaxValue;
                })
                .ToList();

            if (files.Count == 0)
            {
                Console.WriteLine("Nessuna immagine trovata.");
                return;
            }

            using var gif = new MagickImageCollection();

            foreach (string file in files)
            {
                var frame = new MagickImage(file);

                frame.AnimationDelay = 30; // 0,3 secondi
                frame.AnimationTicksPerSecond = 100;

                gif.Add(frame);
            }

            gif[0].AnimationIterations = 1;

            gif.Write(fileGif);

            Console.WriteLine($"GIF creata: {fileGif}");
        }
    }
}
