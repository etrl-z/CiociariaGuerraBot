using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CiociariaGuerraBot.ConsoleApp
{
    internal class Comune
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public double BaricentroOrigX { get; set; }
        public double BaricentroOrigY { get; set; }
        public double BaricentroTerritorioX { get; set; }
        public double BaricentroTerritorioY { get; set; }
        public int? IdProprietario { get; set; }

        public Comune(int id, string nome, double x, double y, int idProprietario)
        {
            Id = id;
            Nome = nome;
            IdProprietario = idProprietario;

            BaricentroOrigX = x;
            BaricentroOrigY = y;

            BaricentroTerritorioX = x;
            BaricentroTerritorioY = y;
        }

        public double DistanzaDa(Comune c2)
        {
            double dx = BaricentroTerritorioX - c2.BaricentroTerritorioX;
            double dy = BaricentroTerritorioY - c2.BaricentroTerritorioY;

            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
