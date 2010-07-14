# dropship.tcl
# Script for Cliffy1000's bomber thing. It's fancy shmancy. Talk to SpaceManiac
# if you really need details. It's quite specialized for the current shape of
# the test dropship.

namespace eval dropship {
	proc signature {x y z} {
		set signature ""
		for {set xx [expr {$x - 1}]} {$xx <= ($x + 1)} {incr xx} {
			for {set zz [expr {$z - 1}]} {$zz <= ($z + 1)} {incr zz} {
				append signature "[getTile $xx $y $zz]."
			}
		}
		return $signature
	}

	proc move {xo yo zo dim dir} {
		# total xdim=+2-2 ydim=+0-5 zdim=+4-2
		if {$dir == "-"} {
			for {set x [expr {$xo - 2}]} {$x <= $xo + 2} {incr x} {
				for {set y [expr {$yo - 5}]} {$y <= $yo} {incr y} {
					for {set z [expr {$zo - 2}]} {$z <= $zo + 4} {incr z} {
						if {$dim == "x"} {
							setTile [expr {$x-1}] $y $z [getTile $x $y $z]
						} elseif {$dim == "y"} {
							setTile $x [expr {$y-1}] $z [getTile $x $y $z]
						} elseif {$dim == "z"} {
							setTile $x $y [expr {$z-1}] [getTile $x $y $z]
						}
					}
				}
			}
			if {$dim == "x"} {
				for {set y [expr {$yo - 5}]} {$y <= $yo} {incr y} {
					for {set z [expr {$zo - 2}]} {$z <= $zo + 4} {incr z} {
						setTile [expr {$xo + 2}] $y $z air
					}
				}
			} elseif {$dim == "y"} {
				for {set x [expr {$xo - 2}]} {$x <= $xo + 2} {incr x} {
					for {set z [expr {$zo - 2}]} {$z <= $zo + 4} {incr z} {
						setTile $x [expr {$yo}] $z air
					}
				}
			} elseif {$dim == "z"} {
				for {set x [expr {$xo - 2}]} {$x <= $xo + 2} {incr x} {
					for {set y [expr {$yo - 5}]} {$y <= $yo} {incr y} {
						setTile $x $y [expr {$zo + 4}] air
					}
				}
			}
		} elseif {$dir == "+"} {
			for {set x [expr {$xo + 2}]} {$x >= $xo - 2} {incr x -1} {
				for {set y [expr {$yo}]} {$y >= $yo - 5} {incr y -1} {
					for {set z [expr {$zo + 4}]} {$z >= $zo - 2} {incr z -1} {
						if {$dim == "x"} {
							setTile [expr {$x+1}] $y $z [getTile $x $y $z]
						} elseif {$dim == "y"} {
							setTile $x [expr {$y+1}] $z [getTile $x $y $z]
						} elseif {$dim == "z"} {
							setTile $x $y [expr {$z+1}] [getTile $x $y $z]
						}
					}
				}
			}
			if {$dim == "x"} {
				for {set y [expr {$yo - 5}]} {$y <= $yo} {incr y} {
					for {set z [expr {$zo - 2}]} {$z <= $zo + 4} {incr z} {
						setTile [expr {$xo - 2}] $y $z air
					}
				}
			} elseif {$dim == "y"} {
				for {set x [expr {$xo - 2}]} {$x <= $xo + 2} {incr x} {
					for {set z [expr {$zo - 2}]} {$z <= $zo + 4} {incr z} {
						setTile $x [expr {$yo - 5}] $z air
					}
				}
			} elseif {$dim == "z"} {
				for {set x [expr {$xo - 2}]} {$x <= $xo + 2} {incr x} {
					for {set y [expr {$yo - 5}]} {$y <= $yo} {incr y} {
						setTile $x $y [expr {$zo - 2}] air
					}
				}
			}
		}
	}

	proc changeBlockCallback {sender x y z newType type} {
		set r [lindex [playerInfo $sender] 6]
		if {($r != "Mod" && $r != "Admin") || $newType != "air" || ($type != "glass" && $type != "leaves")} {
			return
		}
		
		set signature "wood.teal.rock.brick.green.brick.rock.brick.wood."
		
		if {$type == "leaves"} {
			if {$signature == [signature [expr {$x + 1}] [expr {$y + 4}] [expr {$z + 1}]]} {
				tell $sender "[getColorCode green]Gravel reloaded"
				setTile $x $y $z $type
				setTile [expr {$x + 1}] $y [expr {$z + 4}] iron
				setTile [expr {$x + 1}] [expr {$y + 1}] [expr {$z + 4}] gravel
				setTile [expr {$x + 1}] [expr {$y + 2}] [expr {$z + 4}] gravel
			} elseif {$signature == [signature [expr {$x - 1}] [expr {$y + 4}] [expr {$z + 1}]]} {
				tell $sender "[getColorCode green]Dropping gravel"
				setTile $x $y $z $type
				setTile [expr {$x - 1}] $y [expr {$z + 4}] air
				setTile [expr {$x - 1}] [expr {$y - 1}] [expr {$z + 4}] air
				after 2000 "tell $sender {[getColorCode green]Ready to reload}"
			}
		} elseif {$type == "glass"} {
			if {$signature == [signature [expr {$x + 1}] [expr {$y + 2}] [expr {$z + 2}]] && ($y + 2) < 250} {
				setTile $x $y $z $type
				move [expr {$x + 1}] [expr {$y + 2}] [expr {$z + 2}] y +
			} elseif {$signature == [signature $x [expr {$y + 2}] [expr {$z + 2}]]} {
				setTile $x $y $z $type
				move $x [expr {$y + 2}] [expr {$z + 2}] z -
			} elseif {$signature == [signature [expr {$x - 1}] [expr {$y + 2}] [expr {$z + 2}]]} {
				setTile $x $y $z $type
				move [expr {$x - 1}] [expr {$y + 2}] [expr {$z + 2}] y -
			} elseif {$signature == [signature [expr {$x + 1}] [expr {$y + 3}] [expr {$z + 2}]]} {
				setTile $x $y $z $type
				move [expr {$x + 1}] [expr {$y + 3}] [expr {$z + 2}] x -
			} elseif {$signature == [signature $x [expr {$y + 3}] [expr {$z + 2}]]} {
				setTile $x $y $z $type
				move $x [expr {$y + 3}] [expr {$z + 2}] z +
			} elseif {$signature == [signature [expr {$x - 1}] [expr {$y + 3}] [expr {$z + 2}]]} {
				setTile $x $y $z $type
				move [expr {$x - 1}] [expr {$y + 3}] [expr {$z + 2}] x +
			}
		}
	}
	
	proc init {} {
		onPlayerChangeBlock dropship::changeBlockCallback
		scLog "Dropship initialized."
	}
	
	proc shutdown {} {
		dropHook onPlayerChangeBlock dropship::changeBlockCallback
		scLog "Dropship shut down."
	}
}

