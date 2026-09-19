using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CiociariaGuerraBot.ConsoleApp
{
    internal class Municipality
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public double OriginCentroid_X { get; set; }
        public double OriginCentroid_Y { get; set; }
        public double TerritoryCentroid_X { get; set; }
        public double TerritoryCentroid_Y { get; set; }
        public int? OwnerId { get; set; }

        public Municipality() { }

        public Municipality(int _id, string _name, double _x, double _y, int _ownerId)
        {
            Id = _id;
            Name = _name;
            OwnerId = _ownerId;

            OriginCentroid_X = _x;
            OriginCentroid_Y = _y;

            TerritoryCentroid_X = _x;
            TerritoryCentroid_Y = _y;
        }

        public double DistanceFrom(Municipality m2)
        {
            double dx = TerritoryCentroid_X - m2.TerritoryCentroid_X;
            double dy = TerritoryCentroid_Y - m2.TerritoryCentroid_Y;

            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
