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

// Adapted 2026-09-22: Time Bomb placement and explosion from pinned p_user.c/p_enemy.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
namespace ManagedDoom
{
    // Adapted 2026-09-22: pinned P_UseArtifact and A_Scream/A_Explode.
    internal sealed class HereticTimeBomb : IHereticActorActions
    {
        private readonly HereticWorldSession session;
        internal HereticMapActor Actor { get; }
        internal HereticTimeBomb(HereticWorldSession session)
        {
            this.session = session;
            var source = session.Body;
            var def = HereticDefinitions.Actors[(int)HereticActorType.MT_FIREBOMB];
            var animation = new HereticActorState(def.SpawnState, this);
            var body = new Mobj(session.World) { X = source.X + Fixed.FromInt(24) * Trig.Cos(source.Angle),
                Y = source.Y + Fixed.FromInt(24) * Trig.Sin(source.Angle), Z = source.Z,
                Radius = def.Radius, Height = def.Height, Health = def.SpawnHealth,
                Flags = MobjFlags.NoGravity | MobjFlags.Shadow, Target = source,
                LastLook = session.World.Random.Next() % 4,
                Sprite = (Sprite)animation.Definition.Sprite, Frame = animation.Definition.Frame };
            session.World.ThingMovement.SetThingPosition(body);
            body.FloorZ = body.Subsector.Sector.FloorHeight; body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            body.UpdateFrameInterpolationInfo(); Actor = new HereticMapActor(HereticActorType.MT_FIREBOMB, body, animation);
        }
        public bool Supports(HereticAction action) => action == HereticAction.A_Scream || action == HereticAction.A_Explode;
        public void Execute(HereticAction action, HereticActorState animation)
        {
            if (action == HereticAction.A_Scream)
                session.RequestSound(HereticDefinitions.Actors[(int)HereticActorType.MT_FIREBOMB].DeathSound, Actor.Body);
            else if (action == HereticAction.A_Explode)
            {
                Actor.Body.Z += Fixed.FromInt(32); Actor.Body.Flags &= ~MobjFlags.Shadow;
                session.PhoenixRadiusAttack(Actor.Body); session.HitLiquidFloor(Actor.Body);
            }
            else throw new NotSupportedException("Time bomb action: " + action);
        }
    }
    public sealed partial class HereticWorldSession
    {
        private void SpawnTimeBomb() => impactEffects.Add(new HereticTimeBomb(this).Actor);
    }
}
