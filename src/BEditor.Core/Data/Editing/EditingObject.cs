﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;

using BEditor.Data.Property;
using BEditor.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BEditor.Data
{
    /// <summary>
    /// Represents the base class of the edit data.
    /// </summary>
    public class EditingObject : BasePropertyChanged, IEditingObject, IJsonObject
    {
        private Dictionary<string, object?>? _values = new();
        private Type? _ownerType;

        /// <inheritdoc/>
        public SynchronizationContext Synchronize { get; private set; } = AsyncOperationManager.SynchronizationContext;

        /// <inheritdoc/>
        public IServiceProvider? ServiceProvider { get; internal set; }

        /// <inheritdoc/>
        public bool IsLoaded { get; private set; }

        private Dictionary<string, object?> Values => _values ??= new();

        private Type OwnerType => _ownerType ??= GetType();

        /// <inheritdoc/>
        public object? this[EditingProperty property]
        {
            get => GetValue(property);
            set => SetValue(property, value);
        }

        /// <inheritdoc/>
        public TValue GetValue<TValue>(EditingProperty<TValue> property)
        {
            return (TValue)GetValue((EditingProperty)property)!;
        }

        /// <inheritdoc/>
        public object? GetValue(EditingProperty property)
        {
            if (CheckOwnerType(this, property))
            {
                throw new DataException(Strings.TheOwnerTypeDoesNotMatch);
            }

            if (property is IDirectProperty directProp)
            {
                var value = directProp.Get(this);
                if (value is null)
                {
                    value = directProp.Initializer?.Create();

                    if (value is not null)
                    {
                        directProp.Set(this, value);
                    }
                }

                return value;
            }

            if (!Values.ContainsKey(property.Name))
            {
                var value = property.Initializer?.Create();
                Values.Add(property.Name, value);

                return value;
            }

            return Values[property.Name];
        }

        /// <inheritdoc/>
        public void SetValue<TValue>(EditingProperty<TValue> property, TValue value)
        {
            SetValue((EditingProperty)property, value);
        }

        /// <inheritdoc/>
        public void SetValue(EditingProperty property, object? value)
        {
            if (value is not null && CheckValueType(property, value))
            {
                throw new DataException(string.Format(Strings.TheValueWasNotTypeButType, property.ValueType, value.GetType()));
            }
            if (CheckOwnerType(this, property))
            {
                throw new DataException(Strings.TheOwnerTypeDoesNotMatch);
            }

            if (property is IDirectProperty directProp)
            {
                directProp.Set(this, value!);

                return;
            }

            if (AddIfNotExist(property, value))
            {
                return;
            }

            Values[property.Name] = value;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            static void ClearChildren(EditingObject @object)
            {
                if (@object is IParent<IEditingObject> parent)
                {
                    foreach (var child in parent.Children)
                    {
                        child.Clear();
                    }
                }

                if (@object is IKeyFrameProperty property)
                {
                    property.EasingType?.Clear();
                }
            }

            foreach (var value in Values)
            {
                if (value.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            Values.Clear();
            ClearChildren(this);
        }

        /// <inheritdoc/>
        public void Load()
        {
            if (IsLoaded) return;

            if (this is IChild<EditingObject> obj1)
            {
                ServiceProvider = obj1.Parent?.ServiceProvider;
            }

            if (this is IChild<IApplication> child_app)
            {
                ServiceProvider = child_app.Parent?.Services.BuildServiceProvider();
            }

            OnLoad();

            if (this is IParent<EditingObject> obj2)
            {
                foreach (var prop in EditingProperty.PropertyFromKey
                    .Where(i => OwnerType.IsAssignableTo(i.Key.OwnerType))
                    .Select(i => i.Value))
                {
                    var value = this[prop];
                    if (value is PropertyElement p && prop.Initializer is PropertyElementMetadata pmeta)
                    {
                        p.PropertyMetadata = pmeta;
                    }
                }

                foreach (var item in obj2.Children)
                {
                    item.Load();
                }
            }

            IsLoaded = true;
        }

        /// <inheritdoc/>
        public void Unload()
        {
            if (!IsLoaded) return;

            OnUnload();

            if (this is IParent<EditingObject> obj)
            {
                foreach (var item in obj.Children)
                {
                    item.Unload();
                }
            }

            IsLoaded = false;
        }

        /// <inheritdoc/>
        public virtual void GetObjectData(Utf8JsonWriter writer)
        {
            foreach (var prop in EditingProperty.PropertyFromKey
                .Where(i => i.Value.Serializer is not null && OwnerType.IsAssignableTo(i.Key.OwnerType))
                .Select(i => i.Value))
            {
                var value = GetValue(prop);

                if (value is not null)
                {
                    prop.Serializer!.Write(writer, value);
                }
            }
        }

        /// <inheritdoc/>
        public virtual void SetObjectData(JsonElement element)
        {
            Synchronize = AsyncOperationManager.SynchronizationContext;

            foreach (var prop in EditingProperty.PropertyFromKey
                .Where(i => i.Value.Serializer is not null && OwnerType.IsAssignableTo(i.Key.OwnerType))
                .Select(i => i.Value))
            {
                SetValue(prop, prop.Serializer!.Read(element));
            }
        }

        /// <inheritdoc cref="IElementObject.Load"/>
        protected virtual void OnLoad()
        {
        }

        /// <inheritdoc cref="IElementObject.Unload"/>
        protected virtual void OnUnload()
        {
        }

        // 値の型が一致しない場合はtrue
        private static bool CheckValueType(EditingProperty property, object value)
        {
            var valueType = value.GetType();

            return !valueType.IsAssignableTo(property.ValueType);
        }

        // オーナーの型が一致しない場合はtrue
        private static bool CheckOwnerType(EditingObject obj, EditingProperty property)
        {
            var ownerType = obj.GetType();

            return !ownerType.IsAssignableTo(property.OwnerType);
        }

        // 追加した場合はtrue
        private bool AddIfNotExist(EditingProperty property, object? value)
        {
            if (!Values.ContainsKey(property.Name))
            {
                Values.Add(property.Name, value);

                return true;
            }

            return false;
        }
    }
}
