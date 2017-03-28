using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var medCapture = new MediaCapture();
                await medCapture.InitializeAsync();
                var imgFmt = ImageEncodingProperties.CreateJpeg();
                LowLagPhotoCapture llCapture = await medCapture.PrepareLowLagPhotoCaptureAsync(imgFmt);
                var photo = await llCapture.CaptureAsync();
                var bmImage = new BitmapImage();
                await bmImage.SetSourceAsync(photo.Frame);


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
                var relPanel = new RelativePanel();
                var spCtrls = new StackPanel()
                {
                    Orientation = Orientation.Horizontal
                };
                var img = new Image();
                img.Source = bmImage;
                img.MaxHeight = 200;
                img.MaxWidth = 200;
                spCtrls.Children.Add(img);
                relPanel.Children.Add(spCtrls);
                var sb = new StringBuilder();
                var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                foreach (var device in devices)
                {
                    sb.AppendLine($"{device.Name}");
                    if (device.Properties != null)
                    {
                        foreach (var prop in device.Properties)
                        {
                            sb.AppendLine($"  K={prop.Key}  V={prop.Value?.ToString()}");
                        }
                    }
                }
                sb.AppendLine("Done");
                var txt = sb.ToString();
                var vwr = new ScrollViewer();
                RelativePanel.SetBelow(vwr, spCtrls);
                var tb = new TextBlock()
                {
                    //Height = 200,
                    Width = this.ActualWidth,
                    //VerticalAlignment = VerticalAlignment.Stretch,
                    TextWrapping = TextWrapping.Wrap,
                };
                vwr.Content = tb;
                relPanel.Children.Add(vwr);
                this.Content = relPanel;
                tb.Text = txt;

            }
            catch (Exception ex)
            {
                this.Content = new TextBlock() { Text = ex.ToString() };
            }
        }
    }
}


