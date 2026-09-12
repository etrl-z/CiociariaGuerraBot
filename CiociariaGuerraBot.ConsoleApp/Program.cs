using CiociariaGuerraBot.ConsoleApp;
using System.Configuration;

Console.WriteLine("START");

// --- CARICAMENTO CONFIGURAZIONE ---
string? fileMappa = ConfigurationManager.AppSettings["FileMappa"];
if (string.IsNullOrWhiteSpace(fileMappa))
{
    Console.Error.WriteLine("ERRORE: chiave 'FileMappa' non configurata.");
    return;
}

MapRenderer renderer;
try
{
    renderer = new MapRenderer(fileMappa);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"ERRORE durante il caricamento della mappa '{fileMappa}': {ex.Message}");
    return;
}

List<Comune> comuni = renderer._comuni;
if (comuni.Count == 0)
{
    Console.Error.WriteLine("ERRORE: nessun comune caricato dal file mappa.");
    return;
}

Console.WriteLine($"Lista comuni caricata ({comuni.Count} comuni).");

int indexer = 0;

List<int> comuniInGara = GetComuniInGara(comuni);

while (comuniInGara.Count > 1)
{
    Console.WriteLine("----------------------------------------------------------");
    indexer++;
    Console.WriteLine($"TURNO {indexer}");
    Console.WriteLine($"{comuniInGara.Count} Comuni in gara");

    Comune comuneEstratto = comuni[Random.Shared.Next(comuni.Count)];
    Console.WriteLine($"Id estratto: {comuneEstratto.Id} | {comuneEstratto.Nome}");

    Comune comuneAttaccante = comuneEstratto.IdProprietario is int idProprietario
        ? comuni.First(c => c.Id == idProprietario)
        : comuneEstratto;

    Console.WriteLine($"Attaccante: {comuneAttaccante.Id} | {comuneAttaccante.Nome}");

    Comune? comuneConquistato = comuni
        .Where(c => c.Id != comuneAttaccante.Id)
        .Where(c => c.IdProprietario != comuneAttaccante.Id)
        .OrderBy(c => comuneAttaccante.DistanzaDa(c))
        .FirstOrDefault();

    if (comuneConquistato == null)
    {
        Console.WriteLine("Nessun bersaglio disponibile per l'attaccante estratto, salto il turno.");
        continue;
    }

    HandlerConquista(renderer, indexer, comuni, comuneAttaccante.Id, comuneConquistato.Id);

    comuniInGara = GetComuniInGara(comuni);
    Console.WriteLine($"{comuniInGara.Count} {(comuniInGara.Count > 1 ? "Comuni rimanenti" : "Comune rimanente")}.");
}

Console.WriteLine("----------------------------------------------------------");

Comune? winner = comuni.FirstOrDefault(c => c.Id == comuniInGara.FirstOrDefault());

if (winner != null)
{
    renderer.Renderizza(comuni, ++indexer, winner.Id);

    Console.WriteLine($"{winner.Nome} ha interamente conquistato la Ciociaria.");
    Console.WriteLine($"Tutti i territori sono stati unificati e formano ora il Comune di {winner.Nome}.");
}
else
{
    Console.WriteLine("ERRORE: impossibile determinare il vincitore.");
}

Console.WriteLine("Premi un tasto per uscire...");
Console.ReadKey();

static List<int> GetComuniInGara(List<Comune> comuni) =>
    comuni.Select(c => c.IdProprietario ?? c.Id).Distinct().ToList();

static void HandlerConquista(MapRenderer renderer, int indexer, List<Comune> comuni, int comuneAttaccanteId, int comuneConquistatoId)
{
    Comune? comuneAttaccante = comuni.FirstOrDefault(c => c.Id == comuneAttaccanteId);
    Comune? comuneConquistato = comuni.FirstOrDefault(c => c.Id == comuneConquistatoId);

    if (comuneAttaccante == null || comuneConquistato == null)
        return;

    // SALVO IL VECCHIO PROPRIETARIO (non è mai null: o è il proprietario reale, o è il comune stesso)
    Comune oldProprietario = comuneConquistato.IdProprietario is int idProprietario
        ? comuni.First(c => c.Id == idProprietario)
        : comuneConquistato;

    // CAMBIO PROPRIETARIO
    comuneConquistato.IdProprietario = comuneAttaccante.Id;

    GeneraTesto(comuni, comuneAttaccante, comuneConquistato, oldProprietario);

    // RICALCOLO I BARICENTRI DI ATTACCANTE E VECCHIO PROPRIETARIO
    RicalcolaBaricentro(comuneAttaccante, comuni);
    RicalcolaBaricentro(oldProprietario, comuni);

    GeneraReport(comuni);

    renderer.Renderizza(comuni, indexer, comuneAttaccante.Id, comuneConquistato.Id, oldProprietario.Id);
}

static void GeneraTesto(List<Comune> comuni, Comune comuneAttaccante, Comune comuneConquistato, Comune oldProprietario)
{
    Console.Write($"[{DateTime.Now:dd/MM/yyyy - HH:mm:ss}] {comuneAttaccante.Nome} ha conquistato il territorio di {comuneConquistato.Nome}");

    bool eraIndipendente = oldProprietario.Id == comuneConquistato.Id;
    Console.WriteLine(eraIndipendente
        ? "."
        : $", precedentemente appartenente al Comune di {oldProprietario.Nome}.");

    // La sconfitta totale va dichiarata solo quando NESSUN comune fa più riferimento a oldProprietario come proprietario
    bool haAncoraTerritori = comuni.Any(c => c.IdProprietario == oldProprietario.Id);
    if (!haAncoraTerritori)
    {
        Console.WriteLine($"Il Comune di {oldProprietario.Nome} è stato completamente sconfitto.");
    }
}

static void GeneraReport(List<Comune> comuni)
{
    foreach (Comune c in comuni)
    {
        bool isExpanded = c.BaricentroOrigX != c.BaricentroTerritorioX
            || c.BaricentroOrigY != c.BaricentroTerritorioY;

        Comune? proprietario = comuni.FirstOrDefault(x => x.Id == c.IdProprietario);

        Console.WriteLine(
            $"{c.Id,-2} | " +
            $"{c.Nome,-28} | " +
            $"P: {proprietario?.Nome,-28} | " +
            $"X {c.BaricentroOrigX,8:F2} | " +
            $"Y {c.BaricentroOrigY,8:F2} | " +
            $"X_t {c.BaricentroTerritorioX,8:F2} | " +
            $"Y_t {c.BaricentroTerritorioY,8:F2} | " +
            $"{(isExpanded ? "Y" : "False")}"
        );
    }
}

static void RicalcolaBaricentro(Comune proprietario, List<Comune> comuni)
{
    List<Comune> territori = comuni.Where(c => c.Id == proprietario.Id || c.IdProprietario == proprietario.Id).ToList();

    if (territori.Count == 0)
    {
        proprietario.BaricentroTerritorioX = proprietario.BaricentroOrigX;
        proprietario.BaricentroTerritorioY = proprietario.BaricentroOrigY;
        return;
    }

    proprietario.BaricentroTerritorioX = territori.Average(c => c.BaricentroOrigX);
    proprietario.BaricentroTerritorioY = territori.Average(c => c.BaricentroOrigY);
}