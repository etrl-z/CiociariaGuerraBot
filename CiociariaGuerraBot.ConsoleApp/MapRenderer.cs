using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CiociariaGuerraBot.ConsoleApp
{
    class MapRenderer
    {
        private readonly string _fileSvg;
        private readonly string _cartellaOutput = ConfigurationManager.AppSettings["OutputFolder"] ?? String.Empty;

        private readonly XNamespace ns = "http://www.w3.org/2000/svg";
        private readonly XDocument _svg;
        private readonly XElement? _territori;
        private readonly XElement? _nomiComuni;

        private readonly Dictionary<int, XElement> _paths = new();
        private readonly Dictionary<int, XElement> _texts = new();
        private readonly Dictionary<string, string> _coloriCss = new();
        public readonly List<Comune> _comuni = new();

        // Coefficienti della trasformazione affine CAD -> SVG
        private const double ScaleX = 2.834809;
        private const double ScaleY = -2.834698;
        private const double OffsetX = 510.7850;
        private const double OffsetY = 715.5535;

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

            // Trova il gruppo NomiComuni
            _nomiComuni = _svg
                .Descendants(ns + "g")
                .FirstOrDefault(g =>
                    (string?)g.Attribute("id") == "NomiComuni");

            if (_nomiComuni == null)
                throw new Exception("Gruppo 'NomiComuni' non trovato nell'SVG.");

            // Carica gli oggetti Comune nella lista
            CaricaComuni();

            // Indicizza tutti i path per id
            CaricaPathsComuni();

            // Indicizza tutti i text per id
            CaricaNomiComuni();

            // Indicizza tutti i colori del CSS per id
            CaricaColoriCss();

            Directory.CreateDirectory(_cartellaOutput);
        }


        public void Renderizza(List<Comune> comuni, int turno, int? idAttaccante = null, int? idConquistato = null, int? idOldProprietario = null)
        {
            foreach (XElement text in _texts.Values)
            {
                text.SetAttributeValue("style", "display:none");
            }

            foreach (Comune comune in comuni)
            {
                if (!_paths.TryGetValue(comune.Id, out XElement? path))
                {
                    Console.WriteLine($"ATTENZIONE: territorio non trovato nell'SVG: {comune.Nome}");
                    continue;
                }

                // SETTA COLORE DEI TERRITORI
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

                // EVIDENZIA COMUNI COINVOLTI
                string stroke = "#000000";
                string strokeWidth = "1";

                if (idAttaccante != null && (comune.Id == idAttaccante || comune.IdProprietario == idAttaccante))
                {
                    stroke = "#00FF00"; // VERDE
                    strokeWidth = "4";

                    if (comune.Id == idAttaccante)
                        MostraNome(comune);
                }

                if (idConquistato != null && comune.Id == idConquistato)
                {
                    stroke = "#FF0000"; // ROSSO
                    strokeWidth = "4";

                    if (comune.Id == idConquistato)
                        MostraNome(comune);
                }
                else if (idOldProprietario != null && (comune.Id == idOldProprietario || comune.IdProprietario == idOldProprietario))
                {
                    stroke = "#0000FF"; // BLU
                    strokeWidth = "4";

                    if (comune.Id == idOldProprietario)
                        MostraNome(comune);
                }

                // APPLICA STILE
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



        private void MostraNome(Comune comune)
        {
            if (_texts.TryGetValue(comune.Id, out XElement? text))
            {
                text.SetAttributeValue("style", "display:inline");
            }
        }

        private void OrdinaTerritori(List<Comune> comuni, int? idOldProprietario, int? idAttaccante, int? idConquistato)
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

            if (idConquistato != null)
                ordine.Add((int)idConquistato);

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

        private void CaricaComuni()
        {
            foreach (XElement path in _territori!.Descendants(ns + "path"))
            {
                string? idAttr = path.Attribute("id")?.Value;
                string? nomeAttr = path.Attribute("name")?.Value;
                string? xCadAttr = path.Attribute("x_cad")?.Value;
                string? yCadAttr = path.Attribute("y_cad")?.Value;

                if (idAttr == null || !int.TryParse(idAttr, out int id))
                    continue;

                if (xCadAttr == null || yCadAttr == null ||
                    !double.TryParse(xCadAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out double xCad) ||
                    !double.TryParse(yCadAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out double yCad))
                {
                    Console.WriteLine($"ATTENZIONE: coordinate CAD mancanti/non valide per id {id} ({nomeAttr})");
                    continue;
                }

                double xSvg = OffsetX + ScaleX * xCad;
                double ySvg = OffsetY + ScaleY * yCad;

                Comune comune = new(id, nomeAttr ?? String.Empty, xSvg, ySvg);
                _comuni.Add(comune);
            }

            Console.WriteLine($"Caricati {_comuni.Count} Comuni.");
        }

        private void CaricaPathsComuni()
        {
            foreach (XElement path in _territori!.Descendants(ns + "path"))
            {
                string? idAttr = path.Attribute("id")?.Value;

                if (idAttr != null && int.TryParse(idAttr, out int id))
                {
                    _paths[id] = path;
                }
            }

            Console.WriteLine($"Indicizzati {_paths.Count} Territori.");
        }

        private void CaricaNomiComuni()
        {
            foreach (XElement text in _nomiComuni!.Descendants(ns + "text"))
            {
                string? idAttr = text.Attribute("id")?.Value;

                if (idAttr != null && int.TryParse(idAttr, out int id))
                {
                    _texts[id] = text;
                }
            }

            Console.WriteLine($"Indicizzati {_texts.Count} NomiComuni.");
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