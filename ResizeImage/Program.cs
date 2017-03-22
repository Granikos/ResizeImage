using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Granikos.ResizeImage.Properties;

namespace Granikos.ResizeImage
{
    internal class Program
    {
        private readonly static Regex RegexExtension;

        private static ConsoleColor _foreColor;

        static Program()
        {
            Program.RegexExtension = new Regex("^\\.?(.+)", RegexOptions.Compiled);
        }

        public Program()
        {
        }

        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            ImageCodecInfo.GetImageEncoders();
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault<ImageCodecInfo>((ImageCodecInfo t) => t.MimeType == mimeType);
        }

        private static void ResizeImage(string file, string outputDir)
        {
            try
            {
                if (Settings.Default.OutputToCmd)
                {
                    // Write to console, if requested
                    Console.Write(file);
                    Console.Write("...");
                }
                Image image = Image.FromFile(file);
                int width = Settings.Default.OutputSize.Width;
                Size outputSize = Settings.Default.OutputSize;
                Bitmap bitmap = new Bitmap(width, outputSize.Height, PixelFormat.Format32bppArgb);
                int num = Math.Min(image.Height - Settings.Default.TopMargin, image.Width);
                RectangleF rectangleF = new RectangleF(0f, (float)Settings.Default.TopMargin, (float)num, (float)num);
                using (Graphics graphic = Graphics.FromImage(bitmap))
                {
                    graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphic.CompositingQuality = CompositingQuality.HighQuality;
                    graphic.SmoothingMode = SmoothingMode.HighQuality;
                    graphic.DrawImage(image, new RectangleF(new PointF(0f, 0f), Settings.Default.OutputSize), rectangleF, GraphicsUnit.Pixel);
                }
                string str = Path.Combine(outputDir, Path.GetFileName(file));
                if (image.RawFormat != ImageFormat.Jpeg)
                {
                    bitmap.Save(str, image.RawFormat);
                }
                else
                {
                    EncoderParameters encoderParameter = new EncoderParameters(1);
                    encoderParameter.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)Settings.Default.JpegQuality);
                    ImageCodecInfo imageCodecInfo = ((IEnumerable<ImageCodecInfo>)ImageCodecInfo.GetImageEncoders()).FirstOrDefault<ImageCodecInfo>((ImageCodecInfo t) => t.MimeType == "image/jpeg");
                    bitmap.Save(str, imageCodecInfo, encoderParameter);
                }
                if (Settings.Default.OutputToCmd)
                {
                    // Write to console, if requested
                    ConsoleColor foregroundColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("OK");
                    Console.ForegroundColor = foregroundColor;
                }
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                if (Settings.Default.OutputToCmd)
                {
                    // Write to console, if requested
                    ConsoleColor consoleColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(string.Concat("Error: ", exception.Message));
                    Console.ForegroundColor = consoleColor;
                }
            }
        }

        private static void Main(string[] args)
        {
            IEnumerable<string> argStrings;
            Program._foreColor = Console.ForegroundColor;
            string str = args.FirstOrDefault<string>();
            string str1 = args.Skip<string>(1).FirstOrDefault<string>();
            if (string.IsNullOrWhiteSpace(str))
            {
                Program.Usage();
                return;
            }
            if (str.Contains("?"))
            {
                Program.Usage();
                return;
            }
            if (!Directory.Exists(str))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Concat("Could not find directory ", str));
                Program.Usage();
                Environment.Exit(1);
            }
            if (!Directory.Exists(str1))
            {
                Directory.CreateDirectory(str1);
            }
            if (args.Count<string>() > 2)
            {
                argStrings =
                    from s in args.Skip<string>(2).Distinct<string>(StringComparer.InvariantCultureIgnoreCase)
                    from s2 in s.Split(new char[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    let m = Program.RegexExtension.Match(s2.Trim())
                    where m.Success
                    select string.Concat(".", m.Value);
            }
            else
            {
                string defaultImageTypes = Settings.Default.DefaultImageTypes;
                char[] chrArray = new char[] { ',' };
                argStrings =
                    from s in defaultImageTypes.Split(chrArray)
                    let m = Program.RegexExtension.Match(s.Trim())
                    where m.Success
                    select string.Concat(".", m.Value);
            }
            IEnumerable<string> strs1 = argStrings;
            foreach (string str2 in (
                from f in Directory.GetFiles(str)
                from ext in strs1
                where f.EndsWith(ext)
                select f).Distinct<string>())
            {
                Program.ResizeImage(str2, str1);
            }
        }

        private static void Usage()
        {
            Console.ForegroundColor = Program._foreColor;
            Console.WriteLine("Resize Image");
            Console.WriteLine("(c) 2017 Granikos GmbH 6 Co. KG");
            Console.WriteLine();
            Console.WriteLine("ResizeImage.exe SOURCE TARGET [EXT] [EXT] [EXT] ...");
            Console.WriteLine();
            Console.WriteLine("SOURCE\t Source directory for original images");
            Console.WriteLine("TARGET\t Target directory for resized images");
            Console.WriteLine("EXT\t\t Image file extensions to process");
        }
    }
}