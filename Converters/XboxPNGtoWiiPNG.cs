using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LavaManager.Converters
{
    class XboxPNGtoWiiPNG
    {
        // TAKEN FROM https://github.com/trojannemo/Nautilus/blob/master/Nautilus/NemoTools.cs
        public int TextureSize = 512; //default value
        private int TextureDivider = 2; //default value
        public bool isHorizontalTexture;
        public bool isVerticalTexture;
        public string DDS_Format;
        public bool KeepDDS = false;
        private const int FO_DELETE = 0x0003;
        private const int FOF_ALLOWUNDO = 0x0040;           // Preserve undo information, if possible.
        private const int FOF_NOCONFIRMATION = 0x0010;      // Show no confirmation dialog box to the user
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        // Struct which contains information that the SHFileOperation function uses to perform file operations.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.U4)]
            public int wFunc;
            public string pFrom;
            public string pTo;
            public short fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }


        /// <summary>
        /// Will send files or folders to Recycle Bin rather than delete from hard drive
        /// </summary>
        /// <param name="path">Full file / folder path to be recycled</param>
        /// <param name="isFolder">Whether path is to a folder rather than a file</param>
        public void SendtoTrash(string path, bool isFolder = false)
        {
            if (isFolder)
            {
                if (!Directory.Exists(path)) return;
            }
            else
            {
                if (!File.Exists(path)) return;
            }

            try
            {
                var fileop = new SHFILEOPSTRUCT
                {
                    wFunc = FO_DELETE,
                    pFrom = path + '\0' + '\0',
                    fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION
                };
                SHFileOperation(ref fileop);
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Use to resize images up or down or convert across BMP/JPG/PNG/TIF
        /// </summary>
        /// <param name="image_path">Full file path to source image</param>
        /// <param name="image_size">Integer for image size, can be smaller or bigger than source image</param>
        /// <param name="format">Format to save the image in: BMP | JPG | TIF | PNG (default)</param>
        /// <param name="output_path">Full file path to output image</param>
        /// <returns></returns>
        public bool ResizeImage(string image_path, int image_size, string format, string output_path)
        {
            try
            {
                var newImage = Path.GetDirectoryName(output_path) + "\\" + Path.GetFileNameWithoutExtension(output_path);

                Il.ilInit();
                Ilu.iluInit();

                var imageId = new int[10];

                // Generate the main image name to use
                Il.ilGenImages(1, imageId);

                // Bind this image name
                Il.ilBindImage(imageId[0]);

                // Loads the image into the imageId
                if (!Il.ilLoadImage(image_path))
                {
                    return false;
                }
                // Enable overwriting destination file
                Il.ilEnable(Il.IL_FILE_OVERWRITE);

                var height = isHorizontalTexture ? image_size / TextureDivider : image_size;
                var width = isVerticalTexture ? image_size / TextureDivider : image_size;

                //assume we're downscaling, this is better filter
                const int scaler = Ilu.ILU_BILINEAR;

                //resize image
                Ilu.iluImageParameter(Ilu.ILU_FILTER, scaler);
                Ilu.iluScale(width, height, 1);

                if (format.ToLowerInvariant().Contains("bmp"))
                {
                    //disable compression
                    Il.ilSetInteger(Il.IL_BMP_RLE, 0);
                    newImage = newImage + ".bmp";
                }
                else if (format.ToLowerInvariant().Contains("jpg") || format.ToLowerInvariant().Contains("jpeg"))
                {
                    Il.ilSetInteger(Il.IL_JPG_QUALITY, 99);
                    newImage = newImage + ".jpg";
                }
                else if (format.ToLowerInvariant().Contains("tif"))
                {
                    newImage = newImage + ".tif";
                }
                else if (format.ToLowerInvariant().Contains("tga"))
                {
                    Il.ilSetInteger(Il.IL_TGA_RLE, 0);
                    newImage = newImage + ".tga";
                }
                else
                {
                    Il.ilSetInteger(Il.IL_PNG_INTERLACE, 0);
                    newImage = newImage + ".png";
                }

                if (!Il.ilSaveImage(newImage))
                {
                    return false;
                }

                // Done with the imageId, so let's delete it
                Il.ilDeleteImages(1, imageId);

                return File.Exists(newImage);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static byte[] BuildDDSHeader(string format, int width, int height)
        {
            var dds = new byte[] //512x512 DXT5 
                {
                    0x44, 0x44, 0x53, 0x20, 0x7C, 0x00, 0x00, 0x00, 0x07, 0x10, 0x0A, 0x00, 0x00, 0x02, 0x00, 0x00,
                    0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x4E, 0x45, 0x4D, 0x4F, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00,
                    0x04, 0x00, 0x00, 0x00, 0x44, 0x58, 0x54, 0x35, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                };

            switch (format.ToLowerInvariant())
            {
                case "dxt1":
                    dds[87] = 0x31;
                    break;
                case "dxt3":
                    dds[87] = 0x33;
                    break;
                case "normal":
                    dds[84] = 0x41;
                    dds[85] = 0x54;
                    dds[86] = 0x49;
                    dds[87] = 0x32;
                    break;
            }

            switch (height)
            {
                case 8:
                    dds[12] = 0x08;
                    dds[13] = 0x00;
                    break;
                case 16:
                    dds[12] = 0x10;
                    dds[13] = 0x00;
                    break;
                case 32:
                    dds[12] = 0x20;
                    dds[13] = 0x00;
                    break;
                case 64:
                    dds[12] = 0x40;
                    dds[13] = 0x00;
                    break;
                case 128:
                    dds[12] = 0x80;
                    dds[13] = 0x00;
                    break;
                case 256:
                    dds[13] = 0x01;
                    break;
                case 1024:
                    dds[13] = 0x04;
                    break;
                case 2048:
                    dds[13] = 0x08;
                    break;
            }

            switch (width)
            {
                case 8:
                    dds[16] = 0x08;
                    dds[17] = 0x00;
                    break;
                case 16:
                    dds[16] = 0x10;
                    dds[17] = 0x00;
                    break;
                case 32:
                    dds[16] = 0x20;
                    dds[17] = 0x00;
                    break;
                case 64:
                    dds[16] = 0x40;
                    dds[17] = 0x00;
                    break;
                case 128:
                    dds[16] = 0x80;
                    dds[17] = 0x00;
                    break;
                case 256:
                    dds[17] = 0x01;
                    break;
                case 1024:
                    dds[17] = 0x04;
                    break;
                case 2048:
                    dds[17] = 0x08;
                    break;
            }

            if (width == height)
            {
                switch (width)
                {
                    case 8:
                        dds[0x1C] = 0x00; //no mipmaps at this size
                        break;
                    case 16:
                        dds[0x1C] = 0x05;
                        break;
                    case 32:
                        dds[0x1C] = 0x06;
                        break;
                    case 64:
                        dds[0x1C] = 0x07;
                        break;
                    case 128:
                        dds[0x1C] = 0x08;
                        break;
                    case 256:
                        dds[0x1C] = 0x09;
                        break;
                    case 1024:
                        dds[0x1C] = 0x0B;
                        break;
                    case 2048:
                        dds[0x1C] = 0x0C;
                        break;
                }
            }
            return dds;
        }

        /// <summary>
        /// Figure out right DDS header to go with HMX texture
        /// </summary>
        /// <param name="full_header">First 16 bytes of the png_xbox/png_ps3 file</param>
        /// <param name="short_header">Bytes 5-16 of the png_xbox/png_ps3 file</param>
        /// <returns></returns>
        private byte[] GetDDSHeader(IEnumerable<byte> full_header, IEnumerable<byte> short_header)
        {
            //official album art header, most likely to be the one being requested
            var header = BuildDDSHeader("dxt1", 256, 256);

            var headers = Directory.GetFiles(Application.StartupPath + "\\headers\\", "*.header");
            DDS_Format = "UNKNOWN";
            foreach (var head_name in from head in headers let header_bytes = File.ReadAllBytes(head) where full_header.SequenceEqual(header_bytes) || short_header.SequenceEqual(header_bytes) select Path.GetFileNameWithoutExtension(head).ToLowerInvariant())
            {
                DDS_Format = "DXT5";
                if (head_name.Contains("dxt1"))
                {
                    DDS_Format = "DXT1";
                }
                else if (head_name.Contains("normal"))
                {
                    DDS_Format = "NORMAL_MAP";
                }

                var index1 = head_name.IndexOf("_", StringComparison.Ordinal) + 1;
                var index2 = head_name.IndexOf("x", StringComparison.Ordinal);
                var width = Convert.ToInt16(head_name.Substring(index1, index2 - index1));
                index1 = head_name.IndexOf("_", index2, StringComparison.Ordinal);
                index2++;
                var height = Convert.ToInt16(head_name.Substring(index2, index1 - index2));

                header = BuildDDSHeader(DDS_Format.ToLowerInvariant().Replace("_map", ""), width, height);
                break;
            }
            return header;
        }

        /// <summary>
        /// Simple function to safely delete files
        /// </summary>
        /// <param name="file">Full path of file to be deleted</param>
        public void DeleteFile(string file)
        {
            if (string.IsNullOrWhiteSpace(file)) return;
            if (!File.Exists(file)) return;
            try
            {
                File.Delete(file);
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Converts png_xbox files to usable format
        /// </summary>
        /// <param name="rb_image">Full path to the png_xbox / png_ps3 / dds file</param>
        /// <param name="output_path">Full path you'd like to save the converted image</param>
        /// <param name="format">Allowed formats: BMP | JPG | PNG (default)</param>
        /// <param name="delete_original">True: delete | False: keep (default)</param>
        /// <returns></returns>
        public bool ConvertRBImage(string rb_image, string output_path, string format, bool delete_original)
        {
            var ddsfile = Path.GetDirectoryName(output_path) + "\\" + Path.GetFileNameWithoutExtension(output_path) + ".dds";
            var tgafile = ddsfile.Replace(".dds", ".tga");

            TextureSize = 256; //default size album art
            TextureDivider = 2; //default to divide larger size by, always multiples of 2
            isHorizontalTexture = false; //this is a rectangle wider than tall
            isVerticalTexture = false; //this is a rectangle taller than wide

            var nvTool = Application.StartupPath + "\\nvdecompress.exe";
            if (!File.Exists(nvTool))
            {
                MessageBox.Show("nvdecompress.exe is missing and is required\nProcess aborted", "Nemo Tools", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                if (ddsfile != rb_image)
                {
                    DeleteFile(ddsfile);
                }
                DeleteFile(tgafile);

                //read raw file bytes
                var ddsbytes = File.ReadAllBytes(rb_image);

                if (!rb_image.EndsWith(".dds", StringComparison.Ordinal))
                {
                    var buffer = new byte[4];
                    var swap = new byte[4];

                    //get filesize / 4 for number of times to loop
                    //32 is the size of the HMX header to skip
                    var loop = (ddsbytes.Length - 32) / 4;

                    //skip the HMX header
                    var input = new MemoryStream(ddsbytes, 32, ddsbytes.Length - 32);

                    //grab HMX header to compare against known headers
                    var full_header = new byte[16];
                    var file_header = new MemoryStream(ddsbytes, 0, 16);
                    file_header.Read(full_header, 0, 16);
                    file_header.Dispose();

                    //some games have a bunch of headers for the same files, so let's skip the varying portion and just
                    //grab the part that tells us the dimensions and image format
                    var short_header = new byte[11];
                    file_header = new MemoryStream(ddsbytes, 5, 11);
                    file_header.Read(short_header, 0, 11);
                    file_header.Dispose();

                    //create dds file
                    var output = new FileStream(ddsfile, FileMode.Create);
                    var header = GetDDSHeader(full_header, short_header);
                    output.Write(header, 0, header.Length);

                    //here we go
                    for (var x = 0; x <= loop; x++)
                    {
                        input.Read(buffer, 0, 4);

                        //PS3 images are not byte swapped, just DDS images with HMX header on top
                        if (rb_image.EndsWith("_ps3", StringComparison.Ordinal))
                        {
                            swap = buffer;
                        }
                        else
                        {
                            //XBOX images are byte swapped, so we gotta return it
                            swap[0] = buffer[1];
                            swap[1] = buffer[0];
                            swap[2] = buffer[3];
                            swap[3] = buffer[2];
                        }
                        output.Write(swap, 0, 4);
                    }
                    input.Dispose();
                    output.Dispose();
                }
                else
                {
                    ddsfile = rb_image;
                    tgafile = ddsfile.Replace(".dds", ".tga");
                }

                //read raw dds bytes
                ddsbytes = File.ReadAllBytes(ddsfile);

                //grab relevant part of dds header
                var header_stream = new MemoryStream(ddsbytes, 0, 32);
                var size = new byte[32];
                header_stream.Read(size, 0, 32);
                header_stream.Dispose();

                //default to 256x256
                var width = 256;
                var height = 256;

                //get dds dimensions from header
                switch (size[17]) //width byte
                {
                    case 0x00:
                        switch (size[16])
                        {
                            case 0x08:
                                width = 8;
                                break;
                            case 0x10:
                                width = 16;
                                break;
                            case 0x20:
                                width = 32;
                                break;
                            case 0x40:
                                width = 64;
                                break;
                            case 0x80:
                                width = 128;
                                break;
                        }
                        break;
                    case 0x02:
                        width = 512;
                        break;
                    case 0x04:
                        width = 1024;
                        break;
                    case 0x08:
                        width = 2048;
                        break;
                }
                switch (size[13]) //height byte
                {
                    case 0x00:
                        switch (size[12])
                        {
                            case 0x08:
                                height = 8;
                                break;
                            case 0x10:
                                height = 16;
                                break;
                            case 0x20:
                                height = 32;
                                break;
                            case 0x40:
                                height = 64;
                                break;
                            case 0x80:
                                height = 128;
                                break;
                        }
                        break;
                    case 0x02:
                        height = 512;
                        break;
                    case 0x04:
                        height = 1024;
                        break;
                    case 0x08:
                        height = 2048;
                        break;
                }

                if (width > height)
                {
                    isHorizontalTexture = true;
                    TextureDivider = width / height;
                    TextureSize = width;
                }
                else if (height > width)
                {
                    isVerticalTexture = true;
                    TextureDivider = height / width;
                    TextureSize = height;
                }
                else
                {
                    TextureSize = width;
                }

                var arg = "\"" + ddsfile + "\"";
                var startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    FileName = nvTool,
                    Arguments = arg,
                    WorkingDirectory = Application.StartupPath
                };
                var process = Process.Start(startInfo);
                do
                {
                    //
                } while (!process.HasExited);
                process.Dispose();

                if (!ResizeImage(tgafile, TextureSize, format, output_path))
                {
                    DeleteFile(tgafile);
                    return false;
                }
                if (!rb_image.EndsWith(".dds", StringComparison.Ordinal) && !KeepDDS)
                {
                    DeleteFile(ddsfile);
                }
                if (!format.ToLowerInvariant().Contains("tga"))
                {
                    DeleteFile(tgafile);
                }
                if (delete_original)
                {
                    SendtoTrash(rb_image);
                }
                return true;
            }
            catch (Exception)
            {
                if (!rb_image.EndsWith(".dds", StringComparison.Ordinal))
                {
                    DeleteFile(ddsfile);
                }
                return false;
            }
        }

        /// <summary>
        /// Converts images to png_wii format
        /// </summary>
        /// <param name="wimgt_path">Full path to wimgt.exe (REQUIRED)</param>
        /// <param name="image_path">Full path of image to be converted</param>
        /// <param name="output_path">Full path of output image</param>
        /// <param name="delete_original">True: Delete | False: Keep (default)</param>
        /// <returns></returns>
        public bool ConvertImagetoWii(string wimgt_path, string image_path, string output_path, bool delete_original)
        {
            var pngfile = Path.GetDirectoryName(image_path) + "\\converted\\" + Path.GetFileNameWithoutExtension(image_path) + ".png";
            if (!Directory.Exists(Path.GetDirectoryName(image_path) + "\\converted\\"))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(image_path) + "\\converted\\");
            }
            var tplfile = Path.GetDirectoryName(image_path) + "\\" + Path.GetFileNameWithoutExtension(image_path) + ".tpl";
            var origfile = image_path;
            var Headers = new ImageHeaders();

            try
            {
                var ext = Path.GetExtension(image_path);
                if (ext == ".png_xbox" || ext == ".png_ps3")
                {
                    if (!ConvertRBImage(image_path, pngfile, "png", false))
                    {
                        return false;
                    }
                    image_path = pngfile;
                }
                if (!ResizeImage(image_path, 256, "png", pngfile))
                {
                    return false;
                }

                if (File.Exists(wimgt_path))
                {
                    if (image_path != tplfile)
                    {
                        DeleteFile(tplfile);

                        try
                        {
                            var arg = "-d \"" + tplfile + "\" ENC -x TPL.CMPR \"" + pngfile + "\"";
                            var startInfo = new ProcessStartInfo
                            {
                                CreateNoWindow = true,
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                FileName = wimgt_path,
                                Arguments = arg,
                                WorkingDirectory = Application.StartupPath
                            };
                            var process = Process.Start(startInfo);
                            process.WaitForExit(); // Ensure the process completes
                            process.Dispose();

                            if (!File.Exists(tplfile))
                            {
                                return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("There was an error in converting the png_wii file\nThe error was caused by wimgt.exe\nThe error says:" +
                                ex.Message, "Nemo Tools", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            if (image_path != tplfile)
                            {
                                DeleteFile(tplfile);
                            }
                            if (image_path != pngfile)
                            {
                                DeleteFile(pngfile);
                            }
                        }
                    }
                    var wiifile = Path.GetDirectoryName(origfile) + "\\" + Path.GetFileNameWithoutExtension(origfile) + "_keep.png_wii";
                    wiifile = wiifile.Replace("_keep_keep", "_keep"); //in case of double _keep markers for whatever reason

                    DeleteFile(wiifile);
                    if (origfile != pngfile)
                    {
                        DeleteFile(pngfile);
                    }

                    var binaryReader = new BinaryReader(File.OpenRead(tplfile));
                    var binaryWriter = new BinaryWriter(new FileStream(wiifile, FileMode.Create));
                    binaryReader.BaseStream.Position = 64L;
                    binaryWriter.Write(Headers.wii_256x256);
                    var buffer = new byte[64];
                    int num;
                    do
                    {
                        num = binaryReader.Read(buffer, 0, 64);
                        if (num > 0)
                            binaryWriter.Write(buffer);
                    } while (num > 0);
                    binaryWriter.Dispose();
                    binaryReader.Dispose();

                    if (image_path != tplfile && !KeepDDS)
                    {
                        DeleteFile(tplfile);
                    }
                    if (delete_original)
                    {
                        DeleteFile(origfile);
                    }
                    return File.Exists(wiifile);
                }
                MessageBox.Show("Wimgt.exe is missing and is required\nNo png_wii album art was created", "Nemo Tools", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception)
            {
                DeleteFile(pngfile);
                DeleteFile(tplfile);
                return false;
            }
            return false;
        }
    }
}
