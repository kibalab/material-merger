#if UNITY_EDITOR
using System;
using UnityEngine;

namespace K13A.MaterialMerger.Editor.Models
{
    public struct GroupKey : IEquatable<GroupKey>
    {
        public Shader shader;
        public int keywordsHash;
        public int renderQueue;
        public int transparencyKey;

        public bool Equals(GroupKey other)
        {
            return shader == other.shader
                   && keywordsHash == other.keywordsHash
                   && renderQueue == other.renderQueue
                   && transparencyKey == other.transparencyKey;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + (shader ? shader.GetInstanceID() : 0);
                h = h * 31 + keywordsHash;
                h = h * 31 + renderQueue;
                h = h * 31 + transparencyKey;
                return h;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is GroupKey other && Equals(other);
        }

        public static bool operator ==(GroupKey left, GroupKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GroupKey left, GroupKey right)
        {
            return !left.Equals(right);
        }
    }
}
#endif
