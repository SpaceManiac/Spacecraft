
using System;
using System.Collections;

public enum Block2
{
    Air = 0x00,
    Rock = 0x01,
    Grass = 0x02,
}

public class Block
{    
    public const byte Air                 = 0x00;
    public const byte Rock                 = 0x01;
    public const byte Grass             = 0x02;
    public const byte Dirt                 = 0x03;
    public const byte Cobblestone        = 0x04;
    public const byte Wood                 = 0x05;
    public const byte Sapling            = 0x06;
    public const byte Adminium            = 0x07;
    public const byte Water             = 0x08;
    public const byte StillWater         = 0x09;
    public const byte Lava                 = 0x0A;
    public const byte StillLava         = 0x0B;
    public const byte Sand                 = 0x0C;
    public const byte Gravel             = 0x0D;
    public const byte GoldOre             = 0x0E;
    public const byte IronOre             = 0x0F;
    public const byte CoalOre            = 0x10;
    public const byte TreeTrunk         = 0x11;
    public const byte Leaves             = 0x12;
    public const byte Sponge             = 0x13;
    public const byte Glass             = 0x14;
    public const byte RedCloth             = 0x15;
    public const byte OrangeCloth         = 0x16;
    public const byte YellowCloth         = 0x17;
    public const byte LightGreenCloth     = 0x18;
    public const byte GreenCloth         = 0x19;
    public const byte AGreenCloth         = 0x1A;
    public const byte CyanCloth         = 0x1B;
    public const byte BlueCloth         = 0x1C;
    public const byte PurpleCloth         = 0x1D;
    public const byte IndigoCloth         = 0x1E;
    public const byte VioletCloth         = 0x1F;
    public const byte MagentaCloth         = 0x20;
    public const byte PinkCloth         = 0x21;
    public const byte BlackCloth         = 0x22;
    public const byte GrayCloth         = 0x23;
    public const byte WhiteCloth         = 0x24;
    public const byte YellowFlower         = 0x25;
    public const byte RedFlower         = 0x26;
    public const byte BrownMushroom        = 0x27;
    public const byte RedMushroom         = 0x28;
    public const byte Gold                 = 0x29;
    public const byte Iron                 = 0x2A;
    public const byte DoubleStair         = 0x2B;
    public const byte Stair             = 0x2C;
    public const byte Brick             = 0x2D;
    public const byte TNT                 = 0x2E;
    public const byte Books             = 0x2F;
    public const byte MossyCobble         = 0x30;
    public const byte Obsidian             = 0x31;
    
    public static Hashtable Names;
	
	public static bool IsFluid(byte block) {
		return (block == Water || block == StillWater || block == Lava || block == StillLava);
	}
	public static bool IsDecoration(byte block) {
		return (block == YellowFlower || block == RedFlower || block == BrownMushroom || block == RedMushroom);
	}
    
	public static bool IsTransparent(byte block) {
		return (IsDecoration(block) || block == Glass || block == Leaves || block == Air);
	}
	public static bool IsOpaque(byte block) {
		return !IsTransparent(block);
	}
	
	public static bool IsSolid(byte block) {
        return (block != Air && !IsFluid(block) && !IsDecoration(block));
    }
    
    public static void MakeNames() {
        Names = new Hashtable();
        Names["2stair"] = DoubleStair;
        Names["obsidian"] = Obsidian;
        Names["adminium"] = Adminium;
        Names["water"] = Water;
        Names["lava"] = Lava;
    }
    
    // indev only
    public const byte I_Torch            = 0x32;
    public const byte I_Fire            = 0x33;
    public const byte I_InfWater        = 0x34;
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