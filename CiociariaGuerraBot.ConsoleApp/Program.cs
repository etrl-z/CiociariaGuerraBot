using CiociariaGuerraBot.ConsoleApp;
using System.Configuration;

// --- CARICAMENTO CONFIGURAZIONI ---
string? cartellaOutput = ConfigurationManager.AppSettings["OutputFolder"] + $"\\{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";
if (string.IsNullOrWhiteSpace(cartellaOutput))
{
    throw new FileNotFoundException("ERRORE: chiave 'OutputFolder' non configurata.");
}

Directory.CreateDirectory(cartellaOutput);

Logger._logPath = Path.Combine(cartellaOutput, $"Run_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.txt");

string? fileMappa = ConfigurationManager.AppSettings["FileMappa"];
if (string.IsNullOrWhiteSpace(fileMappa))
{
    Logger.Log("ERRORE: chiave 'FileMappa' non configurata.");
    return;
}

Logger.Log("START");

MapRenderer renderer;

try
{
    renderer = new MapRenderer(fileMappa, cartellaOutput);
}
catch (Exception ex)
{
    Logger.Log($"ERRORE durante il caricamento della mappa '{fileMappa}': {ex.Message}");
    return;
}

IReadOnlyList<Comune> comuni = renderer.Comuni;
if (comuni.Count == 0)
{
    Logger.Log("ERRORE: nessun comune caricato dal file mappa.");
    return;
}

Logger.Log($"Lista comuni caricata ({comuni.Count} comuni).");

int indexer = 0;

IReadOnlyList<int> comuniInGara = GetComuniInGara(comuni);

while (comuniInGara.Count > 1)
{
    Logger.Log("---------------------------------------------------------------------------------------------------------------------------");
    indexer++;
    Logger.Log($"TURNO {indexer}");
    Logger.Log($"{comuniInGara.Count} Comuni in gara");

    Comune comuneEstratto = comuni[Random.Shared.Next(comuni.Count)];
    Logger.Log($"Id estratto: {comuneEstratto.Id} | {comuneEstratto.Nome}");

    Comune comuneAttaccante = comuneEstratto.IdProprietario is int idProprietario
        ? comuni.First(c => c.Id == idProprietario)
        : comuneEstratto;

    Logger.Log($"Attaccante: {comuneAttaccante.Id} | {comuneAttaccante.Nome}");

    Comune? comuneConquistato = comuni
        .Where(c => c.Id != comuneAttaccante.Id)
        .Where(c => c.IdProprietario != comuneAttaccante.Id)
        .OrderBy(c => comuneAttaccante.DistanzaDa(c))
        .FirstOrDefault();

    if (comuneConquistato == null)
    {
        Logger.Log("Nessun bersaglio disponibile per l'attaccante estratto, salto il turno.");
        continue;
    }

    HandlerConquista(renderer, indexer, comuni, comuneAttaccante.Id, comuneConquistato.Id);

    comuniInGara = GetComuniInGara(comuni);
    Logger.Log($"{comuniInGara.Count} {(comuniInGara.Count > 1 ? "Comuni rimanenti" : "Comune rimanente")}.");
}

Logger.Log("---------------------------------------------------------------------------------------------------------------------------");

Comune? winner = comuni.FirstOrDefault(c => c.Id == comuniInGara.FirstOrDefault());

if (winner != null)
{
    renderer.Renderizza(comuni, ++indexer, winner.Id);

    Logger.Log($"{winner.Nome} ha interamente conquistato la Ciociaria.");
    Logger.Log($"Tutti i territori sono stati unificati e formano ora il Comune di {winner.Nome}.");
}
else
{
    Logger.Log("ERRORE: impossibile determinare il vincitore.");
}


// GENERAZIONE GIF
// ----------------------------------------------------------------------------------------------------

GifMaker.CreateGif(cartellaOutput);

// ----------------------------------------------------------------------------------------------------


Console.WriteLine("Premi un tasto per uscire...");
Console.ReadKey();

static IReadOnlyList<int> GetComuniInGara(IReadOnlyList<Comune> comuni) =>
    comuni.Select(c => c.IdProprietario ?? c.Id).Distinct().ToList();

static void HandlerConquista(MapRenderer renderer, int indexer, IReadOnlyList<Comune> comuni, int comuneAttaccanteId, int comuneConquistatoId)
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


    renderer.Renderizza(comuni, indexer, comuneAttaccante.Id, comuneConquistato.Id, oldProprietario.Id);


    // RICALCOLO I BARICENTRI DI ATTACCANTE E VECCHIO PROPRIETARIO
    RicalcolaBaricentro(comuneAttaccante, comuni);
    RicalcolaBaricentro(oldProprietario, comuni);

    GeneraReport(comuni);

}

static void GeneraTesto(IReadOnlyList<Comune> comuni, Comune comuneAttaccante, Comune comuneConquistato, Comune oldProprietario)
{
    Logger.Log($"[{DateTime.Now:dd/MM/yyyy - HH:mm:ss}] {comuneAttaccante.Nome} ha conquistato il territorio di {comuneConquistato.Nome}", false);

    bool eraIndipendente = oldProprietario.Id == comuneConquistato.Id;
    Logger.Log(eraIndipendente
        ? "."
        : $", precedentemente appartenente al Comune di {oldProprietario.Nome}.");

    // La sconfitta totale va dichiarata solo quando NESSUN comune fa più riferimento a oldProprietario come proprietario
    bool haAncoraTerritori = comuni.Any(c => c.IdProprietario == oldProprietario.Id);
    if (!haAncoraTerritori)
    {
        Logger.Log($"Il Comune di {oldProprietario.Nome} è stato completamente sconfitto.");
    }
}

static void GeneraReport(IReadOnlyList<Comune> comuni)
{
    foreach (Comune c in comuni)
    {
        Comune? proprietario = comuni.FirstOrDefault(x => x.Id == c.IdProprietario);

        Logger.Log(
            $"{c.Id,-2} | " +
            $"{c.Nome,-28} | " +
            $"P: {proprietario?.Nome,-28} | " +
            $"X {c.BaricentroOrigX,8:F2} | " +
            $"Y {c.BaricentroOrigY,8:F2} | " +
            $"X_t {c.BaricentroTerritorioX,8:F2} | " +
            $"Y_t {c.BaricentroTerritorioY,8:F2}"
        );
    }
}

static void RicalcolaBaricentro(Comune proprietario, IReadOnlyList<Comune> comuni)
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