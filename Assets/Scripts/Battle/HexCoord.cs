using System;
using UnityEngine;

namespace Battle
{
    [Serializable]
    public struct HexCoord : IEquatable<HexCoord>
    {
        public int Q;
        public int R;

        public HexCoord(int q, int r) { Q = q; R = r; }

        // 6 邻居方向（pointy-top 尖顶，按顺时针从 E 开始）
        public static readonly HexCoord[] Directions = new HexCoord[6]
        {
            new( 1,  0),  // 0 E
            new( 1, -1),  // 1 NE
            new( 0, -1),  // 2 NW
            new(-1,  0),  // 3 W
            new(-1,  1),  // 4 SW
            new( 0,  1),  // 5 SE
        };

        public enum Direction { E = 0, NE = 1, NW = 2, W = 3, SW = 4, SE = 5 }

        public HexCoord GetNeighbor(int dir)       => this + Directions[dir];
        public HexCoord GetNeighbor(Direction dir) => GetNeighbor((int)dir);

        public HexCoord[] GetAllNeighbors()
        {
            var result = new HexCoord[6];
            for (int i = 0; i < 6; i++) result[i] = GetNeighbor(i);
            return result;
        }

        /// <summary> 两格之间的最短步数（轴坐标转立方坐标距离公式） </summary>
        public int Distance(HexCoord other)
        {
            int dq = Q - other.Q;
            int dr = R - other.R;
            return (Mathf.Abs(dq) + Mathf.Abs(dq + dr) + Mathf.Abs(dr)) / 2;
        }

        // ── 运算符 ───────────────────────────────────────────────

        public static HexCoord operator +(HexCoord a, HexCoord b) => new(a.Q + b.Q, a.R + b.R);
        public static HexCoord operator -(HexCoord a, HexCoord b) => new(a.Q - b.Q, a.R - b.R);
        public static bool     operator ==(HexCoord a, HexCoord b) => a.Q == b.Q && a.R == b.R;
        public static bool     operator !=(HexCoord a, HexCoord b) => !(a == b);

        public bool Equals(HexCoord other)      => this == other;
        public override bool Equals(object obj) => obj is HexCoord h && Equals(h);
        public override int  GetHashCode()      => (Q, R).GetHashCode();
        public override string ToString()       => $"({Q},{R})";

        // ── Tilemap 坐标互转（pointy-top, odd-r 偏移模式）────────
        // Unity Tilemap 的 Vector3Int 使用偏移坐标，此处采用 odd-r 规则：
        //   奇数行（r 为奇数）向右偏移半格

        public Vector3Int ToVector3Int()
        {
            int col = Q + (R - (R & 1)) / 2;
            return new Vector3Int(col, R, 0);
        }

        public static HexCoord FromVector3Int(Vector3Int v)
        {
            int q = v.x - (v.y - (v.y & 1)) / 2;
            return new HexCoord(q, v.y);
        }
    }
}
