using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Configuration;

namespace BackTP
{
    [ApiVersion(2, 1)]
    public class BackTPPlugin : TerrariaPlugin
    {
        public override string Name => "BackTP";
        public override string Author => "Ramiro Arena";
        public override string Description => "saves the position before a teleport and allows /back.";
        public override Version Version => new Version(1, 0, 0);
        private const double DefaultTeleportThresholdTiles = 120;
        private const int DefaultBackCooldownSeconds = 5;

        private static readonly ConfigFile<BackTPConfig> Config = new();
        private static readonly Dictionary<int, BackState> State = new();

        private static string ConfigPath => Path.Combine(TShock.SavePath, "BackTP.json");

        public BackTPPlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            LoadConfig();

            Commands.ChatCommands.Add(new Command("backtp.use", CmdBack, "back"));

            // loop to detect position jumps
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);

            TShock.Log.ConsoleInfo($"[BackTP] loaded (config: {ConfigPath})");
        }

        private static void LoadConfig()
        {
            try
            {
                var settings = Config.Read(ConfigPath, out bool incompleteSettings);
                if (settings == null)
                {
                    // No config file yet: write the defaults.
                    Config.Settings = new BackTPConfig();
                    Config.Write(ConfigPath);
                    return;
                }

                bool changed = ValidateConfig(Config.Settings);
                if (incompleteSettings || changed)
                    Config.Write(ConfigPath);
            }
            catch (Exception ex)
            {
                Config.Settings = new BackTPConfig();
                TShock.Log.ConsoleError($"[BackTP] could not read config '{ConfigPath}': {ex.Message}");
            }
        }

        private static bool ValidateConfig(BackTPConfig settings)
        {
            bool changed = false;

            if (settings.TeleportThresholdTiles <= 0)
            {
                settings.TeleportThresholdTiles = DefaultTeleportThresholdTiles;
                changed = true;
            }

            if (settings.BackCooldownSeconds < 0)
            {
                settings.BackCooldownSeconds = DefaultBackCooldownSeconds;
                changed = true;
            }

            return changed;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
            }
            base.Dispose(disposing);
        }

        private void OnLeave(LeaveEventArgs args)
        {
            State.Remove(args.Who);
        }

        private void OnGameUpdate(EventArgs args)
        {
            double teleportThreshold = Config.Settings.TeleportThresholdTiles;

            foreach (var p in TShock.Players)
            {
                if (p == null || !p.Active || p.TPlayer == null)
                    continue;

                var idx = p.Index;

                if (!State.TryGetValue(idx, out var st))
                {
                    st = new BackState { LastX = p.TileX, LastY = p.TileY };
                    State[idx] = st;
                    continue;
                }

                // don't save back if dead or there is no valid position
                if (p.Dead)
                {
                    st.LastX = p.TileX;
                    st.LastY = p.TileY;
                    st.WasDeadLastTick = true;
                    continue;
                }

                var curX = p.TileX;
                var curY = p.TileY;

                // distance in tiles since the last tick
                int dx = curX - st.LastX;
                int dy = curY - st.LastY;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                // detects a large jump (probable teleport)
                if (!st.WasDeadLastTick && dist >= teleportThreshold)
                {
                    // save previous position
                    st.BackX = st.LastX;
                    st.BackY = st.LastY;
                    st.HasBack = true;
                }

                st.LastX = curX;
                st.LastY = curY;
                st.WasDeadLastTick = false;
            }
        }

        private static void CmdBack(CommandArgs args)
        {
            var p = args.Player;
            if (p == null) return;

            if (!State.TryGetValue(p.Index, out var st) || !st.HasBack)
            {
                p.SendErrorMessage("No previous location is saved (/back).");
                return;
            }

            var now = DateTime.UtcNow;
            var backCooldown = TimeSpan.FromSeconds(Config.Settings.BackCooldownSeconds);
            if (st.LastBackUseUtc != DateTime.MinValue && (now - st.LastBackUseUtc) < backCooldown)
            {
                var remain = backCooldown - (now - st.LastBackUseUtc);
                p.SendErrorMessage($"Wait {Math.Ceiling(remain.TotalSeconds)}s to use /back again.");
                return;
            }

            // clamp within the world
            int x = Clamp(st.BackX, 0, Main.maxTilesX - 1);
            int y = Clamp(st.BackY, 0, Main.maxTilesY - 1);

            // teleport uses pixels
            p.Teleport(x * 16f, y * 16f);
            st.LastBackUseUtc = now;

            p.SendSuccessMessage($" You returned to your previous position ({x}, {y}).");
        }

        private static int Clamp(int v, int min, int max)
            => v < min ? min : (v > max ? max : v);

        private class BackState
        {
            public int LastX, LastY;
            public int BackX, BackY;
            public bool HasBack;
            public bool WasDeadLastTick;
            public DateTime LastBackUseUtc = DateTime.MinValue;
        }
    }
}
