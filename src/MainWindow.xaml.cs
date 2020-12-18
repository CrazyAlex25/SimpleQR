using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;

using System.Timers;
using System.Windows;

using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WPFMediaKit.DirectShow.Controls;
using ZXing;

namespace SimpleQR
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        IBarcodeReader barcodeReader = new BarcodeReader();

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        protected void OnPropertyChanged([CallerMemberName]string prop="")
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        string _qr;
        public string QR
        {
            get => _qr;
            set
            {
                _qr = value;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

        }

       
       string ParseCode(Bitmap bitmapSource)
        {

                var codes = barcodeReader.DecodeMultiple(new BitmapLuminanceSource(bitmapSource));
                if (codes?.Length > 0)
                {
                    return string.Join(Environment.NewLine, codes.Select(x => x.Text));
                }
                return string.Empty;

        }

        Timer timer;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cbDevices.ItemsSource= MultimediaUtil.VideoInputNames;
            timer = new Timer(100);
            timer.Elapsed += Timer_Elapsed;
            waitNextCode(null,null);

        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var bmp = Dispatcher.Invoke<Bitmap>(() => {
                return Take();
            });
            if(bmp!=null)
            {
                var res = ParseCode(bmp);
                if (!string.IsNullOrEmpty(res))
                {
                    timer.Stop();
                    QR = res;
                    Dispatcher.Invoke(() => { btn_wait.IsEnabled = true; });

                    SaveBMP(bmp);

                    Console.Beep();
                    SystemSounds.Beep.Play();

                   
                }
            }
        }

       private void SaveBMP(Bitmap bmp)
        {
            var dir = Directory.CreateDirectory(System.IO.Path.Combine("images", DateTime.Now.ToString("dd-MM-yyyy")));

            var fileName = DateTime.Now.ToString("HH-mm-ss") + ".jpg";
            var fullPath = System.IO.Path.Combine(dir.FullName, fileName);
            //bmp.Save(System.IO.Path.Combine(dir.FullName, fileName), ImageFormat.Jpeg);



            try
            {
                bmp.Save(fullPath,ImageFormat.Jpeg);
            }
            catch
            {
                Bitmap bitmap = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat);
                Graphics g = Graphics.FromImage(bitmap);
                g.DrawImage(bmp, new System.Drawing.Point(0, 0));
                g.Dispose();
                bmp.Dispose();
                bitmap.Save(fullPath, ImageFormat.Jpeg);
                bmp = bitmap; // preserve clone        
            }


        }



        private void copy_click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(QR);
        }

        private void waitNextCode(object sender, RoutedEventArgs e)
        {
            timer.Start();
            QR = string.Empty;
            btn_wait.IsEnabled = false;
        }

        private Bitmap Take()
        {
            RenderTargetBitmap bmp = new RenderTargetBitmap((int)videoCapElement.ActualWidth, (int)videoCapElement.ActualHeight, 96, 96, PixelFormats.Default);
            bmp.Render(videoCapElement);


            using (MemoryStream stream = new MemoryStream())
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                encoder.Save(stream);
                Bitmap bitmap = new Bitmap(stream);
                return bitmap;
            }
        }

      
       
      
    }
}
