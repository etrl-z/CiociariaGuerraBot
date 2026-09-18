using System.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CiociariaGuerraBot.ConsoleApp
{
    sealed class MapRenderer
    {
        private readonly string _fileSvg;
        private readonly string _outputFolder;

        private static readonly XNamespace Ns = "http://www.w3.org/2000/svg";

        private readonly XDocument _svg;
        private readonly XElement _territori;
        private readonly XElement _nomiComuni;

        private readonly Dictionary<int, XElement> _paths = new();
        private readonly List<Comune> _comuni = new();

        /// <summary>Elenco dei comuni caricati dalla mappa. Gli elementi restano mutabili
        /// (es. IdProprietario), ma la lista stessa non può essere sostituita o alterata
        /// dall'esterno.</summary>
        public IReadOnlyList<Comune> Comuni => _comuni;

        // Coefficienti della trasformazione affine CAD -> SVG
        private const double ScaleX = 2.834809;
        private const double ScaleY = -2.834698;
        private const double OffsetX = 510.7850;
        private const double OffsetY = 715.5535;

        // Pattern a strisce oblique per il territorio appena conquistato
        private const string PatternConquistatoId = "pattern-conquistato";
        private const int PatternStripSize = 8;

        public MapRenderer(string fileSvg, string cartellaOutput)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileSvg);

            if (!File.Exists(fileSvg))
                throw new FileNotFoundException($"File mappa SVG non trovato: '{fileSvg}'.", fileSvg);

            _fileSvg = fileSvg;

            _outputFolder = cartellaOutput;

            // Carica SVG
            _svg = XDocument.Load(_fileSvg);

            // Trova il gruppo Territori
            _territori = _svg
                .Descendants(Ns + "g")
                .FirstOrDefault(g => (string?)g.Attribute("id") == "Territori")
                ?? throw new InvalidOperationException("Gruppo 'Territori' non trovato nell'SVG.");

            // Trova il gruppo NomiComuni
            _nomiComuni = _svg
                .Descendants(Ns + "g")
                .FirstOrDefault(g => (string?)g.Attribute("id") == "NomiComuni")
                ?? throw new InvalidOperationException("Gruppo 'NomiComuni' non trovato nell'SVG.");

            // Indicizza tutti i path per id
            CaricaPathsComuni();

            // Carica gli oggetti Comune nella lista
            CaricaComuni();
        }

        public void Renderizza(IReadOnlyList<Comune> comuni, int turno = 0, int? idAttaccante = null, int? idConquistato = null, int? idOldProprietario = null)
        {
            _nomiComuni.RemoveNodes();

            foreach (Comune comune in comuni)
            {
                if (!_paths.TryGetValue(comune.Id, out XElement? path))
                {
                    Logger.Log($"ATTENZIONE: territorio non trovato nell'SVG: {comune.Nome}");
                    continue;
                }

                // SETTA COLORE DEI TERRITORI
                string? colore = comune.IdProprietario is int idProprietarioColore
                    ? GetFillColorFromPath(idProprietarioColore)
                    : GetFillColorFromPath(comune.Id);

                if (comune.Id == idConquistato && idOldProprietario != null)
                {
                    string? coloreVecchio = GetFillColorFromPath(idOldProprietario.Value);
                    string? coloreNuovo = idAttaccante != null ? GetFillColorFromPath(idAttaccante.Value) : coloreVecchio;

                    colore = $"url(#{AssicuraPatternConquista(comune.Id, coloreVecchio, coloreNuovo)})";
                }

                // EVIDENZIA COMUNI COINVOLTI
                string stroke = "#000000";
                string strokeWidth = "1";

                if (idConquistato != null && comune.Id == idConquistato)
                {
                    stroke = "#FF0000"; // ROSSO
                    strokeWidth = "3";

                    if (comune.Id == idConquistato)
                        MostraNome(comune);
                }
                else if (idAttaccante != null && comune.IdProprietario == idAttaccante)
                {
                    stroke = "#00FF00"; // VERDE
                    strokeWidth = "3";

                    if (comune.Id == idAttaccante)
                        MostraNome(comune);
                }
                else if (idOldProprietario != null && comune.IdProprietario == idOldProprietario)
                {
                    stroke = "#0000FF"; // BLU
                    strokeWidth = "3";

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
            string percorsoOutput = Path.Combine(_outputFolder, nomeFile);

            _svg.Save(percorsoOutput);

            Logger.Log($"Mappa salvata: {percorsoOutput}");

            string fileJpg = Path.Combine(_outputFolder, Path.GetFileNameWithoutExtension(percorsoOutput) + ".jpg");
            GifMaker.ConvertSvgToJpg(percorsoOutput, fileJpg);

            Logger.Log($"Convertito: {Path.GetFileName(percorsoOutput)}");
        }

        private string? GetFillColorFromPath(int id)
        {
            if (!_paths.TryGetValue(id, out XElement? path))
            {
                Logger.Log($"Path non trovato per ID {id}");
                return "#FFFFFF";
            }

            string? style = path.Attribute("style")?.Value;
            return style?
                .Split(';')
                .FirstOrDefault(x => x.TrimStart().StartsWith("fill:"))?
                .Split(':', 2)[1]
                .Trim();
        }

        private void MostraNome(Comune comune)
        {
            if (_paths.TryGetValue(comune.Id, out XElement? path))
            {
                var name = path.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(name))
                    return;

                XElement text = new XElement(Ns + "text");
                text.SetAttributeValue("x", comune.BaricentroTerritorioX.ToString(CultureInfo.InvariantCulture));
                text.SetAttributeValue("y", comune.BaricentroTerritorioY.ToString(CultureInfo.InvariantCulture));
                text.SetAttributeValue("class", "nome-comune");
                text.SetAttributeValue("style", "display:inline");
                text.Value = name;

                _nomiComuni.Add(text);
            }
        }

        private void OrdinaTerritori(IReadOnlyList<Comune> comuni, int? idOldProprietario, int? idAttaccante, int? idConquistato)
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
                ordine.Add(idConquistato.Value);

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

        // Crea (la prima volta) o aggiorna (nei turni successivi) un <pattern> a strisce oblique
        // che alterna il colore del vecchio proprietario e quello dell'attaccante, e lo registra
        // nel <defs> dell'SVG. Restituisce l'id del pattern, da usare come fill="url(#id)".
        private string AssicuraPatternConquista(int idComune, string? coloreVecchio, string? coloreNuovo)
        {
            XElement? defs = _svg.Root!.Element(Ns + "defs");
            if (defs == null)
            {
                defs = new XElement(Ns + "defs");
                _svg.Root!.AddFirst(defs);
            }

            string patternId = $"{PatternConquistatoId}_{idComune}";

            XElement? pattern = defs.Elements(Ns + "pattern")
                .FirstOrDefault(p => (string?)p.Attribute("id") == patternId);

            if (pattern == null)
            {
                pattern = new XElement(Ns + "pattern",
                    new XAttribute("id", patternId),
                    new XAttribute("patternUnits", "userSpaceOnUse"),
                    new XAttribute("patternTransform", "rotate(45)"),
                    new XAttribute("width", PatternStripSize),
                    new XAttribute("height", PatternStripSize));

                defs.Add(pattern);
            }

            // Ricostruisce il contenuto ogni volta: i colori cambiano ad ogni conquista
            // (vecchio proprietario e attaccante sono diversi turno per turno).
            pattern.RemoveNodes();
            pattern.Add(
                new XElement(Ns + "rect",
                    new XAttribute("width", PatternStripSize),
                    new XAttribute("height", PatternStripSize),
                    new XAttribute("fill", coloreVecchio ?? "#FFFFFF")),
                new XElement(Ns + "rect",
                    new XAttribute("width", PatternStripSize / 2),
                    new XAttribute("height", PatternStripSize),
                    new XAttribute("fill", coloreNuovo ?? "#FFFFFF")));

            return patternId;
        }

        private void CaricaPathsComuni()
        {
            foreach (XElement path in _territori.Descendants(Ns + "path"))
            {
                string? idAttr = path.Attribute("id")?.Value;

                if (idAttr != null && int.TryParse(idAttr, out int id))
                {
                    _paths[id] = path;
                }
            }

            Logger.Log($"Indicizzati {_paths.Count} Territori.");
        }

        private void CaricaComuni()
        {
            foreach (XElement path in _territori.Descendants(Ns + "path"))
            {
                string? idAttr = path.Attribute("id")?.Value;
                string? nomeAttr = path.Attribute("name")?.Value;
                string? xCadAttr = path.Attribute("x_cad")?.Value;
                string? yCadAttr = path.Attribute("y_cad")?.Value;

                if (idAttr == null || !int.TryParse(idAttr, out int id))
                    continue;

                if (string.IsNullOrWhiteSpace(nomeAttr))
                {
                    Logger.Log($"ATTENZIONE: nome mancante per id {id}");
                }

                if (xCadAttr == null || yCadAttr == null ||
                    !double.TryParse(xCadAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out double xCad) ||
                    !double.TryParse(yCadAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out double yCad))
                {
                    Logger.Log($"ATTENZIONE: coordinate CAD mancanti/non valide per id {id} ({nomeAttr})");
                    continue;
                }

                double xSvg = OffsetX + ScaleX * xCad;
                double ySvg = OffsetY + ScaleY * yCad;

                Comune comune = new(id, nomeAttr ?? String.Empty, xSvg, ySvg, id);
                _comuni.Add(comune);
            }

            Logger.Log($"Caricati {_comuni.Count} Comuni.");
        }
    }
}