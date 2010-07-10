proc quicktime {args} {
	for {set i 0} {$i < 10} {incr i} {
		set n [expr {10 - $i}]
		after [expr {$i * 1000}] "broadcast $n..."
	}
	after 10000 "broadcast GOOOOOOOOO!"
}

#createChatCommand "countdown" Mod "start a 10-second countdown" quicktime