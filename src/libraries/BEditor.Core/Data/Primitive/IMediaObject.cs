﻿// IMediaObject.cs
//
// Copyright (C) BEditor
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.

using System;

namespace BEditor.Data.Primitive
{
    /// <summary>
    /// Abstraction of media objects.
    /// </summary>
    public interface IMediaObject : IEditingObject
    {
        /// <summary>
        /// Gets the length.
        /// </summary>
        public TimeSpan? Length { get; }
    }
}