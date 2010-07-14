# libSpacecraft.tcl
# These are some Tcl functions you may want to use for other stuff.
# They're quite useful and are designed to work with Spacecraft.

# ------------------------------------------------------------------------------
# We define 'haveLibSpacecraft' so that a script is allowed to complain
#   if libSpacecraft isn't available. Sure, you could try to fool it, but
#   that would probably lead to trouble.
set haveLibSpacecraft 1

# ------------------------------------------------------------------------------
# qf: quick-for
#   This function is for compact for loops during /tcl commands.
#   i.e. `qf x 1 10 {broadcast $x}` will broadcast all numbers 1-10.
#   Not generally used by scripts.
proc qf {var from to code} {
	uplevel 1 [format {
		for {set %s %d} {$%s <= %d} {incr %s} {
			%s
		}
	} $var $from $var $to $var $code]
}

# ------------------------------------------------------------------------------
# qs: quick-source
#   Usage: qs filename.tcl
#   This function is designed for use in dev builds of Spacecraft.
#   It will source the Tcl scripts from the source code directory rather
#   than from the Scripting subdirectory of the execution area. You won't see
#   this used much in finished scripts.
proc qs {f} {
	uplevel #0 "source \[file join .. .. Scripting $f\]"
}

# ------------------------------------------------------------------------------
# cmdQs: quick-source chat command
#   Usage: (chat) /qsrc filename.tcl
#   Calls qs (quick-source) with the argument specified
proc cmdQs {sender args} {
	if {[llength $args] == 0} { return }
	qs [lindex $args 0]
	tell $sender "[getColorCode privatemsg][lindex $args 0] quicksourced"
}
createChatCommand "qsrc" Admin "Quicksource the given script file" cmdQs

# ------------------------------------------------------------------------------
# lremove: list-remove
#   Usage: lremove list ?remove? ?remove...?
#   Given an original list and a list of elements to remove, this function will
#   return a new list with all of the specified elements removed. Useful for
#   removing a player from a list of players in a game mode.
proc lremove args {
	array set opts {-all 0 pattern -exact}
	while {[string match -* [lindex $args 0]]} {
	switch -glob -- [lindex $args 0] {
		-a*	{ set opts(-all) 1 }
		-g*	{ set opts(pattern) -glob }
		-r*	{ set opts(pattern) -regexp }
		--	{ set args [lreplace $args 0 0]; break }
		default {return -code error "unknown option \"[lindex $args 0]\""}
	}
	set args [lreplace $args 0 0]
	}
	set l [lindex $args 0]
	foreach i [join [lreplace $args 0 0]] {
	if {[set ix [lsearch $opts(pattern) $l $i]] == -1} continue
	set l [lreplace $l $ix $ix]
	if {$opts(-all)} {
		while {[set ix [lsearch $opts(pattern) $l $i]] != -1} {
		set l [lreplace $l $ix $ix]
		}
	}
	}
	return $l
}

# ------------------------------------------------------------------------------
# performCuboid: delayed cuboid performer
#   Usage: performCuboid x1 y1 z1 x2 y2 z2 blockType callback
#   This function will perform a cuboid at speeds such that minimal lag will
#   occur, executing 'callback' when the cuboid is complete. For use by any
#   script that must perform a cuboid as part of normal operation.
proc performCuboid {x1 y1 z1 x2 y2 z2 type callback} {
	set i 0
	for {set x $x1} {$x <= $x2} {incr x} {
		for {set y $y1} {$y <= $y2} {incr y} {
			for {set z $z1} {$z <= $z2} {incr z} {
				set time [expr {$i * 20}]
				after $time "setTile $x $y $z $type"
				incr i
			}
		}
	}
	after [expr {$i * 20}] $callback
}

# ------------------------------------------------------------------------------