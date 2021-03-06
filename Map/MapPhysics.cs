using System;
using System.Collections.Generic;

namespace spacecraft
{
	partial class Map
	// Defining physics-related stuff, since this seems so troublesome.
	{
		/// <summary>
		/// How many physics ticks we've done. Not sure why we have this.
		/// </summary>
		private uint physicsCount;
		/// <summary>
		/// Whether we execute physics.
		/// </summary>
		public bool PhysicsOn = true;
		private object PhysicsMutex = new object();

		/// <summary>
		/// A set of all of the coodinates that contain a physics-active block and must be checked.
		/// </summary>
		private List<BlockPosition> ActiveBlocks = new List<BlockPosition>();
		/// <summary>
		/// A set of the tile changes we must perform this tick.
		/// </summary>
		private Dictionary<int, PhysicsTask> PhysicsUpdates = new Dictionary<int, PhysicsTask>();

		private int[,] Heights;
		
		public int ActiveListLength {
			get { return ActiveBlocks.Count; }
		}
		
		public int UpdatesLastTick { get; protected set; }

		System.Diagnostics.Stopwatch Stopwatch = new System.Diagnostics.Stopwatch();

		private void AddActiveBlock(BlockPosition pos)
		{
			ActiveBlocks.Add(pos);
		}

		/// <summary>
		/// Set up physics processing.
		/// </summary>
		private void InitPhysics()
		{
			ActiveBlocks.Clear();
			Heights = new int[xdim, zdim];
			bool CheckingHeight = true;
			
			Stopwatch.Reset();
			Stopwatch.Start();
			for (short x = 0; x < xdim; ++x) {
				for (short z = 0; z < zdim; ++z) {
					CheckingHeight = true;
					Heights[x, z] = 0;
					
					for (short y = (short)(ydim - 1); y >= 0; --y) {
						Block b = GetTile(x, y, z);
						if (CheckingHeight && BlockInfo.IsOpaque(b)) {
							Heights[x, z] = y;
							CheckingHeight = false;
						}
						if (BlockInfo.RequiresPhysics(b))
						{
							ActiveBlocks.Add(new BlockPosition(x, y, z));
						}
					}
				}
			}

			Stopwatch.Stop();
			Spacecraft.Log("Physics initialised in " + ((Stopwatch.ElapsedMilliseconds/100)/10.0).ToString() + " seconds!");
		}

		public void AlertPhysicsAround(BlockPosition pos)
		{
			
			for (short x = -3; x <= 3; ++x)
			{
				for (short y = -3; y <= 3; ++y)
				{
					for (short z = -3; z <= 3; ++z)
					{
						short newX = (short)(pos.x + x);
						short newY = (short)(pos.y + y);
						short newZ = (short)(pos.z + z);
						if (BlockInfo.RequiresPhysics(GetTile(newX, newY,newZ))) {
							AddActiveBlock(new BlockPosition(newX, newY, newZ));
						}
						
						// Map edges should turn to water
						if ((GetTile(newX, newY, newZ) == Block.Air) &&
							(newX == 0 || newZ == 0 || newX == xdim-1 || newZ == zdim-1) &&
							(newY == ydim/2 - 1 || newY == ydim/2 - 2)) {
							SetTile(newX, newY, newZ, Block.Water);
						}
					}
				}
			}
		}

		/// <summary>
		/// Process one physics step.
		/// </summary>
		public void DoPhysics()
		{
			if (!PhysicsOn)
				return;

			if (physicsCount == 0) {
				InitPhysics();
			}

			++physicsCount;

			lock (PhysicsMutex) {
				PhysicsUpdates.Clear();
				
				List<BlockPosition> temp = new List<BlockPosition>(ActiveBlocks.Count + 20);
				temp.AddRange(ActiveBlocks);
				ActiveBlocks.Clear();

				foreach (BlockPosition pos in temp) {
					Block Tile = GetTile(pos);

					// Only process the block if it needs it
					// No need to specifically remove it since we clear at the end anyways
					if (BlockInfo.RequiresPhysics(Tile)) {
						HandlePhysics(pos.x, pos.y, pos.z, Tile);
					}
				}

				// Process physics updates.
				UpdatesLastTick = 0;
				foreach (PhysicsTask task in PhysicsUpdates.Values)
				{
					if(task.To == Block.Undefined) {
						// This block was "claimed" by a sponge or similar.
						// Do nothing.
						continue;
					}
						
					if(GetTile(task.x, task.y, task.z) == task.To) {
						// No need to send updates if it's already that tile
						continue;
					}
					
					SetTile(task.x, task.y, task.z, task.To);
					++UpdatesLastTick;
				}
			}
		}

		/// <summary>
		/// Handle the physics of a specific block, checking surroundings, etc.
		/// </summary>
		/// <param name="X">The X coodinate of the block</param>
		/// <param name="Y">The Y coodinate of the block</param>
		/// <param name="Z">The Z coodinate of the block</param>
		/// <param name="block">The type of block at this position.</param>
		private void HandlePhysics(short X, short Y, short Z, Block block)
		{
			switch (block)
			{
				case Block.Water:
					// flowing water
					for (int x = -1; x <= 1; x++)
					{
						for (int y = -1; y <= 0; y++)
						{
							for (int z = -1; z <= 1; z++)
							{
								if (Math.Abs(x) + Math.Abs(y) + Math.Abs(z) == 1)
								{
									short newX = (short)(x + X);
									short newY = (short)(y + Y);
									short newZ = (short)(z + Z);
								
									int Hash = PhysicsTask.HashOf(newX, newY, newZ);
									
									if(PhysicsUpdates.ContainsKey(Hash) && PhysicsUpdates[Hash].To == Block.Undefined) {
										// Been claimed by a sponge
										// See issue #7, bullet point 3
										continue;
									}

									if (GetTile(newX, newY, newZ) == Block.Lava)
									{
										AddPhysicsUpdate(new PhysicsTask(newX, newY, newZ, Block.Rock));
									}
									else if (BlockInfo.IsFluid(GetTile(newX, newY, newZ)))
									{
										// Do nothing.
									}
									else if (!BlockInfo.IsSolid(GetTile(newX, newY, newZ)))
									{
										AddPhysicsUpdate(new PhysicsTask(newX, newY, newZ, Block.Water));
									}
								}
							}
						}
					}
					
					// turn lava directly above the water level into rock.
					// without this, lava above water will not form stone.
					if(GetTile(X, (short)(Y + 1), Z) == Block.Lava) {
						AddPhysicsUpdate(new PhysicsTask(X, (short)(Y + 1), Z, Block.Rock));
					}
					
					break;

				case Block.Lava:
					// flowing of lava
					for (int x = -1; x <= 1; x++)
					{
						for (int y = -1; y <= 0; y++)
						{
							for (int z = -1; z <= 1; z++)
							{
								if (Math.Abs(x) + Math.Abs(y) + Math.Abs(z) == 1)
								{
									short newX = (short)(x + X);
									short newY = (short)(y + Y);
									short newZ = (short)(z + Z);
								
									int Hash = PhysicsTask.HashOf(newX, newY, newZ);
									
									if(PhysicsUpdates.ContainsKey(Hash) && PhysicsUpdates[Hash].To == Block.Undefined) {
										// Been claimed by a sponge
										// See issue #7, bullet point 3
										continue;
									}

									if (BlockInfo.IsFluid(GetTile(newX, newY, newZ)))
									{
										// Do nothing.
									}
									else if (!BlockInfo.IsSolid(GetTile(newX, newY, newZ)))
									{
										AddPhysicsUpdate(new PhysicsTask(newX, newY, newZ, Block.Lava));
									}
								}
							}
						}
					}
					break;

				case Block.Sponge:
					// prevent water/lava flow
					for (int xDiff = -BlockInfo.SpongeRadius; xDiff <= BlockInfo.SpongeRadius; xDiff++)
					{
						for (int yDiff = -BlockInfo.SpongeRadius; yDiff <= BlockInfo.SpongeRadius; yDiff++)
						{
							for (int zDiff = -BlockInfo.SpongeRadius; zDiff <= BlockInfo.SpongeRadius; zDiff++)
							{
								short newX = (short)(xDiff + X);
								short newY = (short)(yDiff + Y);
								short newZ = (short)(zDiff + Z);
								
								int Hash = PhysicsTask.HashOf(newX, newY, newZ);

								if (PhysicsUpdates.ContainsKey(Hash) && (BlockInfo.IsFluid(PhysicsUpdates[Hash].To)))
								{
									// eliminate the existing fluid update
									// see issue #7, bullet point 1
									AddPhysicsUpdate(new PhysicsTask(newX, newY, newZ, Block.Air));
								}
								else if (BlockInfo.IsFluid(GetTile(newX, newY, newZ)))
								{
									// remove standing liquids
									// see issue #7, bullet point 2
									AddPhysicsUpdate(new PhysicsTask(newX, newY, newZ, Block.Air));
								}
								else if (GetTile(newX, newY, newZ) == Block.Air)
								{
									// "claim" the air space so fluids will not expand here - Block.Undefined will
									// always cause no real change, but will stop the water or lava.
									// see issue #7, bullet point 3
									AddPhysicsUpdate(new PhysicsTask(newX, newY, newZ, Block.Undefined));
								}
							}
						}
					}
					
					// So the sponge stays active even if water doesn't directly touch it
					// We have the Contains here so we don't end up with the same sponge on the list
					// a bajillion times (which apparently is a severe issue otherwise :P)
					if(!ActiveBlocks.Contains(new BlockPosition(X, Y, Z))) {
						AddActiveBlock(new BlockPosition(X, Y, Z));
					}
					break;

				case Block.Sand:
					// gravitize sand
					if (!BlockInfo.IsSolid(GetTile(X, (short)(Y - 1), Z)))
					{
						AddPhysicsUpdate(new PhysicsTask(X, (short)(Y - 1), Z, Block.Sand));
						AddPhysicsUpdate(new PhysicsTask(X, Y, Z, Block.Air));
					}
					break;

				case Block.Gravel:
					// gravitize gravel
					if (!BlockInfo.IsSolid(GetTile(X, (short)(Y - 1), Z)))
					{
						AddPhysicsUpdate(new PhysicsTask(X, (short)(Y - 1), Z, Block.Gravel));
						AddPhysicsUpdate(new PhysicsTask(X, Y, Z, Block.Air));
					}

					break;

				case Block.Unobtanium:
					// for testing purposes
					if (BlockInfo.IsSolid(GetTile(X, (short)(Y - 1), Z)))
					{
						AddPhysicsUpdate(new PhysicsTask(X, (short)(Y + 1), Z, Block.Unobtanium));
						AddPhysicsUpdate(new PhysicsTask(X, Y, Z, Block.Air));
					} else {
						if (!BlockInfo.IsSolid(GetTile(X, (short)(Y - 2), Z)))
						{
							AddPhysicsUpdate(new PhysicsTask(X, (short)(Y - 1), Z, Block.Unobtanium));
							AddPhysicsUpdate(new PhysicsTask(X, Y, Z, Block.Air));
						}
					}
					break;

				default:
					break;
			}
		}

		void AddPhysicsUpdate(PhysicsTask task)
		{
			if (PhysicsUpdates.ContainsKey(task.GetHashCode()))
				PhysicsUpdates.Remove(task.GetHashCode());
			PhysicsUpdates.Add(task.GetHashCode(), task);
		}

		/// <summary>
		/// Recalculates the heightmap, with the given position is solid/not.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <param name="solid">Whether the block is solid.</param>
		void RecalculateHeight(short x, short y, short z, bool opaque)
		{
			if (Heights == null)
			{
				Heights = new int[xdim, zdim];
			}
			
			if (opaque)
			{
				if(Heights[x, z] < y) {
					if(GetTile(x, (short)Heights[x, z], z) == Block.Grass) {
						SetTile(x, (short) Heights[x, z], z, Block.Dirt, false);
					}
					Heights[x, z] = y;
				}
			}
			else
			{
				if (Heights[x, z] <= y)
				{
					Heights[x, z] = 0;
					for (short Y = (short)(y-1); Y >= 0; Y--)
					{
						Block t = GetTile(x, Y, z);
						if (BlockInfo.IsOpaque(t))
						{
							Heights[x, z] = Y;
							if(t == Block.Dirt) {
								SetTile(x, Y, z, Block.Grass, false);
							}
							break;
						}
					}
				}
			}
		}
	}
}