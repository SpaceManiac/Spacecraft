# spelunker.tcl
# This is a fancy Spelunker minigame. Get to the end of the course without
# placing or destroying any blocks. Be sure to change spelunker_party.

namespace eval spelunker {
	# Set this to 0 if you're not on the test server
	set spelunker_party 1

	set spelunker {}
	set spelunker_started 0
	set spelunker_time 0

	proc spelunker {sender args} {
		variable spelunker
		variable spelunker_started
		
		set r [lindex [playerInfo $sender] 6]
		if {$r != "Mod" && $r != "Admin" && $spelunker == ""} {
			tell $sender "[getColorCode commandError]You must be a Mod+ to start a game!"
			return
		}
		if {$spelunker == ""} {
			set spelunker $sender
			broadcast "Spelunker: $sender started a game! 90 seconds until start."
			after 30000 {broadcast "Spelunker: 60 seconds remain!"}
			after 60000 {broadcast "Spelunker: 30 seconds remain!"}
			after 70000 {broadcast "Spelunker: 20 seconds remain! Get ready..."}
			after 80000 {broadcast "Spelunker: 10 seconds remain!"}
			after 81000 {broadcast "Spelunker: 9 seconds remain..."}
			after 82000 {broadcast "Spelunker: 8 seconds..."}
			after 83000 {broadcast "Spelunker: 7 seconds..."}
			after 84000 {broadcast "Spelunker: 6..."}
			after 85000 {broadcast "Spelunker: 5..."}
			after 86000 {broadcast "Spelunker: 4..."}
			after 87000 {broadcast "Spelunker: 3..."}
			after 88000 {broadcast "Spelunker: 2..."}
			after 89000 {broadcast "Spelunker: 1..."}
			after 90000 {spelunker::spelunkerStart}
		} else {
			if {$spelunker_started} {
				tell $sender "[getColorCode commandError]The game is in progress!"
			} elseif {[lsearch $spelunker $sender] >= 0} {
				tell $sender "[getColorCode commandError]You're already signed up!"
			} else {
				lappend spelunker $sender
				broadcast "Spelunker: $sender joined the game!"
			}
		}
		return ""
	}

	proc spelunkerStart {} {
		variable spelunker
		variable spelunker_started
		variable spelunker_time
		variable spelunker_party
		
		broadcast "Spelunker: GOOOOOOOOOOOOO!"
		set spelunker_started 1
		set spelunker_time 0
		foreach player $spelunker {
			playerToLandmark $player spelunkerstart
		}
		
		if {$spelunker_party} {
			for {set x 6} {$x <= 13} {incr x} {
				for {set z 180} {$z <= 187} {incr z} {
					if {$x == 12 && $z == 186} { continue }
					for {set y 46} {$y <= 50} {incr y} {
						if {rand() < 0.2} {
							setTile $x $y $z "sand"
						}
					}
				}
			}
		}
	}

	proc setBlock {name x y z block prevBlock} {
		variable spelunker
		variable spelunker_started
		
		if {$spelunker_started && [lsearch $spelunker $name] >= 0} {
			broadcast "Spelunker: $name tried to change a block!"
			playerToLandmark $name spelunkerspec
			set spelunker [lremove $spelunker $name]
			setTile $x $y $z $prevBlock
		}
	}

	proc spelunkerEnd {} {
		variable spelunker
		variable spelunker_started
		
		if {$spelunker_started} {
			set spelunker ""
			set spelunker_started 0
			broadcast "Spelunker: The game has ended."
		}
	}	

	proc depart {name} {
		variable spelunker
		
		set spelunker [lremove $spelunker $name]
	}

	proc tick {} {
		variable spelunker
		variable spelunker_started
		variable spelunker_time
		variable spelunker_party
		
		if {$spelunker_party} {
			for {set x 6} {$x <= 13} {incr x} {
				for {set z 180} {$z <= 187} {incr z} {
					if {[getTile $x 46 $z] == "sand"} {
						setTile $x 46 $z "air"
						if {$spelunker_started} {
							setTile $x 50 $z "sand"
						}
					}
				}
			}
		}
		
		if {!$spelunker_started} {
			return
		}
		
		set spelunker_time [expr {$spelunker_time + 0.5}]
		set time "${spelunker_time}s"
		
		set markInfo [landmarkInfo spelunkerend]
		set markx [expr {int([lindex $markInfo 0]/32)}]
		set marky [expr {int([lindex $markInfo 1]/32)}]
		set markz [expr {int([lindex $markInfo 2]/32)}]
			
		foreach player $spelunker {
			set info [playerInfo $player]
			set playerx [expr {int([lindex $info 1]/32)}]
			set playery [expr {int([lindex $info 2]/32)}]
			set playerz [expr {int([lindex $info 3]/32)}]
			if {abs($playerx - $markx) <= 1 && abs($playery - $marky) <= 1 && abs($playerz - $markz) <= 1} {
				if {$spelunker_started == 2} {
					broadcast "Spelunker: $player crossed the finish line ($time)!"
				} else {
					set spelunker_started 2
					broadcast "Spelunker: $player WINS ($time)! 90 seconds remain."
					after 90000 spelunker::spelunkerEnd
				}
				set spelunker [lremove $spelunker $player]
			}
		}
		
		if {[llength $spelunker] == 0} {
			broadcast "Spelunker: No players remain."
			spelunkerEnd
		}
	}

	proc init {} {
		createChatCommand "spelunker" Guest "Start or join a game of spelunker!" spelunker::spelunker
		onWorldTick spelunker::tick
		onPlayerChangeBlock spelunker::setBlock
		onPlayerDepart spelunker::depart
		scLog "Spelunker initialized."
	}
}

