using System;

namespace GamepadTester.Models
{
    public sealed class StickState
    {
        public float X { get; set; }
        public float Y { get; set; }

        public float Magnitude
        {
            get { return (float)Math.Sqrt((X * X) + (Y * Y)); }
        }
    }
}
