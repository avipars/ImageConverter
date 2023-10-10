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

namespace ImageConverter
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            //init labels
            System.Windows.Controls.Label HashLabel = new System.Windows.Controls.Label();
            System.Windows.Controls.Label PathLabel = new System.Windows.Controls.Label();
            TextBox Status = new TextBox();
            Image IMPreview = new Image();
        }

        string hash = "";
        string path = "";

 
        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select an image";

            //only allow images, add WEBP, tiff, ico
            openFileDialog.Filter = "Bitmap Image (*.bmp)|*.bmp|JPEG Image (*.jpg, *.jpeg)|*.jpg;*.jpeg|PNG Image (*.png)|*.png|GIF Image (*.gif)|*.gif|HEIF Image (*.heif)|*.heif|WebP Image (*.webp)|*.webp|TIFF Image (*.tiff)|*.tiff|Icon Image (*.ico)|*.ico|HEIC Image (*.heic)|*.heic|All Files|*.*";
            //openFileDialog.Filter = "Image Files (*.bmp, *.jpg, *.png, *.gif, *.heif, *.webp, *.tiff, *.ico, *.heic)|*.bmp;*.jpg;*.png;*.gif;*.heif;*.webp;*.tiff;*.ico;*.heic;";
            openFileDialog.Multiselect = false;
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Load the selected image
                    BitmapImage bitmapImage = new BitmapImage(new Uri(openFileDialog.FileName));
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad; //load image into memory
                    bitmapImage.Freeze(); //prevent memory leaks
                    ComboBoxItem selectedItem = (ComboBoxItem)FormatComboBox.SelectedItem;

                    string format;
                    if (selectedItem == null || selectedItem.Content == null || string.IsNullOrEmpty(selectedItem.Content.ToString()))
                    {
                        format = "";
                        Status.AppendText("Need to select an option " + format);
                        throw new Exception("Need to select an option");

                    }
                    else
                    {
                        format = selectedItem.Content.ToString().ToLower();
                    }

                    path = openFileDialog.FileName;
                    CalculateFileHash(path);

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
                    }

                    IMPreview.Source = bitmapImage; //show preview
                   

                    // Save the converted image
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Title = "Chose where to save the new file";
                    saveFileDialog.Filter = $"{format.ToUpper()} Image|*.{format}";
                    saveFileDialog.ValidateNames = true;
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
                                case "ico":
                                    encoder = new IconBitmapEncoder();
                                    break;
                            }

                            if (encoder == null)
                            {
                                //log an error
                                Console.WriteLine("Encoder is null (format not found)");
                                Status.AppendText("\nEncoder is null (format not found)");
                                return;
                            }
                            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                            encoder.Save(fileStream);
                            fileStream.Close();
                                
                            Status.AppendText("\nImage converted and saved successfully!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Status.Text = $"An error occurred: {ex.Message}";
                }
            }
        }

        private void HashButton_Click(object sender, RoutedEventArgs e)
        {


            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "All Files (*.*)|*.*";
            openFileDialog.AddExtension = true;
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.Multiselect = false;
            openFileDialog.Title = "Select a file";
            HashLabel.Content = "";
            PathLabel.Content = "";

            if (openFileDialog.ShowDialog() == true)
            {
                path = openFileDialog.FileName;
                CalculateFileHash(path);
                //get file properties too
                FileInfo fi = new FileInfo(path);
                Status.AppendText($"\nFile size: {fi.Length} bytes, attributes: {fi.Attributes}, {fi.CreationTime}");
            }
            else
            {
                HashLabel.Content = "";
                PathLabel.Content = "";
                Status.Text = "No file selected";
            }
        }

        private ushort CalculateChecksum16(string filePath)
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
                        for (int i = 0; i < bytesRead; i++)
                        {
                            checksum = (ushort)((checksum + buffer[i]) & 0xFFFF);
                        }
                    }
                }

                return checksum;
            }
            catch (Exception ex)
            {
                // Handle the exception as needed
                Console.WriteLine("Error calculating checksum: " + ex.Message);
                throw; // Re-throw the exception if needed
            }
        }

        private void CalculateFileChecksum(string filePath)
        {
            try
            {
                ushort checksum = CalculateChecksum16(filePath);
                string checksumHex = checksum.ToString("X");

                Status.AppendText("\nChecksum(16 - bit): " + checksum.ToString() + "\n" + checksumHex);
                //convert to HEX


            }
            catch (Exception ex)
            {
                HashLabel.Content = "Error calculating checksum: ";
                Status.AppendText("\nError calculating checksum: " + ex.Message);
            }
        }

        private void CalculateFileHash(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var sha = SHA256.Create()) // You can choose a different hash algorithm (e.g., SHA-1, SHA-256)
                    {
                        byte[] hashBytes = sha.ComputeHash(stream);
                        hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();


                        HashLabel.Content = "Hash: " + hash;
                        PathLabel.Content = "Path: " + filePath;
                    }

                    stream.Close();
                }

            }
            catch (Exception ex)
            {
                HashLabel.Content = "Error calculating hash: ";
                Status.AppendText("\nError calculating hash: " + ex.Message);
            }

            CalculateFileChecksum(filePath);
        }



        private void CopyToClipboardHash_Click(object sender, RoutedEventArgs e)
        {
            CopyClip(hash, "hash");
        }

        private void CopyToClipboardPath_Click(object sender, RoutedEventArgs e)
        {
            CopyClip(path,"path");
        }

        private void CopyClip(string item, string type)
        {
            if (!string.IsNullOrEmpty(item as string))
            {
                Clipboard.SetText(item);
            }
            else
            {
                Status.AppendText($"\nNo {type} to copy.");
            }
        }
    }
}