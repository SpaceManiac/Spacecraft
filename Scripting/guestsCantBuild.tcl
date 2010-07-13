# guestsCantBuild.tcl
# source this file to disallow guests from building

proc guestsCantBuild {sender x y z block oldblock} {
	set rank [lindex [playerInfo $sender] 6]
	if {$rank == "Guest"} {
		setTile $x $y $z $oldblock
		tell $sender "[getColorCode green]Guests aren't allowed to build here!"
	}
}

onPlayerChangeBlock guestsCantBuild