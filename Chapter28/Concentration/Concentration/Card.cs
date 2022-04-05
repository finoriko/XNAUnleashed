using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Concentration
{
    public class Card
    {
        public byte Value;
        public bool IsFaceUp = false;
        public bool IsVisible = true;
        public HalfVector2 Location;
    }
}
