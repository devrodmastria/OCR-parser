using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using Tesseract;

namespace LicenseParser.Services
{
    public class ParserServices
    {
        private byte[] ConvertImageToFormat(byte[] input, int maxWidth, int maxHeight)
        {

            // feed the bytes into a memory stream
            var stream = new MemoryStream(input);

            // create an image object from the memory stream
            var image = Image.FromStream(stream);

            // resize image if needed, keep aspect ratio
            var maxSize = new Size(maxWidth, maxHeight);
            if (image.Width > maxSize.Width || image.Height > maxSize.Height)
            {
                var sizeMultiplier = Math.Min(maxSize.Width / (decimal)image.Width, maxSize.Height / (decimal)image.Height);
                var size = new Size((int)Math.Round(image.Width * sizeMultiplier), (int)Math.Round(image.Height * sizeMultiplier));
                image = new Bitmap(image, size);
            }

            // save the image into a new memory stream in the format specified
            var convertedStream = new MemoryStream();
            image.Save(convertedStream, System.Drawing.Imaging.ImageFormat.Jpeg);

            // convert the image back to a base64 string
            return convertedStream.ToArray();
        }

        private Bitmap GetAreaOfInterest(Bitmap image)
        {
            // default will be the entire image
            double xRatio;
            double yRatio;
            double widthRatio;
            double heightRatio;

            xRatio = 0.3;
            yRatio = 0.35;
            widthRatio = 0.38;
            heightRatio = 0.13;

            // use floor and ceiling to make the box slightly larger when rounding
            var xStart = (int)Math.Floor(image.Width * xRatio);
            var yStart = (int)Math.Floor(image.Height * yRatio);

            // the region can't be larger than the image
            var width = Math.Min(image.Width - xStart, (int)Math.Ceiling(image.Width * widthRatio));
            var height = Math.Min(image.Height - yStart, (int)Math.Ceiling(image.Height * heightRatio));

            return image.Clone(new Rectangle(xStart, yStart, width, height), image.PixelFormat);
        }

        public Tuple<List<string>, Bitmap> OcrFront(byte[] image)
        {
            image = this.ConvertImageToFormat(image, 1920, 1080);

            var result = new List<string>();
            var bmp = new Bitmap(new MemoryStream(image));

            // if the width is less than the height, rotate so the license is oriented properly.
            if (bmp.Width < bmp.Height)
            {
                bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
            }

            // RelativeSearchPath should be used on the server
            // but this returns null when running unit tests, so fall back on BaseDirectory
            var binFolder = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;
            var tesseractDataPath = Path.Combine(binFolder, "tessdata");

            var scannedArea = GetAreaOfInterest(bmp);

                try
                {
                    TesseractEnviornment.CustomSearchPath = Convert.ToString("C:\\Temp\\Tess");
                    using (var ms = new MemoryStream())
                    {
                        scannedArea.Save(ms, System.Drawing.Imaging.ImageFormat.Tiff);

                        using (var engine = new TesseractEngine(tesseractDataPath, "eng", EngineMode.Default))
                        using (var img = Pix.LoadTiffFromMemory(ms.ToArray()).ConvertRGBToGray())
                        using (var page = engine.Process(img))
                        using (var iter = page.GetIterator())
                        {
                            iter.Begin();
                            do
                            {
                                do
                                {
                                    do
                                    {
                                        do
                                        {
                                            result.Add(iter.GetText(PageIteratorLevel.Word));
                                        }
                                        while (iter.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));
                                    }
                                    while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                                }
                                while (iter.Next(PageIteratorLevel.Block, PageIteratorLevel.Para));
                            }
                            while (iter.Next(PageIteratorLevel.Block));
                        }
                    }
                }
                finally
                {

                }

            return new Tuple<List<string>, Bitmap>(result, scannedArea);
        }
    }
}