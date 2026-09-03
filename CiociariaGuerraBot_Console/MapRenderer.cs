using System.Xml.Linq;

namespace CiociariaGuerraBot_Console
{
    class MapRenderer
    {
        private readonly string _fileSvg;

        public MapRenderer(string fileSvg)
        {
            _fileSvg = fileSvg;
        }

        public void Renderizza(List<Comune> comuni, int turno)
        {
            // Carica il file SVG originale
            XDocument svg = XDocument.Load(_fileSvg);

            // Namespace SVG
            XNamespace ns = "http://www.w3.org/2000/svg";

            // Trova il gruppo "Territori"
            XElement? territori = svg
                .Descendants(ns + "g")
                .FirstOrDefault(g => (string?)g.Attribute("id") == "Territori");

            if (territori == null)
            {
                throw new Exception("Gruppo 'Territori' non trovato nell'SVG.");
            }

            // Per ogni comune aggiorniamo il colore del relativo path
            foreach (Comune comune in comuni)
            {
                string idSvg = comune.Id.ToString();

                XElement? path = territori
                    .Descendants(ns + "path")
                    .FirstOrDefault(p => (string?)p.Attribute("id") == idSvg);

                if (path == null)
                {
                    Console.WriteLine(
                        $"ATTENZIONE: territorio non trovato nell'SVG: {comune.Nome}"
                    );

                    continue;
                }

                // non conquistato = proprio colore
                // conquistato = colore del proprietario
                string colore;

                if (comune.IdProprietario == null)
                {
                    colore = GetColore(comune.Id);
                }
                else
                {
                    colore = GetColore(comune.IdProprietario.Value);
                }

                path.SetAttributeValue(
                    "style",
                    "fill:" + colore + ";stroke:#000000;stroke-miterlimit:10"
                );
            }

            // Crea la cartella Output se non esiste
            string cartellaOutput = "Output";

            Directory.CreateDirectory(cartellaOutput);

            // Nome del file
            string nomeFile = $"Mappa_Turno_{turno:D3}.svg";

            string percorsoOutput = Path.Combine(
                cartellaOutput,
                nomeFile
            );

            // Salva
            svg.Save(percorsoOutput);

            Console.WriteLine($"Mappa salvata: {percorsoOutput}");
        }


        private string GetColore(int idProprietario)
        {
            string[] colori =
            {
             "#2bc84b",
             "#465473",
             "#0d1f6d",
             "#cf0d0d",
             "#3caea8",
             "#a1ddf3",
             "#f16e7e",
             "#a19909",
             "#93095b",
             "#bf4a8f",
             "#835454",
             "#5976ba",
             "#014a80",
             "#ca3c4e",
             "#1c5205",
             "#308d5d",
             "#feac0e",
             "#f16624",
             "#eb028c",
             "#916268",
             "#a764a7",
             "#67e732",
             "#fe881f",
             "#f5cf01",
             "#176123",
             "#fe5858",
             "#772437",
             "#d98156",
             "#a2d439",
             "#42933e",
             "#9f1013",
             "#8dc6da",
             "#fe7c3d",
             "#25b3e8",
             "#bf2235",
             "#91278e",
             "#f16c4f",
             "#2e495a",
             "#e3db4f",
             "#d6132a",
             "#cb1c23",
             "#c1b812",
             "#faaf5d",
             "#004c7e",
             "#8781bd",
             "#01bef2",
             "#dacf3b",
             "#fef69a",
             "#1079be",
             "#8cc53f",
             "#faae5d",
             "#670840",
             "#764d25",
             "#fef101",
             "#5ed16e",
             "#f8ae83",
             "#7acbc7",
             "#8393c9",
             "#f5844e",
             "#5d853b",
             "#ac0015",
             "#1c70a8",
             "#672f92",
             "#ad3141",
             "#bdb96b",
             "#e35aac",
             "#a57c52",
             "#e32424",
             "#824a14",
             "#fef468",
             "#fef54d",
             "#b85b5e",
             "#3d7ba2",
             "#cec51b",
             "#036488",
             "#625ea8",
             "#d03f94",
             "#4d9b2d",
             "#ebe354",
             "#5eae84",
             "#a4a17b",
             "#534741",
             "#3fb679",
             "#64b643",
             "#39b44a",
             "#3d931a",
             "#c07830",
             "#78c159",
             "#0072bb",
             "#57b5fe"
            };

            return colori[idProprietario % colori.Length];
        }
    }
}
