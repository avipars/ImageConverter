using ImageMagick;
using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageConverter.Utils
{
    public class FileHelper
    {
        public static OpenFileDialog openFileDialogBuilder(string title, string filter)
        {
            OpenFileDialog ofd = new();
            ofd.Title = title;
            ofd.Filter = filter;
            ofd.Multiselect = true; //many files together is okay
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            return ofd;
        }

        /// <summary>
        /// Helper function to convert bytes to a more readable format
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string bytesToRightFormat(long bytes)
        {
            int baseValue = 1024;
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < suffix.Length && bytes >= baseValue; ++i, bytes /= baseValue)
            {
                dblSByte = bytes / baseValue; //get the right format
            }

            return string.Format("{0:0.##} {1}", dblSByte, suffix[i]);
        }

        /// <summary>
        /// Calculates and returns CRC 16 of any file 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static (string res, Exception? ex) CalculateChecksum16(string filePath)
        {
            ushort checksum = 0;

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[1024]; // Adjust buffer size as needed
                    int bytesRead;

                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        for (int i = 0; i < bytesRead; ++i)
                        {
                            checksum = (ushort)(checksum + buffer[i] & 0xFFFF);
                        }
                    }
                }

                return (checksum.ToString("X"), null); //convert decimal to hex
            }
            catch (Exception ex) // Handle potential exceptions
            {
                return ("Error calculating checksum:", ex);
            }
        }

        /// <summary>
        ///  Helper function to get file info
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string getFileInfo(string path)
        {
            FileInfo fi = new(path);
            string sizeOf = bytesToRightFormat(fi.Length);
            return $"File size: {sizeOf} , Attributes: {fi.Attributes}, Created {fi.CreationTime} Last Accessed {fi.LastAccessTime}\nLast Write Time {fi.LastWriteTime}, Unix File Mode {fi.UnixFileMode}";
        }

        public static bool CopyToClip(string item, string type)
        {
            if (!string.IsNullOrEmpty(item as string))
            {
                Clipboard.SetText(item.Trim());
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool openFolder(string path)
        {
            //gets the file path, and opens surrounding folder with the image selected
            if (!string.IsNullOrEmpty(path))
            {
                string argument = "/select, \"" + path + "\"";
                Process.Start("explorer.exe", argument);
                return true;
            }
            else
            {
                return false;
            }
        }


        public static bool openItem(string path)
        {
            //gets the file path, and opens the image in photo viewer
            if (!string.IsNullOrEmpty(path))
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                return true;
            }
            else
            {
                return false;
            }
        }

        public static SaveFileDialog saveFileDialogBuilder(string filter, string path = "", string filename_tip = "")
        {
            // Save the converted image
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Choose where to save the new file";
            saveFileDialog.Filter = filter;
            saveFileDialog.ValidateNames = true; //only use standard names
            saveFileDialog.CheckFileExists = false; //making a new file
            saveFileDialog.CheckPathExists = true;
            saveFileDialog.AddExtension = true;
            if (!string.IsNullOrEmpty(path))
            { //if we have a path, use it
                string fileWithoutPath = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(filename_tip)) //if we don't have a tip, use the default
                {
                    saveFileDialog.FileName = "converted_" + fileWithoutPath;
                }
                else //use the tip provided
                {
                    saveFileDialog.FileName = filename_tip + fileWithoutPath;
                }
            }
            return saveFileDialog;
        }

        public static ImageFormat getImageFormat(string format)
        {
            ImageFormat imageFormat = null;
            switch (format.ToLower().Trim())   // Get the file extension based on the selected format
            {
                case "jpeg":
                case "jpg":
                    imageFormat = ImageFormat.Jpeg;
                    break;
                case "":
                    imageFormat = ImageFormat.Png; //default to png
                    break;
                case "jfif":
                    imageFormat = ImageFormat.Jpeg; //jfif is jpeg
                    break;
                case "png":
                    imageFormat = ImageFormat.Png;
                    break;
                case "gif":
                    imageFormat = ImageFormat.Gif;
                    break;
                case "tiff":
                    imageFormat = ImageFormat.Tiff;
                    break;
                case "bmp":
                    imageFormat = ImageFormat.Bmp;
                    break;
                case "ico":
                    imageFormat = ImageFormat.Icon;
                    break;
                case "webp":
                    if (!OperatingSystem.IsWindows() || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362))
                    {
                        Console.WriteLine($"WebP not supported on {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
                        imageFormat = null;
                    }
                    else
                    {
                        imageFormat = ImageFormat.Webp;
                    }
                    break;
                case "heif":
                case "heic":
                    if (!OperatingSystem.IsWindows() || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362))
                    {
                        Console.WriteLine($"HEIF/C not supported on {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
                        imageFormat = null;
                    }
                    else
                    {
                        imageFormat = ImageFormat.Heif;
                    }
                    break;
                default:
                    imageFormat = null;
                    Console.WriteLine($"Format {format} not supported, proceed at your own risk");
                    break;
            }
            return imageFormat;
        }

        public static MagickFormat getSpecialFormat(string format)
        {
            return format.ToLower().Trim() switch
            {
                "heif" or "heic" => MagickFormat.Heif,
                "webp" => MagickFormat.WebP,
                "ico" => MagickFormat.Ico,
                "svg" => MagickFormat.Svg,
                _ => MagickFormat.Png,
            };
        }


        public static BitmapEncoder mapStringToBitmapEncoder(string format = "")
        {
            BitmapEncoder encoder = format.ToLower().Trim() switch //get the right encoder based on the format
            {
                "jpeg" or "jpg" or "jfif" => new JpegBitmapEncoder(),
                "png" => new PngBitmapEncoder(),
                "gif" => new GifBitmapEncoder(),
                "tiff" => new TiffBitmapEncoder(),
                "bmp" => new BmpBitmapEncoder(),
                _ => null,
            };
            return encoder;
        }

        public static BitmapSource doNegative(BitmapImage bitmapImage, BitmapEncoder encoder)
        {
            //get the pixels
            int width = bitmapImage.PixelWidth;
            int height = bitmapImage.PixelHeight;
            int stride = width * ((bitmapImage.Format.BitsPerPixel + 7) / 8);
            byte[] pixels = new byte[height * stride];
            bitmapImage.CopyPixels(pixels, stride, 0);

            //negate the pixels
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = (byte)(255 - pixels[i]);
            }

            //create a new bitmap
            BitmapSource negativeBitmap = BitmapSource.Create(width, height, bitmapImage.DpiX, bitmapImage.DpiY, bitmapImage.Format, bitmapImage.Palette, pixels, stride);
            //IMPreview.Source = negativeBitmap; 

            encoder.Frames.Add(BitmapFrame.Create(negativeBitmap));         //set the encoder to the new bitmap
            return negativeBitmap; //provide preview so caller can show it 
        }
        /// <summary>
        /// Special convert function for audio files based on format
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="newPath"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static bool specialConvert(MediaFoundationReader reader, string newPath, string format)
        {
            var newFormat = format.Trim().ToLower();
            switch (newFormat)
            {
                case "aac":
                    MediaFoundationEncoder.EncodeToAac(reader, newPath); //convert it to mp3 and save
                    return true;
                case "wma":
                    MediaFoundationEncoder.EncodeToWma(reader, newPath); //convert it to mp3 and save
                    return true;
                case "mp3":
                    MediaFoundationEncoder.EncodeToMp3(reader, newPath); //convert it to mp3 and save
                    return true;
                default:
                    Console.WriteLine("\nFormat not supported, proceed at your own risk");
                    return false;
            }
        }

        /// <summary>
        /// Load BitmapImage with proper settings to avoid file locking
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static BitmapImage LoadImageWithoutLock(string path)
        {
            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Load image into memory immediately
            bitmapImage.UriSource = new Uri(path);
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // Make it thread-safe and release file handle
            return bitmapImage;
        }

        public static (bool res, Exception? ex) saveSpecial(string format, string inputImagePath, string outputIconPath, uint width = 0, uint height = 0)
        {
            try
            {
                //TODO verify that the sizing changes are working as intended
                using MagickImage inputImage = new(inputImagePath);
                using MagickImage outputImage = (MagickImage)inputImage.Clone(); // Clone the image

                // Set the icon file format
                outputImage.Format = getSpecialFormat(format);
                if (width > 0 && height > 0)
                {
                    outputImage.Resize(width, height); // Resize the image to the specified width and height
                }

                outputImage.Write(outputIconPath);                     // Save the icon file
                Console.WriteLine($"{format} file saved successfully.");
                return (true, null);
            }
            catch (Exception ex)
            {
                //Status.AppendText("\nError saving icon file: " + ex.Message);
                Console.WriteLine("Error saving icon file: " + ex.Message);
                return (false, ex);
            }
        }
        public static BitmapSource doGreyscale(BitmapImage bitmapImage)
        {
            // Convert to greyscale using FormatConvertedBitmap
            FormatConvertedBitmap greyscaleBitmap = new();
            greyscaleBitmap.BeginInit();
            greyscaleBitmap.Source = bitmapImage;
            greyscaleBitmap.DestinationFormat = System.Windows.Media.PixelFormats.Gray8;
            greyscaleBitmap.EndInit();
            greyscaleBitmap.Freeze();

            return greyscaleBitmap;
        }
    }
}
