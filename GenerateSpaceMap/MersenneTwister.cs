/* A C-program for MT19937, with initialization improved 2002/2/10.
 * Coded by Takuji Nishimura and Makoto Matsumoto.
 * This is a faster version by taking Shawn Cokus's optimization,
 * Matthe Bellew's simplification, Isaku Wada's real version.
 * 
 * Before using, initialize the state by using init_genrand(seed)
 * or init_by_array(init_key, key_length).
 * 
 * Copyright (C) 1997 - 2002, Makoto Matsumoto and Takuji Nishimura,
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 
 * 3. The names of its contributors may not be used to endorse or promote
 *    products derived from this software without specific prior written
 *    permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT
 * OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
 * TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 * Any feedback is very welcome.
 * http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/emt.html
 * email: m-mat @ math.sci.hiroshima-u.ac.jp (remove space) */

using System;

namespace GenerateSpaceMap
{
    public class MersenneTwister : Random
    {
        public MersenneTwister(int seed)
        {
            Initialize((uint)seed);
        }

        public MersenneTwister()
            : this(new Random().Next())
        {
        }

        public MersenneTwister(int[] initKey)
        {
            if (initKey == null)
            {
                throw new ArgumentNullException("initKey");
            }

            var initArray = new uint[initKey.Length];
            for (int i = 0; i < initKey.Length; ++i)
            {
                initArray[i] = (uint)initKey[i];
            }

            Initialize(initArray);
        }

        public override int Next()
        {
            return this.Next(int.MaxValue);
        }

        public override int Next(int maxValue)
        {
            if (maxValue <= 1)
            {
                if (maxValue < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return 0;
            }

            return (int)(this.NextDouble() * maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            if (maxValue <= minValue)
            {
                throw new ArgumentOutOfRangeException();
            }
            else if (maxValue == minValue)
            {
                return minValue;
            }

            return this.Next(maxValue - minValue) + minValue;
        }

        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException();
            }

            int length = buffer.Length;
            for (int i = 0; i < length; ++i)
            {
                buffer[i] = (byte)this.Next(256);
            }
        }

        public override double NextDouble()
        {
            return this.Compute53BitRandom(0, InverseOnePlus53BitsOf1s);
        }

        protected uint GenerateUInt32()
        {
            uint y;

            /* _mag01[x] = x * MatrixA  for x=0,1 */
            if (_mti >= N) /* generate N words at one time */
            {
                Int16 kk = 0;

                for (; kk < N - M; ++kk)
                {
                    y = (_mt[kk] & UpperMask) | (_mt[kk + 1] & LowerMask);
                    _mt[kk] = _mt[kk + M] ^ (y >> 1) ^ _mag01[y & 0x1];
                }

                for (; kk < N - 1; ++kk)
                {
                    y = (_mt[kk] & UpperMask) | (_mt[kk + 1] & LowerMask);
                    _mt[kk] = _mt[kk + (M - N)] ^ (y >> 1) ^ _mag01[y & 0x1];
                }

                y = (_mt[N - 1] & UpperMask) | (_mt[0] & LowerMask);
                _mt[N - 1] = _mt[M - 1] ^ (y >> 1) ^ _mag01[y & 0x1];

                _mti = 0;
            }

            y = _mt[_mti++];
            y ^= TemperingShiftU(y);
            y ^= TemperingShiftS(y) & TemperingMaskB;
            y ^= TemperingShiftT(y) & TemperingMaskC;
            y ^= TemperingShiftL(y);

            return y;
        }

        // Period parameters
        private const int N = 624;
        private const int M = 397;
        private const uint MatrixA = 0x9908B0DF; // constant vector a
        private const uint UpperMask = 0x80000000; // most significant w-r bits
        private const uint LowerMask = 0x7FFFFFFF; // least significant r bits

        // Tempering parameters
        private const uint TemperingMaskB = 0x9D2C5680;
        private const uint TemperingMaskC = 0xEFC60000;

        private static uint TemperingShiftU(uint y)
        {
            return (y >> 11);
        }

        private static uint TemperingShiftS(uint y)
        {
            return (y << 7);
        }

        private static uint TemperingShiftT(uint y)
        {
            return (y << 15);
        }

        private static uint TemperingShiftL(uint y)
        {
            return (y >> 18);
        }

        private readonly uint[] _mt = new uint[N]; // the array for the state vector
        private short _mti;

        private static readonly uint[] _mag01 = { 0x0, MatrixA };

        private void Initialize(uint seed)
        {
            _mt[0] = seed & 0xffffffffU;

            for (_mti = 1; _mti < N; _mti++)
            {
                _mt[_mti] = (uint)(1812433253U * (_mt[_mti - 1] ^ (_mt[_mti - 1] >> 30)) + _mti);
                /* See Knuth TAOCP Vol2. 3rd Ed. P.106 for multiplier.
                 * In the previous versions, MSBs of the seed affect
                 * only MSBs of the array _mt[].
                 * 2002/01/09 modified by Makoto Matsumoto */
                _mt[_mti] &= 0xffffffffU;
                // for >32 bit machines
            }
        }

        private void Initialize(uint[] key)
        {
            int i, j, k;
            this.Initialize(19650218U);

            int keyLength = key.Length;
            i = 1;
            j = 0;
            k = (N > keyLength ? N : keyLength);

            for (; k > 0; k--)
            {
                _mt[i] = (uint)((_mt[i] ^ ((_mt[i - 1] ^ (_mt[i - 1] >> 30)) * 1664525U)) + key[j] + j); /* non linear */
                _mt[i] &= 0xffffffffU; // for WORDSIZE > 32 machines
                i++;
                j++;
                if (i >= N) { _mt[0] = _mt[N - 1]; i = 1; }
                if (j >= keyLength)
                    j = 0;
            }

            for (k = N - 1; k > 0; k--)
            {
                _mt[i] = (uint)((_mt[i] ^ ((_mt[i - 1] ^ (_mt[i - 1] >> 30)) * 1566083941U)) - i); /* non linear */
                _mt[i] &= 0xffffffffU; // for WORDSIZE > 32 machines
                i++;

                if (i < N)
                {
                    continue;
                }

                _mt[0] = _mt[N - 1];
                i = 1;
            }

            // MSB is 1; assuring non-zero initial array
            _mt[0] = 0x80000000U;
        }


        /* 9007199254740991.0 is the maximum double value which the
         * 53 significand can hold when the exponent is 0. */
        private const double FiftyThreeBitsOf1s = 9007199254740991.0;

        // Multiply by inverse to (vainly?) try to avoid a division.
        private const double Inverse53BitsOf1s = 1.0 / FiftyThreeBitsOf1s;
        private const double OnePlus53BitsOf1s = FiftyThreeBitsOf1s + 1;
        private const double InverseOnePlus53BitsOf1s = 1.0 / OnePlus53BitsOf1s;

        private double Compute53BitRandom(double translate, double scale)
        {
            // get 27 pseudo-random bits
            var a = (ulong)this.GenerateUInt32() >> 5;
            // get 26 pseudo-random bits
            var b = (ulong)this.GenerateUInt32() >> 6;

            /* shift the 27 pseudo-random bits (a) over by 26
             * bits (* 67108864.0) and add another pseudo-random
             * 26 bits (+ b). */
            return ((a * 67108864.0 + b) + translate) * scale;
        }
    }
}
