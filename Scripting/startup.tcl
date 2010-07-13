# startup.tcl
# Spacecraft will attempt to load any file named startup.tcl
# in its Scripting subdirectory. Feel free to load any other
# script files you need, like libSpacecraft, here.

source Scripting/libSpacecraft.tcl

qs buildRank.tcl
buildRank::init

qs dropship.tcl
dropship::init

qs cuboid.tcl
cuboid::init

qs spelunker.tcl
spelunker::init

