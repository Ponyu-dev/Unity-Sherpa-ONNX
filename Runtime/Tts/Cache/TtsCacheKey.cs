using System;

namespace PonyuDev.SherpaOnnx.Tts.Cache
{
    /// <summary>
    /// Immutable key for TtsResult memoization.
    /// Value equality on (text, speed rounded to 3 decimals, speakerId).
    /// </summary>
    public readonly struct TtsCacheKey : IEquatable<TtsCacheKey>
    {
        public readonly string Text;
        public readonly float Speed;
        public readonly int SpeakerId;

        public TtsCacheKey(string text, float speed, int speakerId)
        {
            Text = text ?? "";
            Speed = (float)Math.Round(speed, 3);
            SpeakerId = speakerId;
        }

        public bool Equals(TtsCacheKey other)
        {
            return Text == other.Text
                && Math.Abs(Speed - other.Speed) < 0.0005f
                && SpeakerId == other.SpeakerId;
        }

        public override bool Equals(object obj)
        {
            return obj is TtsCacheKey key && Equals(key);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Text, Speed, SpeakerId);
        }

        public override string ToString()
        {
            return $"[{SpeakerId}|{Speed:F3}] {Text}";
        }
    }
}
