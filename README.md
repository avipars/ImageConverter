# ImageConverter


![image](https://github.com/avipars/ImageConverter/assets/5733247/aa942aac-0236-401a-8e69-37eff07625e0)

Application:

* Can be used as a portable exe but you need to have the Masgisk.NET.Core DLL (13.4.0) in the same folder as exe 

* Uses WPF, .Net 7.0, Masgisk (for image conversions)

Convert:

* Convert images to other formats (supports major formats in addition to webp and heif),

* Remove EXIF data - the new converted file will not contain any EXIF data

* Allow user to rename the new file and chose where to save it via the dialogs



Get File Info: 

* Supports any file, not just images

* Checksum uses CRC 16, this can be used to verify gigabyte BIOS hash files with their website (for example)

* Hash uses SHA256

* Also returns some basic information including file size in bytes and path 

* Even works for files in the recycle bin
