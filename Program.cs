using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;

namespace HoverGames.RapidIoT.BitmapUtility
{
    internal class Program
    {
        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">Command line arguments. Pass in '--help' to view information about possible values.</param>
        private static void Main(string[] args)
        {
            CommandLineApplication commandLineApplication = new CommandLineApplication
            {
                Name = Assembly.GetExecutingAssembly().GetName().Name,
                Description = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description
            };

            commandLineApplication.HelpOption("-?|-h|--help");
            commandLineApplication.VersionOption("-v|--version", () => { return string.Format("Version {0}", Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion); });

            commandLineApplication.OnExecute(() =>
            {
                commandLineApplication.ShowHelp();

                return 0;
            });

            commandLineApplication.Command("createBitmaps", x =>
            {
                x.Description = "Create bitmap files from code folder.";
                x.HelpOption("-?|-h|--help");

                CommandArgument codeCommandArgument = x.Argument("code", "Code folder path");
                CommandArgument outputCommandArgument = x.Argument("output", "Output folder path");

                CommandOption codeCommandOption = x.Option("-c|--code", "Code folder path", CommandOptionType.SingleValue);
                CommandOption outputCommandOption = x.Option("-o|--output", "Output folder path", CommandOptionType.SingleValue);
                CommandOption replaceCommandOption = x.Option("-r|--replace", "Replace existing files", CommandOptionType.NoValue);

                x.OnExecute(() =>
                {
                    string? codeFolderPath;
                    string? outputFolderPath;

                    if (codeCommandArgument.Value != null && !codeCommandOption.HasValue())
                        codeFolderPath = codeCommandArgument.Value;
                    else if (codeCommandArgument.Value == null && codeCommandOption.HasValue())
                        codeFolderPath = codeCommandOption.Value();
                    else
                        codeFolderPath = null;

                    if (outputCommandArgument.Value != null && !outputCommandOption.HasValue())
                        outputFolderPath = outputCommandArgument.Value;
                    else if (outputCommandArgument.Value == null && outputCommandOption.HasValue())
                        outputFolderPath = outputCommandOption.Value();
                    else
                        outputFolderPath = null;

                    if (string.IsNullOrWhiteSpace(codeFolderPath) || string.IsNullOrWhiteSpace(outputFolderPath))
                        x.ShowHelp();
                    else
                        CreateBitmapFiles(codeFolderPath, outputFolderPath, replaceCommandOption.HasValue());

                    return 0;
                });
            });

            commandLineApplication.Command("createCode", x =>
            {
                x.Description = "Create code file from bitmap file.";
                x.HelpOption("-?|-h|--help");

                CommandArgument bitmapCommandArgument = x.Argument("bitmap", "Bitmap file path");
                CommandArgument outputCommandArgument = x.Argument("output", "Output file path");

                CommandOption bitmapCommandOption = x.Option("-b|--bitmap", "Bitmap file path", CommandOptionType.SingleValue);
                CommandOption outputCommandOption = x.Option("-o|--output", "Output file path", CommandOptionType.SingleValue);
                CommandOption replaceCommandOption = x.Option("-r|--replace", "Replace existing file", CommandOptionType.NoValue);

                x.OnExecute(() =>
                {
                    string? bitmapFilePath;
                    string? outputFilePath;

                    if (bitmapCommandArgument.Value != null && !bitmapCommandOption.HasValue())
                        bitmapFilePath = bitmapCommandArgument.Value;
                    else if (bitmapCommandArgument.Value == null && bitmapCommandOption.HasValue())
                        bitmapFilePath = bitmapCommandOption.Value();
                    else
                        bitmapFilePath = null;

                    if (outputCommandArgument.Value != null && !outputCommandOption.HasValue())
                        outputFilePath = outputCommandArgument.Value;
                    else if (outputCommandArgument.Value == null && outputCommandOption.HasValue())
                        outputFilePath = outputCommandOption.Value();
                    else
                        outputFilePath = null;

                    if (string.IsNullOrWhiteSpace(bitmapFilePath) || string.IsNullOrWhiteSpace(outputFilePath))
                        x.ShowHelp();
                    else
                        CreateCodeFile(bitmapFilePath, outputFilePath, replaceCommandOption.HasValue());

                    return 0;
                });
            });

            try
            {
                commandLineApplication.Execute(args);
            }
            catch (CommandParsingException ex)
            {
                commandLineApplication.Out.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Creates bitmap files in the specified output folder, from a specified folder containing code files.
        /// </summary>
        /// <param name="codeFolderPath">Path to a folder that contains code files.</param>
        /// <param name="outputFolderPath">Path to a folder that will contain the created bitmap files.</param>
        /// <param name="replace">Flag to indicate if files should be replaced if they already exist. Otherwise existing files will not be overwritten.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        private static void CreateBitmapFiles(string codeFolderPath, string outputFolderPath, bool replace = false)
        {
            if (codeFolderPath == null)
                throw new ArgumentNullException(nameof(codeFolderPath));
            else if (codeFolderPath.Trim().Length == 0)
                throw new ArgumentException("Empty value.", nameof(codeFolderPath));

            if (outputFolderPath == null)
                throw new ArgumentNullException(nameof(outputFolderPath));
            else if (outputFolderPath.Trim().Length == 0)
                throw new ArgumentException("Empty value.", nameof(outputFolderPath));

            DirectoryInfo sourceDirectoryInfo = new DirectoryInfo(codeFolderPath);
            DirectoryInfo targetDirectoryInfo = new DirectoryInfo(outputFolderPath);

            if (!sourceDirectoryInfo.Exists)
                throw new ArgumentException("Folder does not exist.", nameof(codeFolderPath));

            FileInfo[] sourceFileInfos = sourceDirectoryInfo.GetFiles();

            for (int i = 0; i < sourceFileInfos.Length; i++)
            {
                FileInfo fileInfo = sourceFileInfos[i];
                byte[] bytes = ReadCodeFile(fileInfo);

                if (bytes.Length > 0)
                {
                    FileInfo targetFileInfo = new FileInfo(Path.Combine(targetDirectoryInfo.FullName, $"{fileInfo.Name}.bmp"));

                    if (replace || !targetFileInfo.Exists)
                        WriteBitmapFile(bytes, targetFileInfo);
                }
            }
        }

        /// <summary>
        /// Creates a code file at the specified path, from a specified bitmap file.
        /// </summary>
        /// <param name="bitmapFilePath">Path to a file that contains a bitmap file.</param>
        /// <param name="outputFilePath">Path to a file that will contain the created code file.</param>
        /// <param name="replace">Flag to indicate if file should be replaced if it already exists. Otherwise existing file will not be overwritten.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        private static void CreateCodeFile(string bitmapFilePath, string outputFilePath, bool replace = false)
        {
            if (bitmapFilePath == null)
                throw new ArgumentNullException(nameof(bitmapFilePath));
            else if (bitmapFilePath.Trim().Length == 0)
                throw new ArgumentException("Empty value.", nameof(bitmapFilePath));

            if (outputFilePath == null)
                throw new ArgumentNullException(nameof(outputFilePath));
            else if (outputFilePath.Trim().Length == 0)
                throw new ArgumentException("Empty value.", nameof(outputFilePath));

            FileInfo sourceFileInfo = new FileInfo(bitmapFilePath);
            FileInfo targetFileInfo = new FileInfo(outputFilePath);

            if (!sourceFileInfo.Exists)
                throw new ArgumentException("File does not exist.", nameof(sourceFileInfo));

            byte[] bytes = ReadBitmapFile(sourceFileInfo);

            if (replace || !targetFileInfo.Exists)
                WriteCodeFile(bytes, targetFileInfo);
        }

        /// <summary>
        /// Reads bytes from a bitmap file.
        /// </summary>
        /// <param name="fileInfo">File containing a bitmap.</param>
        /// <returns>An array of bytes.</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FileNotFoundException"/>
        private static byte[] ReadBitmapFile(FileInfo fileInfo)
        {
            if (fileInfo == null)
                throw new ArgumentNullException(nameof(fileInfo));
            else if (!fileInfo.Exists)
                throw new FileNotFoundException("Code file not found.", fileInfo.FullName);

            List<byte> result = new List<byte>();

            using (FileStream fileStream = fileInfo.OpenRead())
            using (BinaryReader binaryReader = new BinaryReader(fileStream))
            {
                byte[] bytes;

                do
                {
                    bytes = binaryReader.ReadBytes(100);
                    result.AddRange(bytes);
                }
                while (bytes.Length > 0);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Reads bytes from a code file.
        /// </summary>
        /// <param name="fileInfo">File containing code.</param>
        /// <returns>An array of bytes.</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FileNotFoundException"/>
        private static byte[] ReadCodeFile(FileInfo fileInfo)
        {
            if (fileInfo == null)
                throw new ArgumentNullException(nameof(fileInfo));
            else if (!fileInfo.Exists)
                throw new FileNotFoundException("Code file not found.", fileInfo.FullName);

            List<byte> result = new List<byte>();

            using (StreamReader streamReader = fileInfo.OpenText())
            {
                while (!streamReader.EndOfStream)
                {
                    string? line = streamReader.ReadLine();

                    if (line != null)
                    {
                        line = line.Trim();

                        if (line.StartsWith("0x"))
                        {
                            foreach (string byteString in line.Split(",", StringSplitOptions.RemoveEmptyEntries))
                                result.Add((byte)Convert.ToInt32(byteString.Trim(), 16));
                        }
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Writes out a bitmap file from a byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="fileInfo"></param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        private static void WriteBitmapFile(byte[] bytes, FileInfo fileInfo)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            else if (bytes.Length == 0)
                throw new ArgumentException("Empty value.", nameof(bytes));

            if (fileInfo == null)
                throw new ArgumentNullException(nameof(fileInfo));

            using (MemoryStream memoryStream = new MemoryStream(bytes))
            using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(memoryStream))
                bitmap.Save(fileInfo.FullName);
        }

        /// <summary>
        /// Writes out a code file from a byte array.
        /// </summary>
        /// <remarks>
        /// The code file follows a similar syntax to the code files found in the Atmosphere IoT's 'ui_template' folder.
        /// </remarks>
        /// <param name="bytes">Bytes to write into code file.</param>
        /// <param name="fileInfo">File to write.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        private static void WriteCodeFile(byte[] bytes, FileInfo fileInfo)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            else if (bytes.Length == 0)
                throw new ArgumentException("Empty value.", nameof(bytes));

            if (fileInfo == null)
                throw new ArgumentNullException(nameof(fileInfo));

            using (StreamWriter streamWriter = new StreamWriter(fileInfo.FullName))
            {
                streamWriter.WriteLine($"static const unsigned char myImage[{bytes.Length}] = {{");

                for (int i = 0; i < bytes.Length; i++)
                {
                    streamWriter.Write($"0x{bytes[i]:X2}");

                    if (i + 1 < bytes.Length)
                        streamWriter.Write(", ");

                    if ((i + 1) % 40 == 0)
                        streamWriter.WriteLine();
                }

                streamWriter.WriteLine();
                streamWriter.WriteLine("};");
            }
        }
    }
}