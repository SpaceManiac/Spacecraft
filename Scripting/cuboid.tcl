# cuboid.tcl
# procedures for the /cuboid command

array set cuboid {}

proc cuboid {sender args} {
	global cuboid
	
	if {[llength $args] != 1} {
		tell $sender "[getColorCode commandError]Usage: /cuboid type"
		return
	}
	
	set type [lindex $args 0]
	
	if {$type == "cancel"} {
		cuboidFinish $sender
		tell $sender "[getColorCode commandResult]Cuboid cancelled."
		return
	}
	
	if {[catch {getBlockID $type}]} {
		tell $sender "[getColorCode commandError]No such block $type"
		return
	}
	
	set rank [lindex [playerInfo $sender] 6]
	set type [string tolower $type]
	if {$rank != "Mod" && $rank != "Admin" && ($type == "water" || $type == "lava" || $type == "adminium")} {
		tell $sender "[getColorCode commandError]Not allowed to place!"
		return
	}
	
	set cuboid($sender) 1
	set cuboid($sender,type) $type
	
	tell $sender "[getColorCode commandResult]Place one corner of the cuboid. (Or /cuboid cancel)"
}

proc cuboidFinish {sender} {
	global cuboid
	catch {unset cuboid($sender)}
	catch {unset cuboid($sender,type)}
	catch {unset cuboid($sender,x1)}
	catch {unset cuboid($sender,y1)}
	catch {unset cuboid($sender,z1)}
}

proc cuboidSetBlock {sender x y z block oldblock} {
	global cuboid
	if {![info exists cuboid($sender)]} {
		return
	}
	
	if {$cuboid($sender) == 1} {
		set cuboid($sender,x1) $x
		set cuboid($sender,y1) $y
		set cuboid($sender,z1) $z
		set cuboid($sender) 2
		setTile $x $y $z $oldblock
		tell $sender "[getColorCode commandResult]Place the other corner of the cuboid."
	} elseif {$cuboid($sender) == 2} {
		set x1 $cuboid($sender,x1)
		set y1 $cuboid($sender,y1)
		set z1 $cuboid($sender,z1)
		set x2 $x
		set y2 $y
		set z2 $z
		
		if {$x1 > $x2} {
			set temp $x1
			set x1 $x2
			set x2 $temp
		}
		if {$y1 > $y2} {
			set temp $y1
			set y1 $y2
			set y2 $temp
		}
		if {$z1 > $z2} {
			set temp $z1
			set z1 $z2
			set z2 $temp
		}
		
		set i [expr {($x2 - $x1 + 1) * ($y2 - $y1 + 1) * ($z2 - $z1 + 1)}]
		if {$i > 1000} {
			tell $sender "[getColorCode commandError]Cuboid too big! Must be <1000 total blocks"
		} else {
			set type $cuboid($sender,type)
			set total [expr {($x2 - $x1) * ($y2 - $y1) * ($z2 - $z1)}]
			set eta [expr {$total / 50}]
			if {$eta > 5} {
				tell $sender "[getColorCode commandResult]Cuboid running, ETA $eta seconds"
			}
			
			for {set x $x1} {$x <= $x2} {incr x} {
				for {set y $y1} {$y <= $y2} {incr y} {
					for {set z $z1} {$z <= $z2} {incr z} {
						if {[getTile $x $y $z] == "adminium"} {
							tell $sender "[getColorCode commandError]Can't cuboid through adminium!"
							return
						}
					}
				}
			}
			
			performCuboid $x1 $y1 $z1 $x2 $y2 $z2 $type [format {tell %s "[getColorCode commandResult]Cuboid complete"; cuboidFinish %s} $sender $sender]
			scLog "$sender used /cuboid $type ([expr $x2-$x1+1]x[expr $y2-$y1+1]x[expr $z2-$z1+1] == $i)"
		}
	}
}

createChatCommand "cuboid" Builder "Define a cuboid" cuboid
onPlayerChangeBlock cuboidSetBlock