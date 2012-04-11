/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;

namespace GenerateSpaceMap
{
    internal sealed class TemplateEntity
    {
        public readonly int OffsetX;
        public readonly int OffsetY;
        public readonly int Width;
        public readonly int Height;
        public readonly string Category;
        public readonly string BloName;
        public readonly string CfsName;
        public readonly byte[,] Physics;
        public readonly byte[,] Vision;

        public TemplateEntity(
            int offsetX,
            int offsetY,
            int width,
            int height,
            string category,
            string bloName,
            string cfsName)
        {
            this.OffsetX = offsetX;
            this.OffsetY = offsetY;
            this.Width = width;
            this.Height = height;
            this.Category = category;
            this.BloName = bloName;
            this.CfsName = cfsName;
            this.Physics = new byte[width,height];
            this.Vision = new byte[width,height];
        }

        public bool CanPlaceWithPhysics(
            int x,
            int y,
            bool[,] blocked,
            int width,
            int height)
        {
            if (x + this.Width > width ||
                y + this.Height > height)
            {
                return false;
            }

            for (int rx = 0; rx < this.Width; rx++)
            {
                for (int ry = 0; ry < this.Height; ry++)
                {
                    if (this.Physics[rx, ry] > 0 &&
                        blocked[x + rx, y + ry] == true)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool CanPlaceWithVision(
            int x,
            int y,
            bool[,] blocked,
            int width,
            int height)
        {
            if (x + this.Width > width ||
                y + this.Height > height)
            {
                return false;
            }

            for (int rx = 0; rx < this.Width; rx++)
            {
                for (int ry = 0; ry < this.Height; ry++)
                {
                    if (this.Vision[rx, ry] > 0 &&
                        blocked[x + rx, y + ry] == true)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void BlockPhysics(
            int x,
            int y,
            bool[,] blocked)
        {
            for (int rx = 0; rx < this.Width; rx++)
            {
                for (int ry = 0; ry < this.Height; ry++)
                {
                    blocked[x + rx, y + ry] =
                        this.Physics[rx, ry] > 0;
                }
            }
        }

        public void BlockVision(
            int x,
            int y,
            bool[,] blocked)
        {
            for (int rx = 0; rx < this.Width; rx++)
            {
                for (int ry = 0; ry < this.Height; ry++)
                {
                    blocked[x + rx, y + ry] =
                        this.Vision[rx, ry] > 0;
                }
            }
        }

        public void SetupPhysics(string physics)
        {
            if (physics == null)
            {
                throw new ArgumentNullException("physics");
            }

            int i;
            int x = 0, y = 0;
            for (i = 0; i < physics.Length; i++)
            {
                if (char.IsWhiteSpace(physics[i]) == true)
                {
                    continue;
                }

                if (y >= this.Height)
                {
                    throw new ArgumentException("too much data", "physics");
                }

                var value = (int)physics[i];
                if (value < 48 || value > 57)
                {
                    throw new ArgumentException("bad physics value: " + physics[i], "physics");
                }

                this.Physics[x, y] = (byte)(value - 48);

                x++;
                if (x >= this.Width)
                {
                    y++;
                    x = 0;
                }
            }

            if (x != 0 &&
                y != this.Height)
            {
                throw new ArgumentException("not enough data", "physics");
            }
        }

        public void SetupVision(string vision)
        {
            if (vision == null)
            {
                throw new ArgumentNullException("vision");
            }

            int i;
            int x = 0, y = 0;
            for (i = 0; i < vision.Length; i++)
            {
                if (char.IsWhiteSpace(vision[i]) == true)
                {
                    continue;
                }

                if (y >= this.Height)
                {
                    throw new ArgumentException("too much data", "vision");
                }

                var value = (int)vision[i];
                if (value < 48 || value > 57)
                {
                    throw new ArgumentException("bad vision value: " + vision[i], "vision");
                }

                this.Vision[x, y] = (byte)(value - 48);

                x++;
                if (x >= this.Width)
                {
                    y++;
                    x = 0;
                }
            }

            if (x != 0 &&
                y != this.Height)
            {
                throw new ArgumentException("not enough data", "vision");
            }
        }
    }
}
