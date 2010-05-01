using System;
using System.Collections;
using System.Collections.Generic;

public class Player
{
    public static ArrayList ids = new ArrayList();
    
    /// <summary>
    /// A series of lists containing all players of the given rank.
    /// </summary>
    public static Dictionary<Rank, List<string>> RankedPlayers = new Dictionary<Rank, List<string>>();
    
    
    public Rank rank;
    public byte pid;
    public string name;
    public Int16 x, y, z;
    public byte heading;
    public byte pitch;
    public bool placing;
    public byte placeType;
    
    public Player(string username)
    {
        placing = false;
        placeType = Block.Books;
        x = 128; y = 128; z = 128;
        pid = 255;
        name = username;
        for(byte i = 0; i < 100; ++i) {
            if(!ids.Contains(i)) {
                ids.Add(i);
                pid = i;
                break;
            }
        }

		rank = Rank.Guest;
        foreach (Rank key in RankedPlayers.Keys) {
            if (RankedPlayers[key].Contains(username)) {
                rank = key;
                break;
            }
        }
    }
    
    ~Player()
    {
        ids.Remove(pid);
    }
    
    public bool PositionUpdate(Int16 X, Int16 Y, Int16 Z, byte Heading, byte Pitch)
    {
        bool r = !(x == X && y == Y && z == Z && heading == Heading && pitch == Pitch);
        x = X; y = Y; z = Z;
        heading = Heading;
        pitch = Pitch;
        return r;
    }
	
    // static stuff
	
	public static string RankColor(Rank rank)
	{
		switch(rank) {
			case Rank.Banned: return Color.Red;
			case Rank.Guest: return Color.White;
			case Rank.Builder: return Color.Green;
			case Rank.Mod: return Color.Yellow;
			case Rank.Admin: return Color.Blue;
		}
		return Color.Teal;
	}
}
