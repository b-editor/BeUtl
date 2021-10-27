﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia;
using Avalonia.Rendering;

#nullable disable

namespace BEditor.Controls
{
    public class MinMaxCloseControl : TemplatedControl
    {
        private IDisposable _windowStateObservable;
        private IDisposable _windowcanResizeObservable;
        private FluentWindow _owner;
        private Button _minimizeButton;
        private Button _maximizeButton;
        private Button _closeButton;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            if (_minimizeButton != null)
                _minimizeButton.Click -= OnButtonClick;

            if (_maximizeButton != null)
                _maximizeButton.Click -= OnButtonClick;

            if (_closeButton != null)
                _closeButton.Click -= OnButtonClick;

            base.OnApplyTemplate(e);

            _minimizeButton = e.NameScope.Find<Button>("MinimizeButton");
            if (_minimizeButton != null)
                _minimizeButton.Click += OnButtonClick;

            _maximizeButton = e.NameScope.Find<Button>("MaxRestoreButton");
            if (_maximizeButton != null)
            {
                _maximizeButton.Click += OnButtonClick;
                if (_owner != null)
                {
                    OnCanResizeChanged(_owner.CanResize);
                }
            }

            _closeButton = e.NameScope.Find<Button>("CloseButton");
            if (_closeButton != null)
                _closeButton.Click += OnButtonClick;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _owner = this.VisualRoot as FluentWindow;

            if (_owner != null)
            {
                _windowStateObservable = _owner.GetObservable(Window.WindowStateProperty)
                    .Subscribe(OnWindowStateChanged);

                _windowcanResizeObservable = _owner.GetObservable(Window.CanResizeProperty)
                    .Subscribe(OnCanResizeChanged);

                OnCanResizeChanged(_owner.CanResize);
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _windowStateObservable?.Dispose();
            _windowStateObservable = null;
            _windowcanResizeObservable?.Dispose();
            _windowcanResizeObservable = null;
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (_owner == null)
                return;

            if (sender == _minimizeButton)
            {
                _owner.WindowState = WindowState.Minimized;
            }
            else if (sender == _maximizeButton)
            {
                if (_owner.WindowState == WindowState.Maximized)
                {
                    _owner.WindowState = WindowState.Normal;
                }
                else if (_owner.WindowState == WindowState.Normal)
                {
                    _owner.WindowState = WindowState.Maximized;
                }
            }
            else if (sender == _closeButton)
            {
                _owner.Close();
            }
        }

        private void OnCanResizeChanged(bool obj)
        {
            if (_maximizeButton != null)
                _maximizeButton.IsVisible = obj;
        }

        private void OnWindowStateChanged(WindowState state)
        {
            PseudoClasses.Set(":maximized", state == WindowState.Maximized);
        }

        internal bool HitTestMaxButton(Point pos)
        {
            if (_maximizeButton != null)
                return _maximizeButton.HitTestCustom(pos);

            return false;
        }

        internal void FakeMaximizeHover(bool hover)
        {
            if (_maximizeButton != null)
            {
                // We can't set the IsPointerOver property b/c it's readonly and that make things angry
                // so we'll just force set the Pseudoclass
                ((IPseudoClasses)_maximizeButton.Classes).Set(":pointerover", hover);
                //_maximizeButton.SetValue(InputElement.IsPointerOverProperty, hover);
            }
        }

        internal void FakeMaximizePressed(bool pressed)
        {
            if (_maximizeButton != null)
            {
                _maximizeButton.SetValue(Button.IsPressedProperty, pressed);
            }
        }

        internal void FakeMaximizeClick()
        {
            OnButtonClick(_maximizeButton, null);
        }
    }
}
