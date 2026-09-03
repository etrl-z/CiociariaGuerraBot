using CiociariaGuerraBot_Console;

Console.WriteLine("START");

int indexer = 0;
Random rnd = new Random();

MapRenderer renderer = new MapRenderer("C:\\Users\\emili\\Downloads\\Mappa_vettori.svg");

List<Comune> comuni = SetComuni();
Console.WriteLine("Lista comuni caricata");


var buffer = comuni.Where(x => x.IdProprietario == null).ToList();
while (buffer.Count > 1)
{
    Console.WriteLine($"----------------------------------------------------------");

    indexer++;

    Console.WriteLine($"TURNO {indexer}");
    Console.WriteLine($"{buffer.Count} Comuni in gara");

    int rnd_id = rnd.Next(1, 92);
    Console.WriteLine($"Id estratto: {rnd_id}");

    Comune? comuneEstratto = comuni.FirstOrDefault(c => c.Id == rnd_id);

    if (comuneEstratto == null) continue;
    Console.WriteLine($"È stato estratto: {comuneEstratto.Nome}");

    Comune? attaccante;
    if (comuneEstratto.IdProprietario == null)
        attaccante = comuneEstratto;
    else
        attaccante = comuni.FirstOrDefault(c => c.Id == comuneEstratto.IdProprietario);

    if (attaccante == null) continue;
    Console.WriteLine($"Attaccante: {attaccante.Nome}");

    Comune? comunePiuVicino = comuni
    .Where(c => c.Id != attaccante.Id)
    .Where(c => c.IdProprietario != attaccante.Id)
    .OrderBy(c => attaccante.DistanzaDa(c))
    .FirstOrDefault();

    if (comunePiuVicino == null) continue;
    Console.WriteLine($"Conquistato: {comunePiuVicino.Nome}");

    #region POST
    Console.Write($"[DATA], {attaccante.Nome} ha conquistato il territorio di {comunePiuVicino.Nome}");

    if (comunePiuVicino.IdProprietario != null)
    {
        var oldProprietario = comuni.FirstOrDefault(c => c.Id == comunePiuVicino.IdProprietario);
        if (oldProprietario == null) continue;
        Console.WriteLine($", precedentemente appartenente al Comune di {oldProprietario.Nome}.");

        if (comuni.Where(c => c.Id != comunePiuVicino.Id && c.IdProprietario == oldProprietario.Id) == null)
            Console.WriteLine($"{comunePiuVicino.Nome} è stato completamente sconfitto.");
    }
    else
    {
        Console.WriteLine($".");
        Console.WriteLine($"{comunePiuVicino.Nome} è stato completamente sconfitto.");
    }
    #endregion

    attaccante.Assorbi(comunePiuVicino);

    buffer = comuni.Where(x => x.IdProprietario == null).ToList();
    Console.WriteLine($"{buffer.Count} {(buffer.Count > 1 ? "Comuni rimanenti" : "Comune rimanente")}.");



    renderer.Renderizza(comuni, indexer);


}

Console.WriteLine($"----------------------------------------------------------");

var winner = buffer.FirstOrDefault();

if (winner == null) return;
Console.WriteLine($"Ha vinto {winner.Nome}!");

Console.ReadKey();



static List<Comune> SetComuni()
{
    return
    [
        new Comune(1, "Acquafondata", 298.27, -49.63),
        new Comune(2, "Acuto", -72.27, 97.09),
        new Comune(3, "Alatri", 5.39, 48.67),
        new Comune(4, "Alvito", 193.67, 47.00),
        new Comune(5, "Amaseno", -0.16, -113.14),
        new Comune(6, "Anagni", -81.06, 62.64),
        new Comune(7, "Aquino", 178.50, -95.11),
        new Comune(8, "Arce", 105.02, -41.99),
        new Comune(9, "Arnara", 23.46, -30.22),
        new Comune(10, "Arpino", 136.02, 7.97),
        new Comune(11, "Atina", 212.95, -6.96),
        new Comune(12, "Ausonia", 190.53, -174.75),
        new Comune(13, "Belmonte Castello", 223.01, -30.34),
        new Comune(14, "Boville Ernica", 52.00, -23.15),
        new Comune(15, "Broccostella", 147.38, 44.04),
        new Comune(16, "Campoli Appennino", 172.43, 71.39),
        new Comune(17, "Casalattico", 183.65, -17.51),
        new Comune(18, "Casalvieri", 177.95, 7.49),
        new Comune(19, "Cassino", 234.47, -94.53),
        new Comune(20, "Castelliri", 93.61, 33.29),
        new Comune(21, "Castelnuovo Parano", 193.75, -156.14),
        new Comune(22, "Castro dei Volsci", 39.31, -79.98),
        new Comune(23, "Castrocielo", 166.36, -75.93),
        new Comune(24, "Ceccano", 0.36, -41.83),
        new Comune(25, "Ceprano", 77.84, -53.64),
        new Comune(26, "Cervaro", 263.59, -95.59),
        new Comune(27, "Colfelice", 124.82, -55.67),
        new Comune(28, "Colle San Magno", 177.65, -45.43),
        new Comune(29, "Collepardo", 30.92, 81.96),
        new Comune(30, "Coreno Ausonio", 211.12, -183.59),
        new Comune(31, "Esperia", 148.22, -157.18),
        new Comune(32, "Falvaterra", 87.82, -79.60),
        new Comune(33, "Ferentino", -36.01, 29.88),
        new Comune(34, "Filettino", -3.45, 159.95),
        new Comune(35, "Fiuggi", -50.72, 107.36),
        new Comune(36, "Fontana Liri", 110.12, -8.60),
        new Comune(37, "Fontechiari", 154.04, 24.85),
        new Comune(38, "FROSINONE", 0.00, 0.00),
        new Comune(39, "Fumone", -26.01, 59.93),
        new Comune(40, "Gallinaro", 216.71, 16.70),
        new Comune(41, "Giuliano di Roma", -35.24, -52.04),
        new Comune(42, "Guarcino", -10.32, 110.93),
        new Comune(43, "Isola del Liri", 109.57, 33.25),
        new Comune(44, "Monte San Giovanni Campano", 86.43, 11.50),
        new Comune(45, "Morolo", -65.05, 3.92),
        new Comune(46, "Paliano", -128.63, 92.46),
        new Comune(47, "Pastena", 73.79, -101.22),
        new Comune(48, "Patrica", -34.16, -26.29),
        new Comune(49, "Pescosolido", 151.54, 84.17),
        new Comune(50, "Picinisco", 266.32, 12.88),
        new Comune(51, "Pico", 105.49, -114.23),
        new Comune(52, "Piedimonte San Germano", 194.46, -91.38),
        new Comune(53, "Piglio", -85.64, 114.78),
        new Comune(54, "Pignataro Interamna", 204.66, -123.15),
        new Comune(55, "Pofi", 41.74, -49.57),
        new Comune(56, "Pontecorvo", 146.27, -114.12),
        new Comune(57, "Posta Fibreno", 167.57, 40.47),
        new Comune(58, "Ripi", 62.95, 4.11),
        new Comune(59, "Rocca d'Arce", 126.37, -28.91),
        new Comune(60, "Roccasecca", 142.95, -61.08),
        new Comune(61, "San Biagio Saracinisco", 285.12, -7.28),
        new Comune(62, "San Donato Val di Comino", 221.22, 50.98),
        new Comune(63, "San Giorgio a Liri", 202.91, -142.14),
        new Comune(64, "San Giovanni Incarico", 109.74, -84.60),
        new Comune(65, "San Vittore del Lazio", 283.57, -102.09),
        new Comune(66, "Sant'Ambrogio sul Garigliano", 247.84, -148.38),
        new Comune(67, "Sant'Andrea del Garigliano", 242.39, -169.97),
        new Comune(68, "Sant'Apollinare", 231.58, -141.44),
        new Comune(69, "Sant'Elia Fiumerapido", 247.56, -45.95),
        new Comune(70, "Santopadre", 143.06, -20.86),
        new Comune(71, "Serrone", -110.09, 125.43),
        new Comune(72, "Settefrati", 247.33, 34.42),
        new Comune(73, "Sgurgola", -83.81, 24.94),
        new Comune(74, "Sora", 113.06, 61.02),
        new Comune(75, "Strangolagalli", 76.48, -24.53),
        new Comune(76, "Supino", -55.97, -14.93),
        new Comune(77, "Terelle", 209.64, -48.22),
        new Comune(78, "Torre Cajetani", -31.83, 95.39),
        new Comune(79, "Torrice", 31.34, -4.22),
        new Comune(80, "Trevi nel Lazio", -37.55, 142.03),
        new Comune(81, "Trivigliano", -28.39, 80.62),
        new Comune(82, "Vallecorsa", 33.34, -124.14),
        new Comune(83, "Vallemaio", 221.88, -162.09),
        new Comune(84, "Vallerotonda", 283.50, -37.04),
        new Comune(85, "Veroli", 54.45, 52.12),
        new Comune(86, "Vicalvi", 175.93, 27.72),
        new Comune(87, "Vico nel Lazio", 27.71, 100.91),
        new Comune(88, "Villa Latina", 242.52, -15.76),
        new Comune(89, "Villa Santa Lucia", 206.34, -87.03),
        new Comune(90, "Villa Santo Stefano", -7.24, -76.99),
        new Comune(91, "Viticuso", 301.29, -67.63)
    ];
}

class Comune
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public double BaricentroOrigX { get; set; }
    public double BaricentroOrigY { get; set; }

    public double BaricentroTerritorioX { get; set; }
    public double BaricentroTerritorioY { get; set; }

    public int? IdProprietario { get; set; }

    public Comune(int id, string nome, double x, double y)
    {
        Id = id;
        Nome = nome;
        BaricentroOrigX = x;
        BaricentroOrigY = y;

        BaricentroTerritorioX = x;
        BaricentroTerritorioY = y;
    }

    public double DistanzaDa(Comune altro)
    {
        double dx = BaricentroTerritorioX - altro.BaricentroTerritorioX;
        double dy = BaricentroTerritorioY - altro.BaricentroTerritorioY;

        return Math.Sqrt(dx * dx + dy * dy);
    }

    public void Assorbi(Comune c2)
    {
        c2.IdProprietario = Id;

        BaricentroTerritorioX =
            (BaricentroTerritorioX + c2.BaricentroOrigX) / 2;

        BaricentroTerritorioY =
            (BaricentroTerritorioY + c2.BaricentroOrigY) / 2;
    }
}