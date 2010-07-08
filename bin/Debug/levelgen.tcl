setSpawn 32 32 32 0

for {set x 0} {$x < 64} {incr x} { 
for {set y 0} {$y < 64} {incr y} { 
for {set z 0} {$z < 64} {incr z} { 
	if {$y == 32 || $y == 21} {
		set v [ expr rand() ]
		if { $v > 0.5 } { setTile $x $y $z "Dirt" } 
		broadcast $v
	}
	if {$y > 31} {
		setTile $x $y $z "Air"
	} else {
		setTile $x $y $z "Dirt"
	}
	}}}