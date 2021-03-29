﻿using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace BEditor.Data
{
    /// <summary>
    /// The metadata of <see cref="ObjectElement"/>.
    /// </summary>
    /// <param name="Name">The name of the object element.</param>
    /// <param name="CreateFunc">This <see cref="Func{TResult}"/> gets a new instance of the <see cref="ObjectElement"/> object.</param>
    /// <param name="Type">The type of the object that inherits from <see cref="ObjectElement"/>.</param>
    public record ObjectMetadata(string Name, Func<ObjectElement> CreateFunc, Type Type)
    {
        /// <summary>
        /// The metadata of <see cref="ObjectElement"/>.
        /// </summary>
        /// <param name="Name">The name of the object element.</param>
        /// <param name="Create">This <see cref="Func{TResult}"/> gets a new instance of the <see cref="ObjectElement"/> object.</param>
        public ObjectMetadata(string Name, Expression<Func<ObjectElement>> Create)
            : this(Name, Create.Compile(), ((NewExpression)Create.Body).Type)
        {
        }

        /// <summary>
        /// Gets the loaded <see cref="ObjectMetadata"/>.
        /// </summary>
        public static ObservableCollection<ObjectMetadata> LoadedObjects { get; } = new();

        /// <summary>
        /// Create the <see cref="ObjectMetadata"/>.
        /// </summary>
        /// <typeparam name="T">The type of object that inherits from <see cref="ObjectElement"/>.</typeparam>
        /// <param name="Name">The name of the object element.</param>
        /// <returns>A new instance of <see cref="ObjectMetadata"/>.</returns>
        public static ObjectMetadata Create<T>(string Name)
            where T : ObjectElement, new()
        {
            return new(Name, () => new T(), typeof(T));
        }
    }
}
