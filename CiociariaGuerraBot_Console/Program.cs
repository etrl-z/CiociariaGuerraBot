using CiociariaGuerraBot_Console;
using System.Configuration;
using System.Xml.Linq;

Console.WriteLine("START");

int indexer = 0;
Random rnd = new Random();

MapRenderer renderer = new(ConfigurationManager.AppSettings["FileMappa"] ?? String.Empty);

List<Comune> comuni = CaricaComuni();
Console.WriteLine("Lista comuni caricata.");

var TEST_EXTRACTIONS = new int[] { 6, 6, 71, 53, 53, 53, 53 };

//renderer.Renderizza(comuni, 0, 3, 85); HandlerConquista(comuni, comuni.FirstOrDefault(c => c.Id == 3), comuni.FirstOrDefault(c => c.Id == 85));
//renderer.Renderizza(comuni, 1, 3, 29); HandlerConquista(comuni, comuni.FirstOrDefault(c => c.Id == 3), comuni.FirstOrDefault(c => c.Id == 29));
//renderer.Renderizza(comuni, 2, 3, 87); HandlerConquista(comuni, comuni.FirstOrDefault(c => c.Id == 3), comuni.FirstOrDefault(c => c.Id == 87));
//renderer.Renderizza(comuni, 3, 42, 87); HandlerConquista(comuni, comuni.FirstOrDefault(c => c.Id == 42), comuni.FirstOrDefault(c => c.Id == 87));
//renderer.Renderizza(comuni, 4, 42, 29); HandlerConquista(comuni, comuni.FirstOrDefault(c => c.Id == 42), comuni.FirstOrDefault(c => c.Id == 29));
//renderer.Renderizza(comuni, 5, 42, 85); HandlerConquista(comuni, comuni.FirstOrDefault(c => c.Id == 42), comuni.FirstOrDefault(c => c.Id == 85));
//renderer.Renderizza(comuni, 6, 42, 3); HandlerConquista(comuni, comuni.FirstOrDefault(c => c.Id == 42), comuni.FirstOrDefault(c => c.Id == 3));
//return;

var buffer = comuni.Where(x => x.IdProprietario == null).ToList();
while (buffer.Count > 1)
{
    Console.WriteLine($"----------------------------------------------------------");

    //int rnd_id = TEST_EXTRACTIONS[indexer];
    int rnd_id = rnd.Next(1, 92);

    indexer++;

    Console.WriteLine($"TURNO {indexer}");
    Console.WriteLine($"{buffer.Count} Comuni in gara");

    Comune? comuneEstratto = comuni.FirstOrDefault(c => c.Id == rnd_id);

    if (comuneEstratto == null) continue;
    Console.WriteLine($"Id estratto: {rnd_id} | {comuneEstratto.Nome}");

    Comune? comuneAttaccante;
    if (comuneEstratto.IdProprietario == null)
        comuneAttaccante = comuneEstratto;
    else
        comuneAttaccante = comuni.FirstOrDefault(c => c.Id == comuneEstratto.IdProprietario);

    Console.WriteLine($"Attaccante: {comuneAttaccante?.Nome}");

    Comune? comuneConquistato = comuni
    .Where(c => c.Id != comuneAttaccante?.Id)
    .Where(c => c.IdProprietario != comuneAttaccante?.Id)
    .OrderBy(c => comuneAttaccante?.DistanzaDa(c))
    .FirstOrDefault();

    #region Genera Testo
    Console.Write($"[{DateTime.Now:dd/MM/yyyy - HH:mm:ss}] {comuneAttaccante?.Nome} ha conquistato il territorio di {comuneConquistato?.Nome}");

    if (comuneConquistato?.IdProprietario != null)
    {
        var oldProprietario = comuni.FirstOrDefault(c => c.Id == comuneConquistato.IdProprietario);
        Console.WriteLine($", precedentemente appartenente al Comune di {oldProprietario?.Nome}.");

        bool nonHaPiuTerritori = !comuni.Any(c => c.Id != comuneConquistato.Id && c.IdProprietario == oldProprietario?.Id);
        if (nonHaPiuTerritori)
            Console.WriteLine($"Il Comune di {oldProprietario?.Nome} è stato completamente sconfitto."); // revisionare
    }
    else
    {
        Console.WriteLine($".\nIl Comune di {comuneConquistato?.Nome} è stato completamente sconfitto."); // revisionare
    }
    #endregion


    if (comuneAttaccante != null && comuneConquistato != null)
    {
        HandlerConquista(comuni, comuneAttaccante, comuneConquistato);

        // RICARICA IL BUFFER
        buffer = comuni.Where(x => x.IdProprietario == null).ToList();
        Console.WriteLine($"{buffer.Count} {(buffer.Count > 1 ? "Comuni rimanenti" : "Comune rimanente")}.");


        // RENDERIZZA LA MAPPA
        renderer.Renderizza(comuni, indexer, comuneAttaccante.Id, comuneConquistato.Id);
    }

}

Console.WriteLine($"----------------------------------------------------------");

var winner = buffer.FirstOrDefault();

if (winner == null) return;
Console.WriteLine($"Ha vinto {winner.Nome}!");

Console.ReadKey();



static void HandlerConquista(List<Comune> comuni, Comune comuneAttaccante, Comune comuneConquistato)
{
    // SALVO IL VECCHIO PROPRIETARIO
    Comune? oldProprietario = null;
    if (comuneConquistato.IdProprietario != null)
        oldProprietario = comuni.FirstOrDefault(c => c.Id == comuneConquistato.IdProprietario);

    // CAMBIO PROPRIETARIO
    comuneConquistato.IdProprietario = comuneAttaccante.Id;

    // RICALCOLO IL BARICENTRO DEL NUOVO PROPRIETARIO
    RicalcolaBaricentro(comuneAttaccante, comuni);

    // RICALCOLO IL BARICENTRO DEL VECCHIO PROPRIETARIO
    if (oldProprietario != null)
    {
        RicalcolaBaricentro(oldProprietario, comuni);
    }

    // DEBUG
    foreach (Comune c in comuni)
    {
        var isexpanded = !(
        c.BaricentroOrigX == c.BaricentroTerritorioX &&
        c.BaricentroOrigY == c.BaricentroTerritorioY);

        var proprietario = comuni.FirstOrDefault(x => x.Id == c.IdProprietario);

        Console.WriteLine(
            $"{c.Id,-2} | " +
            $"{c.Nome,-28} | " +
            $"P: {proprietario?.Nome,-28} | " +
            $"X {c.BaricentroOrigX,8:F2} | " +
            $"Y {c.BaricentroOrigY,8:F2} | " +
            $"X_t {c.BaricentroTerritorioX,8:F2} | " +
            $"Y_t {c.BaricentroTerritorioY,8:F2} | " +
            $"{(isexpanded ? "Y" : "False")}"
        );
    }
}

static void RicalcolaBaricentro(Comune proprietario, List<Comune> comuni)
{
    var territori = comuni.Where(c => c.Id == proprietario.Id || c.IdProprietario == proprietario.Id).ToList();

    if (territori.Count == 0)
    {
        proprietario.BaricentroTerritorioX = proprietario.BaricentroOrigX;
        proprietario.BaricentroTerritorioY = proprietario.BaricentroOrigY;
        return;
    }

    proprietario.BaricentroTerritorioX = territori.Average(c => c.BaricentroOrigX);
    proprietario.BaricentroTerritorioY = territori.Average(c => c.BaricentroOrigY);
}

static List<Comune> CaricaComuni()
{
    string file = ConfigurationManager.AppSettings["FileComuni"] ?? String.Empty;

    XDocument xml = XDocument.Load(file);

    return xml
        .Descendants("Comune")
        .Select(x => new Comune(
            (int)x.Attribute("id")!,
            (string)x.Attribute("nome")!,
            (double)x.Attribute("x")!,
            (double)x.Attribute("y")!
        ))
        .ToList();
}
