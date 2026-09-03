using CiociariaGuerraBot_Console;
using System.Configuration;
using System.Xml.Linq;

Console.WriteLine("START");

int indexer = 0;
Random rnd = new Random();

MapRenderer renderer = new(ConfigurationManager.AppSettings["FileMappa"] ?? String.Empty);

List<Comune> comuni = SetComuni();
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

    #region Genera Testo
    Console.Write($"[{DateTime.Now:dd/MM/yyyy - HH:mm:ss}] {attaccante.Nome} ha conquistato il territorio di {comunePiuVicino.Nome}");

    if (comunePiuVicino.IdProprietario != null)
    {
        var oldProprietario = comuni.FirstOrDefault(c => c.Id == comunePiuVicino.IdProprietario);
        if (oldProprietario == null) continue;
        Console.WriteLine($", precedentemente appartenente al Comune di {oldProprietario.Nome}.");

        if (comuni.Where(c => c.Id != comunePiuVicino.Id && c.IdProprietario == oldProprietario.Id) == null)
            Console.WriteLine($"Il Comune di {comunePiuVicino.Nome} è stato completamente sconfitto.");
    }
    else
    {
        Console.WriteLine($".\nIl Comune di {comunePiuVicino.Nome} è stato completamente sconfitto.");
    }
    #endregion


    // MODIFICA IL PROPRIETARIO DEL COMUNE CONQUISTATO E SETTA IL NUOVO BARICENTRO DEL TERRITORIO
    attaccante.Assorbi(comunePiuVicino);


    // RICARICA IL BUFFER
    buffer = comuni.Where(x => x.IdProprietario == null).ToList();
    Console.WriteLine($"{buffer.Count} {(buffer.Count > 1 ? "Comuni rimanenti" : "Comune rimanente")}.");


    // RENDERIZZA LA MAPPA
    renderer.Renderizza(comuni, indexer, attaccante.Id, comunePiuVicino.Id);


}

Console.WriteLine($"----------------------------------------------------------");

var winner = buffer.FirstOrDefault();

if (winner == null) return;
Console.WriteLine($"Ha vinto {winner.Nome}!");

Console.ReadKey();


static List<Comune> SetComuni()
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
