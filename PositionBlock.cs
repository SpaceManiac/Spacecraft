using System;

namespace spacecraft
{
    public struct PositionBlock
    {
        public short x, y, z;
        public Block tile;

        public PositionBlock(short X, short Y, short Z, Block Tile)
        {
            x = X; y = Y; z = Z; tile = Tile;
        }
    }

}