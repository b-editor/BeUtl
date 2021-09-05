﻿// Rational.cs
//
// Copyright (C) BEditor
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BEditor.Media
{
    /// <summary>
    /// The rational.
    /// </summary>
    public readonly struct Rational : IEquatable<Rational>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rational"/> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Rational(int value)
            : this(value, 1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rational"/> struct.
        /// </summary>
        /// <param name="numerator">The numerator.</param>
        /// <param name="denominator">The denominator.</param>
        public Rational(int numerator, int denominator)
        {
            if (denominator == 0)
            {
                throw new ArithmeticException("Denominator must not be 0");
            }

            Numerator = numerator;
            Denominator = denominator;
        }

        /// <summary>
        /// Gets the numerator.
        /// </summary>
        public int Numerator { get; }

        /// <summary>
        /// Gets the denominator.
        /// </summary>
        public int Denominator { get; }

        /// <summary>
        /// Returns a new <see cref="Rational"/> object whose value is the result of multiplying the specified <see cref="Rational"/> instance and the specified factor.
        /// </summary>
        /// <param name="left">The value to be multiplied.</param>
        /// <param name="right">The value to be multiplied by.</param>
        /// <returns>A new object that represents the value of the specified <see cref="Rational"/> instance multiplied by the value of the specified factor.</returns>
        public static Rational operator *(Rational left, Rational right)
        {
            return new Rational(left.Numerator * right.Numerator, left.Denominator * right.Denominator);
        }

        /// <summary>
        /// Returns a new <see cref="Rational"/> value which is the result of division of <paramref name="left"/> instance and the specified <paramref name="right"/>.
        /// </summary>
        /// <param name="left">Divident or the value to be divided.</param>
        /// <param name="right">The value to be divided by.</param>
        /// <returns>A new value that represents result of division of <paramref name="left"/> instance by the value of the <paramref name="right"/>.</returns>
        public static Rational operator /(Rational left, Rational right)
        {
            if (right.Numerator == 0)
            {
                throw new DivideByZeroException();
            }

            return new Rational(left.Numerator * right.Denominator, left.Denominator * right.Numerator);
        }

        /// <summary>
        /// Indicates whether two <see cref="Rational"/> instances are equal.
        /// </summary>
        /// <param name="left">The first rational to compare.</param>
        /// <param name="right">The second rational to compare.</param>
        /// <returns>true if the values of <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, false.</returns>
        public static bool operator ==(Rational left, Rational right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two <see cref="Rational"/> instances are not equal.
        /// </summary>
        /// <param name="left">The first rational to compare.</param>
        /// <param name="right">The second rational to compare.</param>
        /// <returns>true if the values of <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, false.</returns>
        public static bool operator !=(Rational left, Rational right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Denominator == 1) return Numerator.ToString();

            return $"{Numerator}/{Denominator}";
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is Rational rational && Equals(rational);
        }

        /// <inheritdoc/>
        public bool Equals(Rational other)
        {
            return Numerator == other.Numerator &&
                   Denominator == other.Denominator;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Numerator, Denominator);
        }
    }
}
