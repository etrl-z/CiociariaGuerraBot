using System.Collections.Generic;
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
        private readonly XElement _territories;
        private readonly XElement _names;

        private readonly Dictionary<int, XElement> _paths = new();
        private readonly Dictionary<int, string> _originalColors = new();

        private readonly List<Municipality> _municipalities = new();

        /// <summary>
        /// List of municipalities loaded from the map. The elements are still editable
        /// (i.e. OwnerId), but the list itself cannot be replaced or altered from outside.
        /// </summary>
        public IReadOnlyList<Municipality> Municipalities => _municipalities;

        // Coefficients of the conversion CAD -> SVG
        private const double ScaleX = 2.834809;
        private const double ScaleY = -2.834698;
        private const double OffsetX = 510.7850;
        private const double OffsetY = 715.5535;

        // Strips Pattern for the territory that has just been conquered
        private const string PatternConqueredId = "pattern-conquered";
        private const int PatternStripSize = 8;

        public MapRenderer(string fileSvg, string outputFolder)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileSvg);

            if (!File.Exists(fileSvg))
                throw new FileNotFoundException($"File mappa SVG non trovato: '{fileSvg}'.", fileSvg);

            _fileSvg = fileSvg;

            _outputFolder = outputFolder;

            // Load SVG
            _svg = XDocument.Load(_fileSvg);

            // Find group 'Territories'
            _territories = _svg
                .Descendants(Ns + "g")
                .FirstOrDefault(g => (string?)g.Attribute("id") == "Territories")
                ?? throw new InvalidOperationException("Gruppo 'Territories' non trovato nell'SVG.");

            // Find group 'Names'
            _names = _svg
                .Descendants(Ns + "g")
                .FirstOrDefault(g => (string?)g.Attribute("id") == "Names")
                ?? throw new InvalidOperationException("Gruppo 'Names' non trovato nell'SVG.");

            // Indicize all the paths in a Dictionary by their id
            LoadPathsFromSVG();

            // Load all the Municipalities in a List<T>
            LoadMunicipalitiesList();
        }

        public string Render(IReadOnlyList<Municipality> municipalities, int turn = 0, int? attackerId = null, int? conqueredId = null, int? oldOwnerId = null, bool isTest = true)
        {
            _names.RemoveNodes();

            _svg.Root!
            .Element(Ns + "defs")?
            .Elements(Ns + "pattern")
            .Where(p => ((string?)p.Attribute("id"))?.StartsWith($"{PatternConqueredId}_") == true)
            .Remove();

            foreach (Municipality municipality in municipalities)
            {
                if (!_paths.TryGetValue(municipality.Id, out XElement? path))
                {
                    Logger.Log($"ATTENZIONE: territorio non trovato nell'SVG: {municipality.Name}");
                    continue;
                }

                // SET THE COLOR FOR TERRITORIES
                string? color = municipality.OwnerId is int ownerId
                    ? GetFillColorFromPath(ownerId)
                    : GetFillColorFromPath(municipality.Id);

                if (municipality.Id == conqueredId && oldOwnerId != null)
                {
                    string? oldColor = GetFillColorFromPath(oldOwnerId.Value);
                    string? newColor = attackerId != null ? GetFillColorFromPath(attackerId.Value) : oldColor;

                    color = $"url(#{AssignConquerPattern(municipality.Id, oldColor, newColor)})";
                }

                // HIGHLIGHT INVOLVED TERRITORIES
                string stroke = "#000000";
                string strokeWidth = "1";

                if (conqueredId != null && municipality.Id == conqueredId)
                {
                    stroke = "#FF0000"; // RED
                    strokeWidth = "3";
                }
                else if (attackerId != null && municipality.OwnerId == attackerId)
                {
                    stroke = "#00FF00"; // GREEN
                    strokeWidth = "3";
                }
                else if (oldOwnerId != null && municipality.OwnerId == oldOwnerId)
                {
                    stroke = "#0000FF"; // BLUE
                    strokeWidth = "3";
                }

                // SHOW NAMES OF ALL INVOLVED MUNICIPALITIES
                if (municipality.Id == conqueredId ||
                    municipality.Id == attackerId ||
                    municipality.Id == oldOwnerId)
                {
                    ShowName(municipality);
                }

                // APPLY STYLE
                path.SetAttributeValue(
                    "style",
                    $"fill:{color};stroke:{stroke};stroke-width:{strokeWidth};stroke-miterlimit:10"
                );
            }


            OrderTerritories(municipalities, oldOwnerId, attackerId, conqueredId);


            // OUTPUT IMG
            string fileName = $"Mappa_Turno_{turn:D3}.svg";
            string outputPath = Path.Combine(_outputFolder, fileName);

            _svg.Save(outputPath);

            Logger.Log($"Mappa salvata: {outputPath}");

            string outputJpg = "";
            if (!isTest)
            {
                string fileJpg = Path.Combine(_outputFolder, Path.GetFileNameWithoutExtension(outputPath) + ".jpg");
                GifMaker.ConvertSvgToJpg(outputPath, fileJpg);

                Logger.Log($"Convertito: {Path.GetFileName(fileJpg)}");
                outputJpg = fileJpg;
            }

            return outputJpg;
        }


        private string GetFillColorFromPath(int id) =>
            _originalColors.TryGetValue(id, out string? c) ? c : "#FFFFFF";


        private void ShowName(Municipality municipality)
        {
            if (_paths.TryGetValue(municipality.Id, out XElement? path))
            {
                var name = path.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(name))
                    return;

                XElement text = new XElement(Ns + "text");
                text.SetAttributeValue("x", municipality.TerritoryCentroid_X.ToString(CultureInfo.InvariantCulture));
                text.SetAttributeValue("y", municipality.TerritoryCentroid_Y.ToString(CultureInfo.InvariantCulture));
                text.SetAttributeValue("class", "municipality-name");
                text.SetAttributeValue("style", "display:inline");
                text.Value = name;

                _names.Add(text);
            }
        }

        /// <summary>
        /// Render the Territories in this order:
        /// 1. Conquered
        /// 2. Attecker
        /// 3. Old Owner
        /// 4. ecc..
        /// </summary>
        private void OrderTerritories(IReadOnlyList<Municipality> municipalities, int? oldOwnerId, int? attackerId, int? conqueredId)
        {
            List<int> order = [];

            if (oldOwnerId != null)
            {
                order.AddRange(
                    municipalities.Where(c => (c.Id == oldOwnerId || c.OwnerId == oldOwnerId) && c.Id != conqueredId).Select(c => c.Id)
                );
            }

            order.AddRange(
                municipalities.Where(c => (c.Id == attackerId || c.OwnerId == attackerId) && c.Id != conqueredId).Select(c => c.Id)
            );

            if (conqueredId != null)
                order.Add(conqueredId.Value);

            foreach (int id in order)
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

        /// <summary>
        /// Creates (first time) or updates (in the following turns) a stripped pattern which alternates the attacker 
        /// and conquered colors, and registers it in the SVG defs. 
        /// Returns the pattern id, to be used as fill="url(#id)".
        /// </summary>
        private string AssignConquerPattern(int municipalityId, string? oldColor, string? newColor)
        {
            XElement? defs = _svg.Root!.Element(Ns + "defs");
            if (defs == null)
            {
                defs = new XElement(Ns + "defs");
                _svg.Root!.AddFirst(defs);
            }

            string patternId = $"{PatternConqueredId}_{municipalityId}";

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

            // Reloads the content every time: the colors changes at every conquer
            // (old owner and attacker are different for every turn).
            pattern.RemoveNodes();
            pattern.Add(
                new XElement(Ns + "rect",
                    new XAttribute("width", PatternStripSize),
                    new XAttribute("height", PatternStripSize),
                    new XAttribute("fill", oldColor ?? "#FFFFFF")),
                new XElement(Ns + "rect",
                    new XAttribute("width", PatternStripSize / 2),
                    new XAttribute("height", PatternStripSize),
                    new XAttribute("fill", newColor ?? "#FFFFFF")));

            return patternId;
        }

        private void LoadPathsFromSVG()
        {
            foreach (XElement path in _territories.Descendants(Ns + "path"))
            {
                string? attrId = path.Attribute("id")?.Value;

                if (attrId != null && int.TryParse(attrId, out int id))
                {
                    _paths[id] = path;

                    string? fill = path.Attribute("style")?.Value?
                    .Split(';').FirstOrDefault(x => x.TrimStart().StartsWith("fill:"))?
                    .Split(':', 2)[1].Trim();

                    _originalColors[id] = fill ?? "#FFFFFF";
                }
            }

            Logger.Log($"Indicizzati {_paths.Count} Territori.");
        }

        private void LoadMunicipalitiesList()
        {
            foreach (XElement path in _territories.Descendants(Ns + "path"))
            {
                string? attrId = path.Attribute("id")?.Value;
                string? attrName = path.Attribute("name")?.Value;
                string? xCadAttr = path.Attribute("x_cad")?.Value;
                string? yCadAttr = path.Attribute("y_cad")?.Value;

                if (attrId == null || !int.TryParse(attrId, out int id))
                    continue;

                if (string.IsNullOrWhiteSpace(attrName))
                {
                    Logger.Log($"ATTENZIONE: nome mancante per id {id}");
                }

                if (xCadAttr == null || yCadAttr == null ||
                    !double.TryParse(xCadAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out double xCad) ||
                    !double.TryParse(yCadAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out double yCad))
                {
                    Logger.Log($"ATTENZIONE: coordinate CAD mancanti/non valide per id {id} ({attrName})");
                    continue;
                }

                double xSvg = OffsetX + ScaleX * xCad;
                double ySvg = OffsetY + ScaleY * yCad;

                Municipality municipality = new(id, attrName ?? String.Empty, xSvg, ySvg, id);
                _municipalities.Add(municipality);
            }

            Logger.Log($"Caricati {_municipalities.Count} Comuni.");
        }
    }
}