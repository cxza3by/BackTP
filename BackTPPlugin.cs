using System;
using System.Collections.Generic;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace BackTP
{
    [ApiVersion(2, 1)]
    public class BackTPPlugin : TerrariaPlugin
    {
        public override string Name => "BackTP";
        public override string Author => "Ramiro Arena";
        public override string Description => "saves the position before a teleport and allows /back.";
        public override Version Version => new Version(1, 0, 0);
        private const double TeleportThresholdTiles = 120; 
        private static readonly TimeSpan BackCooldown = TimeSpan.FromSeconds(5);

        private static readonly Dictionary<int, BackState> State = new();

        public BackTPPlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("backtp.use", CmdBack, "back"));

            // loop to detect position jumps
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);

            TShock.Log.ConsoleInfo("[BackTP] loaded ");
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
                if (!st.WasDeadLastTick && dist >= TeleportThresholdTiles)
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
            if (st.LastBackUseUtc != DateTime.MinValue && (now - st.LastBackUseUtc) < BackCooldown)
            {
                var remain = BackCooldown - (now - st.LastBackUseUtc);
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
