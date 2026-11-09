namespace BackTP
{
    public class BackTPConfig
    {
        /// <summary>
        /// Minimum position change, in tiles, that is treated as a teleport.
        /// </summary>
        public double TeleportThresholdTiles { get; set; } = 120;

        /// <summary>
        /// Cooldown, in seconds, between /back uses.
        /// </summary>
        public int BackCooldownSeconds { get; set; } = 5;
    }
}
