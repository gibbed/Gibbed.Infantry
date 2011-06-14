/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
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
    public static class PerlinNoise
    {
        public static byte[,] Generate(
            int width, int height,
            float frequency,
            float amplitude,
            float persistance,
            int octaves,
            Random rng)
        {
            var noise = GenerateNoise(rng, width, height);
            
            var map = new byte[width, height];
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    var value = GetValue(
                        x, y, width, height,
                        frequency, amplitude, persistance, octaves, noise);
                    
                    value = (value * 0.5f) + 0.5f;
                    value *= 255;

                    map[x, y] = (byte)value;
                }
            }

            return map;
        }

        private static float GetValue(
            int x, int y, int width, int height,
            float frequency,
            float amplitude,
            float persistance,
            int octaves,
            float[,] noise)
        {
            float value = 0.0f;

            for (int i = 0; i < octaves; ++i)
            {
                value += GetSmoothNoise(
                    x * frequency, y * frequency,
                    width, height,
                    noise) * amplitude;
                frequency *= 2.0f;
                amplitude *= persistance;
            }

            if (value < -1.0f)
            {
                value = -1.0f;
            }
            else if (value > 1.0f)
            {
                value = 1.0f;
            }

            return value;
        }

        private static float GetSmoothNoise(
            float x, float y, int width, int height,
            float[,] noise)
        {
            float fractionX = x - (int)x;
            float fractionY = y - (int)y;

            int x1 = ((int)x + width) % width;
            int y1 = ((int)y + height) % height;
            int x2 = ((int)x + width - 1) % width;
            int y2 = ((int)y + height - 1) % height;

            float value = 0.0f;
            value += fractionX * fractionY * noise[x1, y1];
            value += fractionX * (1 - fractionY) * noise[x1, y2];
            value += (1 - fractionX) * fractionY * noise[x2, y1];
            value += (1 - fractionX) * (1 - fractionY) * noise[x2, y2];
            return value;
        }

        private static float[,] GenerateNoise(
            Random rng,
            int width,
            int height)
        {
            var noise = new float[width, height];

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    noise[x, y] = ((float)(rng.NextDouble()) - 0.5f) * 2.0f;
                }
            }

            return noise;
        }
    }
}
