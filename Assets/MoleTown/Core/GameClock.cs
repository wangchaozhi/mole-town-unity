using System;

namespace MoleTown
{
    // 用真实时间计时，作物在关闭游戏后也会继续生长；Offset 供测试快进时间
    public static class GameClock
    {
        public static double Offset;
        public static double Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0 + Offset;
    }
}
