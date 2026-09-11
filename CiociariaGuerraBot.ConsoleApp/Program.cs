using CiociariaGuerraBot.ConsoleApp;
using System.Configuration;
using System.Diagnostics;
using System.Xml.Linq;

Console.WriteLine("START");

int indexer = 0;
Random rnd = new Random();

MapRenderer renderer = new(ConfigurationManager.AppSettings["FileMappa"] ?? String.Empty);

List<Comune> comuni = CaricaComuni();
Console.WriteLine("Lista comuni caricata.");

//renderer.Renderizza(comuni, 0);
//return;

List<int> comuniInGara;
while (true)
{
    comuniInGara = comuni.Select(c => c.IdProprietario ?? c.Id).Distinct().ToList();
    if (comuniInGara.Count <= 1)
        break;

    Console.WriteLine($"----------------------------------------------------------");

    int rnd_id = rnd.Next(1, 92);

    indexer++;

    Console.WriteLine($"TURNO {indexer}");
    Console.WriteLine($"{comuniInGara.Count} Comuni in gara");

    Comune? comuneEstratto = comuni.FirstOrDefault(c => c.Id == rnd_id);

    if (comuneEstratto == null) continue;
    Console.WriteLine($"Id estratto: {rnd_id} | {comuneEstratto.Nome}");

    Comune? comuneAttaccante = comuneEstratto;
    if (comuneEstratto.IdProprietario != null)
        comuneAttaccante = comuni.FirstOrDefault(c => c.Id == comuneEstratto.IdProprietario);

    Console.WriteLine($"Attaccante: {comuneAttaccante?.Id} | {comuneAttaccante?.Nome}");

    Comune? comuneConquistato = comuni
    .Where(c => c.Id != comuneAttaccante?.Id)
    .Where(c => c.IdProprietario != comuneAttaccante?.Id)
    .OrderBy(c => comuneAttaccante?.DistanzaDa(c))
    .FirstOrDefault();

    if (comuneAttaccante != null && comuneConquistato != null)
    {
        HandlerConquista(renderer, indexer, comuni, comuneAttaccante.Id, comuneConquistato.Id);

        // RICARICA IL BUFFER DEI COMUNI IN GARA
        comuniInGara = comuni.Select(c => c.IdProprietario ?? c.Id).Distinct().ToList();
        Console.WriteLine($"{comuniInGara.Count} {(comuniInGara.Count > 1 ? "Comuni rimanenti" : "Comune rimanente")}.");
    }
}

Console.WriteLine($"----------------------------------------------------------");

var winnerId = comuniInGara.FirstOrDefault();
Comune? winner = comuni.FirstOrDefault(c => c.Id == winnerId);

if (winner != null)
{
    renderer.Renderizza(comuni, ++indexer, winner.Id);

    Console.WriteLine($"{winner?.Nome} ha interamente conquistato la Ciociaria.");
    Console.WriteLine($"Tutti i territori sono stati unificati e formano ora il Comune di {winner?.Nome}.");

    Console.ReadKey();
}


static void HandlerConquista(MapRenderer renderer, int indexer, List<Comune> comuni, int comuneAttaccanteId, int comuneConquistatoId)
{
    Comune? comuneAttaccante = comuni.FirstOrDefault(c => c.Id == comuneAttaccanteId);
    Comune? comuneConquistato = comuni.FirstOrDefault(c => c.Id == comuneConquistatoId);

    if (comuneAttaccante == null || comuneConquistato == null)
        return;

    // SALVO IL VECCHIO PROPRIETARIO
    Comune oldProprietario;
    if (comuneConquistato.IdProprietario != null)
        oldProprietario = comuni.First(c => c.Id == comuneConquistato.IdProprietario);
    else
        oldProprietario = comuneConquistato;

    // CAMBIO PROPRIETARIO
    comuneConquistato.IdProprietario = comuneAttaccante.Id;

    // GENERA TESTO
    GeneraTesto(comuni, comuneAttaccante, comuneConquistato, oldProprietario);

    // RICALCOLO IL BARICENTRO DEL NUOVO PROPRIETARIO
    RicalcolaBaricentro(comuneAttaccante, comuni);

    // RICALCOLO IL BARICENTRO DEL VECCHIO PROPRIETARIO
    RicalcolaBaricentro(oldProprietario, comuni);

    GeneraReport(comuni);

    // RENDERIZZA LA MAPPA
    renderer.Renderizza(comuni, indexer, comuneAttaccante.Id, comuneConquistato.Id, oldProprietario.Id);

}

static void GeneraTesto(List<Comune> comuni, Comune comuneAttaccante, Comune comuneConquistato, Comune? oldProprietario)
{
    Console.Write($"[{DateTime.Now:dd/MM/yyyy - HH:mm:ss}] {comuneAttaccante.Nome} ha conquistato il territorio di {comuneConquistato.Nome}");

    if (oldProprietario != null)
    {
        Console.WriteLine($", precedentemente appartenente al Comune di {oldProprietario.Nome}.");

        bool haAncoraTerritori = comuni.Any(c => c.IdProprietario == oldProprietario.Id);
        if (!haAncoraTerritori)
        {
            Console.WriteLine($"Il Comune di {oldProprietario.Nome} è stato completamente sconfitto.");
        }
    }
    else
    {
        Console.WriteLine($". Il Comune di {comuneConquistato.Nome} è stato completamente sconfitto.");
    }
}

static void GeneraReport(List<Comune> comuni)
{
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
