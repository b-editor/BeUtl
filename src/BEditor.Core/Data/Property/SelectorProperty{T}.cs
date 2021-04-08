﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using System.Text.Json;

using BEditor.Command;
using BEditor.Data.Bindings;
using BEditor.Resources;

namespace BEditor.Data.Property
{
    /// <summary>
    /// Represents a property for selecting a single item from an array.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    [DebuggerDisplay("Index = {Index}, Item = {SelectItem}")]
    public class SelectorProperty<T> : PropertyElement<SelectorPropertyMetadata<T>>, IEasingProperty, IBindable<T?>
        where T : IJsonObject, IEquatable<T>
    {
        #region Fields
        private static readonly PropertyChangedEventArgs _selectItemArgs = new(nameof(SelectItem));
        private T? _selectItem;
        private List<IObserver<T?>>? _list;
        private IDisposable? _bindDispose;
        private IBindable<T?>? _bindable;
        private string? _bindHint;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectorProperty"/> class.
        /// </summary>
        /// <param name="metadata">Metadata of this property.</param>
        /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is <see langword="null"/>.</exception>
        public SelectorProperty(SelectorPropertyMetadata<T> metadata)
        {
            PropertyMetadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            _selectItem = metadata.DefaultItem;
        }

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        public T? SelectItem
        {
            get => _selectItem;
            set => SetValue(value, ref _selectItem, _selectItemArgs, this, state =>
            {
                state.RaisePropertyChanged(SelectorProperty._indexArgs);
                foreach (var observer in state.Collection)
                {
                    try
                    {
                        observer.OnNext(state._selectItem);
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                    }
                }
            });
        }

        /// <summary>
        /// Gets the index of the selected <see cref="SelectorPropertyMetadata{T}.ItemSource"/>.
        /// </summary>
        public int Index
        {
            get
            {
                if (SelectItem is null) return -1;

                return PropertyMetadata?.ItemSource?.IndexOf(SelectItem) ?? -1;
            }
        }

        /// <inheritdoc/>
        public T? Value => SelectItem;

        /// <inheritdoc/>
        public string? TargetHint
        {
            get => _bindable?.ToString("#");
            private set => _bindHint = value;
        }

        private List<IObserver<T?>> Collection => _list ??= new();

        #region Methods

        /// <inheritdoc/>
        public override void GetObjectData(Utf8JsonWriter writer)
        {
            base.GetObjectData(writer);

            writer.WritePropertyName(nameof(Value));
            SelectItem?.GetObjectData(writer);

            writer.WriteString(nameof(TargetHint), TargetHint);
        }

        /// <inheritdoc/>
        public override void SetObjectData(JsonElement element)
        {
            base.SetObjectData(element);

            SelectItem = (T)FormatterServices.GetUninitializedObject(typeof(T));
            SelectItem.SetObjectData(element.GetProperty(nameof(Value)));
            TargetHint = element.TryGetProperty(nameof(TargetHint), out var bind) ? bind.GetString() : null;
        }

        /// <summary>
        /// Create a command to change the selected item.
        /// </summary>
        /// <param name="value">New value for <see cref="SelectItem"/>.</param>
        /// <returns>Created <see cref="IRecordCommand"/>.</returns>
        [Pure]
        public IRecordCommand ChangeSelect(T? value) => new ChangeSelectCommand(this, value);

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<T?> observer)
        {
            return BindingHelper.Subscribe(Collection, observer, Value);
        }

        /// <inheritdoc/>
        public void OnCompleted()
        {
        }

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
        }

        /// <inheritdoc/>
        public void OnNext(T? value)
        {
            SelectItem = value;
        }

        /// <inheritdoc/>
        public void Bind(IBindable<T?>? bindable)
        {
            SelectItem = this.Bind(bindable, out _bindable, ref _bindDispose);
        }

        /// <inheritdoc/>
        protected override void OnLoad()
        {
            this.AutoLoad(ref _bindHint);
        }

        #endregion

        #region Commands

        /// <summary>
        /// 選択されているアイテムを変更するコマンド.
        /// </summary>
        private sealed class ChangeSelectCommand : IRecordCommand
        {
            private readonly WeakReference<SelectorProperty<T>> _property;
            private readonly T? _new;
            private readonly T? _old;

            /// <summary>
            /// <see cref="ChangeSelectCommand"/> クラスの新しいインスタンスを初期化します.
            /// </summary>
            /// <param name="property">対象の <see cref="SelectorProperty"/>.</param>
            /// <param name="select">新しいインデックス.</param>
            /// <exception cref="ArgumentNullException"><paramref name="property"/> が <see langword="null"/> です.</exception>
            public ChangeSelectCommand(SelectorProperty<T> property, T? select)
            {
                _property = new(property ?? throw new ArgumentNullException(nameof(property)));
                _new = select;
                _old = property.SelectItem;
            }

            public string Name => Strings.ChangeSelectItem;

            /// <inheritdoc/>
            public void Do()
            {
                if (_property.TryGetTarget(out var target))
                {
                    target.SelectItem = _new;
                }
            }

            /// <inheritdoc/>
            public void Redo()
            {
                Do();
            }

            /// <inheritdoc/>
            public void Undo()
            {
                if (_property.TryGetTarget(out var target))
                {
                    target.SelectItem = _old;
                }
            }
        }

        #endregion
    }
}
