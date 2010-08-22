using System;
using System.Collections.Generic;

namespace spacecraft.AirshipWars
{
	public enum AirshipWarsTeam {
		Spectator, Red, Blue, Referee
	};
	
	public class AirshipWars : GameBase
	{
		List<Player> redTeam;
		List<Player> blueTeam;
		List<Player> referees;
		
		public AirshipWars ()
		{
			Log("Initializing Airship Wars...");
			Reset();
		}
		
		// Methods
		
		private void Reset()
		{
			redTeam = new List<Player>();
			blueTeam = new List<Player>();
			referees = new List<Player>();
		}
		
		private AirshipWarsTeam TeamOf(Player player)
		{
			if (referees.Contains(player)) return AirshipWarsTeam.Referee;
			else if (blueTeam.Contains(player)) return AirshipWarsTeam.Blue;
			else if (redTeam.Contains(player)) return AirshipWarsTeam.Red;
			else return AirshipWarsTeam.Spectator;
		}
		
		private void SetTeam(Player player, AirshipWarsTeam team)
		{
			referees.Remove(player);
			blueTeam.Remove(player);
			redTeam.Remove(player);
			switch (team) {
				case AirshipWarsTeam.Red:
					redTeam.Add(player);
					break;
				case AirshipWarsTeam.Blue:
					blueTeam.Add(player);
					break;
				case AirshipWarsTeam.Referee:
					referees.Add(player);
					break;
			}
		}
		
		private string TeamColor(AirshipWarsTeam team)
		{
			switch(team) {
				case AirshipWarsTeam.Blue: return Color.Blue;
				case AirshipWarsTeam.Red: return Color.Red;
				case AirshipWarsTeam.Referee: return Color.Green;
				case AirshipWarsTeam.Spectator: return Color.Gray;
				default: return Color.White;
			}
		}
		
		// Handlers
		
		override public void PlayerJoins(Player player)
		{
			Inform(player, "Use .red to switch to read team, .blue to switch to blue team");
		}
		
		override public void PlayerQuits(Player player)
		{
			SetTeam(player, AirshipWarsTeam.Spectator);
		}
		
		override public bool CanBuild(Player player, BlockPosition pos, Block type)
		{
			return (type != Block.Books);
		}
		
		override public void PlayerBuilds(Player player, BlockPosition pos, Block type, Block oldType)
		{
			if (type == Block.Air && oldType == Block.TNT) {
				Announce(player.name + " asploded!");
			}
		}
		
		override public string PlayerMessage(Player player, string message)
		{
			if (message == ".red") {
				SetTeam(player, AirshipWarsTeam.Red);
				return "";
			} else if (message == ".blue") {
				SetTeam(player, AirshipWarsTeam.Blue);
				return "";
			}
			return TeamColor(TeamOf(player)) + player.name + Color.White + ": " + message;
		}
		
		override public void WorldTick()
		{
			// Nothing yet
		}
	}
}
