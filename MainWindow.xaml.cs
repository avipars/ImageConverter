using System;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Diagnostics;
using NAudio.Wave;
using NAudio.MediaFoundation;
using ImageMagick;
namespace ImageConverter
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //global variables for hash, filepath
        string lastSavedHash = string.Empty;
        bool hashExists = false;
        string fileName = string.Empty; //filepath


        private void openItem(string path)
        {
            //gets the file path, and opens the image in photo viewer
            if (!string.IsNullOrEmpty(path))
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            else
            {
                Status.AppendText("\nNo file to open");
            }
        }

        private void openFolder(string path)
        {
            //gets the file path, and opens surrounding folder with the image selected
            if (!string.IsNullOrEmpty(path))
            {
                string argument = "/select, \"" + path + "\"";
                Process.Start("explorer.exe", argument);
            }
            else
            {
                Status.AppendText("\nNo folder to open");
            }
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            openFolder(fileName);
        }

        private void OpenImgButton_Click(object sender, RoutedEventArgs e)
        {
            openItem(fileName);
        }

        //private void OpenAudioFolderButton_Click(object sender, RoutedEventArgs e)
        //{
        //    openFolder(fileName);
        //}

        //private void OpenAudioButton_Click(object sender, RoutedEventArgs e)
        //{
        //    openItem(fileName);
        //}
        /// <summary>
        /// Convert audio or video source to audio result
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConvertAudioButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleOpenButtons(true, false);
            //open audio or video format
            string title = "Select a video or audio source file";
            string filter = "All Files|*.*|AAC (*.aac)|*.aac|WMA (*.wma)|*.wma|MP3 (*.mp3)|*.mp3|MP4 (*.mp4)|*.mp4|WAV (*.wav)|*.wav|M4A (*.m4a)|*.m4a"; //todo limit to audio and video formats
            OpenFileDialog openFileDialog = openFileDialogBuilder(title, filter);

            LogsStackPanel.Visibility = Visibility.Visible;

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string path in openFileDialog.FileNames) //go to each file selected
                {
                    try
                    {
                        // Load the selected file
                        ComboBoxItem selectedItem = (ComboBoxItem)FormatAudioComboBox.SelectedItem; //get the selected target format
                        string format = string.Empty;
                        if (selectedItem == null || selectedItem.Content == null || string.IsNullOrEmpty(selectedItem.Content.ToString()))
                        {
                            Status.AppendText("Need to select an option");
                            throw new Exception("Need to select an option");
                        }
                        else
                        {
                            format = selectedItem.Content.ToString().ToLower(); //get format from the spinner box (user shouln't be able to select anything else)
                        }
                        CalculateFileHash(path, "Original file");
                        Status.AppendText("\n" + getFileInfo(path));

                        string filtered = $"{format.ToUpper()} Audio|*.{format}";
                        SaveFileDialog saveFileDialog = saveFileDialogBuilder(filtered, path);

                        if (saveFileDialog.ShowDialog() == true)  //open save dialog
                        {
                            fileName = saveFileDialog.FileName;
                            //convert it to right format
                            //save 
                            MediaFoundationApi.Startup(); //initialize the api
                                                          //convert it to right format
                            bool res = baseConvert(path, fileName, format); // Get the file extension based on the selected format
                                                                            //save 
                            if (res)
                            {
                                ToggleOpenButtons(false, false);  //show ability to open it in folder or file
                            }
                            else
                            {
                                Status.AppendText($"\nIssue with format {format} and {res}");
                            }
                        }
                        MediaFoundationApi.Shutdown(); //close the api
                    }
                    catch (Exception ex)
                    {
                        Status.Text = $"An audio/video file error occurred: {ex.Message} {fileName}";
                        Console.WriteLine($"An audio/video file error occurred: {ex.Message} {fileName}");
                        MediaFoundationApi.Shutdown(); //close the api
                    }
                }
            }
        }

        /// <summary>
        /// Special convert function for audio files based on format
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="newPath"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        private bool specialConvert(MediaFoundationReader reader, string newPath, string format)
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
                //case "mp4":
                //    //convert to mp4
                //    return false; //todo implement
                //case "wav":
                //    //convert to wav
                //    return false; //todo implement
                //case "m4a":
                //    //convert to m4a
                //    return false; //todo implement
                default:
                    Status.AppendText("\nFormat not supported, proceed at your own risk");
                    return false;
            }
        }

        /// <summary>
        /// Base function to convert audio or video source to audio result
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newPath"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        private bool baseConvert(string path, string newPath, string format)
        {
            using (var reader = new MediaFoundationReader(path)) //read the old file
            {
                try
                {
                    return specialConvert(reader, newPath, format); //go to relevant encoder
                }
                catch (Exception ex)
                {
                    Status.Text = $"Issue converting format: {ex.Message}";
                    Console.WriteLine($"Issue converting format: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Toggle the result open file and folder buttons
        /// first is to clear it , 2nd is the type
        /// </summary>
        /// <param name="clear"></param>
        /// <param name="img"></param>
        private void ToggleOpenButtons(bool clear, bool img)
        {
            var collapse = Visibility.Collapsed;
            var show = Visibility.Visible;

            if (clear)
            {
                fileName = string.Empty;
                if (img)
                {
                    OpenImgButton.Visibility = collapse;
                    OpenFolderButton.Visibility = collapse;
                }
                else
                {
                    OpenAudioFolderButton.Visibility = collapse;  //audio
                    OpenAudioButton.Visibility = collapse;
                }
            }
            else
            {
                if (img)
                {
                    OpenImgButton.Visibility = show;
                    OpenFolderButton.Visibility = show;
                }
                else
                {
                    OpenAudioFolderButton.Visibility = show;  //audio
                    OpenAudioButton.Visibility = show;
                }
            }
        }

        private SaveFileDialog saveFileDialogBuilder(string filter, string path = "", string filename_tip = "")
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
                string fileWithoutPath = System.IO.Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(filename_tip)) //if we don't have a tip, use the default
                {
                    saveFileDialog.FileName = "converted_" + fileWithoutPath;
                }
                else //use the tip
                {
                    saveFileDialog.FileName = filename_tip + fileWithoutPath;
                }
            }
            return saveFileDialog;
        }

        private OpenFileDialog openFileDialogBuilder(string title, string filter)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = title;
            ofd.Filter = filter;
            ofd.Multiselect = true; //many files together is okay
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            return ofd;
        }


        private MagickFormat getSpecialFormat(string format)
        {
            switch (format.ToLower().Trim())
            {
                case "heif":
                case "heic":
                    return MagickFormat.Heif;
                case "webp":
                    return MagickFormat.WebP;
                case "ico":
                    return MagickFormat.Ico;
                case "svg":
                    return MagickFormat.Svg;
                default:
                    return MagickFormat.Png;
            }
        }

        private bool saveSpecial(string format, string inputImagePath, string outputIconPath, int width = 0, int height = 0)
        {
            try
            {
                using (MagickImage image = new MagickImage())
                {
                    // Load the input image
                    MagickImage inputImage = new MagickImage(inputImagePath);
                    // Set the icon file format
                    image.Format = getSpecialFormat(format);
                    if (width > 0 && height > 0)
                    {
                        inputImage.Resize(width, height);
                    }

                    image.Write(outputIconPath);                     // Save the icon file
                    Console.WriteLine($"{format} file saved successfully.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Status.AppendText("\nError saving icon file: " + ex.Message);
                Console.WriteLine("Error saving icon file: " + ex.Message);
                return false;
            }

        }


        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleOpenButtons(true, true);
            string title = "Select a source image";
            string filter = "All Files|*.*|Bitmap Image (*.bmp)|*.bmp|JPEG Image (*.jpg, *.jpeg)|*.jpg;*.jpeg|PNG Image (*.png)|*.png|GIF Image (*.gif)|*.gif|HEIF/C Image (*.heif, *.heic)|*.heif|WebP Image (*.webp)|*.webp|TIFF Image (*.tiff)|*.tiff|Icon Image (*.ico)|*.ico";
            OpenFileDialog openFileDialog = openFileDialogBuilder(title, filter);


            LogsStackPanel.Visibility = Visibility.Visible;

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string path in openFileDialog.FileNames)
                {
                    try
                    {
                        bool isNegative = (NegativeButton.IsChecked == true); //gets checkbox status

                        // Load the selected image
                        BitmapImage bitmapImage = new BitmapImage(new Uri(path));
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad; //load image into memory
                        bitmapImage.Freeze(); //prevent memory leaks
                        IMPreview.Source = bitmapImage; //show preview

                        ComboBoxItem selectedItem = (ComboBoxItem)FormatComboBox.SelectedItem; //get the selected target format

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

                        CalculateFileHash(path, "Original image");
                        Status.AppendText("\n" + getFileInfo(path));

                        ImageFormat imageFormat = null;

                        switch (format)   // Get the file extension based on the selected format
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
                            case "heic":
                                if ((!OperatingSystem.IsWindows() || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362)))
                                {
                                    Console.WriteLine("HEIF/C not supported on windows 7");
                                    Status.AppendText("HEIF/C not supported on " + System.Runtime.InteropServices.RuntimeInformation.OSDescription);
                                    return;
                                }
                                imageFormat = ImageFormat.Heif;
                                break;
                            default:
                                imageFormat = null;
                                Status.AppendText($"Format {format} not supported, proceed at your own risk");
                                Console.WriteLine($"Format {format} not supported, proceed at your own risk");
                                break;
                        }

                        if (imageFormat == null)
                        { //major problem if we got here
                            throw new ArgumentNullException("imageFormat", "imageFormat is null");
                        }

                        // Save the converted image
                        string filtered = $"{format.ToUpper()} Image|*.{format}";
                        SaveFileDialog saveFileDialog = saveFileDialogBuilder(filtered, path);

                        bool res = false;
                        if (saveFileDialog.ShowDialog() == true)
                        {
                            fileName = saveFileDialog.FileName;
                            string newFormat = format.ToLower().Trim();
                            if (newFormat.Equals("heif") || newFormat.Equals("heic") 
                                || newFormat.Equals("webp") || newFormat.Equals("svg"))
                            {
                                res = saveSpecial(path, fileName, newFormat);
                            }
                            else if (newFormat.Equals("ico"))
                            {
                                res =  saveSpecial(path, fileName, newFormat, 256, 256);
                            }
                            else
                            {
                                using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
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
                                            Console.WriteLine("Format not supported");
                                            res = false;
                                            break;
                                    }

                                    if (encoder == null)
                                    {
                                        Console.WriteLine("Encoder is null (format not found)");
                                        Status.AppendText("\nEncoder is null (format not found)");
                                        res = false;
                                        //log an error
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
                                            res = false;
                                        }
                                    }
                                    encoder.Frames.Add(BitmapFrame.Create(bitmapImage));

                                    encoder.Save(fileStream);
                                    fileStream.Close(); //done with file
                                    res = true;
                                }
                            }
                            if (res)
                            {
                                //done regular and special
                                Status.AppendText("\nImage converted and saved successfully!");
                                CalculateFileHash(saveFileDialog.FileName, "Converted image");
                                Status.AppendText("\n" + getFileInfo(path));
                                ToggleOpenButtons(false, true); //show ability to open it in folder or file
                            }
                            else
                            {
                                Status.AppendText($"\nIssue with format {format} and {res}");
                                throw new Exception($"Issue with format {format} and {res}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ToggleOpenButtons(true, true);
                        Status.Text = $"An image file error occurred: {ex.Message}";
                        Console.WriteLine($"An image file error occurred: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Helper function to convert bytes to a more readable format
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string bytesToRightFormat(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < suffix.Length && bytes >= 1024; ++i, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0; //get the right format
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
            openFileDialog.Multiselect = true;
            openFileDialog.Title = "Select a file or files";

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string path in openFileDialog.FileNames)
                {
                    CalculateFileHash(path);
                    Status.AppendText("\n" + getFileInfo(path));
                }
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
                //"https://www.virustotal.com/gui/search/" + lastSavedHash; //checks for new ones also
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
            Status.Text = string.Empty; //clear the box
            LogsStackPanel.Visibility = Visibility.Collapsed; //nothing to show
        }

        private void LogCopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (Status.Text.Contains("No log to copy."))
            {
                Status.Text = string.Empty;
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