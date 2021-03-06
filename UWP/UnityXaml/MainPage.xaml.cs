﻿// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using System;
using UnityPlayer;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UnityXAML
{
    public sealed partial class MainPage : Page
    {
        private WinRTBridge.WinRTBridge bridge;

        private SplashScreen splash;
        private Rect splashImageRect;
        private WindowSizeChangedEventHandler onResizeHandler;
        private bool isPhone = false;

        /// <summary>
        /// The code in this method is generated by Unity 
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;

            AppCallbacks appCallbacks = AppCallbacks.Instance;
            // Setup scripting bridge
            bridge = new WinRTBridge.WinRTBridge();
            appCallbacks.SetBridge(bridge);

            bool isWindowsHolographic = false;

#if UNITY_HOLOGRAPHIC
            // If application was exported as Holographic check if the device actually supports it,
            // otherwise we treat this as a normal XAML application
            isWindowsHolographic = AppCallbacks.IsMixedRealitySupported();
#endif

            if (isWindowsHolographic)
            {
                appCallbacks.InitializeViewManager(Window.Current.CoreWindow);
            }
            else
            {
                appCallbacks.RenderingStarted += () => { RemoveSplashScreen(); };

                if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1))
                    isPhone = true;

                appCallbacks.SetSwapChainPanel(GetSwapChainPanel());
                appCallbacks.SetCoreWindowEvents(Window.Current.CoreWindow);
                appCallbacks.InitializeD3DXAML();

                splash = ((App)App.Current).splashScreen;
                GetSplashBackgroundColor();
                OnResize();
                onResizeHandler = new WindowSizeChangedEventHandler((o, e) => OnResize());
                Window.Current.SizeChanged += onResizeHandler;
                UnityPlayer.AppCallbacks.Instance.Initialized += OnInitialized;
            }
        }

        /// <summary>
        /// The code in this method is generated by Unity 
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            splash = (SplashScreen)e.Parameter;
            OnResize();
        }

        /// <summary>
        /// The code in this method is generated by Unity 
        /// </summary>
        private void OnResize()
        {
            if (splash != null)
            {
                splashImageRect = splash.ImageLocation;
                PositionImage();
            }
        }

        /// <summary>
        /// The code in this method is generated by Unity 
        /// </summary>
        private void PositionImage()
        {
            var inverseScaleX = 1.0f;
            var inverseScaleY = 1.0f;
            if (isPhone)
            {
                inverseScaleX = inverseScaleX / DXSwapChainPanel.CompositionScaleX;
                inverseScaleY = inverseScaleY / DXSwapChainPanel.CompositionScaleY;
            }

            ExtendedSplashImage.SetValue(Canvas.LeftProperty, splashImageRect.X * inverseScaleX);
            ExtendedSplashImage.SetValue(Canvas.TopProperty, splashImageRect.Y * inverseScaleY);
            ExtendedSplashImage.Height = splashImageRect.Height * inverseScaleY;
            ExtendedSplashImage.Width = splashImageRect.Width * inverseScaleX;
        }

        /// <summary>
        /// The code in this method is generated by Unity 
        /// </summary>
        private async void GetSplashBackgroundColor()
        {
            try
            {
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///AppxManifest.xml"));
                string manifest = await FileIO.ReadTextAsync(file);
                int idx = manifest.IndexOf("SplashScreen");
                manifest = manifest.Substring(idx);
                idx = manifest.IndexOf("BackgroundColor");
                if (idx < 0)  // background is optional
                    return;
                manifest = manifest.Substring(idx);
                idx = manifest.IndexOf("\"");
                manifest = manifest.Substring(idx + 1);
                idx = manifest.IndexOf("\"");
                manifest = manifest.Substring(0, idx);
                int value = 0;
                bool transparent = false;
                if (manifest.Equals("transparent"))
                    transparent = true;
                else if (manifest[0] == '#') // color value starts with #
                    value = Convert.ToInt32(manifest.Substring(1), 16) & 0x00FFFFFF;
                else
                    return; // at this point the value is 'red', 'blue' or similar, Unity does not set such, so it's up to user to fix here as well
                byte r = (byte)(value >> 16);
                byte g = (byte)((value & 0x0000FF00) >> 8);
                byte b = (byte)(value & 0x000000FF);

                await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.High, delegate()
                    {
                        byte a = (byte)(transparent ? 0x00 : 0xFF);
                        ExtendedSplashGrid.Background = new SolidColorBrush(Color.FromArgb(a, r, g, b));
                    });
            }
            catch (Exception)
            {}
        }

        /// <summary>
        /// The code in this method is generated by Unity 
        /// </summary>
        public SwapChainPanel GetSwapChainPanel()
        {
            return DXSwapChainPanel;
        }

        /// <summary>
        /// The code in this method is generated by Unity 
        /// </summary>
        public void RemoveSplashScreen()
        {
            DXSwapChainPanel.Children.Remove(ExtendedSplashGrid);
            if (onResizeHandler != null)
            {
                Window.Current.SizeChanged -= onResizeHandler;
                onResizeHandler = null;
            }
        }

        #if !UNITY_WP_8_1
        /// <summary>
        /// Called when the user selected the xaml navigate button.
        /// We will then instruct the webview to navigate to a URI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNavigateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Uri targetUri = new Uri("https://developer.microsoft.com");
                _WebView.Navigate(targetUri);
            }
            catch (Exception myE)
            {
                // Bad address 
                String str = String.Format("<h1>Address is invalid, try again.  Details --> {0}</h1>", myE.Message);
                _WebView.NavigateToString(str);
            }

        }

        /// <summary>
        /// Called in response to the user clicking the xaml button to send a message to the unity component of the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSendToUnityClick(object sender, RoutedEventArgs e)
        {
            // Validate unity is up and running as expected
            if (UnityPlayer.AppCallbacks.Instance.IsInitialized())
            {
                // Unity interactions must occure on the App thread
                UnityPlayer.AppCallbacks.Instance.InvokeOnAppThread(new UnityPlayer.AppCallbackItem(() =>
                {
                    String msg = "Message from XAML";
                    // Use the communications helper to send the message to unity.
                    Communications.SendMessageToUnity(msg);
                }
                ), false);
            }
        }
#endif

        /// <summary>
        /// Called when the page is initialized.
        /// Here we are using it to setup the channel for unity to talk back to the xaml
        /// </summary>
        private void OnInitialized()
        {
            AppCallbacks.Instance.InvokeOnAppThread(() =>
            {
                Communications.SetEventCallback(UnityToXAMLEventCallback);
            }, false);
        }

        private int eventWasReceivedCount = 0;

        /// <summary>
        /// This will be called as the endpoint for a message event from Unity.
        /// </summary>
        /// <param name="arg"></param>
        public void UnityToXAMLEventCallback(object arg)
        {
            UnityPlayer.AppCallbacks.Instance.InvokeOnUIThread(new UnityPlayer.AppCallbackItem(() =>
            {
                // Ensure this runs on the UI Thread so we can update the UX
                eventWasReceivedCount++;
                _EventsFromUnity.Text = "Event received " + eventWasReceivedCount + " times";
            }
            ), false);
        }
    }


    public delegate void UnityEvent(object arg);

    /// <summary>
    /// This class is used as the communication bridge between the XAML portion and the Unity portion of the application.
    /// It handles the work of finding the appropriate unity object and scripts to interact with.
    /// </summary>
    public sealed class Communications
    {
        /// <summary>
        /// Called to send a message to the unity components
        /// </summary>
        /// <param name="msg"></param>
        public static void SendMessageToUnity(string msg)
        {
            // Get the object the communication class is attached to.
            UnityEngine.GameObject gameObject = UnityEngine.GameObject.Find("Camera");
            if (gameObject != null)
            {
                // Call the scripts on the game object we found, to do any work we need done.
                gameObject.GetComponent<ButtonHandlers>().ShowFeedback(msg);
            }
            else
            {
                throw new Exception("Camera not found, have you exported the correct scene?");
            }
        }

        /// <summary>
        /// Connects the event delegates to be able to receive events from unity.
        /// </summary>
        /// <param name="e"></param>
        public static void SetEventCallback(UnityEvent e)
        {
            // Get the object the communication class is attached to.
            UnityEngine.GameObject gameObject = UnityEngine.GameObject.Find("Camera");
            if (gameObject != null)
            {
                // Create an event object on that class so we can send events.
                var bh = gameObject.GetComponent<ButtonHandlers>();
                if (bh != null)
                {
                    bh.onEvent = new ButtonHandlers.OnEvent(e);
                }
            }
            else
            {
                throw new Exception("Camera not found, have you exported the correct scene?");
            }
        }
    }

}
