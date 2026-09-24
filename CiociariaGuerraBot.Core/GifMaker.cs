using ImageMagick;

namespace CiociariaGuerraBot.Core
{
    public class GifMaker
    {
        public static void ConvertAllSvgToJpg(string folder)
        {
            foreach (string fileSvg in Directory.GetFiles(folder, "*.svg"))
            {
                string fileJpg = Path.Combine(
                    folder,
                    Path.GetFileNameWithoutExtension(fileSvg) + ".jpg"
                );

                ConvertSvgToJpg(fileSvg, fileJpg);
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

        public static void CreateGif(string folder)
        {
            string fileName = $"Run_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.gif";
            string fileGif = Path.Combine(folder, fileName);

            var files = Directory
                .GetFiles(folder, "*.jpg")
                .Where(f => !Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f =>
                {
                    string name = Path.GetFileNameWithoutExtension(f);
                    string number = new string(name.Where(char.IsDigit).ToArray());
                    return int.TryParse(number, out int n) ? n : int.MaxValue;
                })
                .ToList();

            if (files.Count == 0)
            {
                Logger.Log("Nessuna immagine trovata.");
                return;
            }

            using var gif = new MagickImageCollection();

            foreach (string file in files)
            {
                var frame = new MagickImage(file);

                frame.AnimationDelay = 30; // 0,3 seconds
                frame.AnimationTicksPerSecond = 100;

                gif.Add(frame);
            }

            gif[0].AnimationIterations = 1;

            ExecuteWithProgressBar(() => gif.Write(fileGif));
        }

        static void ExecuteWithProgressBar(Action operation)
        {
            bool completed = false;

            Task task = Task.Run(() =>
            {
                operation();
                completed = true;
            });

            string[] animation =
            {
                "[::        ]",
                "[ :::      ]",
                "[  :::     ]",
                "[   :::    ]",
                "[    :::   ]",
                "[     :::  ]",
                "[      ::: ]",
                "[       :::]",
                "[      ::: ]",
                "[     :::  ]",
                "[    :::   ]",
                "[   :::    ]",
                "[  :::     ]",
                "[ :::      ]"
            };

            int i = 0;

            while (!completed)
            {
                Console.Write($"\r{animation[i++ % animation.Length]} Elaborazione...");
                Thread.Sleep(100);
            }

            task.Wait();

            Console.WriteLine("\r[::::::::::] Completato!     ");
        }
    }
}