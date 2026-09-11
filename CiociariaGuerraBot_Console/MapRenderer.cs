using System.Configuration;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CiociariaGuerraBot_Console
{
    class MapRenderer
    {
        private readonly string _fileSvg;
        private readonly string _cartellaOutput = ConfigurationManager.AppSettings["OutputFolder"] ?? String.Empty;

        private readonly XNamespace ns = "http://www.w3.org/2000/svg";
        private readonly XDocument _svg;
        private XElement? _territori;

        private readonly Dictionary<string, string> _coloriCss = new();
        private readonly Dictionary<int, XElement> _paths = new();

        public MapRenderer(string fileSvg)
        {
            _fileSvg = fileSvg;

            // Carica SVG
            _svg = XDocument.Load(_fileSvg);

            // Trova il gruppo Territori
            _territori = _svg
                .Descendants(ns + "g")
                .FirstOrDefault(g =>
                    (string?)g.Attribute("id") == "Territori");

            if (_territori == null)
                throw new Exception("Gruppo 'Territori' non trovato nell'SVG.");

            // Indicizza tutti i path per id
            CaricaPaths();

            // Indicizza tutti i colori del CSS per id
            CaricaColoriCss();

            Directory.CreateDirectory(_cartellaOutput);
        }


        public void Renderizza(List<Comune> comuni, int turno, int idAttaccante, int idConquistato = 0, int? idOldProprietario = 0)
        {
            foreach (Comune comune in comuni)
            {
                if (!_paths.TryGetValue(comune.Id, out XElement? path))
                {
                    Console.WriteLine($"ATTENZIONE: territorio non trovato nell'SVG: {comune.Nome}");
                    continue;
                }

                // SETTA COLORE DEL TERRITORIO
                string colore;
                if (comune.IdProprietario == null)
                {
                    colore = GetColore(comune.Id);
                }
                else
                {
                    colore = GetColore(comune.IdProprietario.Value);
                }

                if (comune.Id == idConquistato)
                {
                    if (idOldProprietario != null)
                        colore = GetColore((int)idOldProprietario);
                }

                // SETTA BORDO
                string stroke = "#000000";
                string strokeWidth = "1";

                if (comune.Id == idAttaccante || comune.IdProprietario == idAttaccante)
                {
                    stroke = "#00FF00"; // VERDE
                    strokeWidth = "4";
                }

                if (comune.Id == idConquistato)
                {
                    stroke = "#FF0000"; // ROSSO
                    strokeWidth = "4";
                }
                else if (comune.Id == idOldProprietario || comune.IdProprietario == idOldProprietario)
                {
                    stroke = "#0000FF"; // BLU
                    strokeWidth = "4";
                }

                // APPLICA
                path.SetAttributeValue(
                    "style",
                    $"fill:{colore};stroke:{stroke};stroke-width:{strokeWidth};stroke-miterlimit:10"
                );
            }

            // RENDERIZZA I TERRITORI NELL'ORDINE:
            // 1. CONQUISTATO
            // 2. ATTACCANTE
            // 3. VECCHIO PROPRIETARIO
            // 4. ecc..

            OrdinaTerritori(comuni, idOldProprietario, idAttaccante, idConquistato);


            // OUTPUT IMG

            string nomeFile = $"Mappa_Turno_{turno:D3}.svg";

            string percorsoOutput = Path.Combine(_cartellaOutput, nomeFile);

            _svg.Save(percorsoOutput);

            Console.WriteLine($"Mappa salvata: {percorsoOutput}");
        }

        private void OrdinaTerritori(List<Comune> comuni, int? idOldProprietario, int idAttaccante, int idConquistato)
        {
            List<int> ordine = [];

            if (idOldProprietario != null)
            {
                ordine.AddRange(
                    comuni.Where(c => (c.Id == idOldProprietario || c.IdProprietario == idOldProprietario) && c.Id != idConquistato).Select(c => c.Id)
                );
            }

            ordine.AddRange(
                comuni.Where(c => (c.Id == idAttaccante || c.IdProprietario == idAttaccante) && c.Id != idConquistato).Select(c => c.Id)
            );

            ordine.Add(idConquistato);

            foreach (int id in ordine)
            {
                if (!_paths.TryGetValue(id, out XElement? path))
                    continue;

                XElement? parent = path.Parent;

                if (parent == null)
                    continue;

                path.Remove();
                parent.Add(path);
            }
        }

        private void CaricaPaths()
        {
            foreach (XElement path in _territori!.Descendants(ns + "path"))
            {
                string? idAttr = path.Attribute("id")?.Value;

                if (idAttr != null && int.TryParse(idAttr, out int id))
                {
                    _paths[id] = path;
                }
            }

            Console.WriteLine($"Indicizzati {_paths.Count} territori.");
        }


        private void CaricaColoriCss()
        {
            var style = _svg
                .Descendants()
                .FirstOrDefault(e =>
                    e.Name.LocalName == "style");

            if (style == null)
            {
                throw new Exception("Nessun elemento <style> trovato nell'SVG.");
            }

            string css = style.Value;

            var regex = new Regex(
                @"\.(s\d+)\s*\{\s*fill:\s*(#[0-9a-fA-F]{6})",
                RegexOptions.Compiled
            );

            foreach (Match match in regex.Matches(css))
            {
                string classe = match.Groups[1].Value;

                string colore = match.Groups[2].Value;

                _coloriCss[classe] = colore;
            }

            Console.WriteLine($"Caricate {_coloriCss.Count} classi CSS.");
        }


        private string GetColore(int id)
        {
            if (!_paths.TryGetValue(id, out XElement? path))
            {
                Console.WriteLine($"Path non trovato per ID {id}");
                return "#FFFFFF";
            }

            string? classe = path.Attribute("class")?.Value;

            if (string.IsNullOrEmpty(classe))
            {
                Console.WriteLine($"Classe CSS non trovata per ID {id}");
                return "#FFFFFF";
            }

            if (_coloriCss.TryGetValue(classe, out string? colore))
            {
                return colore;
            }

            Console.WriteLine($"Colore non trovato per classe {classe}");

            return "#FFFFFF";
        }
    }
}