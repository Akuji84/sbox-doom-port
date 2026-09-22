using ManagedDoom;
using Sandbox;
using System.Security.Cryptography;
using static Sandbox.SboxManagedDoomShellBridgeService;

var root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
foreach (var length in new[] {0,1,55,56,63,64,65,127,128,1024})
{
    var bytes = new byte[length]; new Random(length).NextBytes(bytes);
    if (!SaveDataHash.Compute(bytes).SequenceEqual(SHA256.HashData(bytes))) throw new Exception("SHA-256 mismatch at " + length);
}
Console.WriteLine("PASS managed save hash matches platform SHA-256 across padding/block boundaries");
var wadPath = Path.Combine(root, "Assets/doom/freedoom1.wad");
using var content = GameContent.CreateDummy(wadPath);
var options = new GameOptions { GameMode = content.Wad.GameMode, GameVersion = content.Wad.GameVersion, MissionPack = content.Wad.MissionPack };
var game = new DoomGame(content, options);
game.InitNew(GameSkill.Medium, 1, 1);
var commands = Enumerable.Range(0,4).Select(_ => new TicCmd()).ToArray();
for (var i=0; i<5; i++) game.Update(commands);
var oldWorld = game.World;
var oldPlayer = options.Players[0];
var side = oldWorld.Map.Sides[0];
side.TextureOffset = Fixed.FromInt(37);
side.RowOffset = Fixed.FromInt(19);
var line = oldWorld.Map.Lines.First(l => l.FrontSide != null);
var button = oldWorld.Specials.SavedButtons[0];
button.Line = line; button.Position = ButtonPosition.Top; button.Texture = line.FrontSide.TopTexture;
button.Timer = 4; button.SoundOrigin = line.SoundOrigin;
line.FrontSide.TopTexture = 0;
var sector = oldWorld.Map.Sectors.First(s => s.SpecialData == null);
var lift = new Platform(oldWorld) {
    Sector = sector, Speed = Fixed.FromInt(1), Low = sector.FloorHeight,
    High = sector.FloorHeight, Status = PlatformState.Up, OldStatus = PlatformState.Up,
    Type = PlatformType.RaiseAndChange, ThinkerState = ThinkerState.Active
};
oldWorld.Thinkers.Add(lift); sector.SpecialData = lift; oldWorld.SectorAction.AddActivePlatform(lift);
oldWorld.Map.Sectors[0].SoundTarget = oldPlayer.Mobj;
var saved = SaveAndLoad.SaveToMemory(game, "regression");
var originalLevelTime = oldWorld.LevelTime;

void Assert(bool value, string message) { if (!value) throw new Exception(message); Console.WriteLine("PASS " + message); }
void Reject(byte[] bytes, string message)
{
    var worldBefore = game.World; var playerBefore = options.Players[0]; var ticBefore = game.GameTic;
    var before = SaveAndLoad.SaveToMemory(game,"before");
    var rejected=false;
    try { SaveAndLoad.LoadFromMemory(game, bytes); } catch { rejected=true; }
    Assert(rejected && ReferenceEquals(worldBefore,game.World) && ReferenceEquals(playerBefore,options.Players[0])
        && ticBefore==game.GameTic && before.SequenceEqual(SaveAndLoad.SaveToMemory(game,"before")), message);
}
Reject(saved[..^1], "truncated save leaves running world and options unchanged");
var wrong = (byte[])saved.Clone(); wrong[36] ^= 1;
SHA256.HashData(wrong.AsSpan(0,wrong.Length-32)).CopyTo(wrong.AsSpan(wrong.Length-32));
Reject(wrong, "wrong WAD identity leaves running game unchanged");
// Valid checksum, but truncated body: exercises staged parsing, not just integrity checking.
var malformed = new byte[saved.Length-80];
Array.Copy(saved,malformed,malformed.Length-32);
SHA256.HashData(malformed.AsSpan(0,malformed.Length-32)).CopyTo(malformed.AsSpan(malformed.Length-32));
Reject(malformed, "malformed checksummed body cannot partially replace game");
SaveAndLoad.LoadFromMemory(game,saved);
Assert(!ReferenceEquals(oldWorld,game.World) && game.World.LevelTime==originalLevelTime,"validated world swaps in with correct clock");
Assert(game.World.Map.Sides[0].TextureOffset==Fixed.FromInt(37) && game.World.Map.Sides[0].RowOffset==Fixed.FromInt(19),"sidedef offsets round-trip");
Assert(game.World.Specials.SavedButtons[0].Timer==4,"button timer round-trips");
Assert(ReferenceEquals(game.World.Map.Sectors[0].SoundTarget, options.Players[0].Mobj), "sector sound target references restored player");
Assert(saved.SequenceEqual(SaveAndLoad.SaveToMemory(game,"regression")),"complete serialized state round-trips byte-for-byte");
for(var i=0;i<5;i++) game.Update(commands);
Assert(game.World.Specials.SavedButtons[0].Timer==0,"restored button expires");
Assert(game.World.Map.Sectors[sector.Number].SpecialData==null,"restored lift completes without missing-registry exception");
Assert(game.World.Map.Lines[Array.IndexOf(oldWorld.Map.Lines,line)].FrontSide.TopTexture==button.Texture,"restored switch returns to its original texture");
Assert(game.World.Game==game && ReferenceEquals(game.World.Options,options),"restored world uses original owning game/options");

var config=new Config(); var baseMusic=config.audio_musicvolume; var baseSfx=config.audio_soundvolume;
var shell=new ShellLaunchConfig { ShellVolume=.5, Controls=new ShellControlsPayload { Bindings=new() { ["forward"]="" }, Settings=new() } };
for(var i=0;i<5;i++) SboxManagedDoomShellControlsMapper.Apply(config,shell);
Assert(config.audio_musicvolume==baseMusic && config.audio_soundvolume==baseSfx && config.ShellMasterVolume==.5f,"shell gain never compounds base volume");
Assert(config.key_forward.Keys.Count==0 && config.key_forward.MouseButtons.Count==0,"explicit unbound action is actually unbound");
shell.Controls.Settings.Music=false; SboxManagedDoomShellControlsMapper.Apply(config,shell);
shell.Controls.Settings.Music=true; SboxManagedDoomShellControlsMapper.Apply(config,shell);
Assert(config.ShellMusicEnabled && config.audio_musicvolume==baseMusic,"mute then unmute retains base music volume");
config.Save("regression.cfg"); var persisted=new Config("regression.cfg");
Assert(persisted.ShellMasterVolume==1 && persisted.key_forward.Keys.Count==0,"runtime gain is not saved and unbound key persists");

int TurnAt(int fps) {
    var input=new SboxManagedDoomInput(new Config());input.GrabMouse();
    int sum=0,ticks=0;var cmd=new TicCmd();
    for(int frame=0;frame<fps;frame++) {
        Input.MouseDelta=new Vector2 { x=140f/fps };input.CaptureFrameInput(true);
        int target=(frame+1)*35/fps;
        while(ticks<target) { input.BuildTicCmd(cmd);sum+=cmd.AngleTurn;ticks++; }
    }
    return sum;
}
Assert(Math.Abs(TurnAt(35)-TurnAt(140))<=8 && Math.Abs(TurnAt(20)-TurnAt(140))<=8,"mouse turn is stable across zero-tick and catch-up frames");
var mapInput=new SboxManagedDoomInput(new Config());Input.Keyboard.Keys.Add("M");
Assert(mapInput.UpdateAutomapKey(out var down) && down && !mapInput.UpdateAutomapKey(out _),"automap binding emits one edge while held");
Input.Keyboard.Keys.Clear();
Assert(mapInput.UpdateAutomapKey(out down) && !down,"automap binding emits release");
var session = new SboxManagedDoomMultiplayerSessionComponent { PvpActive = true, PvpMap = "E1M1", PvpLaunchSerial = 1 };
var mode = new SboxManagedDoomMultiplayerModeController();
mode.BeginPvpLaunch(session, session.PvpMap);
Assert(!mode.ShouldRestartPvp(session), "same launch serial does not restart a pending round");
session.PvpLaunchSerial++;
Assert(mode.ShouldRestartPvp(session), "new serial restarts the joiner even on the same map");
var doom = new Doom(new CommandLineArgs(Array.Empty<string>()), new Config(), content, null, null, null, null);
doom.NewGame(GameSkill.Medium,1,1);
for(var i=0;i<80;i++) doom.Update();
mode.UpdatePvpLoadedState(doom,session,null);
Assert(mode.PvpMatchLoaded && mode.ShouldRestartPvp(session), "loaded joiner recognizes new same-map round");
mode.ResetRuntimeState();
Assert(mode.TryCreatePvpLaunchPlan(doom, new SboxManagedDoomInput(new Config()), session, false, out var plan)
    && plan.ConsolePlayer==1 && plan.MapName=="E1M1", "reset joiner gets a valid launch plan for the same map");
var watchdog = new SboxManagedDoomProgressWatchdog();
var now = new DateTime(2026,9,9);
Assert(!watchdog.HasTimedOut(now,1,10,false,0,0)
    && !watchdog.HasTimedOut(now.AddSeconds(14),1,10,false,0,0)
    && watchdog.HasTimedOut(now.AddSeconds(15),1,10,false,0,0), "connected peer without command progress times out");
Assert(!watchdog.HasTimedOut(now.AddSeconds(16),1,11,false,0,0), "command progress resets timeout");
Assert(!watchdog.HasTimedOut(now.AddSeconds(17),1,11,true,1,0)
    && !watchdog.HasTimedOut(now.AddSeconds(40),1,11,true,1,2)
    && watchdog.HasTimedOut(now.AddSeconds(70),1,11,true,1,2), "recovery progress grants grace but cannot stall forever");
watchdog.Reset();
Assert(!watchdog.HasTimedOut(now.AddMinutes(5),2,0,false,0,0), "new match starts with a fresh timeout");
byte[] RunFreedoomAfterWadSwitch()
{
    SboxManagedDoomFileSystem.SetHostWadPaths(wadPath);
    var args = new CommandLineArgs(new[] { "-iwad", wadPath });
    using var originalContent = new GameContent(args);
    var originalGame = new DoomGame(originalContent, new GameOptions(args, originalContent));
    originalGame.InitNew(GameSkill.Medium, 1, 1);
    var input = Enumerable.Range(0, 4).Select(_ => new TicCmd()).ToArray();
    for (var tic = 0; tic < 140; tic++) originalGame.Update(input);
    return SaveAndLoad.SaveToMemory(originalGame, "original campaign");
}
var freedoomBaseline = RunFreedoomAfterWadSwitch();
foreach (var name in new[] { "fsfc1", "fssc1" })
{
    var path = Path.Combine(root, "Assets/doom/" + name + ".wad");
    SboxManagedDoomFileSystem.SetHostWadPaths(path);
    var launchArgs = new CommandLineArgs(new[] { "-iwad", path });
    using var campaign = new GameContent(launchArgs);
    Assert(campaign.Wad.IsFreedomScoops && campaign.Wad.HasFiveMapCampaign
        && campaign.Wad.GameMode == GameMode.Shareware, name + " uses its five-map campaign profile");
    var cfg = new Config();
    var renderer = new ManagedDoom.Video.Renderer(cfg, campaign);
    var instance = new Doom(launchArgs, cfg, campaign, null, null, null, null);
    var pixels = new byte[renderer.Width * renderer.Height * 4];
    for (var map = 1; map <= 5; map++)
    {
        instance.NewGame(GameSkill.Medium, 1, map);
        for(var tic=0;tic<100;tic++) instance.Update();
        Assert(instance.Game.World.Options.Map == map, name + " started requested map " + map);
        renderer.Render(instance, pixels, Fixed.Zero);
        var state = SaveAndLoad.SaveToMemory(instance.Game, "campaign");
        SaveAndLoad.LoadFromMemory(instance.Game, state);
        Assert(state.SequenceEqual(SaveAndLoad.SaveToMemory(instance.Game,"campaign")),
            name + " E1M" + map + " loads, renders and restores complete save state");
    }
    instance.Game.InitNew(GameSkill.Medium, 4, 9);
    Assert(instance.Options.Episode==1 && instance.Options.Map==5, name + " excludes placeholder map slots");
    foreach (var secret in new[] { false, true })
    {
        for (var map = 1; map <= 5; map++)
        {
            instance.Game.InitNew(GameSkill.Medium, 1, map);
            instance.Game.Update(commands);
            if (secret) instance.Game.World.SecretExitLevel();
            else instance.Game.World.ExitLevel();
            instance.Game.Update(commands);
            instance.Game.Update(commands);
            Assert(map == 5 ? instance.Game.State == GameState.Finale
                : instance.Game.State == GameState.Intermission && instance.Options.IntermissionInfo.NextLevel == map,
                name + " E1M" + map + (secret ? " secret" : " normal") + " exit stays in the finished campaign");
            renderer.Render(instance, pixels, Fixed.Zero);
        }
    }
    foreach (var deathmatch in new[] { 0, 1 })
    for (var networkMap = 1; networkMap <= 5; networkMap++)
    {
        var peerOptions = new GameOptions(launchArgs, campaign) { NetGame = true, Deathmatch = deathmatch };
        foreach (var player in peerOptions.Players) player.InGame = true;
        var peer = new DoomGame(campaign, peerOptions);
        peer.InitNew(GameSkill.Medium, 1, networkMap);
        Assert(peerOptions.Players.All(player => player.Mobj != null),
            name + " E1M" + networkMap + " spawns all four " + (deathmatch == 0 ? "co-op" : "PvP") + " players");
        var initial = SaveAndLoad.SaveToMemory(peer, "network");
        void Simulate()
        {
            for (var tic = 0; tic < 140; tic++)
            {
                commands[0].ForwardMove = (sbyte)(tic % 20 < 10 ? 25 : -25);
                commands[1].AngleTurn = (short)(tic % 2 == 0 ? 256 : -256);
                commands[0].Buttons = commands[1].Buttons = (byte)(tic % 3 == 0 ? TicCmdButtons.Attack : 0);
                peer.Update(commands);
            }
        }
        Simulate();
        var expected = SaveAndLoad.SaveToMemory(peer, "network");
        SaveAndLoad.LoadFromMemory(peer, initial);
        Simulate();
        Assert(expected.SequenceEqual(SaveAndLoad.SaveToMemory(peer, "network")),
            name + " E1M" + networkMap + (deathmatch == 0 ? " co-op" : " PvP") + " four-player simulation replays deterministically after restore");
        foreach (var command in commands) command.Clear();
    }
    Assert(freedoomBaseline.SequenceEqual(RunFreedoomAfterWadSwitch()),
        "switching from " + name + " back to Freedoom restores original simulation behavior");
}
CompatibilitySnapshots.Verify(root, args.Contains("--record-compatibility"));
GameProfileChecks.Verify();
HereticPreviewChecks.Verify(root);
HereticMovementChecks.Verify(root);
HereticActorChecks.Verify(root);
HereticSpawnChecks.Verify(root);
HereticHeightChecks.Verify(root);
HereticDamageChecks.Verify(root);
HereticClinkChecks.Verify(root);
HereticWandChecks.Verify(root);
HereticImpactChecks.Verify(root);
HereticShootChecks.Verify(root);
HereticSoundChecks.Verify(root);
HereticAmmoChecks.Verify(root);
HereticArmorChecks.Verify(root);
HereticFeedbackChecks.Verify(root);
HereticCrossbowChecks.Verify(root);
HereticSkullRodChecks.Verify(root);
HereticPhoenixChecks.Verify(root);
HereticTerrainChecks.Verify(root);
HereticMaceProjectileChecks.Verify(root);
HereticMaceWeaponChecks.Verify(root);
CompatibilitySnapshots.Verify(root, false); // Switching back after Heretic must preserve Doom definitions.
Console.WriteLine("All regression checks passed.");
