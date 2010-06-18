using System;
using System.Collections;
using System.Collections.Generic;

namespace spacecraft
{
    partial class Map
        // Defining physics-related stuff, since this seems so troublesome.
    {

        private uint physicsCount;
        public bool PhysicsOn = true;
        private object PhysicsMutex = new object();

        private List<List<BlockPosition>> ActiveBlocks = new List<List<BlockPosition>>();
        private List<PhysicsTask> PhysicsUpdates = new List<PhysicsTask>();
        private List<Pair<int, int>> ItemsToBeRemoved = new List<Pair<int, int>>();

        System.Diagnostics.Stopwatch Stopwatch = new System.Diagnostics.Stopwatch();
        
        public void DoPhysics()
        {
            ++physicsCount;
            if (Spacecraft.DEBUG) // Debug timer stuff. Not strictly necessary, but useful.
            {
                Stopwatch.Reset();
                Stopwatch.Start();
            }

            ItemsToBeRemoved.Clear();
            PhysicsUpdates.Clear();

            for (int index = 0; index < ActiveBlocks.Count; index++)
            {
                for (int i = 0; i < ActiveBlocks[index].Count; i++)
                {
                    BlockPosition pos = ActiveBlocks[index][i];
                    Block Tile = GetTile(pos);

                    // Check to see whether this location still needs to be on the list of physics-active blocks.
                    if (!BlockInfo.RequiresPhysics(Tile))
                    {
                        // If it isn't, add it to the list needed to be removed.
                        ItemsToBeRemoved.Add(new Pair<int, int>(index, i));
                    }
                    else
                    {
                        HandlePhysics(pos.x, pos.y, pos.z, Tile);
                    }
                }
            }


            foreach (Pair<int, int> key in ItemsToBeRemoved)
            { // Purge ActiveBlocks of needless entries.
                ActiveBlocks[key.First].RemoveAt(key.Second);
            }

            foreach (var task in PhysicsUpdates)
            {
                SetTile(task.x, task.y, task.z, task.To);
            }

            if (Spacecraft.DEBUG)
            {
                Stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine("Physics:" + Stopwatch.ElapsedMilliseconds);
            }
        }

        private void HandlePhysics(short X, short Y, short Z, Block block)
        {
            switch (block)
            {
                case Block.Water:
                    bool Sponged = false; 
                    List<PhysicsTask> spreadTiles = new List<PhysicsTask>();

                    for (int xDiff = -1; xDiff <= 1; xDiff++)
                    {
                        for (int yDiff = -1; yDiff <= 1; yDiff++)
                        {
                            for (int zDiff = -1; zDiff <= 1; zDiff++)
                            {
                                short newX = (short)(X + xDiff);
                                short newY = (short)(Y + yDiff);
                                short newZ = (short)(Z + zDiff);
                                if (!Sponged)
                                {
                                    if (Y <= 0)
                                    {
                                        spreadTiles.Add(new PhysicsTask(newX, newY, newZ, Block.Water));
                                    }

                                    if (GetTile(newX, newY, newZ) == Block.Sponge)
                                    {
                                        AddPhysicsUpdate(new PhysicsTask(X, Y, Z, Block.Air));
                                        spreadTiles.Clear();
                                        Sponged = true;
                                    }
                                }
                            }
                        }
                    }
                    break;
                
                
                default:
                    break;
            }
        }


        void AddPhysicsUpdate(PhysicsTask task)
        {
            lock (PhysicsMutex)
            {
                PhysicsUpdates.Add(task);
            }
        }

    }

}