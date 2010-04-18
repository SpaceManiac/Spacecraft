using System;
using System.IO;
using System.Collections;

public class Map
{
	private byte[] _data;
	public short xdim, ydim, zdim;
	public short xspawn, yspawn, zspawn;
	public byte headingspawn, pitchspawn;
	private uint physicsCount;
	private bool physicsSuspended = false;
	
	public Map()
	{
		physicsCount = 0;
		_data = null; xdim = 0; ydim = 0; zdim = 0;
	}
	
	public void Generate()
	{
		physicsCount = 0;
		Spacecraft.Log("Generating map...");
		
		xdim = 64;
		ydim = 32;
		zdim = 64;
		xspawn = (short)(16*xdim);  // center spawn
		yspawn = (short)(16*ydim);
		zspawn = (short)(16*zdim);
		_data = new byte[xdim * ydim * zdim];
		for(short x = 0; x < xdim; ++x) {
			for(short z = 0; z < zdim; ++z) {
				for(short y = 0; y < ydim/2; ++y) {
					SetTile(x, y, z, Block.Dirt);
				}
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
		if(physicsSuspended) return;
		// run twice per second
		physicsCount++;
		
		ArrayList FluidList = new ArrayList();
		ArrayList SpongeList = new ArrayList();
		
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
						// water & lava
						if(tile == Block.Water || tile == Block.Lava) {
							if (GetTile((short)(x + 1), y, z) == Block.Air) {
								FluidList.Add(new PositionBlock((short)(x + 1), y, z, tile));
							}
							if (GetTile((short)(x - 1), y, z) == Block.Air) {
								FluidList.Add(new PositionBlock((short)(x - 1), y, z, tile));
							}
							if (GetTile(x, (short)(y - 1), z) == Block.Air) {
								FluidList.Add(new PositionBlock(x, (short)(y - 1), z, tile));
							}
							if (GetTile(x, y, (short)(z + 1)) == Block.Air) {
								FluidList.Add(new PositionBlock(x, y, (short)(z + 1), tile));
							}
							if (GetTile(x, y, (short)(z - 1)) == Block.Air) {
								FluidList.Add(new PositionBlock(x, y, (short)(z - 1), tile));
							}
						}
					}
					// sponges
					if(tile == Block.Sponge) {
						for(short diffX = -2; diffX <= 2; diffX++) {
							for(short diffY = -2; diffY <= 2; diffY++) {
								for(short diffZ = -2; diffZ <= 2; diffZ++) {
									byte t2 = GetTile((short)(x + diffX), (short)(y + diffY), (short)(z + diffZ));
									if(t2 == Block.Water || t2 == Block.Lava) {
										SpongeList.Add(new PositionBlock((short)(x + diffX), (short)(y + diffY), (short)(z + diffZ), Block.Air));
									}
								}
							}
						}
					}
				}
			}
		}
		
		foreach(PositionBlock task in FluidList) {
			SetSend(srv, task.x, task.y, task.z, task.tile);
		}
		foreach(PositionBlock task in SpongeList) {
			SetSend(srv, task.x, task.y, task.z, task.tile);
		}
	}
	
	public void Dehydrate(Server srv)
	{
		//physicsSuspended = true;
		for(short x = 0; x < xdim; ++x) {
			for(short y = 0; y < ydim; ++y) {
		 		for(short z = 0; z < zdim; ++z) {
					if(GetTile(x,y,z) == Block.Water || GetTile(x,y,z) == Block.Lava) {
						SetTile(srv, x, y, z, Block.Air);
					}
				}
			}
		}
		SetSend(0,0,0,GetTile(0,0,0))
		//physicsSuspended = false;
	}
	
	public void SetSend(Server srv, short x, short y, short z, byte tile)
	{
		if(x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) return;
		SetTile(x, y, z, tile);
		srv.SendAll(Connection.PacketSetBlock(x, y, z, tile));
	}
	
	public int BlockIndex(short x, short y, short z)
	{
		return ((y * zdim + z) * xdim + x);
	}
	
	public void SetTile(short x, short y, short z, byte tile)
	{
		if(x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) return;
		_data[BlockIndex(x, y, z)] = tile;
	}
	
	public byte GetTile(short x, short y, short z)
	{
		if(x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) return Block.Adminium;
		return _data[BlockIndex(x, y, z)];
	}
	
	public byte[] data { get { return _data; } }
	public int Length { get { return xdim * ydim * zdim; } }
}
