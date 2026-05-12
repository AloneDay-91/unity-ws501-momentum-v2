// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 4.0.25
// 

using Colyseus.Schema;
#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

public partial class GameState : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public GameState() { }
	[Type(0, "map", typeof(MapSchema<PlayerState>))]
	public MapSchema<PlayerState> players = null;

	[Type(1, "string")]
	public string status = default(string);

	[Type(2, "number")]
	public float countdownRemaining = default(float);

	[Type(3, "number")]
	public float elapsedTime = default(float);

	[Type(4, "string")]
	public string winnerSessionId = default(string);

	[Type(5, "string")]
	public string mapName = default(string);
}

