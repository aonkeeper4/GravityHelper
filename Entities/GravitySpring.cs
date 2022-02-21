// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity(
        "GravityHelper/GravitySpringFloor = LoadFloor",
        "GravityHelper/GravitySpringCeiling = LoadCeiling",
        "GravityHelper/GravitySpringWallLeft = LoadWallLeft",
        "GravityHelper/GravitySpringWallRight = LoadWallRight")]
    public class GravitySpring : Entity
    {
        // ReSharper disable once UnusedMember.Global
        public static bool RequiresHooks(EntityData data) => data.Enum<GravityType>("gravityType").RequiresHooks();

        public Color DisabledColor = Color.White;
        public bool VisibleWhenDisabled;

        public bool PlayerCanUse { get; }
        public Orientations Orientation { get; }
        public GravityType GravityType { get; }
        public float Cooldown { get; }

        private string getAnimId(string id) => GravityType switch
        {
            GravityType.None => $"none_{id}",
            GravityType.Normal => $"normal_{id}",
            GravityType.Inverted => $"invert_{id}",
            GravityType.Toggle => $"toggle_{id}",
            _ => id,
        };

        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        private Sprite _sprite;
        private Wiggler _wiggler;
        private StaticMover _staticMover;
        private float _cooldownRemaining;

        public static Entity LoadFloor(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new GravitySpring(entityData, offset, Orientations.Floor);

        public static Entity LoadCeiling(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new GravitySpring(entityData, offset, Orientations.Ceiling);

        public static Entity LoadWallLeft(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new GravitySpring(entityData, offset, Orientations.WallLeft);

        public static Entity LoadWallRight(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new GravitySpring(entityData, offset, Orientations.WallRight);

        public GravitySpring(EntityData data, Vector2 offset, Orientations orientation)
            : base(data.Position + offset)
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            PlayerCanUse = data.Bool("playerCanUse", true);
            GravityType = data.Enum<GravityType>("gravityType");
            Cooldown = data.Float("cooldown", 1f);

            Orientation = orientation;

            Add(new PlayerCollider(OnCollide));
            Add(_sprite = GFX.SpriteBank.Create("gravitySpring"));
            _sprite.Play(getAnimId("idle"));

            switch (Orientation)
            {
                case Orientations.Floor:
                    _sprite.Rotation = 0;
                    Collider = new Hitbox(16f, 6f, -8f, -6f);
                    break;

                case Orientations.WallLeft:
                    _sprite.Rotation = (float) Math.PI / 2f;
                    Collider = new Hitbox(6, 16f, 0f, -8f);
                    break;

                case Orientations.WallRight:
                    _sprite.Rotation = (float) -Math.PI / 2f;
                    Collider = new Hitbox(6, 16f, -6f, -8f);
                    break;

                case Orientations.Ceiling:
                    _sprite.Rotation = (float) Math.PI;
                    Collider = new Hitbox(16f, 6f, -8f, 0f);
                    break;
            }

            Depth = Depths.Above - 1;

            Add(_staticMover = new StaticMover
            {
                OnAttach = p => Depth = p.Depth + 1,
                SolidChecker = Orientation switch
                {
                    Orientations.WallLeft => s => CollideCheck(s, Position - Vector2.UnitX),
                    Orientations.WallRight => s => CollideCheck(s, Position + Vector2.UnitX),
                    Orientations.Ceiling => s => CollideCheck(s, Position - Vector2.UnitY),
                    _ => s => CollideCheck(s, Position + Vector2.UnitY),
                },
                JumpThruChecker = Orientation switch
                {
                    Orientations.WallLeft => jt => CollideCheck(jt, Position - Vector2.UnitX),
                    Orientations.WallRight => jt => CollideCheck(jt, Position + Vector2.UnitX),
                    Orientations.Ceiling => jt => CollideCheck(jt, Position - Vector2.UnitY),
                    _ => jt => CollideCheck(jt, Position + Vector2.UnitY),
                },
                OnShake = amount => _sprite.Position += amount,
                OnEnable = OnEnable,
                OnDisable = OnDisable,
            });

            Add(_wiggler = Wiggler.Create(1f, 4f, v => _sprite.Scale.Y = 1 + v * 0.2f));
        }

        private void OnEnable()
        {
            Visible = Collidable = true;
            _sprite.Color = Color.White;
            _sprite.Play(getAnimId("idle"));
        }

        private void OnDisable()
        {
            Collidable = false;
            if (VisibleWhenDisabled)
            {
                _sprite.Play("disabled");
                _sprite.Color = DisabledColor;
            }
            else
                Visible = false;
        }

        public override void Update()
        {
            base.Update();

            if (_cooldownRemaining > 0)
            {
                _cooldownRemaining = Math.Max(0, _cooldownRemaining - Engine.DeltaTime);
                // TODO: update sprite to show cooldown
            }
        }

        private void OnCollide(Player player)
        {
            // ignore spring if dream dashing, if we're not allowed to use it, or if we're on cooldown
            if (player.StateMachine.State == Player.StDreamDash || !PlayerCanUse)
                return;

            // ignore spring if moving away
            var realY = GravityHelperModule.ShouldInvertPlayer ? -player.Speed.Y : player.Speed.Y;
            switch (Orientation)
            {
                case Orientations.Floor when realY < 0:
                case Orientations.Ceiling when realY > 0:
                case Orientations.WallLeft when player.Speed.X > 240:
                case Orientations.WallRight when player.Speed.X < -240:
                    return;
            }

            // set gravity and cooldown if not on cooldown
            if (GravityType != GravityType.None && _cooldownRemaining == 0f)
            {
                GravityHelperModule.PlayerComponent?.SetGravity(GravityType);
                _cooldownRemaining = Cooldown;
                // TODO: update sprite to show cooldown
            }

            // boing!
            bounceAnimate();

            // bounce player away
            switch (Orientation)
            {
                case Orientations.Floor:
                    if (GravityHelperModule.ShouldInvertPlayer)
                        InvertedSuperBounce(player, Top);
                    else
                        player.SuperBounce(Top);
                    break;

                case Orientations.Ceiling:
                    if (!GravityHelperModule.ShouldInvertPlayer)
                        InvertedSuperBounce(player, Bottom);
                    else
                        player.SuperBounce(Bottom);
                    break;

                case Orientations.WallLeft:
                    player.SideBounce(1, CenterRight.X, CenterRight.Y);
                    break;

                case Orientations.WallRight:
                    player.SideBounce(-1, CenterLeft.X, CenterLeft.Y);
                    break;
            }
        }

        private void bounceAnimate()
        {
            Audio.Play("event:/game/general/spring", BottomCenter);
            _staticMover.TriggerPlatform();
            _sprite.Play(getAnimId("bounce"), true);
            _wiggler.Start();
        }

        public override void Render()
        {
            if (Collidable)
                _sprite.DrawOutline();
            base.Render();
        }

        public enum Orientations
        {
            Floor,
            WallLeft,
            WallRight,
            Ceiling,
        }

        public static void InvertedSuperBounce(Player self, float fromY)
        {
            if (self.StateMachine.State == Player.StBoost && self.CurrentBooster != null)
            {
                self.CurrentBooster.PlayerReleased();
                self.CurrentBooster = null;
            }

            Collider collider = self.Collider;
            self.Collider = self.GetNormalHitbox();
            self.MoveV(GravityHelperModule.ShouldInvertPlayer ? self.Bottom - fromY : fromY - self.Top);
            if (!self.Inventory.NoRefills)
                self.RefillDash();
            self.RefillStamina();

            using (var data = new DynData<Player>(self))
            {
                data["jumpGraceTimer"] = 0f;
                data["varJumpTimer"] = 0f;
                data["dashAttackTimer"] = 0.0f;
                data["gliderBoostTimer"] = 0.0f;
                data["wallSlideTimer"] = 1.2f;
                data["wallBoostTimer"] = 0.0f;
                data["varJumpSpeed"] = 0f;
                data["launched"] = false;
            }

            self.StateMachine.State = Player.StNormal;
            self.AutoJump = false;
            self.AutoJumpTimer = 0.0f;
            self.Speed.X = 0.0f;
            self.Speed.Y = 185f;

            var level = self.SceneAs<Level>();
            level?.DirectionalShake(GravityHelperModule.ShouldInvertPlayer ? -Vector2.UnitY : Vector2.UnitY, 0.1f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            self.Sprite.Scale = new Vector2(0.5f, 1.5f);
            self.Collider = collider;
        }
    }
}
