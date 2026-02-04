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
namespace ImageConverter;

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
        if (!FileHelper.openItem(path))
        {
            Status.AppendText("\nNo file to open");
        }
    }

    private void openFolder(string path)
    {
        //gets the file path, and opens surrounding folder with the image selected
        if (!FileHelper.openFolder(path))
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
        OpenFileDialog openFileDialog = FileHelper.openFileDialogBuilder(title, filter);

        LogsStackPanel.Visibility = Visibility.Visible;

        if (openFileDialog.ShowDialog() == true)
        {
            int total = openFileDialog.FileNames.Length;
            int j = 0;
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
                        format = selectedItem.Content.ToString().ToLower(); //get format from the spinner box (user shouldn't be able to select anything else)
                    }
                    AudioInfo.Text = $"Processing {++j} of {total} audio files\n";
                    CalculateFileHash(path, "Original file");
                    Status.AppendText("\n" + FileHelper.getFileInfo(path));

                    string filtered = $"{format.ToUpper()} Audio|*.{format}";
                    SaveFileDialog saveFileDialog = FileHelper.saveFileDialogBuilder(filtered, path);

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
        bool res = FileHelper.specialConvert(reader, newPath, format);
        if (!res)
        {
            Status.AppendText("\nFormat not supported, proceed at your own risk");
        }
        return res;
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


    private bool saveSpecial(string format, string inputImagePath, string outputIconPath, uint width = 0, uint height = 0)
    {
        var result = FileHelper.saveSpecial(format, inputImagePath, outputIconPath, width, height);
        if (result.res && result.ex == null)
        {
            Status.AppendText($"{format} file saved successfully.");
            return true;
        }
        else
        {
            Status.AppendText("\nError saving icon file: " + result.ex.Message);
            return false;
        }
    }


    private void ConvertButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleOpenButtons(true, true);
        string title = "Select a source image";
        string filter = "All Files|*.*|Bitmap Image (*.bmp)|*.bmp|JPEG Image (*.jpg, *.jpeg)|*.jpg;*.jpeg|PNG Image (*.png)|*.png|GIF Image (*.gif)|*.gif|HEIF/C Image (*.heif, *.heic)|*.heif|WebP Image (*.webp)|*.webp|TIFF Image (*.tiff)|*.tiff|Icon Image (*.ico)|*.ico";
        OpenFileDialog openFileDialog = FileHelper.openFileDialogBuilder(title, filter);


        LogsStackPanel.Visibility = Visibility.Visible;

        if (openFileDialog.ShowDialog() == true)
        {
            int total = openFileDialog.FileNames.Length;
            int j = 0;
            foreach (string path in openFileDialog.FileNames)
            {
                try
                {

                    ImageInfo.Text = $"Processing {++j} of {total} images\n";
                    // get resolution of file
                    string resolution = string.Empty;
                    bool isNegative = (NegativeButton.IsChecked == true); //gets checkbox status

                    // Load the selected image
                    BitmapImage bitmapImage = new BitmapImage(new Uri(path));
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad; //load image into memory
                    bitmapImage.Freeze(); //prevent memory leaks
                    IMPreview.Source = bitmapImage; //show preview

                    ComboBoxItem selectedItem = (ComboBoxItem)FormatComboBox.SelectedItem; //get the selected target format

                    resolution = $"{bitmapImage.PixelWidth}x{bitmapImage.PixelHeight}";
                    ImageInfo.Text += $" Resolution: {resolution}px";

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
                    Status.AppendText("\n" + FileHelper.getFileInfo(path));

                    ImageFormat imageFormat = FileHelper.getImageFormat(format) ?? throw new ArgumentNullException("imageFormat", "imageFormat is null");

                    // Save the converted image
                    string filtered = $"{format.ToUpper()} Image|*.{format}";
                    SaveFileDialog saveFileDialog = FileHelper.saveFileDialogBuilder(filtered, path);

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
                            using (FileStream fileStream = new(fileName, FileMode.Create))
                            {

                                BitmapEncoder encoder = FileHelper.mapStringToBitmapEncoder(format);
                                if (encoder == null)
                                {
                                    Console.WriteLine($"Encoder is null (format {format} not found/supported)");
                                    Status.AppendText($"\nEncoder is null (format {format} not found/supported)");
                                    res = false;
                                    //log an error
                                    return;
                                }

                                if (isNegative) //negate all colors/pixels (COPILOT)
                                {
                                    try
                                    {
                                        IMPreview.Source = FileHelper.doNegative(bitmapImage, encoder);; //show preview
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
                            Status.AppendText("\n" + FileHelper.getFileInfo(path));
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
                Status.AppendText("\n" + FileHelper.getFileInfo(path));
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
    private void CalculateChecksum16(string filePath)
    {
        var (res, ex) = FileHelper.CalculateChecksum16(filePath);
        if (ex != null)
        {
            Status.AppendText("\nError calculating checksum: " + ex.Message);
            Console.WriteLine("Error calculating checksum: " + ex.Message);
        }
        else
        {
            Status.AppendText($"\nChecksum (CRC 16 Hex): {res}");
        }
    }

   

    /// <summary>
    /// Calculates SHA256, CR16 and some more hashes of any file
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

        CalculateChecksum16(filePath);
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
        if (!FileHelper.CopyToClip(item, type))
        {
            Status.AppendText($"\nNo {type} to copy.");
        }
    }
}