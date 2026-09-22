// s&Doom modification: 2026-09-22, opt-in native Clink test encounter integration.
// s&Doom modification: 2026-09-22, sector riders and ordinary environmental death response.
// s&Doom modification: 2026-09-22, scenery support and swept vertical collision.
//
// Copyright(C) 1993-1996 Id Software, Inc.
// Copyright(C) 1993-2008 Raven Software
// Copyright(C) 2005-2014 Simon Howard
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//

// P_user.c

// Adapted 2026-09-18 for the shared ManagedDoom engine.
// Reference: Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
// Movement/environment adapted from p_user.c, p_mobj.c, p_spec.c and p_telept.c.
using System;
using System.Collections.Generic;
using System.Linq;
namespace ManagedDoom
{
    /// <summary>Single-player navigation checkpoint; combat and campaign runtime remain disabled.</summary>
    public sealed partial class HereticWorldSession
    {
        private readonly World world;
        private readonly List<HereticMapActor> actors = new();
        public IReadOnlyList<HereticMapActor> Actors { get; }
        public int UnsupportedMapThings { get; private set; }
        public int UnknownMapThings { get; private set; }
        private readonly List<(LineDef line, int position, int texture, int until)> buttons = new();
        private bool useDown;
        private int tic;
        public HereticPlayerState State { get; } = new();
        public Player Camera { get; }
        public Mobj Body => Camera.Mobj;
        public World World => world;
        public bool ExitRequested { get; private set; }
        public bool SecretExitRequested { get; private set; }
        public HereticWorldSession(GameContent content, int episode = 1, int map = 1, GameSkill skill = GameSkill.Medium)
        {
            if ((uint)skill > (uint)GameSkill.Nightmare) throw new ArgumentOutOfRangeException(nameof(skill));
            Actors = actors.AsReadOnly();
            world = World.CreateGeometryPreview(content, episode, map);
            world.EnableHereticGeometryInteractions();
            world.HereticSession = this;
            var start = world.Map.Things.FirstOrDefault(t => t.Type == 1)
                ?? throw new InvalidOperationException("Heretic navigation requires a player-one start.");
            Camera = world.Options.Players[0];
            Camera.Mobj = new Mobj(world) { X = start.X, Y = start.Y, Angle = start.Angle,
                Radius = Fixed.FromInt(16), Height = Fixed.FromInt(56), Health = 100,
                Flags = MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.DropOff | MobjFlags.NoSector,
                Player = Camera };
            world.ThingMovement.SetThingPosition(Body);
            Body.FloorZ = Body.Subsector.Sector.FloorHeight;
            Body.CeilingZ = Body.Subsector.Sector.CeilingHeight;
            Body.Z = Body.FloorZ;
            Camera.ViewHeight = Fixed.FromInt(41);
            Camera.ViewZ = Body.Z + Camera.ViewHeight;
            foreach (var thing in world.Map.Things)
            {
                var decision = HereticMapSpawns.Decide(thing, skill);
                if (decision.Disposition == HereticSpawnDisposition.Unsupported) UnsupportedMapThings++;
                if (decision.Disposition == HereticSpawnDisposition.Unknown) UnknownMapThings++;
                if (decision.Disposition == HereticSpawnDisposition.Spawn) SpawnMapActor(thing, decision.Type);
            }
            foreach (var sector in world.Map.Sectors)
            {
                switch ((int)sector.Special)
                {
                    case 1: world.LightingChange.SpawnLightFlash(sector); break;
                    case 2: world.LightingChange.SpawnStrobeFlash(sector, StrobeFlash.FastDark, false); break;
                    case 3: world.LightingChange.SpawnStrobeFlash(sector, StrobeFlash.SlowDark, false); break;
                    case 4: world.LightingChange.SpawnStrobeFlash(sector, StrobeFlash.FastDark, false); sector.Special = (SectorSpecial)4; break;
                    case 8: world.LightingChange.SpawnGlowingLight(sector); break;
                    case 10: world.SectorAction.SpawnDoorCloseIn30(sector); break;
                    case 12: world.LightingChange.SpawnStrobeFlash(sector, StrobeFlash.SlowDark, true); break;
                    case 13: world.LightingChange.SpawnStrobeFlash(sector, StrobeFlash.FastDark, true); break;
                    case 14: world.SectorAction.SpawnDoorRaiseIn5Mins(sector); break;
                }
            }
        }
        // Artifact acquisition/use is wired in the inventory chunk. This entry point
        // supplies the flight power to the isolated mechanics fixture/editor toggle.
        public void GrantFlight(int ticks) { State.FlightTics = Math.Max(0, ticks); }
        public void Tick(HereticCommand command)
        {
            if (ExitRequested) return;
            if (State.Health <= 0) { TickDead(); return; }
            Camera.UpdateFrameInterpolationInfo();
            Body.UpdateFrameInterpolationInfo();
            foreach (var sector in world.Map.Sectors) sector.UpdateFrameInterpolationInfo();
            world.SetPreviewTime(tic);
            var onGround = Body.Z <= Body.FloorZ;
            if (Body.ReactionTime > 0) Body.ReactionTime--;
            else
            {
                Body.Angle += new Angle(unchecked((uint)(command.Turn << 16)));
                if (onGround || StandingOnActor || State.Flying)
                {
                    Thrust(Body.Angle, new Fixed(Math.Clamp((int)command.Forward, -50, 50) * 2048));
                    Thrust(Body.Angle - Angle.Ang90, new Fixed(Math.Clamp((int)command.Side, -40, 40) * 2048));
                }
                LookAndFly(command);
            }
            EnvironmentForces(onGround);
            if (State.Health <= 0) { TickDead(); return; }
            MoveHorizontal(command);
            MoveVertical();
            if (command.Use && !useDown) Use();
            useDown = command.Use;
            PickupKeys();
            foreach (var actor in actors) actor.Tick();
            if (command.SelectWeapon is HereticWeapon selected) GoldWand?.SelectWeapon(selected);
            TickClinkTest(State.Health > 0 && command.TestAttack);
            world.Thinkers.Run();
            UpdateSwitchesAndScroll();
            UpdateView();
            if (State.FlightTics > 0 && --State.FlightTics == 0) State.Flying = false;
            tic++;
        }
        private void Thrust(Angle angle, Fixed amount)
        {
            if (!(State.Flying && Body.Z > Body.FloorZ) && (int)Body.Subsector.Sector.Special == 15) amount >>= 2;
            Body.MomX += amount * Trig.Cos(angle);
            Body.MomY += amount * Trig.Sin(angle);
        }
        private void LookAndFly(HereticCommand command)
        {
            if (command.CenterLook) State.Centering = true;
            else
            {
                var look = State.LookDirection + Math.Clamp((int)command.Look, -7, 7) * 5;
                if (look >= -110 && look <= 90) State.LookDirection = look;
            }
            if (State.Centering)
            {
                State.LookDirection -= Math.Sign(State.LookDirection) * 8;
                if (Math.Abs(State.LookDirection) < 8) { State.LookDirection = 0; State.Centering = false; }
            }
            if (command.Land) { State.Flying = false; State.FlyHeight = 0; }
            else if (command.Fly != 0 && State.FlightTics > 0)
            {
                State.Flying = true;
                State.FlyHeight = Math.Clamp((int)command.Fly, -7, 7) * 2;
            }
            if (State.Flying) { Body.MomZ = Fixed.FromInt(State.FlyHeight); State.FlyHeight /= 2; }
        }
        private void MoveHorizontal(HereticCommand command)
        {
            Body.MomX = new Fixed(Math.Clamp(Body.MomX.Data, -30 * Fixed.FracUnit, 30 * Fixed.FracUnit));
            Body.MomY = new Fixed(Math.Clamp(Body.MomY.Data, -30 * Fixed.FracUnit, 30 * Fixed.FracUnit));
            var x = Body.MomX; var y = Body.MomY;
            do
            {
                var split = Math.Abs(x.Data) > 15 * Fixed.FracUnit || Math.Abs(y.Data) > 15 * Fixed.FracUnit;
                var dx = split ? x / 2 : x; var dy = split ? y / 2 : y;
                x -= dx; y -= dy;
                if (!world.ThingMovement.TryMove(Body, Body.X + dx, Body.Y + dy)) world.ThingMovement.SlideMove(Body);
            } while (x != Fixed.Zero || y != Fixed.Zero);
            if (!State.Flying && Body.Z > Body.FloorZ && !StandingOnActor) return;
            if (Math.Abs(Body.MomX.Data) < 0x1000 && Math.Abs(Body.MomY.Data) < 0x1000 && command.Forward == 0 && command.Side == 0)
                Body.MomX = Body.MomY = Fixed.Zero;
            else
            {
                var friction = new Fixed(State.Flying && Body.Z > Body.FloorZ ? 0xeb00 : (int)Body.Subsector.Sector.Special == 15 ? 0xf900 : 0xe800);
                Body.MomX *= friction; Body.MomY *= friction;
            }
        }
        private bool OverlapsSolidActor(Mobj actor) => (actor.Flags & MobjFlags.Solid) != 0 &&
            Fixed.Abs(Body.X - actor.X) < Body.Radius + actor.Radius &&
            Fixed.Abs(Body.Y - actor.Y) < Body.Radius + actor.Radius;
        internal Mobj SupportingActor
        {
            get
            {
                foreach (var actor in actors)
                    if (OverlapsSolidActor(actor.Body) && Body.Z == actor.Body.Z + actor.Body.Height) return actor.Body;
                return null;
            }
        }
        public bool StandingOnActor => SupportingActor != null;
        private void MoveVertical()
        {
            if (Body.Z < Body.FloorZ)
            {
                Camera.ViewHeight -= Body.FloorZ - Body.Z;
                Camera.DeltaViewHeight = (Fixed.FromInt(41) - Camera.ViewHeight) / 8;
            }
            var support = Body.FloorZ;
            var ceiling = Body.CeilingZ;
            foreach (var actor in actors)
            {
                if (!OverlapsSolidActor(actor.Body)) continue;
                var top = actor.Body.Z + actor.Body.Height;
                if (Body.Z >= top && top > support) support = top;
                if (Body.Z + Body.Height <= actor.Body.Z && actor.Body.Z < ceiling) ceiling = actor.Body.Z;
            }
            Body.Z += Body.MomZ;
            if (State.Flying && Body.Z > Body.FloorZ && (tic & 2) != 0)
                Body.Z += Trig.Sin(new Angle((uint)(((409L * tic >> 2) & 8191) << 19)));
            if (Body.Z <= support)
            {
                Body.Z = support;
                if (!State.Flying && Body.MomZ < Fixed.FromInt(-8))
                {
                    Camera.DeltaViewHeight = Body.MomZ >> 3;
                    State.Centering = true;
                }
                if (Body.MomZ < Fixed.Zero) Body.MomZ = Fixed.Zero;
            }
            else if (!State.Flying) Body.MomZ -= Body.MomZ == Fixed.Zero ? Fixed.FromInt(2) : Fixed.One;
            if (Body.Z + Body.Height > ceiling)
            { Body.Z = ceiling - Body.Height; if (Body.MomZ > Fixed.Zero) Body.MomZ = Fixed.Zero; }
        }
        private void UpdateView()
        {
            Camera.ViewHeight += Camera.DeltaViewHeight;
            if (Camera.ViewHeight > Fixed.FromInt(41)) { Camera.ViewHeight = Fixed.FromInt(41); Camera.DeltaViewHeight = Fixed.Zero; }
            if (Camera.ViewHeight < Fixed.FromInt(41) / 2) { Camera.ViewHeight = Fixed.FromInt(41) / 2; if (Camera.DeltaViewHeight <= Fixed.Zero) Camera.DeltaViewHeight = Fixed.Epsilon; }
            if (Camera.DeltaViewHeight != Fixed.Zero)
            {
                Camera.DeltaViewHeight += Fixed.One / 4;
                if (Camera.DeltaViewHeight == Fixed.Zero) Camera.DeltaViewHeight = Fixed.Epsilon;
            }
            var bob = (Body.MomX * Body.MomX + Body.MomY * Body.MomY) / 4;
            if (bob > Fixed.FromInt(16)) bob = Fixed.FromInt(16);
            if (State.Flying && Body.Z > Body.FloorZ) bob = Fixed.One / 2;
            Camera.ViewZ = Body.Z + Camera.ViewHeight + bob / 2 * Trig.Sin(new Angle((uint)((409L * tic & 8191) << 19)));
            var flat = world.Map.Flats[Body.Subsector.Sector.FloorFlat].Name;
            if (Body.Z <= Body.FloorZ && (flat == "FLTWAWA1" || flat == "FLTFLWW1" || flat == "FLTLAVA1" || flat == "FLATHUH1" || flat == "FLTSLUD1")) Camera.ViewZ -= Fixed.FromInt(10);
            Camera.ViewZ = new Fixed(Math.Clamp(Camera.ViewZ.Data, (Body.FloorZ + Fixed.FromInt(4)).Data, (Body.CeilingZ - Fixed.FromInt(4)).Data));
        }
        private void EnvironmentForces(bool onGround)
        {
            var sector = Body.Subsector.Sector;
            var special = (int)sector.Special;
            int[] wind = { 5, 10, 25 }; int[] current = { 5, 10, 25, 30, 35 };
            Angle[] directions = { Angle.Ang0, Angle.Ang90, Angle.Ang270, Angle.Ang180 };
            if (special >= 40 && special <= 51)
            {
                var index = special - 40; var amount = new Fixed(2048 * wind[index % 3]);
                Body.MomX += amount * Trig.Cos(directions[index / 3]); Body.MomY += amount * Trig.Sin(directions[index / 3]);
            }
            if (!onGround) return;
            if (special >= 20 && special <= 39) { var index = special - 20; Thrust(directions[index / 5], new Fixed(2048 * current[index % 5])); }
            if (special == 4) Thrust(Angle.Ang0, new Fixed(2048 * 28));
            if ((special == 4 || special == 5 || special == 16) && (tic & 15) == 0) DamageEnvironment(special == 16 ? 8 : 5);
            if (special == 7 && (tic & 31) == 0) DamageEnvironment(4);
            if (special == 9) { State.Secrets++; sector.Special = 0; }
        }
        internal void DamageEnvironment(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (State.Health <= 0 || amount == 0) return;
            State.Health = Math.Max(0, State.Health - amount);
            Body.Health = State.Health;
            if (State.Health != 0) return;
            State.Flying = false; State.FlightTics = 0; State.FlyHeight = 0;
            State.Message = "You died.";
            Body.Flags &= ~(MobjFlags.Shootable | MobjFlags.Solid | MobjFlags.Float | MobjFlags.SkullFly | MobjFlags.NoGravity);
            Body.Flags |= MobjFlags.Corpse | MobjFlags.DropOff;
            Body.Height >>= 2;
        }
        // Ordinary player death camera/physics from p_user.c and p_inter.c.
        // Corpse animation, attack-specific deaths and respawning require later combat/campaign work.
        private void TickDead()
        {
            Camera.UpdateFrameInterpolationInfo();
            Body.UpdateFrameInterpolationInfo();
            foreach (var sector in world.Map.Sectors) sector.UpdateFrameInterpolationInfo();
            world.SetPreviewTime(tic);
            MoveHorizontal(default);
            MoveVertical();
            foreach (var actor in actors) actor.Tick();
            TickClinkTest(false);
            world.Thinkers.Run();
            UpdateSwitchesAndScroll();
            Camera.DeltaViewHeight = Fixed.Zero;
            Camera.ViewHeight = new Fixed(Math.Max(Fixed.FromInt(6).Data, (Camera.ViewHeight - Fixed.One).Data));
            State.LookDirection -= Math.Sign(State.LookDirection) * Math.Min(6, Math.Abs(State.LookDirection));
            Camera.ViewZ = new Fixed(Math.Clamp((Body.Z + Camera.ViewHeight).Data,
                (Body.FloorZ + Fixed.FromInt(4)).Data, (Body.CeilingZ - Fixed.FromInt(4)).Data));
            tic++;
        }
        private void PickupKeys()
        {
            for (var i = actors.Count - 1; i >= 0; i--)
            {
                var actor = actors[i];
                if (actor.Key == HereticKeys.None) continue;
                var body = actor.Body; var dz = body.Z - Body.Z;
                if (Math.Abs((body.X - Body.X).Data) >= (body.Radius + Body.Radius).Data || Math.Abs((body.Y - Body.Y).Data) >= (body.Radius + Body.Radius).Data || dz > Body.Height || dz < Fixed.FromInt(-32)) continue;
                State.Keys |= actor.Key; State.Message = actor.Key + " key";
                world.ThingMovement.UnsetThingPosition(body);
                actor.Animation.SetState(HereticStateId.S_NULL);
                actors.RemoveAt(i);
            }
        }
        private void SpawnMapActor(MapThing thing, HereticActorType type)
        {
            var def = HereticDefinitions.Actors[(int)type];
            var tics = HereticDefinitions.States[(int)def.SpawnState].Tics;
            var animation = new HereticActorState(def.SpawnState, initialTics: tics > 0 ? 1 + world.Random.Next() % tics : null);
            // Explicit shared spatial flags; behavior flags remain family-owned.
            var flags = (MobjFlags)0;
            if ((def.Flags & HereticActorFlags.MF_SOLID) != 0) flags |= MobjFlags.Solid;
            if ((def.Flags & HereticActorFlags.MF_NOGRAVITY) != 0) flags |= MobjFlags.NoGravity;
            if ((def.Flags & HereticActorFlags.MF_SPAWNCEILING) != 0) flags |= MobjFlags.SpawnCeiling;
            if ((def.Flags & HereticActorFlags.MF_NOSECTOR) != 0) flags |= MobjFlags.NoSector;
            if ((def.Flags & HereticActorFlags.MF_NOBLOCKMAP) != 0 || HereticMapSpawns.KeyFor(type) != HereticKeys.None) flags |= MobjFlags.NoBlockMap;
            if ((thing.Flags & ThingFlags.Ambush) != 0) flags |= MobjFlags.Ambush;
            var body = new Mobj(world) { X = thing.X, Y = thing.Y, Angle = thing.Angle, Radius = def.Radius,
                Height = def.Height, Health = def.SpawnHealth, Flags = flags, Sprite = (Sprite)animation.Definition.Sprite, Frame = animation.Definition.Frame };
            world.ThingMovement.SetThingPosition(body);
            body.FloorZ = body.Subsector.Sector.FloorHeight;
            body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            body.Z = (flags & MobjFlags.SpawnCeiling) != 0 ? body.CeilingZ - body.Height : body.FloorZ;
            body.UpdateFrameInterpolationInfo();
            actors.Add(new HereticMapActor(type, body, animation));
        }
        private void Use()
        {
            world.PathTraversal.PathTraverse(Body.X, Body.Y, Body.X + 64 * Trig.Cos(Body.Angle), Body.Y + 64 * Trig.Sin(Body.Angle), PathTraverseFlags.AddLines,
                intercept =>
                {
                    var line = intercept.Line;
                    if (line.Special != 0) { UseLine(line, Geometry.PointOnLineSide(Body.X, Body.Y, line)); return false; }
                    world.MapCollision.LineOpening(line); return world.MapCollision.OpenRange > Fixed.Zero;
                });
        }
        private void LocalDoor(LineDef line)
        {
            var code = (int)line.Special;
            var key = code == 26 || code == 32 ? HereticKeys.Blue : code == 27 || code == 34 ? HereticKeys.Yellow : code == 28 || code == 33 ? HereticKeys.Green : HereticKeys.None;
            if (key != HereticKeys.None && (State.Keys & key) == 0) { State.Message = "You need the " + key.ToString().ToLowerInvariant() + " key."; return; }
            if (line.BackSide == null) return;
            var sector = line.BackSide.Sector;
            var openOnly = code >= 31 && code <= 34;
            if (sector.SpecialData is VerticalDoor door)
            { if (!openOnly) door.Direction = door.Direction == -1 ? 1 : -1; return; }
            if (sector.SpecialData != null) return;
            var created = new VerticalDoor(world) { Sector = sector, Direction = 1, Speed = Fixed.FromInt(2), TopWait = 150,
                Type = openOnly ? VerticalDoorType.Open : VerticalDoorType.Normal,
                TopHeight = world.SectorAction.FindLowestCeilingSurrounding(sector) - Fixed.FromInt(4) };
            sector.SpecialData = created; world.Thinkers.Add(created);
            if (openOnly) line.Special = 0;
        }
        private bool Door(LineDef line, VerticalDoorType type, int multiplier)
        {
            var before = world.Map.Sectors.Where(s => s.SpecialData == null).ToArray();
            var result = world.SectorAction.DoDoor(line, type);
            if (multiplier != 1) foreach (var sector in before) if (sector.SpecialData is VerticalDoor door) door.Speed *= multiplier;
            return result;
        }
        private void ChangeSwitch(LineDef line, bool repeat)
        {
            if (!repeat) line.Special = 0;
            if (buttons.Any(b => b.line == line)) return;
            var side = line.FrontSide;
            foreach (var pair in new[] { ("SW1OFF", "SW1ON"), ("SW2OFF", "SW2ON") })
            {
                int a = world.Map.Textures.GetNumber(pair.Item1), b = world.Map.Textures.GetNumber(pair.Item2);
                if (a < 0 || b < 0) continue;
                for (var position = 0; position < 3; position++)
                {
                    int texture = position == 0 ? side.TopTexture : position == 1 ? side.MiddleTexture : side.BottomTexture;
                    if (texture != a && texture != b) continue;
                    SetSwitchTexture(line, position, texture == a ? b : a);
                    if (repeat) buttons.Add((line, position, texture, tic + 35));
                    return;
                }
            }
        }
        private static void SetSwitchTexture(LineDef line, int position, int texture)
        { if (position == 0) line.FrontSide.TopTexture = texture; else if (position == 1) line.FrontSide.MiddleTexture = texture; else line.FrontSide.BottomTexture = texture; }
        private void UpdateSwitchesAndScroll()
        {
            for (var i = buttons.Count - 1; i >= 0; i--) if (tic >= buttons[i].until) { var b = buttons[i]; SetSwitchTexture(b.line, b.position, b.texture); buttons.RemoveAt(i); }
            foreach (var line in world.Map.Lines) { if ((int)line.Special == 48) line.FrontSide.TextureOffset += Fixed.One; else if ((int)line.Special == 99) line.FrontSide.TextureOffset -= Fixed.One; }
        }
        private void Teleport(LineDef line, int side)
        {
            if (side != 0) return;
            foreach (var sector in world.Map.Sectors.Where(s => s.Tag == line.Tag))
            foreach (var start in world.Map.Things.Where(t => t.Type == 14))
            {
                if (Geometry.PointInSubsector(start.X, start.Y, world.Map).Sector != sector) continue;
                var aboveFloor = Body.Z - Body.FloorZ;
                if (!world.ThingMovement.TeleportMove(Body, start.X, start.Y)) continue;
                Body.Z = State.FlightTics > 0 && aboveFloor > Fixed.Zero ? Fixed.Min(Body.FloorZ + aboveFloor, Body.CeilingZ - Body.Height) : Body.FloorZ;
                if (Body.Z == Body.FloorZ) { State.LookDirection = 0; State.Centering = false; }
                Body.Angle = start.Angle; Body.MomX = Body.MomY = Body.MomZ = Fixed.Zero; Body.ReactionTime = 18;
                UpdateView(); Body.UpdateFrameInterpolationInfo(); Camera.UpdateFrameInterpolationInfo(); return;
            }
        }
        private void RequestExit(bool secret) { ExitRequested = true; SecretExitRequested = secret; State.Message = "Level exit reached (campaign progression is a later chunk)."; }
    }
}
