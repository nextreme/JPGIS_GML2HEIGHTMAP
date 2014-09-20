using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Text.RegularExpressions;
using System.IO;

namespace JPGIS_GML2HEIGHTMAP
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private string saveFilename = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void listview_filelist_Drop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                this.listview_filelist.Items.Add(files[0]);
                this.saveFilename = System.IO.Path.GetFullPath(files[0]);
                this.saveFilename = this.saveFilename.Replace(".xml", ".png");
            }
            Console.WriteLine(this.saveFilename);
        }

        private void button_convert_Click(object sender, RoutedEventArgs e)
        {
            foreach(var filepath in this.listview_filelist.Items)
            {
                this.GMLparse(filepath.ToString());
            }
        }

        private void button_exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void GMLparse(string filepath)
        {
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(filepath);

            XmlNamespaceManager xnm = new XmlNamespaceManager(xdoc.NameTable);
            xnm.AddNamespace("gml", "http://www.opengis.net/gml/3.2");

            XmlElement dataXe = (XmlElement)xdoc.SelectSingleNode("//gml:tupleList", xnm);
            XmlElement lowXe = (XmlElement)xdoc.SelectSingleNode("//gml:low", xnm);
            XmlElement highXe = (XmlElement)xdoc.SelectSingleNode("//gml:high", xnm);

            string[] lowPoint = lowXe.InnerText.Split(' ');
            string[] highPoint = highXe.InnerText.Split(' ');
            int xSize = int.Parse(highPoint[0]) - int.Parse(lowPoint[0]) + 1;
            int ySize = int.Parse(highPoint[1]) - int.Parse(lowPoint[1]) + 1;
            string data = dataXe.InnerText;

            DrawMap(xSize, ySize, data);
        }

        private void DrawMap(int xSize, int ySize, string data)
        {
            float[,] heights = new float[xSize, ySize];

            Regex regex = new Regex("[^,\\r\\n]+,[^,\\r\\n]+");
            MatchCollection mc = regex.Matches(data);

            int index = 0;
            foreach (Match m in mc)
            {
                string[] unit = m.Value.Split(',');
                heights[index % xSize, index / xSize] = float.Parse(unit[1]);
                index++;
            }

            this.canvas_map.Children.Clear();

            for(int x = 0; x < xSize; x++)
            {
                for(int y = 0; y < ySize; y++)
                {
                    Rectangle r = new Rectangle();
                    r.SetValue(Canvas.LeftProperty, (double)x * 2);
                    r.SetValue(Canvas.TopProperty, (double)y * 2);
                    double height = Math.Floor(heights[x, y] / 2);

                    //Console.WriteLine(string.Format("x : {0} / y : {1} / h : {2}", x * 2, y * 2, heights[x, y] * 5));

                    Color fillColor;

                    if(height <= 0)
                    {
                        fillColor = Colors.Black;
                    }
                    else if (height > 600)
                    {
                        fillColor = Colors.White;
                    }
                    else
                    {
                        //byte colorValue = byte.Parse((600 - height).ToString());
                        //Console.WriteLine(string.Format("height : {0}", height));
                        byte colorValue = byte.Parse(height.ToString());
                        //Console.WriteLine(colorValue);
                        fillColor = Color.FromRgb(colorValue, colorValue, colorValue);
                    }
                    r.Fill = new SolidColorBrush(fillColor);
                    this.canvas_map.Children.Add(r);
                }
            }
        }

        private void button_save_Click(object sender, RoutedEventArgs e)
        {
            if(this.canvas_map.Children.Count > 0)
            {
                /*
                canvas_map.Arrange(new Rect(0, 0, canvas_map.Width, canvas_map.Height));

                RenderTargetBitmap render = new RenderTargetBitmap((Int32)canvas_map.Width, (Int32)canvas_map.Height, 96, 96, PixelFormats.Default);
                render.Render(canvas_map);

                var enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(render));

                using(FileStream fs = new FileStream(this.saveFilename, FileMode.Create))
                {
                    enc.Save(fs);
                    fs.Close();
                }

                */

                /*
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)this.canvas_map.Width, (int)this.canvas_map.Height, 96d, 96d, PixelFormats.Default);
                rtb.Render(this.canvas_map);

                var crop = new CroppedBitmap(rtb, new Int32Rect(0, 0, (int)this.canvas_map.Width, (int)this.canvas_map.Height));
                BitmapEncoder pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(crop));
                using(var fs = File.OpenWrite(this.saveFilename))
                {
                    pngEncoder.Save(fs);
                }
                */

                Rect bounds = VisualTreeHelper.GetDescendantBounds(this.canvas_map);
                double dpi = 96d;

                RenderTargetBitmap rtb = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, dpi, dpi, PixelFormats.Default);
                DrawingVisual dv = new DrawingVisual();
                using (DrawingContext dc = dv.RenderOpen())
                {
                    VisualBrush vb = new VisualBrush(this.canvas_map);
                    dc.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size));
                }

                rtb.Render(dv);

                var enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));

                using (FileStream fs = new FileStream(this.saveFilename, FileMode.Create))
                {
                    enc.Save(fs);
                    fs.Close();
                }


                this.canvas_map.Children.Clear();
                this.listview_filelist.Items.Clear();
            }
        }

        private void button_clear_Click(object sender, RoutedEventArgs e)
        {
            this.canvas_map.Children.Clear();
        }

    }
}
