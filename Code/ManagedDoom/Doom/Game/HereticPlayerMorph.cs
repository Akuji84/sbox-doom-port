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

// Adapted 2026-09-22: P_ChickenMorphPlayer/P_UndoPlayerChicken/P_ChickenPlayerThink.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
namespace ManagedDoom
{
    public sealed partial class HereticWorldSession
    {
        private HereticWeapon preChickenWeapon;
        internal bool MorphPlayer()
        {
            if (State.Health <= 0 || GoldWand == null) return false;
            if (State.ChickenTics > 0)
            {
                if (State.ChickenTics < 40 * 35 - 35 && State.WeaponPowerTics == 0) State.WeaponPowerTics = 40 * 35;
                return false;
            }
            if (State.InvulnerabilityTics > 0) return false;
            var old = Body;
            preChickenWeapon = GoldWand.ReadyWeapon;
            MorphFog(old);
            ReplacePlayerBody(NewPlayerBody(Fixed.FromInt(24), 30));
            State.Health = 30; State.ArmorPoints = State.ArmorType = 0;
            State.InvisibilityTics = State.WeaponPowerTics = 0;
            State.ChickenTics = 40 * 35;
            GoldWand.ActivateBeak(); UpdateView();
            return true;
        }
        private Mobj NewPlayerBody(Fixed height, int health) => new Mobj(world)
        {
            X = Body.X, Y = Body.Y, Z = Body.Z, Angle = Body.Angle,
            Radius = Fixed.FromInt(16), Height = height, Health = health,
            Flags = MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.DropOff | MobjFlags.NoSector |
                (State.Flying ? MobjFlags.NoGravity : 0), Player = Camera,
            Subsector = Body.Subsector, FloorZ = Body.FloorZ, CeilingZ = Body.CeilingZ
        };
        private void ReplacePlayerBody(Mobj replacement)
        {
            var old = Body;
            world.ThingMovement.UnsetThingPosition(old);
            old.Flags &= ~(MobjFlags.Solid | MobjFlags.Shootable); old.Player = null;
            Camera.Mobj = replacement;
            replacement.LastLook = world.Random.Next() % 4;
            world.ThingMovement.SetThingPosition(replacement);
            replacement.UpdateFrameInterpolationInfo(); Camera.UpdateFrameInterpolationInfo();
        }
        internal bool UndoPlayerChicken()
        {
            if (State.ChickenTics == 0 || State.Health <= 0) return false;
            var candidate = NewPlayerBody(Fixed.FromInt(56), 100);
            var flags = Body.Flags; Body.Flags &= ~MobjFlags.Solid;
            bool fits;
            try
            {
                fits = world.ThingMovement.CheckPosition(candidate, candidate.X, candidate.Y) &&
                    candidate.Z >= world.ThingMovement.CurrentFloorZ && candidate.Z + candidate.Height <= world.ThingMovement.CurrentCeilingZ;
                if (fits) { candidate.FloorZ = world.ThingMovement.CurrentFloorZ; candidate.CeilingZ = world.ThingMovement.CurrentCeilingZ; }
            }
            finally { Body.Flags = flags; }
            if (!fits) { State.ChickenTics = 2 * 35; return false; }
            ReplacePlayerBody(candidate); Body.ReactionTime = 18;
            State.ChickenTics = State.WeaponPowerTics = 0; State.Health = 100;
            GoldWand.RestoreAfterChicken(preChickenWeapon);
            SpawnTeleportFog(Body.X + Fixed.FromInt(20) * Trig.Cos(Body.Angle), Body.Y + Fixed.FromInt(20) * Trig.Sin(Body.Angle), Body.Z + Fixed.FromInt(32));
            UpdateView(); return true;
        }
        private void ThinkPlayerChicken()
        {
            if (State.ChickenTics == 0 || (State.ChickenTics & 15) != 0) return;
            if (Body.MomX + Body.MomY == Fixed.Zero && world.Random.Next() < 160)
                Body.Angle += new Angle(unchecked((uint)((world.Random.Next() - world.Random.Next()) << 19)));
            if (Body.Z <= Body.FloorZ && world.Random.Next() < 32)
            {
                Body.MomZ += Fixed.One; RequestSound(HereticSoundId.sfx_chicpai, Body); return;
            }
            if (world.Random.Next() < 48) RequestSound(HereticSoundId.sfx_chicact, Body);
        }
    }
}
