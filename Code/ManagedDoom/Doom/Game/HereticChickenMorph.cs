// s&Doom modification: 2026-09-24, native Iron Lich combat integration.
// s&Doom modification: 2026-09-24, restore original monster type after morph.
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

// Adapted 2026-09-22: P_ChickenMorph/P_UpdateChicken and chicken actions.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
namespace ManagedDoom
{
    public sealed partial class HereticClinkTestEnemy
    {
        public bool IsChicken => Combatant.Type == HereticActorType.MT_CHICKEN;
        internal int ChickenTics { get; private set; }
        internal bool MorphToChicken()
        {
            if (Combatant.Type == HereticActorType.MT_HEAD || IsChicken || Body.Health <= 0 || (Body.Flags & MobjFlags.Shootable) == 0) return false;
            var old = Body;
            var replacement = new HereticCombatant(session.World, HereticActorType.MT_CHICKEN, this);
            session.MorphFog(old);
            replacement.Body.LastLook = session.World.Random.Next() % 4;
            ChickenTics = 40 * 35 + session.World.Random.Next();
            replacement.Body.Flags |= old.Flags & MobjFlags.Shadow;
            ReplaceBody(replacement, old);
            return true;
        }
        private void ReplaceBody(HereticCombatant replacement, Mobj old)
        {
            var movement = session.World.ThingMovement;
            movement.UnsetThingPosition(old);
            old.Flags &= ~(MobjFlags.Shootable | MobjFlags.Solid);
            var body = replacement.Body;
            body.X = old.X; body.Y = old.Y; body.Z = old.Z;
            body.Angle = old.Angle; body.Target = old.Target;
            Combatant = replacement;
            movement.SetThingPosition(body);
            body.FloorZ = body.Subsector.Sector.FloorHeight;
            body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            body.UpdateFrameInterpolationInfo();
        }
        internal bool UpdateChicken(int elapsed)
        {
            if (!IsChicken || Body.Health <= 0) return false;
            ChickenTics -= elapsed;
            if (ChickenTics > 0) return false;
            var old = Body;
            var restored = new HereticCombatant(session.World, OriginalType, this);
            var body = restored.Body;
            body.X = old.X; body.Y = old.Y; body.Z = old.Z; body.Subsector = old.Subsector;
            var movement = session.World.ThingMovement;
            // Probe without the chicken's own solid footprint. A failed probe
            // leaves its identity, health, flags, target and spatial link intact.
            var flags = old.Flags; old.Flags &= ~MobjFlags.Solid;
            bool fits;
            try
            {
                fits = movement.CheckPosition(body, body.X, body.Y) &&
                    body.Z >= movement.CurrentFloorZ && body.Z + body.Height <= movement.CurrentCeilingZ;
            }
            finally { old.Flags = flags; }
            if (!fits) { ChickenTics = 5 * 35; return false; }
            body.LastLook = session.World.Random.Next() % 4;
            ReplaceBody(restored, old); ChickenTics = 0;
            session.MorphFog(body);
            return true;
        }
    }
    public sealed partial class HereticWorldSession
    {
        // Enabled by Morph Ovum use in the single-player combat preview.
        // Only registered supported enemies participate in this lifecycle.
        internal bool EnemyMorphEnabled { get; set; }
        internal bool MorphTestEnemy(Mobj body)
        {
            foreach (var enemy in testEnemies)
                if (enemy.Body == body) return enemy.MorphToChicken();
            return false;
        }
        internal void MorphFog(Mobj body) => SpawnTeleportFog(body.X, body.Y, body.Z + Fixed.FromInt(32));
        internal void SpawnChickenFeathers(Mobj source)
        {
            var random = world.Random;
            var count = source.Health > 0 ? (random.Next() < 32 ? 2 : 1) : 5 + (random.Next() & 3);
            for (var i = 0; i < count; i++)
            {
                var effect = SpawnLiquidEffect(source, HereticActorType.MT_FEATHER);
                var body = effect.Body;
                body.Z = source.Z + Fixed.FromInt(20); body.Target = source;
                body.MomX = new Fixed((random.Next() - random.Next()) << 8);
                body.MomY = new Fixed((random.Next() - random.Next()) << 8);
                body.MomZ = Fixed.One + new Fixed(random.Next() << 9);
                effect.Animation.SetState((HereticStateId)((int)HereticStateId.S_FEATHER1 + (random.Next() & 7)));
                body.Sprite = (Sprite)effect.Animation.Definition.Sprite; body.Frame = effect.Animation.Definition.Frame;
                body.UpdateFrameInterpolationInfo();
            }
        }
    }
}
