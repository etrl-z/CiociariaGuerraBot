using CiociariaGuerraBot_Console;
using System.Configuration;
using System.Xml.Linq;

Console.WriteLine("START");

int indexer = 0;
Random rnd = new Random();

MapRenderer renderer = new(ConfigurationManager.AppSettings["FileMappa"] ?? String.Empty);

List<Comune> comuni = CaricaComuni();
Console.WriteLine("Lista comuni caricata.");


var buffer = comuni.Where(x => x.IdProprietario == null).ToList();
while (buffer.Count > 1)
{
    Console.WriteLine($"----------------------------------------------------------");

    indexer++;

    Console.WriteLine($"TURNO {indexer}");
    Console.WriteLine($"{buffer.Count} Comuni in gara");

    int rnd_id = rnd.Next(1, 92);
    Comune? comuneEstratto = comuni.FirstOrDefault(c => c.Id == rnd_id);

    if (comuneEstratto == null) continue;
    Console.WriteLine($"Id estratto: {rnd_id} | {comuneEstratto.Nome}");

    Comune? comuneAttaccante;
    if (comuneEstratto.IdProprietario == null)
        comuneAttaccante = comuneEstratto;
    else
        comuneAttaccante = comuni.FirstOrDefault(c => c.Id == comuneEstratto.IdProprietario);

    Console.WriteLine($"Attaccante: {comuneAttaccante?.Nome}");

    Comune? comunePiuVicino = comuni
    .Where(c => c.Id != comuneAttaccante?.Id)
    .Where(c => c.IdProprietario != comuneAttaccante?.Id)
    .OrderBy(c => comuneAttaccante?.DistanzaDa(c))
    .FirstOrDefault();

    #region Genera Testo
    Console.Write($"[{DateTime.Now:dd/MM/yyyy - HH:mm:ss}] {comuneAttaccante?.Nome} ha conquistato il territorio di {comunePiuVicino?.Nome}");

    if (comunePiuVicino?.IdProprietario != null)
    {
        var oldProprietario = comuni.FirstOrDefault(c => c.Id == comunePiuVicino.IdProprietario);
        Console.WriteLine($", precedentemente appartenente al Comune di {oldProprietario?.Nome}.");

        bool nonHaPiuTerritori = !comuni.Any(c => c.Id != comunePiuVicino.Id && c.IdProprietario == oldProprietario?.Id);
        if (nonHaPiuTerritori)
            Console.WriteLine($"Il Comune di {oldProprietario?.Nome} è stato completamente sconfitto.");
    }
    else
    {
        Console.WriteLine($".\nIl Comune di {comunePiuVicino?.Nome} è stato completamente sconfitto.");
    }
    #endregion


    if (comuneAttaccante != null && comunePiuVicino != null)
    {

        // MODIFICA IL PROPRIETARIO DEL COMUNE CONQUISTATO E SETTA IL NUOVO BARICENTRO DEL TERRITORIO
        comuneAttaccante.Assorbi(comunePiuVicino);


        // RICARICA IL BUFFER
        buffer = comuni.Where(x => x.IdProprietario == null).ToList();
        Console.WriteLine($"{buffer.Count} {(buffer.Count > 1 ? "Comuni rimanenti" : "Comune rimanente")}.");


        // RENDERIZZA LA MAPPA
        renderer.Renderizza(comuni, indexer, comuneAttaccante.Id, comunePiuVicino.Id);
    }

}

Console.WriteLine($"----------------------------------------------------------");

var winner = buffer.FirstOrDefault();

if (winner == null) return;
Console.WriteLine($"Ha vinto {winner.Nome}!");

Console.ReadKey();


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
