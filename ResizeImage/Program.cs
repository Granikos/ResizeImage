// MIT License
//
// Copyright (c) 2017 Granikos GmbH & Co. KG (https://www.granikos.eu)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Granikos.ResizeImage.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Granikos.ResizeImage
{
    internal class Program
    {
        private readonly static Regex RegexExtension;

        private static ConsoleColor _foregroundColor;

        static Program()
        {
            Program.RegexExtension = new Regex("^\\.?(.+)", RegexOptions.Compiled);
        }

        /// <summary>
        /// Get image codec for given MIME Type
        /// </summary>
        /// <param name="mimeType">String containing the MIME type</param>
        /// <returns>ImageCodecInfo</returns>
        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            ImageCodecInfo.GetImageEncoders();
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault<ImageCodecInfo>((ImageCodecInfo t) => t.MimeType == mimeType);
        }

        /// <summary>
        /// Resize an image depending on settings file and save it to the outputDir
        /// </summary>
        /// <param name="file">Image file with absolute file path</param>
        /// <param name="outputDir">Target directory for saving the resized image</param>
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

        /// <summary>
        /// Main program
        /// </summary>
        /// <param name="args">String array containing the arguments
        /// ARG1 = Absolute path to source directory
        /// ARG2 = Absolute path to target directory
        /// ARG3 .. ARGx = File extionsion (jpg, bmp, etc.)
        /// </param>
        private static void Main(string[] args)
        {
            IEnumerable<string> argStrings;
            Program._foregroundColor = Console.ForegroundColor;
            string argStr = args.FirstOrDefault<string>();
            string argStr1 = args.Skip<string>(1).FirstOrDefault<string>();

            if (string.IsNullOrWhiteSpace(argStr))
            {
                Program.Usage();
                return;
            }

            if (argStr.Contains("?"))
            {
                Program.Usage();
                return;
            }

            if (!Directory.Exists(argStr))
            {
                // If source directory does not exist -> exit
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Concat("Could not find directory ", argStr));
                Program.Usage();
                Environment.Exit(1);
            }

            if (!Directory.Exists(argStr1))
            {
                // Create target directory, if not exists
                Directory.CreateDirectory(argStr1);
            }

            if (args.Count<string>() > 2)
            {
                // Handle file extensions
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

            IEnumerable<string> extensionStrings = argStrings;
            foreach (string str2 in (
                from files in Directory.GetFiles(argStr)
                from extension in extensionStrings
                where files.EndsWith(extension)
                select files).Distinct<string>())
            {
                Program.ResizeImage(str2, argStr1);
            }
        }

        /// <summary>
        /// Usage information written to console
        /// </summary>
        private static void Usage()
        {
            // Write some fancy usage information
            Console.ForegroundColor = Program._foregroundColor;
            Console.WriteLine("Resize Image Tool");
            Console.WriteLine("(c) 2017 Granikos GmbH & Co. KG");
            Console.WriteLine();
            Console.WriteLine("ResizeImage.exe SOURCE TARGET [EXT] [EXT] [EXT] ...");
            Console.WriteLine();
            Console.WriteLine("SOURCE\t Source directory for original images");
            Console.WriteLine("TARGET\t Target directory for resized images");
            Console.WriteLine("EXT\t\t Image file extensions to process");
        }
    }
}