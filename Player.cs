using System;
using System.Collections;

public class Player
{
	public byte pid;
	public string name;
	public Int16 x, y, z;
	public byte heading;
	public byte pitch;
	public static ArrayList ids = new ArrayList();
	
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
				return;
			}
		}
	}
	
	~Player()
	{
		ids.Remove(pid);
	}
	
	public static bool IsAdmin(string name)
	{
		return (name == "SpaceManiac");
	}
	
	public static bool IsModPlus(string name)
	{
		return (name == "Blocky" || IsAdmin(name));
	}
	
	public bool PositionUpdate(Int16 X, Int16 Y, Int16 Z, byte Heading, byte Pitch)
	{
		bool r = !(x == X && y == Y && z == Z && heading == Heading && pitch == Pitch);
		x = X; y = Y; z = Z;
		heading = Heading;
		pitch = Pitch;
		return r;
	}
}
