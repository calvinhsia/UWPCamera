using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWPCamera
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Image _img = new Image();
        TextBlock _tb = new TextBlock();
        object _timerLock = new object();
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }
        MediaCapture _medCapture;
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var cameratoUse = 0; // use first camera found
                var cameraDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                switch (cameraDevices.Count)
                {
                    case 0:
                        this.Content = new TextBlock() { Text = "No camera found" };
                        return;
                    case 1:
                        break;
                    default:
                        int ndx = 0;
                        foreach (var cam in cameraDevices)
                        { // high priority foir front camera
                            if (cam.EnclosureLocation?.Panel == Windows.Devices.Enumeration.Panel.Front)
                            {
                                cameratoUse = ndx;
                                break;
                            }
                            ndx++;
                        }
                        break;
                }
                Action<DeviceInformation> initMediaCapture = async (dev) =>
                {
                    _medCapture = new MediaCapture();
                    MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                    settings.VideoDeviceId = dev.Id;
                    await _medCapture.InitializeAsync(settings);
                };
                initMediaCapture(cameraDevices[cameratoUse]);
                var relPanel = new RelativePanel();
                var spCtrls = new StackPanel()
                {
                    Orientation = Orientation.Horizontal
                };
                _img.HorizontalAlignment = HorizontalAlignment.Center;
                var btnSwitchCamera = new Button()
                {
                    Content = cameraDevices[cameratoUse].EnclosureLocation?.Panel,
                    IsEnabled = cameraDevices.Count > 1
                };
                ToolTipService.SetToolTip(btnSwitchCamera, new ToolTip() { Content = "Click to switch camera front/back if available" });
                spCtrls.Children.Add(btnSwitchCamera);
                btnSwitchCamera.Click += (oc, ec) =>
                {
                    try
                    {
                        Monitor.Enter(_timerLock);
                        if (++cameratoUse == cameraDevices.Count)
                        {
                            cameratoUse = 0;
                        }
                        btnSwitchCamera.Content = cameraDevices[cameratoUse].EnclosureLocation?.Panel;
                        initMediaCapture(cameraDevices[cameratoUse]);
                    }
                    finally
                    {
                        Monitor.Exit(_timerLock);
                    }
                };
                relPanel.Children.Add(spCtrls);
                relPanel.Children.Add(_img);
                RelativePanel.SetBelow(_img, spCtrls);

                var btnQuit = new Button()
                {
                    Content = "Quit"
                };
                btnQuit.Click += (oq, eq) =>
                  {
                      Application.Current.Exit();
                  };
                spCtrls.Children.Add(btnQuit);
                spCtrls.Children.Add(_tb);
                var tmr = new DispatcherTimer();
                tmr.Interval = TimeSpan.FromSeconds(4);
                tmr.Tick += async (ot, et) =>
                 {
                     if (Monitor.TryEnter(_timerLock))
                     {
                         try
                         {
                             _tb.Text = DateTime.Now.ToString("MM/dd/yy hh:mm:ss tt");
                             var bmImage = await TakePictureAsync();
                             _img.Source = bmImage;
                             _img.HorizontalAlignment = HorizontalAlignment.Center;
                         }
                         catch (Exception ex)
                         {
                             _tb.Text += ex.ToString();
                         }
                     }
                     Monitor.Exit(_timerLock);
                 };
                tmr.Start();
                //var sb = new StringBuilder();
                //var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                //foreach (var device in devices)
                //{
                //    sb.AppendLine($"{device.Name}");
                //    if (device.Properties != null)
                //    {
                //        foreach (var prop in device.Properties)
                //        {
                //            sb.AppendLine($"  K={prop.Key}  V={prop.Value?.ToString()}");
                //        }
                //    }
                //}
                //sb.AppendLine("Done");
                //var txt = sb.ToString();
                //var vwr = new ScrollViewer();
                //RelativePanel.SetBelow(vwr, spCtrls);
                //var tb = new TextBlock()
                //{
                //    //Height = 200,
                //    Width = this.ActualWidth,
                //    //VerticalAlignment = VerticalAlignment.Stretch,
                //    TextWrapping = TextWrapping.Wrap,
                //};
                //vwr.Content = tb;
                //tb.Text = txt;
                //relPanel.Children.Add(vwr);
                this.Content = relPanel;

            }
            catch (Exception ex)
            {
                this.Content = new TextBlock() { Text = ex.ToString() };
            }
        }

        async Task<BitmapImage> TakePictureAsync()
        {
            var imgFmt = ImageEncodingProperties.CreateJpeg();
            var llCapture = await _medCapture.PrepareLowLagPhotoCaptureAsync(imgFmt);
            var photo = await llCapture.CaptureAsync();
            var bmImage = new BitmapImage();

            await bmImage.SetSourceAsync(photo.Frame);
            await llCapture.FinishAsync();
            return bmImage;

            //var camCapUI = new CameraCaptureUI();
            //camCapUI.PhotoSettings.AllowCropping = true;
            //camCapUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            //var storageFile = await camCapUI.CaptureFileAsync(CameraCaptureUIMode.Photo);
            //var bmImage = new BitmapImage();
            //if (storageFile != null)
            //{
            //    using (var strm = await storageFile.OpenReadAsync())
            //    {
            //        bmImage.SetSource(strm);
            //    }
            //}
        }
    }
}


