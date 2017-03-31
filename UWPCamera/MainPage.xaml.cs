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
        Button _btnSwitchCamera;
        CheckBox _chkCycleCameras;
        Image _img = new Image();
        TextBlock _tb = new TextBlock();
        object _timerLock = new object();

        int _cameratoUse = 0;
        DeviceInformationCollection _cameraDevices = null;
        MediaCapture _medCapture;
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var deviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.VideoCapture);
                deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(
                        (wat, info) =>
                        {
                            lock (_timerLock)
                            {
                                _cameraDevices = null;// force reload
                            }
                        }
                    );
                deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(
                    (wat, info) =>
                    {
                        lock (_timerLock)
                        {
                            _cameraDevices = null;// force reload
                        }
                    }
                    );
                deviceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(
                    (wat, info) =>
                    {
                        lock (_timerLock)
                        {
                            _cameraDevices = null;// force reload
                        }
                    }
                    );
                deviceWatcher.Stopped += new TypedEventHandler<DeviceWatcher, object>(
                    (wat, obj) =>
                    {
                        deviceWatcher.Start();
                    }
                    );
                deviceWatcher.Start();
                var relPanel = new RelativePanel();
                var spCtrls = new StackPanel()
                {
                    Orientation = Orientation.Horizontal
                };
                _img.HorizontalAlignment = HorizontalAlignment.Center;
                _btnSwitchCamera = new Button()
                {
                    IsEnabled = _cameraDevices?.Count > 1,
                    Width = 240
                };
                SetBtnSwitchLabel();
                ToolTipService.SetToolTip(_btnSwitchCamera, new ToolTip()
                {
                    Content = "Click to switch camera front/back if available"
                });
                spCtrls.Children.Add(_btnSwitchCamera);
                _btnSwitchCamera.Click += async (oc, ec) =>
                 {
                     //can't use await inside a lock()
                     Monitor.TryEnter(_timerLock);
                     try
                     {
                         await initMediaCaptureAsync(fIncrementCameraTouse: true);
                     }
                     finally
                     {
                         Monitor.Exit(_timerLock);
                     }
                 };
                _chkCycleCameras = new CheckBox()
                {
                    Content = "Cycle Cameras",
                    IsChecked = false
                };
                ToolTipService.SetToolTip(_chkCycleCameras, new ToolTip()
                {
                    Content = "Automatically switch through all attached cameras"
                });
                spCtrls.Children.Add(_chkCycleCameras);
                relPanel.Children.Add(spCtrls);
                var tbInterval = new TextBox()
                {
                    Text = "5"
                };
                spCtrls.Children.Add(tbInterval);
                var btnQuit = new Button()
                {
                    Content = "Quit"
                };
                spCtrls.Children.Add(btnQuit);
                btnQuit.Click += (oq, eq) =>
                  {
                      Application.Current.Exit();
                  };
                spCtrls.Children.Add(_tb);
                relPanel.Children.Add(_img);
                RelativePanel.SetBelow(_img, spCtrls);
                var tmr = new DispatcherTimer();
                tmr.Interval = TimeSpan.FromSeconds(4);
                tbInterval.LostFocus += (otb, etb) =>
                 {
                     tmr.Interval = TimeSpan.FromSeconds(double.Parse(tbInterval.Text));
                 };
                tmr.Tick += (ot, et) =>
                {
                    lock (_timerLock)
                    {
                        LookForCameraAndTakeAPicture();
                    }
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

        async void LookForCameraAndTakeAPicture()
        {
            try
            {
                bool fWasCycling = _chkCycleCameras.IsChecked == true;
                _tb.Text = DateTime.Now.ToString("MM/dd/yy hh:mm:ss tt");
                // do we need to initialize or reinitialize?
                if (_cameraDevices == null || _cameraDevices.Count == 0)
                {
                    _chkCycleCameras.IsChecked = false;
                    await initializeCamerasAsync();
                }
                if (_chkCycleCameras.IsChecked == true)
                {
                    await initMediaCaptureAsync(fIncrementCameraTouse: true);
                }
                var bmImage = await TakePictureAsync();
                _img.Source = bmImage;
                _img.HorizontalAlignment = HorizontalAlignment.Center;
                if (fWasCycling && _cameraDevices?.Count > 1)
                {
                    _chkCycleCameras.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                _tb.Text += ex.ToString();
                _cameraDevices = null; // will reset looking for camera
                var comex = ex as COMException;
                if (comex != null)
                {
                    if (comex.Message.Contains("The video recording device is no longer present"))
                    {
                        // could be more specific
                    }
                }
            }
        }

        async Task initializeCamerasAsync()
        {
            _cameratoUse = 0;
            _chkCycleCameras.IsChecked = false;
            _chkCycleCameras.IsEnabled = false;
            _cameraDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            switch (_cameraDevices.Count)
            {
                case 0:
                    _btnSwitchCamera.Content = " No camera found";
                    _chkCycleCameras.IsChecked = false;
                    break;
                case 1:
                    _chkCycleCameras.IsChecked = false;
                    _btnSwitchCamera.IsEnabled = false;
                    break;
                default:
                    _btnSwitchCamera.IsEnabled = true;
                    int ndx = 0;
                    foreach (var cam in _cameraDevices)
                    { // high priority for front camera
                        if (cam.EnclosureLocation?.Panel == Windows.Devices.Enumeration.Panel.Front)
                        {
                            _cameratoUse = ndx;
                            break;
                        }
                        ndx++;
                    }
                    break;
            }
            if (_cameraDevices.Count > 0)
            {
                _chkCycleCameras.IsEnabled = _cameraDevices.Count > 1;
                await initMediaCaptureAsync();
            }
        }

        void SetBtnSwitchLabel()
        {
            var camName = "No Camera";
            if (_cameraDevices != null )
            {
                camName = _cameraDevices[_cameratoUse].Name;
            }
            //var camLoc = _cameraDevices?[_cameratoUse]?.EnclosureLocation;
            //if (camLoc == null)
            //{
            //    camName = $"USB Cam{_cameratoUse}";
            //}
            //else
            //{
            //    camName = camLoc.Panel.ToString();
            //}
            _btnSwitchCamera.Content = camName;
        }

        async Task initMediaCaptureAsync(bool fIncrementCameraTouse = false)
        {
            try
            {
                Monitor.Enter(_timerLock);
                if (fIncrementCameraTouse)
                {
                    if (++_cameratoUse == _cameraDevices.Count)
                    {
                        _cameratoUse = 0;
                    }
                }
                SetBtnSwitchLabel();
                _medCapture = new MediaCapture();
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                //settings.StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo;
                //settings.PhotoCaptureSource = PhotoCaptureSource.VideoPreview;
                settings.VideoDeviceId = _cameraDevices[_cameratoUse].Id;
                await _medCapture.InitializeAsync(settings);
            }
            finally
            {
                Monitor.Exit(_timerLock);
            }
            //                    var exposuretime = _medCapture.VideoDeviceController.ExposureControl.Value;
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


