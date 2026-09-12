using System.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CiociariaGuerraBot.ConsoleApp
{
    sealed class MapRenderer
    {
        private readonly string _fileSvg;
        private readonly string _cartellaOutput;

        private static readonly XNamespace Ns = "http://www.w3.org/2000/svg";

        private static readonly Regex CssColorRegex = new(
            @"\.(s\d+)\s*\{\s*fill:\s*(#[0-9a-fA-F]{6})",
            RegexOptions.Compiled);

        private readonly XDocument _svg;
        private readonly XElement _territori;
        private readonly XElement _nomiComuni;

        private readonly Dictionary<int, XElement> _paths = new();
        private readonly Dictionary<int, XElement> _texts = new();
        private readonly Dictionary<string, string> _coloriCss = new();

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

        public MapRenderer(string fileSvg)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileSvg);

            if (!File.Exists(fileSvg))
                throw new FileNotFoundException($"File mappa SVG non trovato: '{fileSvg}'.", fileSvg);

            _fileSvg = fileSvg;

            string? cartellaOutput = ConfigurationManager.AppSettings["OutputFolder"];
            if (string.IsNullOrWhiteSpace(cartellaOutput))
                throw new InvalidOperationException("La chiave 'OutputFolder' non è configurata in App.config.");

            _cartellaOutput = cartellaOutput;

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

            // Indicizza tutti i text per id
            CaricaNomiComuni();

            // Indicizza tutti i colori del CSS per id
            CaricaColoriCss();

            // Carica gli oggetti Comune nella lista
            CaricaComuni();

            // Aggiorna le coordinate dei nomi nell'SVG in base ai Comune appena caricati
            AssegnaCoordinateTesti();

            Directory.CreateDirectory(_cartellaOutput);
        }

        public void Renderizza(IReadOnlyList<Comune> comuni, int turno, int? idAttaccante = null, int? idConquistato = null, int? idOldProprietario = null)
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
                string colore = comune.IdProprietario is int idProprietarioColore
                    ? GetColore(idProprietarioColore)
                    : GetColore(comune.Id);

                if (comune.Id == idConquistato && idOldProprietario != null)
                {
                    colore = GetColore(idOldProprietario.Value);
                }

                // EVIDENZIA COMUNI COINVOLTI
                string stroke = "#000000";
                string strokeWidth = "1";

                if (idAttaccante != null && (comune.Id == idAttaccante || comune.IdProprietario == idAttaccante))
                {
                    stroke = "#00FF00"; // VERDE
                    strokeWidth = "3";

                    if (comune.Id == idAttaccante)
                        MostraNome(comune);
                }

                if (idConquistato != null && comune.Id == idConquistato)
                {
                    stroke = "#FF0000"; // ROSSO
                    strokeWidth = "3";

                    if (comune.Id == idConquistato)
                    MostraNome(comune);
                }
                else if (idOldProprietario != null && (comune.Id == idOldProprietario || comune.IdProprietario == idOldProprietario))
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
            string percorsoOutput = Path.Combine(_cartellaOutput, nomeFile);

            _svg.Save(percorsoOutput);

            Console.WriteLine($"Mappa salvata: {percorsoOutput}");
        }

        private void MostraNome(Comune comune)
        {
            if (_texts.TryGetValue(comune.Id, out XElement? text))
            {
                text.SetAttributeValue("x", comune.BaricentroTerritorioX.ToString(CultureInfo.InvariantCulture));
                text.SetAttributeValue("y", comune.BaricentroTerritorioY.ToString(CultureInfo.InvariantCulture));
                text.SetAttributeValue("class", "nome-comune");
                text.SetAttributeValue("style", "display:inline");
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

            Console.WriteLine($"Indicizzati {_paths.Count} Territori.");
        }

        private void CaricaNomiComuni()
        {
            foreach (XElement text in _nomiComuni.Descendants(Ns + "text"))
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
            XElement? style = _svg.Descendants().FirstOrDefault(e => e.Name.LocalName == "style");

            if (style == null)
                throw new InvalidOperationException("Nessun elemento <style> trovato nell'SVG.");

            string css = style.Value;

            foreach (Match match in CssColorRegex.Matches(css))
            {
                string classe = match.Groups[1].Value;
                string colore = match.Groups[2].Value;

                _coloriCss[classe] = colore;
            }

            Console.WriteLine($"Caricate {_coloriCss.Count} classi CSS.");
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
                    Console.WriteLine($"ATTENZIONE: nome mancante per id {id}");
                }

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

        // Posiziona ogni <text> del gruppo NomiComuni sulle coordinate SVG del relativo Comune.
        private void AssegnaCoordinateTesti()
        {
            foreach (Comune comune in _comuni)
            {
                if (_texts.TryGetValue(comune.Id, out XElement? text))
                {
                    text.SetAttributeValue("x", comune.BaricentroOrigX.ToString(CultureInfo.InvariantCulture));
                    text.SetAttributeValue("y", comune.BaricentroOrigY.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    Console.WriteLine($"ATTENZIONE: testo non trovato per id {comune.Id} ({comune.Nome})");
                }
            }
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