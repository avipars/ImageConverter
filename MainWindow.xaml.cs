using System;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Reflection.Emit;
using System.Collections;
using System.Runtime.Intrinsics.Arm;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Diagnostics;

namespace ImageConverter
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //bind to components
            TextBox Status = new TextBox();
            Image IMPreview = new Image(); //new image
        }

        string lastSavedHash = "";
        bool hashExists = false;

  
        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select a source image";

            //only allow images, add WEBP, tiff, ico
            openFileDialog.Filter = "All Files|*.*|Bitmap Image (*.bmp)|*.bmp|JPEG Image (*.jpg, *.jpeg)|*.jpg;*.jpeg|PNG Image (*.png)|*.png|GIF Image (*.gif)|*.gif|HEIF/C Image (*.heif, *.heic)|*.heif|WebP Image (*.webp)|*.webp|TIFF Image (*.tiff)|*.tiff|Icon Image (*.ico)|*.ico";
            openFileDialog.Multiselect = false; //only 1 image at a time
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;

            LogsStackPanel.Visibility = Visibility.Visible;


            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    bool isNegative = (NegativeButton.IsChecked == true);

                    // Load the selected image
                    BitmapImage bitmapImage = new BitmapImage(new Uri(openFileDialog.FileName));
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad; //load image into memory
                    bitmapImage.Freeze(); //prevent memory leaks
                    IMPreview.Source = bitmapImage; //show preview


                    ComboBoxItem selectedItem = (ComboBoxItem)FormatComboBox.SelectedItem;

                    string format;
                    if (selectedItem == null || selectedItem.Content == null || string.IsNullOrEmpty(selectedItem.Content.ToString()))
                    {
                        format = "";
                        Status.AppendText("Need to select an option");
                        throw new Exception("Need to select an option");
                    }
                    else
                    {

                        format = selectedItem.Content.ToString().ToLower(); //get format from the spinner box (user shouln't be able to select anything else)
                    }

                    string path = openFileDialog.FileName;
                    CalculateFileHash(path, "Original image");
                    Status.AppendText("\n" + getFileInfo(path));

                    // Get the file extension based on the selected format
                    ImageFormat imageFormat = null;

                    switch (format)
                    {
                        case "jpeg":
                            imageFormat = ImageFormat.Jpeg;
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
                            if ((!OperatingSystem.IsWindows() || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362)))
                            {
                                Console.WriteLine("WebP not supported on windows 7");
                                Status.AppendText("WebP not supported on " + System.Runtime.InteropServices.RuntimeInformation.OSDescription);
                                return;
                            }
                            imageFormat = ImageFormat.Webp;
                            break;
                        case "heif":
                            if ((!OperatingSystem.IsWindows() || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362)))
                            {
                                Console.WriteLine("HEIF not supported on windows 7");
                                Status.AppendText("HEIF not supported on " + System.Runtime.InteropServices.RuntimeInformation.OSDescription);
                                return;
                            }
                            imageFormat = ImageFormat.Heif;
                            break;
                        default:
                            imageFormat = null;
                            Status.AppendText("\nFormat not supported, proceed at your own risk");
                            break;
                    }

                    if (imageFormat == null)
                    { //major problem if we got here
                        throw new ArgumentNullException("imageFormat", "imageFormat is null");
                    }

                    // Save the converted image
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Title = "Chose where to save the new file";
                    saveFileDialog.Filter = $"{format.ToUpper()} Image|*.{format}";
                    saveFileDialog.ValidateNames = true;
                    saveFileDialog.CheckFileExists = false;
                    saveFileDialog.CheckPathExists = true;
                    saveFileDialog.AddExtension = true;
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        using (FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                        {
                            BitmapEncoder encoder = null;
                            switch (format)
                            {
                                case "jpeg":
                                    encoder = new JpegBitmapEncoder();
                                    break;
                                case "png":
                                    encoder = new PngBitmapEncoder();
                                    break;
                                case "gif":
                                    encoder = new GifBitmapEncoder();
                                    break;
                                case "tiff":
                                    encoder = new TiffBitmapEncoder();
                                    break;
                                case "bmp":
                                    encoder = new BmpBitmapEncoder();
                                    break;
                                default:
                                    encoder = null;
                                    Status.AppendText("\nFormat not supported");
                                    break;
                            }

                            if (encoder == null)
                            {
                                Console.WriteLine("Encoder is null (format not found)");
                                Status.AppendText("\nEncoder is null (format not found)");      //log an error
                                return;
                            }

                            if (isNegative) //negate all colors/pixels (COPILOT)
                            {
                                try
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
                                    IMPreview.Source = negativeBitmap; //show preview

                                    //set the encoder to the new bitmap
                                    encoder.Frames.Add(BitmapFrame.Create(negativeBitmap));
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Error negating image: " + ex.Message);
                                    Status.AppendText("\nError negating image: " + ex.Message);
                                }
                            }
                         

                            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                            
                            encoder.Save(fileStream);
                            fileStream.Close(); //done with file

                            Status.AppendText("\nImage converted and saved successfully!");

                            CalculateFileHash(saveFileDialog.FileName, "Converted image");
                            Status.AppendText("\n" + getFileInfo(path));

                        }
                    }
                }
                catch (Exception ex)
                {
                    Status.Text = $"An error occurred: {ex.Message}";
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// elper function to convert bytes to a more readable format
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string bytesToRightFormat(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return String.Format("{0:0.##} {1}", dblSByte, suffix[i]);
        }

        /// <summary>
        ///  Helper function to get file info
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string getFileInfo(string path)
        {
            FileInfo fi = new FileInfo(path);
            string sizeOf = bytesToRightFormat(fi.Length);
            return $"File size: {sizeOf} , attributes: {fi.Attributes}, Created {fi.CreationTime} Last Accessed {fi.LastAccessTime}";
        }

        private void HashButton_Click(object sender, RoutedEventArgs e)
        {
            LogsStackPanel.Visibility = Visibility.Visible; //nothing to show

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All Files (*.*)|*.*";
            openFileDialog.AddExtension = true;
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.Multiselect = false;
            openFileDialog.Title = "Select a file";

            if (openFileDialog.ShowDialog() == true)
            {
                string path = openFileDialog.FileName;
                CalculateFileHash(path);
                Status.AppendText("\n" + getFileInfo(path));
            }
            else
            {
                Status.Text = "No file selected";
            }

        }

        private void copyHashButton_Click(object sender, RoutedEventArgs e)
        {
            if (hashExists && lastSavedHash.Length > 0)
            {
                CopyClip(lastSavedHash, "hash");
                LogsStackPanel.Visibility = Visibility.Visible; //nothing to show
            }
            else
            {
                Status.AppendText("\nNo hash to copy");
            }
        }

        private void VirusButton_Click(object sender, RoutedEventArgs e)
        {
            //take the sha256 hash and open in virustotal in browser
            if (hashExists && lastSavedHash.Length > 0)
            {
                //string search = "https://www.virustotal.com/gui/search/" + lastSavedHash; //checks for new ones also
                string url = "https://www.virustotal.com/gui/file/" + lastSavedHash + "/detection";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else
            {
                Status.AppendText("\nNo hash to check");
            }
        }
        /// <summary>
        /// Calculates and returns CRC 16 of any file 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string CalculateChecksum16(string filePath)
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
                            checksum = (ushort)((checksum + buffer[i]) & 0xFFFF);
                        }
                    }
                }

                return checksum.ToString("X"); //convert decimal to hex
            }
            catch (Exception ex) // Handle potential exceptions
            {
                Status.AppendText("\nError calculating checksum: " + ex.Message);
                Console.WriteLine("Error calculating checksum: " + ex.Message);
                throw; // Re-throw the exception as issue calculating checksum
            }
        }

        private void CalculateFileChecksum(string filePath)
        {
            try
            {
                Status.AppendText($"\nChecksum (CRC 16 Hex): {CalculateChecksum16(filePath)}");
            }
            catch (Exception ex)
            {
                Status.AppendText("\nError calculating checksum: " + ex.Message);
                Console.WriteLine("Error calculating checksum: " + ex.Message);
            }
        }

        /// <summary>
        /// Calculates SHA256 hash of any file
        /// </summary>
        /// <param name="filePath"></param>
        private void CalculateFileHash(string filePath, string info = "")
        {
            try
            {
                LogsStackPanel.Visibility = Visibility.Visible;

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var sha = SHA256.Create()) // You can choose a different hash algorithm (e.g., SHA-1, SHA-256)
                    {
                        byte[] hashBytes = sha.ComputeHash(stream);
                        string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                        if (!string.IsNullOrEmpty(info))
                        {
                            Status.AppendText("\n" + info + ":");
                        }
                        Status.AppendText("\nPath: " + filePath);
                        Status.AppendText("\nHash (SHA256): " + hash);
                        lastSavedHash = hash;
                        hashExists = true;
                        CopyHashButton.Visibility = Visibility.Visible;
                        VirushButton.Visibility = Visibility.Visible;
                    }
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Status.AppendText("\nError calculating hash: " + ex.Message);
                Console.WriteLine("Error calculating hash: " + ex.Message);
                hashExists = false;
                CopyHashButton.Visibility = Visibility.Collapsed;
                VirushButton.Visibility = Visibility.Collapsed;
            }

            CalculateFileChecksum(filePath);
        }

        private void LogButton_Click(object sender, RoutedEventArgs e)
        {
            Status.Text = ""; //clear the box
            LogsStackPanel.Visibility = Visibility.Collapsed; //nothing to show
        }

        private void LogCopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (Status.Text.Contains("No log to copy."))
            {
                Status.Text = "";
                LogsStackPanel.Visibility = Visibility.Collapsed; //nothing to show
            }
            else
            {
                CopyClip(Status.Text, "log");
            }
        }

        private void CopyClip(string item, string type)
        {
            if (!string.IsNullOrEmpty(item as string))
            {
                Clipboard.SetText(item.Trim());
            }
            else
            {
                Status.AppendText($"\nNo {type} to copy.");
            }
        }
    }
}