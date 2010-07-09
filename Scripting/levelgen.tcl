# levelgen.tcl
# this is a basic Tcl version of the standard flatgrass generation


proc generateLevel {xdim ydim zdim} {
	setSpawn [expr {$xdim/2}] [expr {$ydim/2}] [expr {$zdim/2}] 0

	scLog "$xdim,$ydim,$zdim"
	for {set x 0} {$x < $xdim} {incr x} { 
		for {set y 0} {$y < ($ydim / 2)} {incr y} { 
			for {set z 0} {$z < $zdim} {incr z} { 
				if {$y == ($ydim / 2 - 1)} {
					setTile $x $y $z grass false
				} else {
					setTile $x $y $z dirt false
				}
			}
		}
		if {$x % 10 == 0} {
			scLog $x
		}
	}
}

#onLevelGeneration generateLevel