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

public partial class PlayerState : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public PlayerState() { }
	[Type(0, "number")]
	public float playerNumber = default(float);

	[Type(1, "string")]
	public string pseudo = default(string);

	[Type(2, "number")]
	public float posX = default(float);

	[Type(3, "number")]
	public float posY = default(float);

	[Type(4, "number")]
	public float posZ = default(float);

	[Type(5, "number")]
	public float velX = default(float);

	[Type(6, "number")]
	public float velY = default(float);

	[Type(7, "number")]
	public float velZ = default(float);

	[Type(8, "number")]
	public float rotY = default(float);

	[Type(9, "boolean")]
	public bool isGrounded = default(bool);

	[Type(10, "boolean")]
	public bool isSliding = default(bool);

	[Type(11, "boolean")]
	public bool isStunned = default(bool);

	[Type(12, "number")]
	public float horizontalInput = default(float);

	[Type(13, "number")]
	public float score = default(float);

	[Type(14, "number")]
	public float distanceTraveled = default(float);

	[Type(15, "number")]
	public float survivalTime = default(float);

	[Type(16, "number")]
	public float collectibles = default(float);

	[Type(17, "boolean")]
	public bool hasFinished = default(bool);

	[Type(18, "boolean")]
	public bool isAlive = default(bool);

	[Type(19, "boolean")]
	public bool isManuallySliding = default(bool);

	[Type(20, "boolean")]
	public bool isLandingHard = default(bool);

	[Type(21, "number")]
	public float actionSeq = default(float);

	[Type(22, "number")]
	public float actionId = default(float);
}

