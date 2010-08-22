using System;

namespace spacecraft
{
	public class GameBase
	{
		public static void Announce(string text) {
			Server.theServ.MessageAll(Color.Green + text);
		}
		public static void Inform(Player p, string text) {
			p.PrintMessage(Color.DarkGreen + text);
		}
		public static void Log(string text) {
			Spacecraft.Log("[Game] " + text);
		}
	
		// === Callbacks ===
		
		// PlayerJoins: called when a player connects.
		virtual public void PlayerJoins(Player player) { }
		
		// PlayerQuits: called when a player disconnects.
		virtual public void PlayerQuits(Player player) { }
		
		// CanBuild: return whether the given player may build that type at that location.
		virtual public bool CanBuild(Player player, BlockPosition pos, Block type) { return true; }
		
		// PlayerBuilds: called when a player places or deletes a block.
		virtual public void PlayerBuilds(Player player, BlockPosition pos, Block type, Block oldType) { }
		
		// PlayerMessage: allows for adjusting of messages. Return "" to cancel or null to pass through.
		virtual public string PlayerMessage(Player player, string message) { return null; }
		
		// WorldTick: called every half-second, directly after physics updates.
		virtual public void WorldTick() { }
	}
}
