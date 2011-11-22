using System.Xml.Serialization;
using Engine.Math;
using Microsoft.Xna.Framework.Content;

namespace SpaceData
{
    public class ShipData
    {
        public string Name;

        public Fixed Radius;
        public string Texture;

        public int Health;
        public int Fuel;

        public Fixed Acceleration;
        public Fixed RotationSpeed;

        [ContentSerializer(Optional=true)]
        public byte SmallWeapons;
        [ContentSerializer(Optional = true)]
        public byte MediumWeapons;
        [ContentSerializer(Optional = true)]
        public byte LargeWeapons;

        [ContentSerializer(Optional = true)]
        public byte ItemSlots;
    }

    public enum WeaponSize
    {
        [XmlEnum(Name = "Small")]
        Small,
        [XmlEnum(Name = "Medium")]
        Medium,
        [XmlEnum(Name = "Large")]
        Large
    }

    public class WeaponData
    {
        public string Name;

        public WeaponSize Size;

        public Fixed Damage;
        public Fixed FireRate;
    }

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
}
