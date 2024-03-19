# ImageConverter


![image](https://github.com/avipars/ImageConverter/assets/5733247/b07c73c5-68c7-4192-89b6-2f3095ec3b31)

Download/Run:

* Go to [Releases](https://github.com/avipars/ImageConverter/releases) and click on "ImageConverter.exe" in the latest release and run it
  
* It's a portable exe... windows will prompt you with a warning before your un the app as it's not signed by them... if you want to run it, click more info -> run anyways.

Alternateively, you can compile it yourselv via Visual Studio 2022

  
Application:

* Uses WPF, .Net 7.0, Masgisk (for image conversions) Version 13.4.0 - at time of publishing

Convert:

* Convert images to other formats (supports major formats in addition to webp and heif),

* Supports batch image conversions

* Can save the negative of an image as well

* Remove EXIF data - the new converted file will not contain any EXIF data

* Allow user to rename the new file and chose where to save it via the dialogs


Get File Info: 

* Supports any file, not just images

* Checksum uses CRC 16, this can be used to verify gigabyte BIOS hash files with their website (for example)

* Hash uses SHA256

* Also returns some basic information including file size in bytes and path 

* Even works for files in the recycle bin

* Can be used to compare files via checksum, hash, and size

* And has a button to check the hash in VirusTotal, if you click the button it will open the site with your SHA256 file hash in the browser
  
* Upon clicking "open in VirusTotal": It will send ONLY the SHA256 hash to them (no files, or any other data)  
