using UnityEngine;

namespace MoleTown
{
    public enum WhackKind { Bug, GoldBug, Baby }

    public static class WhackRules
    {
        public static int Points(WhackKind kind) => kind switch
        {
            WhackKind.Bug => 1,
            WhackKind.GoldBug => 3,
            WhackKind.Baby => -2,   // 别敲到鼹鼠宝宝
            _ => 0,
        };

        public static int ApplyHit(int score, WhackKind kind) => Mathf.Max(0, score + Points(kind));

        public static int Reward(int score) => score;   // 每分 1 金豆
    }
}
