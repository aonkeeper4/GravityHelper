// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityRefill")]
    public class GravityRefill : Entity
    {
        // properties
        public bool OneUse { get; }
        public int Charges { get; }
        public bool RefillsDash { get; }
        public bool RefillsStamina { get; }
        public float RespawnTime { get; }

        // components
        private readonly Sprite _sprite;
        private readonly Sprite _flash;
        private readonly Image _outline;
        private readonly Wiggler _wiggler;
        private readonly BloomPoint _bloom;
        private readonly VertexLight _light;
        private readonly SineWave _sine;

        // particles
        private readonly ParticleType p_shatter = Refill.P_Shatter;
        private readonly ParticleType p_regen = Refill.P_Regen;
        private readonly ParticleType p_glow = Refill.P_Glow;

        private Level _level;
        private float _respawnTimeRemaining;

        public GravityRefill(Vector2 position, int charges, bool oneUse, bool refillsDash, bool refillsStamina, float respawnTime)
            : base(position)
        {
            Charges = charges;
            OneUse = oneUse;
            RefillsDash = refillsDash;
            RefillsStamina = refillsStamina;
            RespawnTime = respawnTime;

            Collider = new Hitbox(16f, 16f, -8f, -8f);
            Depth = -100;

            var path = "objects/refill";

            // add components
            Add(new PlayerCollider(OnPlayer),
                _outline = new Image(GFX.Game[$"{path}/outline"]) {Visible = false},
                _sprite = new Sprite(GFX.Game, $"{path}/idle"),
                _flash = new Sprite(GFX.Game, $"{path}/flash") {OnFinish = _ => _flash.Visible = false},
                _wiggler = Wiggler.Create(1f, 4f, v => _sprite.Scale = _flash.Scale = Vector2.One * (float) (1.0 + (double) v * 0.2)),
                new MirrorReflection(),
                _bloom = new BloomPoint(0.8f, 16f),
                _light = new VertexLight(Color.White, 1f, 16, 48),
                _sine = new SineWave(0.6f, 0.0f));

            // configure components
            _outline.CenterOrigin();
            _sprite.AddLoop("idle", "", 0.1f);
            _sprite.Play("idle");
            _sprite.CenterOrigin();
            _flash.Add("flash", "", 0.05f);
            _flash.CenterOrigin();
            _sine.Randomize();

            updateY();
        }

        public GravityRefill(EntityData data, Vector2 offset)
            : this(data.Position + offset,
                data.Int("charges", 1),
                data.Bool("oneUse"),
                data.Bool("refillsDash", true),
                data.Bool("refillsStamina", true),
                data.Float("respawnTime", 2.5f))
        {
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            _level = SceneAs<Level>();
        }

        public override void Update()
        {
            base.Update();

            if (_respawnTimeRemaining > 0.0)
            {
                _respawnTimeRemaining -= Engine.DeltaTime;
                if (_respawnTimeRemaining <= 0.0)
                    respawn();
            }
            else if (Scene.OnInterval(0.1f))
                _level.ParticlesFG.Emit(p_glow, 1, Position, Vector2.One * 5f);

            updateY();

            _light.Alpha = Calc.Approach(_light.Alpha, _sprite.Visible ? 1f : 0.0f, 4f * Engine.DeltaTime);
            _bloom.Alpha = _light.Alpha * 0.8f;

            if (!Scene.OnInterval(2f) || !_sprite.Visible) return;

            _flash.Play("flash", true);
            _flash.Visible = true;
        }

        private void respawn()
        {
            if (Collidable) return;
            Collidable = true;

            _sprite.Visible = true;
            _outline.Visible = false;
            Depth = -100;
            _wiggler.Start();
            Audio.Play("event:/game/general/diamond_return", Position);
            _level.ParticlesFG.Emit(p_regen, 16, Position, Vector2.One * 2f);
        }

        private void updateY() => _flash.Y = _sprite.Y = _bloom.Y = _sine.Value * 2f;

        public override void Render()
        {
            if (_sprite.Visible)
                _sprite.DrawOutline();
            base.Render();
        }

        private void OnPlayer(Player player)
        {
            bool canUse = RefillsDash && player.Dashes < player.MaxDashes ||
                          RefillsStamina && player.Stamina < 20 ||
                          GravityHelperModule.Instance.GravityRefillCharges < Charges;

            if (!canUse) return;

            if (RefillsDash) player.RefillDash();
            if (RefillsStamina) player.RefillStamina();
            GravityHelperModule.Instance.GravityRefillCharges = Charges;

            Audio.Play("event:/game/general/diamond_touch", Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            Collidable = false;
            Add(new Coroutine(refillRoutine(player)));
            _respawnTimeRemaining = RespawnTime;
        }

        private IEnumerator refillRoutine(Player player)
        {
            GravityRefill refill = this;
            Celeste.Freeze(0.05f);
            yield return null;

            refill._level.Shake();
            refill._sprite.Visible = refill._flash.Visible = false;
            if (!refill.OneUse)
                refill._outline.Visible = true;
            refill.Depth = 8999;
            yield return 0.05f;

            float direction = player.Speed.Angle();
            refill._level.ParticlesFG.Emit(refill.p_shatter, 5, refill.Position, Vector2.One * 4f, direction - (float)Math.PI / 2f);
            refill._level.ParticlesFG.Emit(refill.p_shatter, 5, refill.Position, Vector2.One * 4f, direction + (float)Math.PI / 2f);
            SlashFx.Burst(refill.Position, direction);

            if (refill.OneUse)
                refill.RemoveSelf();
        }
    }
}
