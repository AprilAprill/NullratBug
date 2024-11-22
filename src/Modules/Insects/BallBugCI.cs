using DevInterface;
using Mono.Cecil.Cil;
using UnityEngine;
using RWCustom;
using Random = UnityEngine.Random;
namespace RegionKit.Modules.Insects
{
	internal static class BallBugCI
	{
		internal static void Apply()
		{
			On.InsectCoordinator.CreateInsect += InsectCoordinator_CreateInsect;
			On.InsectCoordinator.TileLegalForInsect += InsectCoordinator_TileLegalForInsect;
			On.InsectCoordinator.EffectSpawnChanceForInsect += InsectCoordinator_EffectSpawnChanceForInsect;
			On.InsectCoordinator.RoomEffectToInsectType += InsectCoordinator_RoomEffectToInsectType;
			_CommonHooks.PostRoomLoad += PostRoomLoad;
		}

		internal static void Undo()
		{
			On.InsectCoordinator.CreateInsect -= InsectCoordinator_CreateInsect;
			On.InsectCoordinator.TileLegalForInsect -= InsectCoordinator_TileLegalForInsect;
			On.InsectCoordinator.EffectSpawnChanceForInsect -= InsectCoordinator_EffectSpawnChanceForInsect;
			On.InsectCoordinator.RoomEffectToInsectType -= InsectCoordinator_RoomEffectToInsectType;
			_CommonHooks.PostRoomLoad -= PostRoomLoad;
		}
		
		private static bool InsectCoordinator_TileLegalForInsect(On.InsectCoordinator.orig_TileLegalForInsect orig, CosmeticInsect.Type type, Room room, Vector2 testPos)
		{
			bool aiNoNarrow = !room.readyForAI || !room.aimap.getAItile(testPos).narrowSpace;
			return (type == _Enums.BallBugA || type == _Enums.BallBugB) ? !room.GetTile(testPos).AnyWater && aiNoNarrow : orig(type, room, testPos);
		}
		private static void PostRoomLoad(Room self)
		{
			for (int i = 0; i < self.roomSettings.effects.Count; i++)
			{
				if (self.roomSettings.effects[i].type == _Enums.BallBugsA || self.roomSettings.effects[i].type == _Enums.BallBugsB)
				{
					if (self.insectCoordinator == null)
					{
						self.insectCoordinator = new InsectCoordinator(self);
						self.AddObject(self.insectCoordinator);
					}
					self.insectCoordinator.AddEffect(self.roomSettings.effects[i]);
				}
			}
		}
		private static CosmeticInsect.Type InsectCoordinator_RoomEffectToInsectType(On.InsectCoordinator.orig_RoomEffectToInsectType orig, RoomSettings.RoomEffect.Type type)
		{
			if (type == _Enums.BallBugsA)
				return _Enums.BallBugA;
			else if (type == _Enums.BallBugsB)
				return _Enums.BallBugB;
			else
				return orig(type);
		}
		private static void InsectCoordinator_CreateInsect(On.InsectCoordinator.orig_CreateInsect orig, InsectCoordinator self, CosmeticInsect.Type type, Vector2 pos, InsectCoordinator.Swarm swarm)
		{
			if (!InsectCoordinator.TileLegalForInsect(type, self.room, pos))
			{
				return;
			}
			if (self.room.world.rainCycle.TimeUntilRain < Random.Range(1200, 1600))
			{
				return;
			}

			if (type == _Enums.BallBugA || type == _Enums.BallBugB)
			{
				CosmeticInsect insect = new BallBug(self.room, pos, type == _Enums.BallBugA);

				self.allInsects.Add(insect);
				if (swarm != null)
				{
					swarm.members.Add(insect);
					insect.mySwarm = swarm;
				}
				self.room.AddObject(insect);
			}
			else
			{
				orig(self, type, pos, swarm);
			}
		}
		private static bool InsectCoordinator_EffectSpawnChanceForInsect(On.InsectCoordinator.orig_EffectSpawnChanceForInsect orig, CosmeticInsect.Type type, Room room, Vector2 testPos, float effectAmount)
		{
			if (type == _Enums.BallBugA || type == _Enums.BallBugB)
			{
				return Mathf.Pow(Random.value, 1f - effectAmount) > (room.readyForAI ? room.aimap.getTerrainProximity(testPos) : 5) * 0.05f;
			}
			return orig(type, room, testPos, effectAmount);
		}
		
	}
	
	/// <summary>
	/// By April
	/// Ball-type creature.
	/// </summary>
	public class BallBug : CosmeticInsect
	{
		public Vector2 rot;
		public Vector2? sitPos;
		public Vector2 pos;
		public Vector2 lastPos;
		public Vector2 vel;
		public Vector2 wallDir;
		public int timeUntilMoving;
		public bool isCollide;
		public bool A;
		public bool wandering;
		private float colorFac;
		public BallBug(Room room, Vector2 pos, bool A) : base(room, pos, A ? _Enums.BallBugA : _Enums.BallBugB)
		{
			
			this.pos = pos;
			this.A = A;
			wandering = true;
			//moves bugs to ground
			while (!room.GetTile(this.pos).Solid)
			{
				this.pos.y -= 20f;
				if(this.pos.y < 0f)
				{
					break;
				}
			}
			this.pos = room.MiddleOfTile(this.pos);
			this.pos.y += 20f;
			// Random palette color
			colorFac = Mathf.Pow(Random.value, 2);
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			lastPos = pos;
			//move
			pos += vel;
			//chance to stop every frame
			
			//gravity
			if (!isCollide)
			{
				vel.y -= room.gravity;
			}
			wallDir = Custom.RNV() * 0.1f;
			Vector2 reflection = Vector2.zero;
			
			
			if(room.GetTile(new Vector2(this.pos.x,pos.y-20f)).Solid)
			{
				vel *= 0.8f;
				if (Random.Range(0, 300) == 0)
				{
					vel.y += Random.Range(5, 20);
				}
			}
			isCollide = false;
		}

		public override void Act()
		{
			base.Act();
			//UnityEngine.Debug.Log("IM ACTING :STEAMHAPPY:");
		}

		public override void WallCollision(IntVector2 dir, bool first)
		{
			base.WallCollision(dir, first);
			isCollide = true;
			pos -= dir.ToVector2();
		}
		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			base.InitiateSprites(sLeaser, rCam);
			sLeaser.sprites = new FSprite[1];
			sLeaser.sprites[0] = new FSprite("Circle20", true);
			AddToContainer(sLeaser, rCam, null);
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
			sLeaser.sprites[0].SetPosition(Vector2.Lerp(lastPos, pos, timeStacker)-camPos);
			
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			base.ApplyPalette(sLeaser, rCam, palette);
		//	Color palCol = palette.texture.GetPixel((int)Mathf.Lerp(14f, 20f, colorFac), 2);
		//	Color effCol = palette.texture.GetPixel(30, A ? 4 : 2);
			//Color blackColor = palette.blackColor;
			//Color bodyColor = Color.Lerp(Color.Lerp(palCol, effCol, 0.925f), palette.fogColor, 0.15f * palette.fogAmount + 0.2f * palette.darkness);
			//sLeaser.sprites[0].color = bodyColor;
		}
		
	}
}
