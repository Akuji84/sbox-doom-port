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

// Heretic world activation tables

// Adapted 2026-09-18 for the shared ManagedDoom engine.
// Reference: Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
// Action dispatch adapted from p_spec.c and p_switch.c.
using System;
namespace ManagedDoom
{
    public sealed partial class HereticWorldSession
    {
        public bool UseLine(LineDef line, int side = 0)
        {
            if (side != 0) return false;
            switch ((int)line.Special)
            {
            //===============================================
            //      MANUALS
            //===============================================
        case 1:                // Vertical Door
        case 26:               // Blue Door/Locked
        case 27:               // Yellow Door /Locked
        case 28:               // Green Door /Locked

        case 31:               // Manual door open
        case 32:               // Blue locked door open
        case 33:               // Green locked door open
        case 34:               // Yellow locked door open
            LocalDoor(line);
            break;
            //===============================================
            //      SWITCHES
            //===============================================
        case 7:                // Switch_Build_Stairs (8 pixel steps)
            if (world.SectorAction.BuildStairs(line, StairType.Build8, true))
            {
                ChangeSwitch(line, false);
            }
            break;
        case 107:              // Switch_Build_Stairs_16 (16 pixel steps)
            if (world.SectorAction.BuildStairs(line, StairType.Turbo16, true))
            {
                ChangeSwitch(line, false);
            }
            break;
        case 9:                // Change Donut
            if (world.SectorAction.DoDonut(line))
                ChangeSwitch(line, false);
            break;
        case 11:               // Exit level
            RequestExit(false);
            ChangeSwitch(line, false);
            break;
        case 14:               // Raise Floor 32 and change texture
            if (world.SectorAction.DoPlatform(line, PlatformType.RaiseAndChange, 32))
                ChangeSwitch(line, false);
            break;
        case 15:               // Raise Floor 24 and change texture
            if (world.SectorAction.DoPlatform(line, PlatformType.RaiseAndChange, 24))
                ChangeSwitch(line, false);
            break;
        case 18:               // Raise Floor to next highest floor
            if (world.SectorAction.DoFloor(line, FloorMoveType.RaiseFloorToNearest))
                ChangeSwitch(line, false);
            break;
        case 20:               // Raise Plat next highest floor and change texture
            if (world.SectorAction.DoPlatform(line, PlatformType.RaiseToNearestAndChange, 0))
                ChangeSwitch(line, false);
            break;
        case 21:               // PlatDownWaitUpStay
            if (world.SectorAction.DoPlatform(line, PlatformType.DownWaitUpStay, 0))
                ChangeSwitch(line, false);
            break;
        case 23:               // Lower Floor to Lowest
            if (world.SectorAction.DoFloor(line, FloorMoveType.LowerFloorToLowest))
                ChangeSwitch(line, false);
            break;
        case 29:               // Raise Door
            if (Door(line, VerticalDoorType.Normal, 1))
                ChangeSwitch(line, false);
            break;
        case 41:               // Lower Ceiling to Floor
            if (world.SectorAction.DoCeiling(line, CeilingMoveType.LowerToFloor))
                ChangeSwitch(line, false);
            break;
        case 71:               // Turbo Lower Floor
            if (world.SectorAction.DoFloor(line, FloorMoveType.TurboLower))
                ChangeSwitch(line, false);
            break;
        case 49:               // Lower Ceiling And Crush
            if (world.SectorAction.DoCeiling(line, CeilingMoveType.LowerAndCrush))
                ChangeSwitch(line, false);
            break;
        case 50:               // Close Door
            if (Door(line, VerticalDoorType.Close, 1))
                ChangeSwitch(line, false);
            break;
        case 51:               // Secret EXIT
            RequestExit(true);
            ChangeSwitch(line, false);
            break;
        case 55:               // Raise Floor Crush
            if (world.SectorAction.DoFloor(line, FloorMoveType.RaiseFloorCrush))
                ChangeSwitch(line, false);
            break;
        case 101:              // Raise Floor
            if (world.SectorAction.DoFloor(line, FloorMoveType.RaiseFloor))
                ChangeSwitch(line, false);
            break;
        case 102:              // Lower Floor to Surrounding floor height
            if (world.SectorAction.DoFloor(line, FloorMoveType.LowerFloor))
                ChangeSwitch(line, false);
            break;
        case 103:              // Open Door
            if (Door(line, VerticalDoorType.Open, 1))
                ChangeSwitch(line, false);
            break;
            //===============================================
            //      BUTTONS
            //===============================================
        case 42:               // Close Door
            if (Door(line, VerticalDoorType.Close, 1))
                ChangeSwitch(line, true);
            break;
        case 43:               // Lower Ceiling to Floor
            if (world.SectorAction.DoCeiling(line, CeilingMoveType.LowerToFloor))
                ChangeSwitch(line, true);
            break;
        case 45:               // Lower Floor to Surrounding floor height
            if (world.SectorAction.DoFloor(line, FloorMoveType.LowerFloor))
                ChangeSwitch(line, true);
            break;
        case 60:               // Lower Floor to Lowest
            if (world.SectorAction.DoFloor(line, FloorMoveType.LowerFloorToLowest))
                ChangeSwitch(line, true);
            break;
        case 61:               // Open Door
            if (Door(line, VerticalDoorType.Open, 1))
                ChangeSwitch(line, true);
            break;
        case 62:               // PlatDownWaitUpStay
            if (world.SectorAction.DoPlatform(line, PlatformType.DownWaitUpStay, 1))
                ChangeSwitch(line, true);
            break;
        case 63:               // Raise Door
            if (Door(line, VerticalDoorType.Normal, 1))
                ChangeSwitch(line, true);
            break;
        case 64:               // Raise Floor to ceiling
            if (world.SectorAction.DoFloor(line, FloorMoveType.RaiseFloor))
                ChangeSwitch(line, true);
            break;
        case 66:               // Raise Floor 24 and change texture
            if (world.SectorAction.DoPlatform(line, PlatformType.RaiseAndChange, 24))
                ChangeSwitch(line, true);
            break;
        case 67:               // Raise Floor 32 and change texture
            if (world.SectorAction.DoPlatform(line, PlatformType.RaiseAndChange, 32))
                ChangeSwitch(line, true);
            break;
        case 65:               // Raise Floor Crush
            if (world.SectorAction.DoFloor(line, FloorMoveType.RaiseFloorCrush))
                ChangeSwitch(line, true);
            break;
        case 68:               // Raise Plat to next highest floor and change texture
            if (world.SectorAction.DoPlatform(line, PlatformType.RaiseToNearestAndChange, 0))
                ChangeSwitch(line, true);
            break;
        case 69:               // Raise Floor to next highest floor
            if (world.SectorAction.DoFloor(line, FloorMoveType.RaiseFloorToNearest))
                ChangeSwitch(line, true);
            break;
        case 70:               // Turbo Lower Floor
            if (world.SectorAction.DoFloor(line, FloorMoveType.TurboLower))
                ChangeSwitch(line, true);
            break;
                    default: return false;
            }
            return true;
        }
        internal void CrossLine(LineDef line, int side, Mobj thing)
        {
            if (thing != Body) return;
            switch ((int)line.Special)
            {
            //====================================================
            // TRIGGERS
            //====================================================
        case 2:                // Open Door
            Door(line, VerticalDoorType.Open, 1);
            line.Special = 0;
            break;
        case 3:                // Close Door
            Door(line, VerticalDoorType.Close, 1);
            line.Special = 0;
            break;
        case 4:                // Raise Door
            Door(line, VerticalDoorType.Normal, 1);
            line.Special = 0;
            break;
        case 5:                // Raise Floor
            world.SectorAction.DoFloor(line, FloorMoveType.RaiseFloor);
            line.Special = 0;
            break;
        case 6:                // Fast Ceiling Crush & Raise
            world.SectorAction.DoCeiling(line, CeilingMoveType.FastCrushAndRaise);
            line.Special = 0;
            break;
        case 8:                // Trigger_Build_Stairs (8 pixel steps)
            world.SectorAction.BuildStairs(line, StairType.Build8, true);
            line.Special = 0;
            break;
        case 106:              // Trigger_Build_Stairs_16 (16 pixel steps)
            world.SectorAction.BuildStairs(line, StairType.Turbo16, true);
            line.Special = 0;
            break;
        case 10:               // PlatDownWaitUp
            world.SectorAction.DoPlatform(line, PlatformType.DownWaitUpStay, 0);
            line.Special = 0;
            break;
        case 12:               // Light Turn On - brightest near
            world.SectorAction.LightTurnOn(line, 0);
            line.Special = 0;
            break;
        case 13:               // Light Turn On 255
            world.SectorAction.LightTurnOn(line, 255);
            line.Special = 0;
            break;
        case 16:               // Close Door 30
            Door(line, VerticalDoorType.Close30ThenOpen, 1);
            line.Special = 0;
            break;
        case 17:               // Start Light Strobing
            world.SectorAction.StartLightStrobing(line);
            line.Special = 0;
            break;
        case 19:               // Lower Floor
            world.SectorAction.DoFloor(line, FloorMoveType.LowerFloor);
            line.Special = 0;
            break;
        case 22:               // Raise floor to nearest height and change texture
            world.SectorAction.DoPlatform(line, PlatformType.RaiseToNearestAndChange, 0);
            line.Special = 0;
            break;
        case 25:               // Ceiling Crush and Raise
            world.SectorAction.DoCeiling(line, CeilingMoveType.CrushAndRaise);
            line.Special = 0;
            break;
        case 30:               // Raise floor to shortest texture height
            // on either side of lines
            world.SectorAction.DoFloor(line, FloorMoveType.RaiseToTexture);
            line.Special = 0;
            break;
        case 35:               // Lights Very Dark
            world.SectorAction.LightTurnOn(line, 35);
            line.Special = 0;
            break;
        case 36:               // Lower Floor (TURBO)
            world.SectorAction.DoFloor(line, FloorMoveType.TurboLower);
            line.Special = 0;
            break;
        case 37:               // LowerAndChange
            world.SectorAction.DoFloor(line, FloorMoveType.LowerAndChange);
            line.Special = 0;
            break;
        case 38:               // Lower Floor To Lowest
            world.SectorAction.DoFloor(line, FloorMoveType.LowerFloorToLowest);
            line.Special = 0;
            break;
        case 39:               // TELEPORT!
            Teleport(line, side);
            line.Special = 0;
            break;
        case 40:               // RaiseCeilingLowerFloor
            world.SectorAction.DoCeiling(line, CeilingMoveType.RaiseToHighest);
            world.SectorAction.DoFloor(line, FloorMoveType.LowerFloorToLowest);
            line.Special = 0;
            break;
        case 44:               // Ceiling Crush
            world.SectorAction.DoCeiling(line, CeilingMoveType.LowerAndCrush);
            line.Special = 0;
            break;
        case 52:               // EXIT!
            RequestExit(false);
            line.Special = 0;
            break;
        case 53:               // Perpetual Platform Raise
            world.SectorAction.DoPlatform(line, PlatformType.PerpetualRaise, 0);
            line.Special = 0;
            break;
        case 54:               // Platform Stop
            world.SectorAction.StopPlatform(line);
            line.Special = 0;
            break;
        case 56:               // Raise Floor Crush
            world.SectorAction.DoFloor(line, FloorMoveType.RaiseFloorCrush);
            line.Special = 0;
            break;
        case 57:               // Ceiling Crush Stop
            world.SectorAction.CeilingCrushStop(line);
            line.Special = 0;
            break;
        case 58:               // Raise Floor 24
            world.SectorAction.DoFloor(line, FloorMoveType.RaiseFloor24);
            line.Special = 0;
            break;
        case 59:               // Raise Floor 24 And Change
            world.SectorAction.DoFloor(line, FloorMoveType.RaiseFloor24AndChange);
            line.Special = 0;
            break;
        case 104:              // Turn lights off in sector(tag)
            world.SectorAction.TurnTagLightsOff(line);
            line.Special = 0;
            break;
        case 105:              // Trigger_SecretExit
            RequestExit(true);
            line.Special = 0;
            break;

            //====================================================
            // RE-DOABLE TRIGGERS
            //====================================================

        case 72:               // Ceiling Crush
            world.SectorAction.DoCeiling(line, CeilingMoveType.LowerAndCrush);
            break;
        case 73:               // Ceiling Crush and Raise
            world.SectorAction.DoCeiling(line, CeilingMoveType.CrushAndRaise);
            break;
        case 74:               // Ceiling Crush Stop
            world.SectorAction.CeilingCrushStop(line);
            break;
        case 75:               // Close Door
            Door(line, VerticalDoorType.Close, 1);
            break;
        case 76:               // Close Door 30
            Door(line, VerticalDoorType.Close30ThenOpen, 1);
            break;
        case 77:               // Fast Ceiling Crush & Raise
            world.SectorAction.DoCeiling(line, CeilingMoveType.FastCrushAndRaise);
            break;
        case 79:               // Lights Very Dark
            world.SectorAction.LightTurnOn(line, 35);
            break;
        case 80:               // Light Turn On - brightest near
            world.SectorAction.LightTurnOn(line, 0);
            break;
        case 81:               // Light Turn On 255
            world.SectorAction.LightTurnOn(line, 255);
            break;
        case 82:               // Lower Floor To Lowest
            world.SectorAction.DoFloor(line, FloorMoveType.LowerFloorToLowest);
            break;
        case 83:               // Lower Floor
            world.SectorAction.DoFloor(line, FloorMoveType.LowerFloor);
            break;
        case 84:               // LowerAndChange
            world.SectorAction.DoFloor(line, FloorMoveType.LowerAndChange);
            break;
        case 86:               // Open Door
            Door(line, VerticalDoorType.Open, 1);
            break;
        case 87:               // Perpetual Platform Raise
            world.SectorAction.DoPlatform(line, PlatformType.PerpetualRaise, 0);
            break;
        case 88:               // PlatDownWaitUp
            world.SectorAction.DoPlatform(line, PlatformType.DownWaitUpStay, 0);
            break;
        case 89:               // Platform Stop
            world.SectorAction.StopPlatform(line);
            break;
        case 90:               // Raise Door
            Door(line, VerticalDoorType.Normal, 1);
            break;
        case 100:              // Retrigger_Raise_Door_Turbo
            Door(line, VerticalDoorType.Normal, 3);
            break;
        case 91:               // Raise Floor
            world.SectorAction.DoFloor(line, FloorMoveType.RaiseFloor);
            break;
        case 92:               // Raise Floor 24
            world.SectorAction.DoFloor(line, FloorMoveType.RaiseFloor24);
            break;
        case 93:               // Raise Floor 24 And Change
            world.SectorAction.DoFloor(line, FloorMoveType.RaiseFloor24AndChange);
            break;
        case 94:               // Raise Floor Crush
            world.SectorAction.DoFloor(line, FloorMoveType.RaiseFloorCrush);
            break;
        case 95:               // Raise floor to nearest height and change texture
            world.SectorAction.DoPlatform(line, PlatformType.RaiseToNearestAndChange, 0);
            break;
        case 96:               // Raise floor to shortest texture height
            // on either side of lines
            world.SectorAction.DoFloor(line, FloorMoveType.RaiseToTexture);
            break;
        case 97:               // TELEPORT!
            Teleport(line, side);
            break;
        case 98:               // Lower Floor (TURBO)
            world.SectorAction.DoFloor(line, FloorMoveType.TurboLower);
            break;

            }
        }
    }
}
