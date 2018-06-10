﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoysOfEfficiency
{
    public  class RectangleE
    {
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }

        public RectangleE(Rectangle parent)
        {
            X = parent.Left;
            Y = parent.Top;
            Width = parent.Width;
            Height = parent.Height;
        }

        public RectangleE(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool IsInternalPoint(float x, float y)
        {
            return x >= X && x <= X + Width && y >= Y && y < Y + Height;
        }
    }
}
