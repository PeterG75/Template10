﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Prism.Services
{
    public class GestureService : IGestureService
    {
        private static Dictionary<CoreWindow, IGestureService> _cache
            = new Dictionary<CoreWindow, IGestureService>();

        public static IGestureService GetForCurrentView(CoreWindow window = null)
        {
            if (!_cache.ContainsKey(window ?? Window.Current.CoreWindow))
            {
                throw new Exception("Not setup for current view.");
            }
            return _cache[Window.Current.CoreWindow];
        }

        public static void SetupForCurrentView(CoreWindow window)
        {
            if (_cache.ContainsKey(window))
            {
                throw new Exception("Already setup for current view.");
            }
            _cache.Add(window, new GestureService(window));

            // remove when closed

            void Window_Closed(CoreWindow sender, CoreWindowEventArgs args)
            {
                window.Closed -= Window_Closed;
                if (_cache.ContainsKey(window))
                {
                    (_cache[window] as GestureService).Dispose(window);
                    _cache.Remove(window);
                }
            }
            window.Closed += Window_Closed;
        }

        private GestureService(CoreWindow window)
        {
            window.Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;
            window.PointerPressed += CoreWindow_PointerPressed;
            SystemNavigationManager.GetForCurrentView().BackRequested += GestureService_BackRequested;
        }

        public event EventHandler MenuRequested;
        public event EventHandler BackRequested;
        public event EventHandler SearchRequested;
        public event EventHandler RefreshRequested;
        public event EventHandler ForwardRequested;
        public event TypedEventHandler<object, KeyDownEventArgs> KeyDown;

        #region Barrier

        List<GestureBarrier> _barriers = new List<GestureBarrier>();
        public GestureBarrier CreateBarrier(Gesture gesture)
        {
            GestureBarrier barrier = null;
            return barrier = new GestureBarrier
            {
                Gesture = gesture,
                Complete = () => _barriers.Remove(barrier),
            };
        }
        bool IfCanRaiseEvent(Gesture evt, Action action)
        {
            if (_barriers.Any(x => x.Gesture.Equals(evt)))
            {
                return false;
            }
            action();
            return true;
        }

        #endregion  

        public bool RaiseRefreshRequested() => IfCanRaiseEvent(Gesture.Refresh, () => RefreshRequested?.Invoke(this, EventArgs.Empty));
        public bool RaiseBackRequested() => IfCanRaiseEvent(Gesture.Back, () => BackRequested?.Invoke(this, EventArgs.Empty));
        public bool RaiseForwardRequested() => IfCanRaiseEvent(Gesture.Forward, () => ForwardRequested?.Invoke(this, EventArgs.Empty));
        public bool RaiseSearchRequested() => IfCanRaiseEvent(Gesture.Search, () => SearchRequested?.Invoke(this, EventArgs.Empty));
        public bool RaiseMenuRequested() => IfCanRaiseEvent(Gesture.Menu, () => MenuRequested?.Invoke(null, EventArgs.Empty));

        private void Dispose(CoreWindow window)
        {
            window.Dispatcher.AcceleratorKeyActivated -= Dispatcher_AcceleratorKeyActivated;
            window.PointerPressed -= CoreWindow_PointerPressed;
            SystemNavigationManager.GetForCurrentView().BackRequested -= GestureService_BackRequested;
        }

        private void GestureService_BackRequested(object sender, BackRequestedEventArgs e)
        {
            RaiseBackRequested();
        }

        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs e)
        {
            var properties = e.CurrentPoint.Properties;
            // Ignore button chords with the left, right, and middle buttons
            if (properties.IsLeftButtonPressed || properties.IsRightButtonPressed ||
                properties.IsMiddleButtonPressed)
                return;
            TestForNavigateRequested(e, properties);
        }

        private void Dispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs e)
        {
            if (!e.EventType.ToString().Contains("Down") || e.Handled)
            {
                return;
            }
            var args = new KeyDownEventArgs(e.VirtualKey) { EventArgs = e };
            TestForSearchRequested(args);
            TestForMenuRequested(args);
            TestForNavigateRequested(args);
            KeyDown?.Invoke(null, args);
        }

        private void TestForNavigateRequested(KeyDownEventArgs e)
        {
            if ((e.VirtualKey == VirtualKey.GoBack)
                || (e.VirtualKey == VirtualKey.NavigationLeft)
                || (e.VirtualKey == VirtualKey.GamepadMenu)
                || (e.VirtualKey == VirtualKey.GamepadLeftShoulder)
                || (e.OnlyAlt && e.VirtualKey == VirtualKey.Back)
                || (e.OnlyAlt && e.VirtualKey == VirtualKey.Left))
            {
                Debug.WriteLine($"{nameof(GestureService)}.{nameof(BackRequested)}");
                RaiseBackRequested();
            }
            else if ((e.VirtualKey == VirtualKey.GoForward)
                || (e.VirtualKey == VirtualKey.NavigationRight)
                || (e.VirtualKey == VirtualKey.GamepadRightShoulder)
                || (e.OnlyAlt && e.VirtualKey == VirtualKey.Right))
            {
                Debug.WriteLine($"{nameof(GestureService)}.{nameof(ForwardRequested)}");
                RaiseForwardRequested();
            }
            else if ((e.VirtualKey == VirtualKey.Refresh)
                || (e.VirtualKey == VirtualKey.F5))
            {
                Debug.WriteLine($"{nameof(GestureService)}.{nameof(RefreshRequested)}");
                RaiseRefreshRequested();
            }
            // this is still a preliminary value?
            else if ((e.VirtualKey == VirtualKey.M) && e.OnlyAlt)
            {
                Debug.WriteLine($"{nameof(GestureService)}.{nameof(MenuRequested)}");
                RaiseMenuRequested();
            }
        }

        private void TestForNavigateRequested(PointerEventArgs e, PointerPointProperties properties)
        {
            // If back or foward are pressed (but not both) 
            var backPressed = properties.IsXButton1Pressed;
            var forwardPressed = properties.IsXButton2Pressed;
            if (backPressed ^ forwardPressed)
            {
                e.Handled = true;
                if (backPressed)
                {
                    Debug.WriteLine($"{nameof(GestureService)}.{nameof(BackRequested)}");
                    RaiseBackRequested();
                }
                else if (forwardPressed)
                {
                    Debug.WriteLine($"{nameof(GestureService)}.{nameof(ForwardRequested)}");
                    RaiseForwardRequested();
                }
            }
        }

        private void TestForMenuRequested(KeyDownEventArgs args)
        {
            if (args.VirtualKey == VirtualKey.GamepadMenu)
            {
                Debug.WriteLine($"{nameof(GestureService)}.{nameof(MenuRequested)}");
                RaiseMenuRequested();
            }
        }

        private void TestForSearchRequested(KeyDownEventArgs args)
        {
            if (args.OnlyControl && args.Character.ToString().ToLower().Equals("e"))
            {
                Debug.WriteLine($"{nameof(GestureService)}.{nameof(SearchRequested)}");
                RaiseSearchRequested();
            }
        }
    }
}
