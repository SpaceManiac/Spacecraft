using System;
using System.Collections;
using System.Collections.Generic;
namespace spacecraft
{
    public enum Block : byte
    {
        Undefined = 255, // for error checking

        Air = 0,
        Rock = 1,
        Grass = 2,
        Dirt = 3,
        Cobblestone = 4,
        Wood = 5,
        Sapling = 6,
        Adminium = 7,
        Water = 8,
        StillWater = 9,
        Lava = 10,
        StillLava = 11,
        Sand = 12,
        Gravel = 13,
        GoldOre = 14,
        IronOre = 15,
        CoalOre = 16,
        Log = 17,
        Leaves = 18,
        Sponge = 19,
        Glass = 20,

        Red = 21,
        Orange = 22,
        Yellow = 23,
        Lime = 24,
        Green = 25,
        Teal = 26,
        Aqua = 27,
        Cyan = 28,
        Blue = 29,
        Indigo = 30,
        Violet = 31,
        Magenta = 32,
        Pink = 33,
        Black = 34,
        Gray = 35,
        White = 36,

        YellowFlower = 37,
        RedFlower = 38,
        RedMushroom = 39,
        BrownMushroom = 40,

        Gold = 41,
        Iron = 42,
        DoubleStair = 43,
        Stair = 44,
        Brick = 45,
        TNT = 46,
        Books = 47,
        MossyCobblestone = 48,
        Obsidian = 49,

        // indev only
        /*I_Torch = 0x32,
        I_Fire = 0x33,
        I_InfWater = 0x34*/
        // Not needed, since we don't do INDEV.
    }

    public static class BlockInfo
    {
        public static Dictionary<string, Block> names;

        public static bool IsFluid(Block block)
        {
            return (block == Block.Water || block == Block.StillWater
                    || block == Block.Lava || block == Block.StillLava);
        }
        public static bool IsDecoration(Block block)
        {
            return (block == Block.YellowFlower || block == Block.RedFlower ||
                    block == Block.BrownMushroom || block == Block.RedMushroom ||
                    block == Block.Sapling);
        }

        public static bool IsTransparent(Block block)
        {
            return (IsDecoration(block) || block == Block.Glass || block == Block.Leaves || block == Block.Air);
        }
        public static bool IsOpaque(Block block)
        {
            return !IsTransparent(block);
        }

        public static bool IsSolid(Block block)
        {
            return (block != Block.Air && !IsFluid(block) && !IsDecoration(block));
        }

        static BlockInfo()
        {
            names = new Dictionary<string, Block>();

            foreach (string block in Enum.GetNames(typeof(Block)))
            {
                names.Add(block.ToLower(), (Block)Enum.Parse(typeof(Block), block));
            }

            names["none"] = Block.Air;
            names["nothing"] = Block.Air;
            names["empty"] = Block.Air;
            names["soil"] = Block.Dirt;
            names["stone"] = Block.Rock;
            names["rocks"] = Block.Cobblestone;
            names["plant"] = Block.Sapling;
            names["admincrete"] = Block.Adminium;
            names["admin"] = Block.Adminium;
            names["ore"] = Block.IronOre;
            names["coal"] = Block.CoalOre;
            names["trunk"] = Block.Log;
            names["treetrunk"] = Block.Log;
            names["foliage"] = Block.Leaves;
            names["grey"] = Block.Gray;
            names["flower"] = Block.YellowFlower;
            names["mushroom"] = Block.BrownMushroom;
            names["steel"] = Block.Iron;
            names["metal"] = Block.Iron;
            names["silver"] = Block.Iron;
            names["stairs"] = Block.DoubleStair;
            names["bricks"] = Block.Brick;
            names["dynamite"] = Block.TNT;
            names["bookcase"] = Block.Books;
            names["shelf"] = Block.Books;
            names["shelves"] = Block.Books;
            names["book"] = Block.Books;
            names["moss"] = Block.MossyCobblestone;
            names["mossy"] = Block.MossyCobblestone;
            names["mossystone"] = Block.MossyCobblestone;
            names["mossyrocks"] = Block.MossyCobblestone;
            names["mossystones"] = Block.MossyCobblestone;
            names["dark"] = Block.Obsidian;
        }

        public static bool NameExists(string key)
        {
            return names.ContainsKey(key);
        }
    }


    public partial class Packet
    {
        public enum PacketType : byte
        {
            UNKNOWN = 0xFF, // Used by PacketFromLength, when the length does not match any packet.
            Ident = 0x00, // client & server
            Ping = 0x01, // server
            LevelInit = 0x02, // server
            LevelChunk = 0x03, // server
            LevelFinish = 0x04, // server
            PlayerSetBlock = 0x05, // client
            ServerSetBlock = 0x06, // server
            SpawnPlayer = 0x07, // server
            PositionUpdate = 0x08, // client & server
            U_PositionUpdate = 0x09, // unused server
            U_PositionUpdate2 = 0x0a, // unused server
            U_OrientUpdate = 0x0b, // unused server
            DespawnPlayer = 0x0c, // server
            Message = 0x0d, // client & server
            Kick = 0x0e, // server
        }

        public enum PacketLength : int
        {   
            Ident = 131, // client & server
            Ping = 1, // server
            LevelInit = 1, // server
            LevelChunk = 1028, // server
            LevelFinish = 7, // server
            PlayerSetBlock = 9, // client
            ServerSetBlock = 8, // server
            SpawnPlayer = 74, // server
            PositionUpdate = 10, // client & server
            U_PositionUpdate = 10, // unused server
            U_PositionUpdate2 = 8, // unused server
            U_OrientUpdate = 4, // unused server
            DespawnPlayer = 2, // server
            Message = 66, // client & server
            Kick = 65, // server
        }

        static public PacketType PacketFromLength(int length)
        {
            PacketType val;
            // TODO: Make this better.

            switch (length)
            {
                case 131:
                    val= PacketType.Ident;
                    break;
                case 1:
                    val = PacketType.Ping;
                    // LevelInit is /also/ one byte long, but there should be no reason we need to identify it by it's length.
                    break;
                case 1028:
                    val = PacketType.LevelChunk;
                    break;
                case 7:
                    val = PacketType.LevelFinish;
                    break;
                case 9:
                    val = PacketType.PlayerSetBlock;
                    break;
                case 8:
                    val = PacketType.ServerSetBlock;
                    break;
                // Ditto PacketType.U_PositionUpdate2;
                case 74:
                    val = PacketType.SpawnPlayer;
                    break;
                case 10:
                    val = PacketType.PositionUpdate;
                    break;
                    // Ditto U_PositionUpdate;
                case 4:
                    val = PacketType.U_OrientUpdate;
                    break;
                case 2:
                    val = PacketType.DespawnPlayer;
                    break;
                case 66:
                    val = PacketType.Message;
                    break;
                case 65:
                    val = PacketType.Kick;
                    break;
                default:
                     val = PacketType.UNKNOWN;
                     break;
            }

            return val;
        }
    }

    public static class PacketLengthInfo
    {
        public static int Lookup(Packet.PacketType p)
        {
            string name = Enum.GetName(typeof(Packet.PacketType), p);
            return (int)Enum.Parse(typeof(Packet.PacketLength), name);
        }
    }

    public static class Color
    {
        public const string Black = "&0";
        public const string DarkBlue = "&1";
        public const string DarkGreen = "&2";
        public const string DarkTeal = "&3";
        public const string DarkRed = "&4";
        public const string Purple = "&5";
        public const string DarkYellow = "&6";
        public const string Gray = "&7";
        public const string DarkGray = "&8";
        public const string Blue = "&9";
        public const string Green = "&a";
        public const string Teal = "&b";
        public const string Red = "&c";
        public const string Pink = "&d";
        public const string Yellow = "&e";
        public const string White = "&f";
    }

    public enum Rank
    {
        Banned = -1,
        Guest,
        Builder,
        Mod,
        Admin
    }
}