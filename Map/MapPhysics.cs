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
	
	private void AddActiveBlock(BlockPosition pos) {
	int list = Spacecraft.random.Next(0, ActiveBlocks.Count - 1);
	lock(ActiveBlocks[list]) {
		 	ActiveBlocks[list].Add(pos);
		}
	}
	
	private void InitPhysics()
	{
		ActiveBlocks.Clear();
		for(int i = 0; i < 16; ++i) {
			ActiveBlocks.Add(new List<BlockPosition>());
		}
		
		for(short x = 0; x < xdim; ++x) {
			for(short y = 0; y < ydim; ++y) {
				for(short z = 0; z < zdim; ++z) {
	if (BlockInfo.RequiresPhysics(GetTile(x, y, z))) {
		AddActiveBlock(new BlockPosition(x, y, z));
	}
	}
	}
	}
	List<string> lengths = new List<string>();
	for(int i = 0; i < ActiveBlocks.Count; ++i) {
		lengths.Add(ActiveBlocks[i].Count.ToString());
	}
	Spacecraft.Log("ActiveBlocks initialized, lengths (" + String.Join(", ", lengths.ToArray()) + ")");
	}
	
	public void AlertPhysicsAround(BlockPosition pos) {
		for(short x = -1; x <= 1; ++x) {
			for(short y = -1; y <= 1; ++y) {
				for(short z = -1; z <= 1; ++z) {
					short newX = (short)(pos.x + x);
					short newY = (short)(pos.y + y);
					short newZ = (short)(pos.z + z);
					if (Math.Abs(x) + Math.Abs(y) + Math.Abs(z) == 1) {
						AddActiveBlock(new BlockPosition(newX, newY, newZ));
					}
				}
			}
		}
	}
	
	public void DoPhysics()
	{
		if(physicsCount == 0) {
			InitPhysics();
		}
		
	++physicsCount;
	if (Spacecraft.DEBUG) // Debug timer stuff. Not strictly necessary, but useful.
	{
	Stopwatch.Reset();
	Stopwatch.Start();
	}
	
	lock(PhysicsMutex) {
	ItemsToBeRemoved.Clear();
	PhysicsUpdates.Clear();
	
	for (int index = 0; index < ActiveBlocks.Count; index++)
	{
		lock(ActiveBlocks[index]) {
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
		ActiveBlocks[index].Clear();
	}
	}
	
				// needs fixing!
	//foreach (Pair<int, int> key in ItemsToBeRemoved) {
	//    ActiveBlocks[key.First].RemoveAt(key.Second);
	//}
	
	foreach (var task in PhysicsUpdates) {
		AddActiveBlock(new BlockPosition(task.x, task.y, task.z));
	SetTile(task.x, task.y, task.z, task.To);
	}
	}

	if (Spacecraft.DEBUG)
	{
	Stopwatch.Stop();
	System.Diagnostics.Debug.WriteLine("Physics:" + Stopwatch.ElapsedMilliseconds);
	}
	}

	private void HandlePhysics(short X, short Y, short Z, Block block)
	{
	switch (block) {
	case Block.Water:
	//bool Sponged = false; 
	//List<PhysicsTask> spreadTiles = new List<PhysicsTask>();

	for (int x = -1; x <= 1; x++) {
	for (int y = -1; y <= 0; y++) {
	for (int z = -1; z <= 1; z++) {
	if (Math.Abs(x) + Math.Abs(y) + Math.Abs(z) == 1) {
	short newX = (short)(x + X);
	short newY = (short)(y + Y);
	short newZ = (short)(z + Z);

	if (GetTile(newX, newY, newZ) == Block.Lava) {
	AddPhysicsUpdate(new PhysicsTask(newX, newY, newZ, Block.Rock));
	}
	else if (GetTile(newX, newY, newZ) == Block.Water) {
		// Do nothing.
	}
	else if (!BlockInfo.IsSolid(GetTile(newX, newY, newZ))) {
	AddPhysicsUpdate(new PhysicsTask(newX, newY, newZ, Block.Lava));
	}
	}
	}
	}
	}
	break;
	
	case Block.Lava:
	//bool Sponged = false; 
	//List<PhysicsTask> spreadTiles = new List<PhysicsTask>();

	for (int x = -1; x <= 1; x++) {
	for (int y = -1; y <= 0; y++) {
	for (int z = -1; z <= 1; z++) {
	if (Math.Abs(x) + Math.Abs(y) + Math.Abs(z) == 1) {
	short newX = (short)(x + X);
	short newY = (short)(y + Y);
	short newZ = (short)(z + Z);

	if (GetTile(newX, newY, newZ) == Block.Lava) {
		// Do nothing.
	}
	else if (!BlockInfo.IsSolid(GetTile(newX, newY, newZ))) {
	AddPhysicsUpdate(new PhysicsTask(newX, newY, newZ, Block.Water));
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
	PhysicsUpdates.Add(task);
	}
	}
}