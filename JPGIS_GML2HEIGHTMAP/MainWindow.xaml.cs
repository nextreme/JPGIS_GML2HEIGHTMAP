using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;

using BitMiracle.LibTiff.Classic;

namespace JPGIS_GML2HEIGHTMAP
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private string saveFilename = null;
        //private float scale = 1.0; 最大655.36m
        private float scale = 2.0f;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void listview_filelist_Drop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                foreach(var file in files)
                {
                    this.listview_filelist.Items.Add(file);
                }
            }
        }

        private void button_exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void GMLparse(string filepath)
        {
            this.saveFilename = System.IO.Path.GetFullPath(filepath);
            this.saveFilename = this.saveFilename.Replace(".xml", ".tiff");
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

            this.createDataArray(xSize, ySize, data);

        }

        private void createDataArray(int xSize, int ySize, string data)
        {
            var resolution = 72;
            var bitPerSample = 16;
            var samplePerPixel = 1;

            Regex regex = new Regex("[^,\\r\\n]+,[^,\\r\\n]+");
            MatchCollection mc = regex.Matches(data);

            using (Tiff output = Tiff.Open(this.saveFilename, "w"))
            {
                if (output == null)
                {
                    return;
                }

                ushort[] image = new ushort[xSize * ySize];
                int index = 0;
                foreach (Match m in mc)
                {
                    string[] unit = m.Value.Split(',');
                    if (float.Parse(unit[1]) < 0)
                    {
                        image[index] = 0;
                    }
                    else
                    {
                        image[index] = (ushort)(float.Parse(unit[1]) * 100 / this.scale);
                    }
                    index++;
                }

                output.SetField(TiffTag.IMAGEWIDTH, xSize);
                output.SetField(TiffTag.IMAGELENGTH, ySize);
                output.SetField(TiffTag.SAMPLESPERPIXEL, samplePerPixel);
                output.SetField(TiffTag.BITSPERSAMPLE, bitPerSample);
                output.SetField(TiffTag.ORIENTATION, BitMiracle.LibTiff.Classic.Orientation.TOPLEFT);
                output.SetField(TiffTag.XRESOLUTION, resolution);
                output.SetField(TiffTag.YRESOLUTION, resolution);
                output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                output.SetField(TiffTag.COMPRESSION, Compression.NONE);
                output.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);

                byte[] byteBuffer = new byte[image.Length * sizeof(ushort)];
                Buffer.BlockCopy(image, 0, byteBuffer, 0, byteBuffer.Length);
                output.WriteEncodedStrip(0, byteBuffer, byteBuffer.Length);
                output.WriteDirectory();
            }
        }

        private void button_batch_Click(object sender, RoutedEventArgs e)
        {
            foreach (var filepath in this.listview_filelist.Items)
            {
                this.label_status_processing_filename.Content = System.IO.Path.GetFileName(filepath.ToString());
                this.GMLparse(filepath.ToString());
            }
            MessageBox.Show("Batch job has been finished");
            this.listview_filelist.Items.Clear();
        }

    }
}
