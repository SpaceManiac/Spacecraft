# /bother chat command

proc bother {sender args} {
	if {[llength $args] < 1} {
		return -code error "Please specify a player"
	}
	set name [lindex $args 0]
	set info [playerInfo $name]
	foreach {id x y z heading pitch rank} $info {}
	set x [expr {int($x / 32)}]
	set y [expr {int($y / 32)}]
	set z [expr {int($z / 32)}]
	for {set xn [expr {$x - 1}]} {$xn <= ($x + 1)} {incr xn} {
		for {set zn [expr {$z - 1}]} {$zn <= ($z + 1)} {incr zn} {
			for {set yn $y} {$yn >= ($y - 3)} {incr yn -1} {
				setTile $xn $yn $zn air
			}
		}
	}
	tell $name "&5$sender used /bother on you!"
	scLog "$sender bother'd $name"
}

createChatCommand bother Builder "Remove some blocks near a player" bother