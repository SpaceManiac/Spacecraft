using System;
using System.IO;

public class Map
{
	private byte[] _data;
	public short xdim, ydim, zdim;
	public short xspawn, yspawn, zspawn;
	public byte headingspawn, pitchspawn;
	private uint physicsCount;
	
	public Map()
	{
		physicsCount = 0;
		_data = null; xdim = 0; ydim = 0; zdim = 0;
	}
	
	public void Generate()
	{
		physicsCount = 0;
		Spacecraft.Log("Generating map...");
		
		xdim = 32; ydim = 32; zdim = 32;
		xspawn = 128; yspawn = 128; zspawn = 128;
		_data = new byte[xdim * ydim * zdim];
		for(short x = 0; x < xdim; ++x) {
			for(short y = 0; y < ydim; ++y) {
				SetTile(x, y, 0, Block.Wood);
			}
		}
		for(short x = 0; x < xdim; ++x) {
			for(short z = 0; z < zdim; ++z) {
				SetTile(x, 0, z, Block.Brick);
			}
		}
		for(short y = 0; y < ydim; ++y) {
			for(short z = 0; z < zdim; ++z) {
				SetTile(0, y, z, Block.Rock);
			}
		}
	}
	
	public void Load(string filename)
	{
		physicsCount = 0;
		Spacecraft.Log("Loading " + filename + "...");
		byte[] bytes = File.ReadAllBytes(filename);
		xdim = BitConverter.ToInt16(bytes, 0);
		ydim = BitConverter.ToInt16(bytes, 2);
		zdim = BitConverter.ToInt16(bytes, 4);
		xspawn = BitConverter.ToInt16(bytes, 6);
		yspawn = BitConverter.ToInt16(bytes, 8);
		zspawn = BitConverter.ToInt16(bytes, 10);
		headingspawn = bytes[12];
		pitchspawn = bytes[13];
		// total header length: 32 bytes
		_data = new byte[Length];
		Array.Copy(bytes, 32, _data, 0, _data.Length);
	}
	
	public void Save(string filename)
	{
		if(filename != "level.dat") Spacecraft.Log("Saving " + filename + "...");
		FileStream o = File.OpenWrite(filename);
		byte[] header = new byte[32];
		Array.Copy(BitConverter.GetBytes(xdim), 0, header, 0, 2);
		Array.Copy(BitConverter.GetBytes(ydim), 0, header, 2, 2);
		Array.Copy(BitConverter.GetBytes(zdim), 0, header, 4, 2);
		Array.Copy(BitConverter.GetBytes(xspawn), 0, header, 6, 2);
		Array.Copy(BitConverter.GetBytes(yspawn), 0, header, 8, 2);
		Array.Copy(BitConverter.GetBytes(zspawn), 0, header, 10, 2);
		header[12] = headingspawn;
		header[13] = pitchspawn;
		o.Write(header, 0, header.Length);
		o.Write(_data, 0, _data.Length);
		o.Close();
	}
	
	public void Physics(Server srv)
	{
		// run twice per second
		physicsCount++;
		for(short x = 0; x < xdim; ++x) {
			for(short y = 0; y < ydim; ++y) {
		 		for(short z = 0; z < zdim; ++z) {
					byte tile = GetTile(x, y, z);
					if(physicsCount % 10 == 0) {
						// grass
						bool lit = true;
						for(short y2 = (short)(y + 1); y2 < ydim; ++y2) {
							if(Block.IsSolid(GetTile(x, y2, z))) {
								lit = false;
								break;
							}
						}
						if(tile == Block.Dirt && lit && Server.rnd.NextDouble() < 0.2) {
							SetSend(srv, x, y, z, Block.Grass);
						}
						if(tile == Block.Grass && !lit && Server.rnd.NextDouble() < 0.7) {
							SetSend(srv, x, y, z, Block.Dirt);
						}
					}
					if(physicsCount % 2 == 0) {
						if(tile == Block.Water || tile == Block.Lava) {
							for(short diffX = -1; diffX <= 1; diffX++) {
								for(short diffZ = -1; diffZ <= 1; diffZ++) {
									for(short diffY = -1; diffY <= 0; diffY++) {
										if (GetTile((short)(x + diffX), (short)(y + diffY), (short)(z + diffZ)) == Block.Air) {
											SetSend(srv, (short)(x + diffX), (short)(y + diffY), (short)(z + diffZ), tile);
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}
	
	public void SetSend(Server srv, short x, short y, short z, byte tile)
	{
		SetTile(x, y, z, tile);
		srv.SendAll(Connection.PacketSetBlock(x, y, z, tile));
	}
	
	public int BlockIndex(short x, short y, short z)
	{
		return ((y * zdim + z) * xdim + x);
	}
	
	public void SetTile(short x, short y, short z, byte tile)
	{
		if(x >= xdim || y >= ydim || z >= zdim) return;
		_data[BlockIndex(x, y, z)] = tile;
	}
	
	public byte GetTile(short x, short y, short z)
	{
		if(x >= xdim || y >= ydim || z >= zdim) return Block.Adminium;
		return _data[BlockIndex(x, y, z)];
	}
	
	public byte[] data { get { return _data; } }
	public int Length { get { return xdim * ydim * zdim; } }
}
