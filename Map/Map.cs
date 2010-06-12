using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using System.Net;

namespace spacecraft
{
	[Serializable()]
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

		public short xdim { get; protected set; }
		public short ydim { get; protected set; }
		public short zdim { get; protected set; }
		public Position spawn { get; protected set; }
		public byte spawnHeading { get; protected set; }

		public Dictionary<string, string> meta = new Dictionary<string, string>();
		public Dictionary<string, Pair<Position, byte>> landmarks = new Dictionary<string, Pair<Position, byte>>();

        private Dictionary<BlockPosition, Block> PhysicsUpdates = new Dictionary<BlockPosition, Block>();
		private uint physicsCount;
		public bool PhysicsOn = true;
		private object PhysicsMutex = new object();

		public Map()
		{
			physicsCount = 0;
			//data = new byte[];
			xdim = 0;
			ydim = 0;
			zdim = 0;

			DefaultDepth = (short) Config.GetInt("Depth", 64);
			DefaultHeight = (short)Config.GetInt("Height", 64);
			DefaultWidth = (short)Config.GetInt("Width", 64);

			DefaultDepth = (short)Math.Max((short)0, Math.Min(DefaultDepth, (short)255));
			DefaultWidth = (short)Math.Max((short)0, Math.Min(DefaultWidth, (short)255));
			DefaultHeight = (short)Math.Max((short)0, Math.Min(DefaultHeight, (short)255));
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
			spawn = new Position((short)(16 * xdim), (short)(16 * ydim + 64), (short)(16 * zdim));
			// Spawn the player in the (approximate) center of the map. Each block is 32x32x32 pixels.
			data = new byte[Length];
			for (short x = 0; x < xdim; ++x) {
				for (short z = 0; z < zdim; ++z) {
					for (short y = 0; y < ydim / 2; ++y) {
						if (y == ydim / 2 - 1) {
							SetTile(x, y, z, Block.Grass);
						} else {
							SetTile(x, y, z, Block.Dirt);
						}
					}
				}
			}
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

		// ==== Simulation ====
        System.Diagnostics.Stopwatch Stopwatch = new System.Diagnostics.Stopwatch();
		public void Physics()
		{
			if (!PhysicsOn) return;
			// run twice per second
			physicsCount++;

            PhysicsUpdates.Clear();
            
			lock(PhysicsMutex) {
				short y_1 = (short)(ydim / 2 - 1);
				short y_2 = (short)(ydim / 2 - 2);
				short z2 = (short)(zdim - 1);
				for (short x = 0; x < xdim; ++x) {
					if(GetTile(x, y_1, 0) == Block.Air) {
						PhysicsUpdates.Add(new BlockPosition(x, y_1, 0), Block.Water);
					}
					if(GetTile(x, y_1, z2) == Block.Air) {
						PhysicsUpdates.Add(new PhysicsTask(x, y_1, z2, Block.Water));
					}
					if(GetTile(x, y_2, 0) == Block.Air) {
						PhysicsUpdates.Add(new PhysicsTask(x, y_2, 0, Block.Water));
					}
					if(GetTile(x, y_2, z2) == Block.Air) {
						PhysicsUpdates.Add(new PhysicsTask(x, y_2, z2, Block.Water));
					}
				}
				short x2 = (short)(xdim - 1);
				for (short z = 1; z < zdim - 1; ++z) {
					if(GetTile(0, y_1, z) == Block.Air) {
						PhysicsUpdates.Add(new PhysicsTask(0, y_1, z, Block.Water));
					}
					if(GetTile(x2, y_1, z) == Block.Air) {
						PhysicsUpdates.Add(new PhysicsTask(x2, y_1, z, Block.Water));
					}
					if(GetTile(0, y_2, z) == Block.Air) {
						PhysicsUpdates.Add(new PhysicsTask(0, y_2, z, Block.Water));
					}
					if(GetTile(x2, y_2, z) == Block.Air) {
						PhysicsUpdates.Add(new PhysicsTask(x2, y_2, z, Block.Water));
					}
				}

                Stopwatch.Reset();
                Stopwatch.Start();
				for (short x = 0; x < xdim; ++x)
				{
					for (short y = 0; y < ydim; ++y)
					{
						for (short z = 0; z < zdim; ++z)
						{
							Block tile = GetTile(x, y, z);
							if (physicsCount % 2 == 0)
							{
								// grass
								bool lit = true;
								for (short y2 = (short)(y + 1); y2 < ydim; ++y2)
								{
									if (BlockInfo.IsOpaque(GetTile(x, y2, z)))
									{
										lit = false;
										break;
									}
								}
								if (tile == Block.Dirt && lit && Spacecraft.random.NextDouble() < 0.2)
								{
									SetTile(x, y, z, Block.Grass);
								}
								if (tile == Block.Grass && !lit && Spacecraft.random.NextDouble() < 0.7)
								{
									SetTile(x, y, z, Block.Dirt);
								}
							}
							// water & lava
							if (tile == Block.Water || tile == Block.Lava)
							{
								if (tile != Block.Lava || physicsCount % 2 == 0)
								{
									Block under = GetTile(x, (short)(y - 1), z);
                                    // Commented out bit was for volumetric water.
									if (true)//!BlockInfo.IsFluid(under) && under != Block.Air)
									{
										if (GetTile((short)(x + 1), y, z) == Block.Air)
										{
											PhysicsUpdates.Add(new PhysicsTask((short)(x + 1), y, z, tile));
										}
										if (GetTile((short)(x - 1), y, z) == Block.Air)
										{
											PhysicsUpdates.Add(new PhysicsTask((short)(x - 1), y, z, tile));
										}
										if (GetTile(x, y, (short)(z + 1)) == Block.Air)
										{
											PhysicsUpdates.Add(new PhysicsTask(x, y, (short)(z + 1), tile));
										}
										if (GetTile(x, y, (short)(z - 1)) == Block.Air)
										{
											PhysicsUpdates.Add(new PhysicsTask(x, y, (short)(z - 1), tile));
										}
									}
									if (GetTile(x, (short)(y - 1), z) == Block.Air)
									{
										PhysicsUpdates.Add(new PhysicsTask(x, (short)(y - 1), z, tile));
									}
								}
							}
							// sponges
							if (tile == Block.Sponge)
							{
								for (short diffX = -2; diffX <= 2; diffX++)
								{
									for (short diffY = -2; diffY <= 2; diffY++)
									{
										for (short diffZ = -2; diffZ <= 2; diffZ++)
										{
                                            /*if (BlockInfo.IsFluid(GetTile((short)(x + diffX), (short)(y + diffY), (short)(z + diffZ))) 
                                                || GetTile((short)(x + diffX), (short)(y + diffY), (short)(z + diffZ)) == Block.Air )
                                            {*/
                                              //  PhysicsUpdates.Add(new BlockPosition((short)(x + diffX), (short)(y + diffY), (short)(z + diffZ)), Block.Air);
                                            //}

                                            BlockPosition current = new BlockPosition((short)(x + diffX), (short)(y + diffY), (short)(z + diffZ));

                                            if (PhysicsUpdates.ContainsKey(current) && BlockInfo.IsFluid((PhysicsUpdates[current])))
                                            {
                                                PhysicsUpdates[current] = Block.Air;
                                            }
										}
									}
								}
							}
							// sand and gravel
							if (tile == Block.Sand || tile == Block.Gravel)
							{
								short lowY = y;
								if(GetTile(x, (short)(lowY - 1), z) == Block.Air || BlockInfo.IsFluid(GetTile(x, (short)(lowY - 1), z))) {
									--lowY;
								}
								if(lowY != y) {
									PhysicsUpdates.Add(new PhysicsTask(x, y, z, Block.Air));
									PhysicsUpdates.Add(new PhysicsTask(x, lowY, z, tile));
								}
							}
						} // z
					} // y
				} // x
                
				int flUpdates = 0;
                PhysicsUpdates.Reverse();
				foreach (PhysicsTask task in PhysicsUpdates) 
                {
                    // If it isn't the wanted tile already, and the tile hasn't changed since we made the list entry...
                    if (GetTile(task.x, task.y, task.z) == task.From)
                    {
                        // Update it.
                        SetTile(task.x, task.y, task.z, task.To);
                        if (++flUpdates >= MaxPhysicsPerTick) 
                            break; // ONOS! Too much physics. Abort!
                    }
				}
			} // lock(physicsMutex)
            Stopwatch.Stop();
            Spacecraft.Debug("Tasks this step: {0}", PhysicsUpdates.Count);
            Spacecraft.Debug("Time taken this step: {0}", Stopwatch.ElapsedMilliseconds);
		}

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
			if (x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) return;
			data[BlockIndex(x, y, z)] = (byte)tile;
			if(BlockChange != null)
				BlockChange(this, new BlockPosition(x, y, z), tile);
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