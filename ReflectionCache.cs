using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper
{
    // ReSharper disable InconsistentNaming
    internal static class ReflectionCache
    {
        public static Type GetTypeByName(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Reverse().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);

        public static Type GetModdedTypeByName(string module, string name)
        {
            var mod = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == module);
            return mod?.GetType().Assembly.GetType(name);
        }

        #region Player

        public static readonly FieldInfo Player_DashCooldownTimer = typeof(Player).GetField("dashCooldownTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Player_DuckHitbox = typeof(Player).GetField("duckHitbox", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Player_DuckHurtbox = typeof(Player).GetField("duckHurtbox", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Player_DuckingLightOffset = typeof(Player).GetField("duckingLightOffset", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Player_LastClimbMove = typeof (Player).GetField("lastClimbMove", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Player_NormalHitbox = typeof(Player).GetField("normalHitbox", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Player_NormalHurtbox = typeof(Player).GetField("normalHurtbox", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Player_NormalLightOffset = typeof(Player).GetField("normalLightOffset", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Player_OnGround = typeof(Player).GetField("onGround", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Player_StarFlyHitbox = typeof(Player).GetField("starFlyHitbox", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Player_StarFlyHurtbox = typeof(Player).GetField("starFlyHurtbox", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Player_VarJumpSpeed = typeof(Player).GetField("varJumpSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Player_VarJumpTimer = typeof(Player).GetField("varJumpTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo Player_DashCoroutine = typeof(Player).GetMethod("DashCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo Player_DashCorrectCheck = typeof(Player).GetMethod("DashCorrectCheck", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo Player_JumpThruBoostBlockedCheck = typeof(Player).GetMethod("JumpThruBoostBlockedCheck", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo Player_OrigUpdateSprite = typeof(Player).GetMethod("orig_UpdateSprite", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo Player_OrigWallJump = typeof(Player).GetMethod("orig_WallJump", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo Player_OrigUpdate = typeof(Player).GetMethod(nameof(Player.orig_Update));

        #endregion

        #region Misc

        public static readonly FieldInfo Actor_MovementCounter = typeof (Actor).GetField("movementCounter", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Bumper_RespawnTimer = typeof(Bumper).GetField("respawnTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Spring_PlayerCanUse = typeof(Spring).GetField("playerCanUse", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Sprite_Animations = typeof(Sprite).GetField("animations", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo Level_NextLevel = typeof(Level).GetMethod("NextLevel", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo Level_OrigTransitionRoutine = typeof(Level).GetMethod("orig_TransitionRoutine", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo Lookout_LookRoutine = typeof(Lookout).GetMethod("Lookout_LookRoutine", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo PlayerDeadBody_DeathRoutine = typeof(PlayerDeadBody).GetMethod("DeathRoutine", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo SolidTiles_SurfaceSoundIndexAt = typeof(SolidTiles).GetMethod("SurfaceSoundIndexAt", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo VirtualJoystick_SetValue = typeof(VirtualJoystick).GetProperty("Value")?.GetSetMethod(true);
        public static readonly object[] VirtualJoystick_SetValue_Params = { Vector2.Zero };

        #endregion

        #region Optional Dependencies

        private static Type fancyFallingBlockType;

        public static Type FancyFallingBlockType => fancyFallingBlockType ??=
            GetModdedTypeByName("FancyTileEntities", "Celeste.Mod.FancyTileEntities.FancyFallingBlock");

        private static MethodInfo fancyFallingBlock_SurfaceSoundIndexAt;

        public static MethodInfo FancyFallingBlock_SurfaceSoundIndexAt => fancyFallingBlock_SurfaceSoundIndexAt ??=
            FancyFallingBlockType?.GetMethod("SurfaceSoundIndexAt", BindingFlags.Instance | BindingFlags.NonPublic);

        #endregion

        #region Reflection Extensions

        public static bool GetOnGround(this Player player) => (bool)Player_OnGround.GetValue(player);
        public static int GetLastClimbMove(this Player player) => (int)Player_LastClimbMove.GetValue(player);
        public static void SetLastClimbMove(this Player player, int value) => Player_LastClimbMove.SetValue(player, value);
        public static Hitbox GetNormalHitbox(this Player player) => (Hitbox) Player_NormalHitbox.GetValue(player);
        public static Hitbox GetNormalHurtbox(this Player player) => (Hitbox) Player_NormalHurtbox.GetValue(player);
        public static Hitbox GetDuckHitbox(this Player player) => (Hitbox) Player_DuckHitbox.GetValue(player);
        public static Hitbox GetDuckHurtbox(this Player player) => (Hitbox) Player_DuckHurtbox.GetValue(player);
        public static Hitbox GetStarFlyHitbox(this Player player) => (Hitbox) Player_StarFlyHitbox.GetValue(player);
        public static Hitbox GetStarFlyHurtbox(this Player player) => (Hitbox) Player_StarFlyHurtbox.GetValue(player);
        public static Dictionary<string, Sprite.Animation> GetAnimations(this Sprite sprite) => (Dictionary<string, Sprite.Animation>) Sprite_Animations.GetValue(sprite);
        public static void SetNormalLightOffset(this Player player, Vector2 value) => Player_NormalLightOffset.SetValue(player, value);
        public static void SetDuckingLightOffset(this Player player, Vector2 value) => Player_DuckingLightOffset.SetValue(player, value);
        public static void SetVarJumpTimer(this Player player, float value) => Player_VarJumpTimer.SetValue(player, value);
        public static void SetVarJumpSpeed(this Player player, float value) => Player_VarJumpSpeed.SetValue(player, value);
        public static void SetDashCooldownTimer(this Player player, float value) => Player_DashCooldownTimer.SetValue(player, value);
        public static bool GetPlayerCanUse(this Spring spring) => (bool) Spring_PlayerCanUse.GetValue(spring);
        public static void CallNextLevel(this Level level, Vector2 at, Vector2 dir) => Level_NextLevel.Invoke(level, new object[]{at, dir});
        public static int CallSurfaceSoundIndexAt(this SolidTiles solidTiles, Vector2 readPosition) => (int) SolidTiles_SurfaceSoundIndexAt.Invoke(solidTiles, new object[] {readPosition});
        public static bool CallDashCorrectCheck(this Player player, Vector2 add) => (bool)Player_DashCorrectCheck.Invoke(player, new object[] {add});
        public static bool CallJumpThruBoostBlockedCheck(this Player player) => (bool)Player_JumpThruBoostBlockedCheck.Invoke(player, new object[0]);
        public static void SetMovementCounter(this Actor actor, Vector2 value) => Actor_MovementCounter.SetValue(actor, value);
        public static Vector2 GetMovementCounter(this Actor actor) => (Vector2) Actor_MovementCounter.GetValue(actor);

        public static int CallFancyFallingBlockSurfaceSoundIndexAt(this FallingBlock fallingBlock, Vector2 readPosition)
        {
            if (FancyFallingBlock_SurfaceSoundIndexAt == null) return -1;
            return (int) FancyFallingBlock_SurfaceSoundIndexAt.Invoke(fallingBlock, new object[] {readPosition});
        }

        public static void SetValue(this VirtualJoystick virtualJoystick, Vector2 value)
        {
            VirtualJoystick_SetValue_Params[0] = value;
            VirtualJoystick_SetValue.Invoke(virtualJoystick, VirtualJoystick_SetValue_Params);
        }

        #endregion
    }
}
