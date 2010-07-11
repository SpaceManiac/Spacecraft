# cuboid.tcl

proc cuboid {sender args} {
	if {[llength $args] < 3} {
		tell $sender "[getColorCode commanderror]Usage: cuboid {x1 y1 z1} {x2 y2 z2} type"
		return
	}
	set coords1 [lindex $args 0]
	set coords2 [lindex $args 1]
	set type [lindex $args 2]
	if {[llength $coords1] != 3 || [llength $coords2] != 3} {
		tell $sender "[getColorCode commanderror]Wrong length for coordinate sets!"
		return
	}
	
	foreach {x1 y1 z1} $coords1 {}
	foreach {x2 y2 z2} $coords2 {}
	
	qf x $x1 $x2 {
		qf y $y1 $y2 {
			qf z $z1 $z2 {
				setTile $x $y $z $type
			}
		}
	}
}

proc z {args} {
	catch {eval cuboid "" $args} x
	return $x
}