using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using System.Net;

namespace spacecraft
{
	public partial class Map
	{
		// continued in MapIO.cs

		public delegate void BlockChangeHandler(Map map, BlockPosition pos, Block type);
		/// <summary>
		/// Triggred when a block is changed by map processes
		/// </summary>
		public event BlockChangeHandler BlockChange;

		public const uint levelFormatID = 0xFC000002;
	/// <summary>
	/// Max changse to the map per physics tick.
	/// </summary>
	public const int MaxPhysicsPerTick = 1000; 

		private static short DefaultHeight, DefaultWidth, DefaultDepth;
		public byte[] data { get; protected set; }
		public int Length { get { return xdim * ydim * zdim; } }

		public short xdim;
		public short ydim;
		public short zdim;
		public Position spawn;
		public byte spawnHeading;

		public Dictionary<string, string> meta = new Dictionary<string, string>();
		public Dictionary<string, Pair<Position, byte>> landmarks = new Dictionary<string, Pair<Position, byte>>();
	
		public Map()
		{
			physicsCount = 0;
			//data = new byte[];
			xdim = 0;
			ydim = 0;
			zdim = 0;

			DefaultDepth = (short)Config.GetInt("depth", 64);
			DefaultHeight = (short)Config.GetInt("height", 64);
			DefaultWidth = (short)Config.GetInt("width", 64);
			
			if(!IsValidDimension(DefaultDepth) || !IsValidDimension(DefaultHeight) || !IsValidDimension(DefaultWidth)) {
				Spacecraft.Log("Specified default map dimensions are invalid, defaulting to 256x256x256");
				DefaultDepth = DefaultHeight = DefaultWidth = 256;
			}

			DefaultDepth = (short)Math.Max((short)0, Math.Min(DefaultDepth, (short)2048));
			DefaultWidth = (short)Math.Max((short)0, Math.Min(DefaultWidth, (short)2048));
			DefaultHeight = (short)Math.Max((short)0, Math.Min(DefaultHeight, (short)2048));
		}

		public string[] GetLandmarkList()
		{
			List<string> l = new List<string>();
			foreach (KeyValuePair<string, Pair<Position, byte>> pair in landmarks)
			{
				l.Add(pair.Key);
			}
			return l.ToArray();
		}

		public void SetSpawn(Position p, byte heading)
		{
			spawn = p;
			spawnHeading = heading;
		}

		public void Generate()
		{
			physicsCount = 0;
			Spacecraft.Log("Generating map...");

			xdim = DefaultWidth;
			ydim = DefaultHeight;
			zdim = DefaultDepth;
			spawn = new Position((short)(16 * xdim), (short)(16 * ydim + 48), (short)(16 * zdim));
			// Spawn the player in the (approximate) center of the map. Each block is 32x32x32 pixels.
			data = new byte[xdim * ydim * zdim];
			for (short x = 0; x < xdim; ++x) {
	if(x == (short)(xdim / 2)) {
		Spacecraft.Log("Generation 50% complete");
	}
				for (short z = 0; z < zdim; ++z) {
					for (short y = 0; y < ydim / 2; ++y) {
						if (y == ydim / 2 - 1) {
							SetTile(x, y, z, Block.Grass, true);
						} else {
							SetTile(x, y, z, Block.Dirt, true);
						}
					}
				}
			}
			Spacecraft.Log("Generation complete");
		}

		// zips a copy of the block array
		public void GetCompressedCopy(Stream stream, bool prependBlockCount)
		{
			using (GZipStream compressor = new GZipStream(stream, CompressionMode.Compress)) {
				if (prependBlockCount) {
					// convert block count to big-endian
					int convertedBlockCount = IPAddress.HostToNetworkOrder(data.Length);
					// write block count to gzip stream
					compressor.Write(BitConverter.GetBytes(convertedBlockCount), 0, sizeof(int));
				}
				compressor.Write(data, 0, data.Length);
			}
		}

	public void CopyBlocks(byte[] source, int offset)
	{
	data = new byte[xdim * ydim * zdim];
	Array.Copy(source, offset, data, 0, data.Length);
	}

	public bool ValidateBlockTypes()
	{
	for (int i = 0; i < data.Length; ++i)
	{
	if (data[i] > (byte)Block.Maximum)
	{
	return false;
	}
	}
	return true;
	}

		// ==== Simulation ====
	/*System.Diagnostics.Stopwatch Stopwatch = new System.Diagnostics.Stopwatch();
		public void DoPhysics()
		{
			if (!PhysicsOn) return;
			// run twice per second
			physicsCount++;
	Stopwatch.Reset();
	Stopwatch.Start();

	PhysicsUpdates.Clear();
	
			lock(PhysicsMutex) {
	lock (PhysicsBlocks)
	{
	List<int> RemovedItems = new List<int>();

	for (int i = 0; i < PhysicsBlocks.Count; i++)
	{
	var key = PhysicsBlocks[i];
	Block Tile = GetTile(key);
	if (!BlockInfo.RequiresPhysics(GetTile(key)))
	{
	RemovedItems.Add(i);
	}
	else
	{
	switch (Tile)
	{
	case Block.Water:
	for (int xDiff = -1; xDiff <= 1; xDiff++)
	{
	for (int yDiff = -1; yDiff <= 0; yDiff++)
	{
	for (int zDiff = -1; zDiff <= 1; zDiff++)
	{
	if (Math.Abs(xDiff + yDiff + zDiff) == 1)
	{
	short newX = (short)(xDiff + key.xDiff);
	short newY = (short)(yDiff + key.yDiff);
	short newZ = (short)(zDiff + key.zDiff);

	if (GetTile(newX, newY, newZ) == Block.Lava)
	{
	AddPhysicsUpdate(new BlockPosition(newX, newY, newZ), Block.Rock);
	}
	else if (GetTile(newX, newY, newZ) == Block.Water)
	{
	}
	else if (!BlockInfo.IsSolid(GetTile(newX, newY, newZ)))
	{
	AddPhysicsUpdate(new BlockPosition(newX, newY, newZ), Block.Water);
	}
	}
	}
	}
	}
	break;

	case Block.Lava:
	for (int xDiff = -1; xDiff <= 1; xDiff++)
	{
	for (int yDiff = -1; yDiff <= 0; yDiff++)
	{
	for (int zDiff = -1; zDiff <= 1; zDiff++)
	{
	short newX = (short)(xDiff + key.xDiff);
	short newY = (short)(yDiff + key.yDiff);
	short newZ = (short)(zDiff + key.zDiff);

	if (GetTile(newX, newY, newZ) == Block.Water)
	{
	AddPhysicsUpdate(new BlockPosition(newX, newY, newZ), Block.Rock);
	}
	else if (GetTile(newX, newY, newZ) == Block.Lava)
	{
	}
	else if (!BlockInfo.IsSolid(GetTile(newX, newY, newZ)))
	{
	AddPhysicsUpdate(new BlockPosition(newX, newY, newZ), Block.Lava);
	}
	}
	}
	}
	break;

	case Block.Sand: // If the tile immediatly under the sand is not solid, move the sand down one.
	if (!BlockInfo.IsSolid(GetTile(key.xDiff, (short)(key.yDiff - 1), key.zDiff)))
	{
	AddPhysicsUpdate(new BlockPosition(key.xDiff, key.yDiff, key.zDiff), Block.Air);
	AddPhysicsUpdate(new BlockPosition(key.xDiff, (short)(key.yDiff - 1), key.zDiff), Block.Sand);
	}
	break;

	case Block.Sponge: // For each of the tiles within sponge radius, remove any fluid.
	for (int xDiff = -BlockInfo.SpongeRadius; xDiff <= BlockInfo.SpongeRadius; xDiff++)
	{
	for (int yDiff = -BlockInfo.SpongeRadius; yDiff <= BlockInfo.SpongeRadius; yDiff++)
	{
	for (int zDiff = -BlockInfo.SpongeRadius; zDiff <= BlockInfo.SpongeRadius; zDiff++)
	{
	short newX = (short)(xDiff + key.xDiff);
	short newY = (short)(yDiff + key.yDiff);
	short newZ = (short)(zDiff + key.zDiff);
	BlockPosition pos = new BlockPosition(newX, newY, newZ);

	if (BlockInfo.IsFluid(GetTile(newX, newY, newZ)) || (PhysicsUpdates.ContainsKey(pos)) && BlockInfo.IsFluid(PhysicsUpdates[pos]))
	{
	AddPhysicsUpdate(new BlockPosition(newX, newY, newZ), Block.Air);
	}
	}
	}
	}
	break;

	case Block.Grass:
	if (BlockInfo.IsSolid(GetTile(key.xDiff, (short)(key.yDiff + 1), key.zDiff)))
	{
	AddPhysicsUpdate(new BlockPosition(key.xDiff, key.yDiff, key.zDiff), Block.Dirt);
	}
	break;
	case Block.Dirt:
	if (!BlockInfo.IsSolid(GetTile(key.xDiff, (short)(key.yDiff + 1), key.zDiff)))
	{
	AddPhysicsUpdate(new BlockPosition(key.xDiff, key.yDiff, key.zDiff), Block.Grass);
	}
	break;

	default:
	break;
	}
	}
	}
	for (int i = 0; i < RemovedItems.Count; i++)
	{
	PhysicsBlocks.RemoveAt(RemovedItems[i]);
	}
	}
	int flUpdates = 0;
	foreach (KeyValuePair<BlockPosition, Block> KV in PhysicsUpdates)
	{
	// If it isn't the wanted tile already, and the tile hasn't changed since we made the list entry...                        // Update it.
	SetTile(KV.Key.xDiff, KV.Key.yDiff, KV.Key.zDiff, KV.Value);
	if (++flUpdates >= MaxPhysicsPerTick)
	break; // ONOS! Too much physics. Abort!
	}
	}
			 // lock(physicsMutex)
	Stopwatch.Stop();
	Spacecraft.Debug("Tasks this step: {0}", PhysicsUpdates.Count);
	Spacecraft.Debug("Time taken this step: {0}", Stopwatch.ElapsedMilliseconds);
		}

	void AddPhysicsUpdate(BlockPosition pos, Block tile)
	{
	if (PhysicsUpdates.ContainsKey(pos))
	PhysicsUpdates.Remove(pos);
	PhysicsUpdates.Add(pos, tile);
	}*/

	public void ReplaceAll(Block From, Block To, int max)
		{
			lock(PhysicsMutex) {
				int total = 0;
				for (short x = 0; x < xdim; x++) {
					for (short y = 0; y < ydim; y++) {
						for (short z = 0; z < zdim; z++) {
							if (GetTile(x, y, z) == From) {
								SetTile(x, y, z, To);
								if(++total >= max) return;
							}
						}
					}
				}
			} // lock(PhysicsMutex)
		}

		public void Dehydrate()
		{
			int max = xdim * ydim * zdim;
			ReplaceAll(Block.Water, Block.Air, max);
			ReplaceAll(Block.Lava, Block.Air, max);
			ReplaceAll(Block.StillWater, Block.Air, max);
			ReplaceAll(Block.StillLava, Block.Air, max);
		}

	public void SetTile(short x, short y, short z, Block tile)
	{
	SetTile(x, y, z, tile, false);
	}

	System.Diagnostics.Stopwatch Stop = new System.Diagnostics.Stopwatch();

    public void SetTile(short x, short y, short z, Block tile, bool overide)
    {
        Stop.Reset();
        Stop.Start();
        if (!overide) // Override should be used to override all physics checking. 
        {
            Block previous = GetTile(x,y,z);

            if (BlockInfo.RequiresPhysics(tile))
            {
                lock (ActiveBlocks)
                {
                    ActiveBlocks.Add(new BlockPosition(x, y, z));
                }
            }
        }

        if (x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) return;
        data[BlockIndex(x, y, z)] = (byte)tile;


        if (BlockChange != null)
            BlockChange(this, new BlockPosition(x, y, z), tile);

        Stop.Stop();
        if (!overide)
        {
            System.Diagnostics.Debug.WriteLine("SetTile took " + Stop.ElapsedMilliseconds + "ms");
        }
    }

	public Block GetTile(BlockPosition pos)
	{
	return GetTile(pos.x, pos.y, pos.z);
	}

		public Block GetTile(short x, short y, short z)
		{
			if (x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) return Block.Adminium;
			return (Block)data[BlockIndex(x, y, z)];
		}

		public int BlockIndex(short x, short y, short z)
		{
			return ((y * zdim + z) * xdim + x);
		}

		public static int BlockIndex(short x, short y, short z, short xdim, short zdim)
		{
			return ((y * zdim + z) * xdim + x);
		}
	}
}