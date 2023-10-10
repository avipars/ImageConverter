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


namespace ImageConverter
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            System.Windows.Controls.Label HashLabel = new System.Windows.Controls.Label();
            System.Windows.Controls.Label PathLabel = new System.Windows.Controls.Label();
            TextBox Status = new TextBox();
        }

        //Image Image { get; set; }

        string hash = "";
        string path = "";

        //private void SelectImageButton(object sender, RoutedEventArgs e)
        //{
        //    OpenFileDialog openFileDialog = new OpenFileDialog();
        //    openFileDialog.Title = "Select image";
        //    if (openFileDialog.ShowDialog() == true)
        //    {
        //        // Code to load the selected image
        //        // Display the image or save the path for later use
        //        try
        //        {
        //            // Load the selected image
        //            BitmapImage bitmapImage = new BitmapImage(new Uri(openFileDialog.FileName));

        //            // Display the image or save the path for later use
        //            //Image.Source = bitmapImage; // Replace "YourImageControl" with the actual name of your image control in the XAML

        //            // Alternatively, you can save the path for later use
        //            path = openFileDialog.FileName;
        //            CalculateFileHash(path);
        //        }
        //        catch (Exception ex)
        //        {
        //            Status.AppendText($"An error occurred: {ex.Message}");
        //            //MessageBox.Show($"An error occurred: {ex.Message}", "Image Converter", MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //    }
        //}

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select an image";
        
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Load the selected image
                    BitmapImage bitmapImage = new BitmapImage(new Uri(openFileDialog.FileName));

                    ComboBoxItem selectedItem = (ComboBoxItem)FormatComboBox.SelectedItem;

                    string format;
                    if (selectedItem == null || selectedItem.Content == null ||  string.IsNullOrEmpty(selectedItem.Content.ToString())) {
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
                        
                        case "webp": 
                            if ((!OperatingSystem.IsWindows() || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362)))
                            {
                                Console.WriteLine("WebP not supported on windows 7");
                                Status.AppendText("WebP not supported on " + System.Runtime.InteropServices.RuntimeInformation.OSDescription);
                                return;
                            }
                          
                            imageFormat = ImageFormat.Webp;
                            break;
                            
                        // Add other format cases here
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
                    //close OG image

                    // Save the converted image
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Title = "Chose where to save the new file";
                    saveFileDialog.Filter = $"{format.ToUpper()} Image|*.{format}";
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
                                    // Add other format cases here
                            }

                            if(encoder == null)
                            {
                                //log an error
                                Console.WriteLine("Encoder is null");
                                Status.AppendText("\nEncoder is null");
                                return;
                            }
                            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                            encoder.Save(fileStream);
                            fileStream.Close();

                            //MessageBox.Show("Image converted and saved successfully!", "Image Converter");
                            Status.AppendText("\nImage converted and saved successfully!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"An error occurred: {ex.Message}", "Image Converter", MessageBoxButton.OK, MessageBoxImage.Error);
                    Status.Text = $"An error occurred: {ex.Message}";
                }
            }
        }

        private void HashButton_Click(object sender, RoutedEventArgs e)
        {
          

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "All Files (*.*)|*.*";
            HashLabel.Content = "";
            PathLabel.Content = "";

            if (openFileDialog.ShowDialog() == true)
            {
                path = openFileDialog.FileName;
                CalculateFileHash(path);

            }
            else
            {
                HashLabel.Content = "";
                PathLabel.Content = "";
                Status.Text = "No file selected";

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
                PathLabel.Content = "ERROR";
                Status.AppendText("\nError calculating hash: " + ex.Message);

                //MessageBox.Show("Error calculating hash: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(hash as string))
            {
                //remove the prefix
                Clipboard.SetText(hash);
                //MessageBox.Show("Hash copied to clipboard.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                //MessageBox.Show("No hash to copy.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                Status.AppendText("\nNo hash to copy.");

            }
        }

        private void CopyToClipboardButton2_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(path as string))
            {
                Clipboard.SetText(path);
                //MessageBox.Show("Hash copied to clipboard.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                //MessageBox.Show("No Path to copy.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                Status.AppendText("\nNo path to copy.");

            }
        }
    }

}

