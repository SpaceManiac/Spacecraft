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

        private Dictionary<BlockPosition, Block> PhysicsUpdates = new Dictionary<BlockPosition, Block>();
        private List<PhysicsTask> PhysicsBlocks = new List<PhysicsTask>();

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

			DefaultDepth = (short)Config.GetInt("depth", 64);
			DefaultHeight = (short)Config.GetInt("height", 64);
			DefaultWidth = (short)Config.GetInt("width", 64);

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
                Spacecraft.Log("X:" + x + " / " + xdim);
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
        System.Diagnostics.Stopwatch Stopwatch = new System.Diagnostics.Stopwatch();
		public void Physics()
		{
			if (!PhysicsOn) return;
			// run twice per second
			physicsCount++;

            PhysicsUpdates.Clear();
            
			lock(PhysicsMutex) {
			
                /*short y_1 = (short)(ydim / 2 - 1);
				short y_2 = (short)(ydim / 2 - 2);
				short z2 = (short)(zdim - 1);
				for (short x = 0; x < xdim; ++x) {
					if(GetTile(x, y_1, 0) == Block.Air) {
						AddPhysicsUpdate(new BlockPosition(x, y_1, 0), Block.Water);
					}
					if(GetTile(x, y_1, z2) == Block.Air) {
						AddPhysicsUpdate(new BlockPosition(x, y_1, z2), Block.Water);
					}
					if(GetTile(x, y_2, 0) == Block.Air) {
						AddPhysicsUpdate(new BlockPosition(x, y_2, 0), Block.Water);
					}
					if(GetTile(x, y_2, z2) == Block.Air) {
						AddPhysicsUpdate(new BlockPosition(x, y_2, z2), Block.Water);
					}
				}
				short x2 = (short)(xdim - 1);
				for (short z = 1; z < zdim - 1; ++z) {
					if(GetTile(0, y_1, z) == Block.Air) {
						AddPhysicsUpdate(new BlockPosition(0, y_1, z), Block.Water);
					}
					if(GetTile(x2, y_1, z) == Block.Air) {
						AddPhysicsUpdate(new BlockPosition(x2, y_1, z), Block.Water);
					}
					if(GetTile(0, y_2, z) == Block.Air) {
						AddPhysicsUpdate(new BlockPosition(0, y_2, z), Block.Water);
					}
					if(GetTile(x2, y_2, z) == Block.Air) {
						AddPhysicsUpdate(new BlockPosition(x2, y_2, z), Block.Water);
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
											AddPhysicsUpdate(new BlockPosition((short)(x + 1), y, z), tile);
										}
										if (GetTile((short)(x - 1), y, z) == Block.Air)
										{
											AddPhysicsUpdate(new BlockPosition((short)(x - 1), y, z), tile);
										}
										if (GetTile(x, y, (short)(z + 1)) == Block.Air)
										{
											AddPhysicsUpdate(new BlockPosition(x, y, (short)(z + 1)), tile);
										}
										if (GetTile(x, y, (short)(z - 1)) == Block.Air)
										{
											AddPhysicsUpdate(new BlockPosition(x, y, (short)(z - 1)), tile);
										}
									}
									if (GetTile(x, (short)(y - 1), z) == Block.Air)
									{
										AddPhysicsUpdate(new BlockPosition(x, (short)(y - 1), z), tile);
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
                                            
                                              //  AddPhysicsUpdate(new BlockPosition((short)(x + diffX), (short)(y + diffY), (short)(z + diffZ)), Block.Air);
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
									AddPhysicsUpdate(new BlockPosition(x, y, z), Block.Air);
									AddPhysicsUpdate(new BlockPosition(x, lowY, z), tile);
								}
							}
						} // z
					} // y
				} // x
                

				*/
                lock (PhysicsBlocks)
                {


                    PhysicsBlocks.Sort();
                    foreach (var key in PhysicsBlocks)
                    {
                        switch (key.To)
                        {
                            case Block.Water:
                                for (int x = -1; x <= 1; x++)
                                {
                                    for (int y = -1; y <= 0; y++)
                                    {
                                        for (int z = -1; z <= 1; z++)
                                        {
                                            if (Math.Abs(x+y+z)==1)
                                            {
                                                short newX = (short)(x + key.x);
                                                short newY = (short)(y + key.y);
                                                short newZ = (short)(z + key.z);

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
                                for (int x = -1; x <= 1; x++)
                                {
                                    for (int y = -1; y <= 0; y++)
                                    {
                                        for (int z = -1; z <= 1; z++)
                                        {
                                            short newX = (short)(x + key.x);
                                            short newY = (short)(y + key.y);
                                            short newZ = (short)(z + key.z);

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
                                if (!BlockInfo.IsSolid(GetTile(key.x, (short)(key.y - 1), key.z)))
                                {
                                    AddPhysicsUpdate(new BlockPosition(key.x, key.y, key.z), Block.Air);
                                    AddPhysicsUpdate(new BlockPosition(key.x, (short)(key.y - 1), key.z), Block.Sand);
                                }
                                break;

                            case Block.Sponge: // For each of the tiles within sponge radius, remove any fluid.
                                for (int x = -BlockInfo.SpongeRadius; x <= BlockInfo.SpongeRadius; x++)
                                {
                                    for (int y = -BlockInfo.SpongeRadius; y <= BlockInfo.SpongeRadius; y++)
                                    {
                                        for (int z = -BlockInfo.SpongeRadius; z <= BlockInfo.SpongeRadius; z++)
                                        {
                                            short newX = (short)(x + key.x);
                                            short newY = (short)(y + key.y);
                                            short newZ = (short)(z + key.z);
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
                                if (BlockInfo.IsSolid(GetTile(key.x, (short)(key.y + 1), key.z)))
                                {
                                    AddPhysicsUpdate(new BlockPosition(key.x, key.y, key.z), Block.Dirt);
                                }
                                break;
                            case Block.Dirt:
                                if (!BlockInfo.IsSolid(GetTile(key.x, (short)(key.y + 1), key.z)))
                                {
                                    AddPhysicsUpdate(new BlockPosition(key.x, key.y, key.z), Block.Grass);
                                }
                                break;


                            default:
                                break;
                        }
                    }
                }
                int flUpdates = 0;
                foreach (KeyValuePair<BlockPosition, Block> KV in PhysicsUpdates)
                {
                    // If it isn't the wanted tile already, and the tile hasn't changed since we made the list entry...                        // Update it.
                    SetTile(KV.Key.x, KV.Key.y, KV.Key.z, KV.Value);
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
            SetTile(x, y, z, tile, false);
        }

		public void SetTile(short x, short y, short z, Block tile, bool overide)
		{
            if (!overide) // Override should be used to override all physics checking. 
            {
                Block previous = GetTile(x, y, z);


                PhysicsTask task = new PhysicsTask(x, y, z, previous);

                if (PhysicsBlocks.Contains(task))
                {
                    lock (PhysicsBlocks)
                    {
                        PhysicsBlocks.Remove(task);
                    }
                }
                if (BlockInfo.RequiresPhysics(tile))
                {
                    lock (PhysicsBlocks)
                    {
                        PhysicsBlocks.Add(new PhysicsTask(x, y, z, tile));
                    }
                }

                if (GetTile(x, (short)(y - 1), z) == Block.Dirt || GetTile(x, (short)(y - 1), z) == Block.Grass) // We've revealed a 
                {
                    PhysicsTask down = new PhysicsTask(x, (short)(y - 1), z, tile);
                    lock (PhysicsBlocks)
                    {
                        if (!PhysicsBlocks.Contains(down))
                            PhysicsBlocks.Add(down);
                    }
                }
            }

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