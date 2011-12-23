using System.Xml.Serialization;
using Engine.Math;
using Microsoft.Xna.Framework.Content;

namespace Space.Data
{
    public class ItemData
    {
        public string Name;

        [ContentSerializer(Optional = true)]
        public int Health;
        [ContentSerializer(Optional = true)]
        public int Fuel;

        [ContentSerializer(Optional = true)]
        public Fixed Acceleration;
        [ContentSerializer(Optional = true)]
        public Fixed MaxSpeed;
        [ContentSerializer(Optional = true)]
        public Fixed RotationSpeed;

        [ContentSerializer(Optional = true)]
        public Fixed Damage;
        [ContentSerializer(Optional = true)]
        public Fixed FireRate;
    }

    public enum DropType
    {
        [XmlEnum(Name = "Weapon")]
        Weapon,
        [XmlEnum(Name = "Item")]
        Item
    }

    public class DropData
    {
        public string ID;
        public DropType Type;
        public string Name;
        public float Chance;
    }
    
    public class MobData
    {
        public string Name;

        public int Level;

        public string ShipName;
        public string[] Weapons;
        public string[] Items;

        public string[] Drops;
    }

    public class WorldConstaints
    {
        /// <summary>
        /// Average overall radius of a solar system.
        /// </summary>
        public Fixed SolarSystemRadiusMean;

        /// <summary>
        /// The allowed deviation from the mean for a solar system, as a
        /// fraction of it's mean size.
        /// </summary>
        public Fixed SolarSystemRadiusStdDevFraction;

        /// <summary>
        /// The chance, in percent, that a solar system has more than one sun.
        /// </summary>
        public Fixed SolarSystemMultiSunChance;

        /// <summary>
        /// The number of suns a system may have at max.
        /// </summary>
        public int SolarSystemMaxSuns;

        /// <summary>
        /// The average size of a planet's radius.
        /// </summary>
        public Fixed PlanetRadiusMean;

        /// <summary>
        /// The allowed deviation from the mean when generating a planet,
        /// as a fraction of the average planet size.
        /// </summary>
        public Fixed PlanetRadiusStdDev;
        
        /// <summary>
        /// The upward deviation from the minimum buffer (being: next lower
        /// orbit + (that planet's moon's orbit + radius if it has one, else
        /// the planet's orbit + radius).
        /// </summary>
        public Fixed PlanetOrbitStdDev;

        /// <summary>
        /// The mean size of a moon as a fraction of the planet it'll belong to.
        /// </summary>
        public Fixed MoonRadiusMean;

        /// <summary>
        /// The standard deviation of a moon's size as the fraction of its
        /// radius.
        /// </summary>
        public Fixed MoonRadiusStdDevFraction;
    }
}
