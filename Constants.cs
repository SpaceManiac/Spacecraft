using System;
using System.Collections;
using System.Collections.Generic;

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
    I_Torch            = 0x32,
    I_Fire            = 0x33,
    I_InfWater        = 0x34
}

public static class BlockInfo
{
    public static Dictionary<string, Block> names;
	
	public static bool IsFluid(Block block) {
		return (block == Block.Water || block == Block.StillWater
		        || block == Block.Lava || block == Block.StillLava);
	}
	public static bool IsDecoration(Block block) {
		return (block == Block.YellowFlower || block == Block.RedFlower ||
		        block == Block.BrownMushroom || block == Block.RedMushroom ||
		        block == Block.Sapling);
	}
    
	public static bool IsTransparent(Block block) {
		return (IsDecoration(block) || block == Block.Glass || block == Block.Leaves || block == Block.Air);
	}
	public static bool IsOpaque(Block block) {
		return !IsTransparent(block);
	}
	
	public static bool IsSolid(Block block) {
        return (block != Block.Air && !IsFluid(block) && !IsDecoration(block));
    }
    
    static BlockInfo() {
        names = new Dictionary<string, Block>();
		
        foreach(string block in Enum.GetNames(typeof(Block))) {
            names.Add(block.ToLower(), (Block) Enum.Parse(typeof(Block), block));
        }
		
        names["none"] = Block.Air;
        names["nothing"] = Block.Air;
        names["empty"] = Block.Air;
        names["soil"] = Block.Dirt;
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

public class Packet
{
    public const byte Ident                = 0x00; // client & server
    public const byte Ping                 = 0x01; // server
    public const byte LevelInit         = 0x02; // server
    public const byte LevelChunk         = 0x03; // server
    public const byte LevelFinish         = 0x04; // server
    public const byte PlayerSetBlock     = 0x05; // client
    public const byte ServerSetBlock     = 0x06; // server
    public const byte SpawnPlayer         = 0x07; // server
    public const byte PositionUpdate     = 0x08; // client & server
    public const byte U_PositionUpdate    = 0x09; // unused server
    public const byte U_PositionUpdate2 = 0x0a; // unused server
    public const byte U_OrientUpdate    = 0x0b; // unused server
    public const byte DespawnPlayer        = 0x0c; // server
    public const byte Message            = 0x0d; // client & server
    public const byte Kick                = 0x0e; // server
}

public class PacketLen
{
    public const short Ident                = 131; // client & server
    public const short Ping                 = 1; // server
    public const short LevelInit         = 1; // server
    public const short LevelChunk         = 1028; // server
    public const short LevelFinish         = 7; // server
    public const short PlayerSetBlock     = 9; // client
    public const short ServerSetBlock     = 8; // server
    public const short SpawnPlayer         = 74; // server
    public const short PositionUpdate     = 10; // client & server
    public const short U_PositionUpdate    = 10; // unused server
    public const short U_PositionUpdate2 = 8; // unused server
    public const short U_OrientUpdate    = 4; // unused server
    public const short DespawnPlayer        = 2; // server
    public const short Message            = 66; // client & server
    public const short Kick                = 65; // server
    
    public static short Lookup(byte p) {
        switch(p) {
            case Packet.Ident: return Ident;
            case Packet.Ping: return Ping;
            case Packet.LevelInit: return LevelInit;
            case Packet.LevelChunk: return LevelChunk;
            case Packet.LevelFinish: return LevelFinish;
            case Packet.PlayerSetBlock: return PlayerSetBlock;
            case Packet.ServerSetBlock: return ServerSetBlock;
            case Packet.SpawnPlayer: return SpawnPlayer;
            case Packet.PositionUpdate: return PositionUpdate;
            case Packet.U_PositionUpdate: return U_PositionUpdate;
            case Packet.U_PositionUpdate2: return U_PositionUpdate2;
            case Packet.U_OrientUpdate: return U_OrientUpdate;
            case Packet.DespawnPlayer: return DespawnPlayer;
            case Packet.Message: return Message;
            case Packet.Kick: return Kick;
        }
        return 0;
    }
}

public class Color
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

public enum Rank {
	Banned = -1,
	Guest,
	Builder,
	Mod,
	Admin
}
