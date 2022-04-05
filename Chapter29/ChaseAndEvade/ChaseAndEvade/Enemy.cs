using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ChaseAndEvade
{
    public class Enemy
    {
        public enum AIState { Attack, Retreat, Search }

        public AIState State;
        public Vector3 Position;
        public Vector3 Velocity;
        public Color Color;
        public float ChangeDirectionTimer;
        public Vector3 RandomVelocity;
        public int RandomSeconds;
        public int Health;
    }
}
