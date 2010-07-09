# walls.tcl

proc walls {} {
	# x=0 and x=255 walls
	for {set z 0} {$z < 256} {incr z} {
		for {set y 0} {$y < 128} {incr y} {
			setTile 0 $y $z adminium
			setTile 255 $y $z adminium
		}
	}
	
	# z=0 and z=255 walls
	for {set x 0} {$x < 256} {incr x} {
		for {set y 0} {$y < 128} {incr y} {
			setTile $x $y 0 adminium
			setTile $x $y 255 adminium
		}
	}
	
	# y=0 walls
	for {set x 0} {$x < 256} {incr x} {
		for {set z 0} {$z < 256} {incr z} {
			setTile $x 0 $z adminium
		}
	}
}