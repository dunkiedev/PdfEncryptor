using System;
using System.IO;
using System.Text;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Configuration;

namespace PdfEncryptor
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfiguration builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddCommandLine(args)
                .Build();

            AppConfiguration appConfig = builder.GetSection("appConfiguration").Get<AppConfiguration>();

            string sourceFolder = GetSourceFolder(appConfig);
            string outputFolder = GetOutputFolder(sourceFolder, appConfig);
            Console.Out.WriteLine($"Source folder: {sourceFolder}");
            Console.Out.WriteLine($"Output folder: {outputFolder}");

            string[] pdfFiles = Directory.GetFiles(sourceFolder,"*.pdf");
            if (pdfFiles.Length == 0)
            {
                WriteErrorToConsole("\nNo PDF files found in the source folder.");
            }
            else
            {
                Console.Out.WriteLine($"\nNumber of PDF files found in source folder: {pdfFiles.Length}");

                Directory.CreateDirectory(outputFolder);

                int successCount = 0;
                foreach (string pdfFile in pdfFiles)
                {
                    string fileName = Path.GetFileName(pdfFile);
                    string destinationFile = Path.Combine(outputFolder, fileName);

                    bool fileCreated = false;
                    try
                    {
                        Console.Out.Write($"{fileName}... ");
                        EncryptPdfWithPassword(pdfFile, appConfig.SourcePassword,
                            destinationFile, appConfig.UserPassword, appConfig.OwnerPassword);
                        fileCreated = true;
                        Console.Out.Write("Created... ");
                        VerifyPdf(destinationFile, appConfig.UserPassword);
                        WriteSuccessToConsole("Verified");

                        if (appConfig.DeleteSourceFile)
                        {
                            File.Delete(pdfFile);
                        }
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        if (!fileCreated)
                        {
                            WriteErrorToConsole($"Error creating file! {ex.Message}");
                        }
                        else
                        {
                            WriteErrorToConsole($"Output file not verified! {ex.Message}");
                        }
                        
                        //might be that the destination file was created, even if empty - tidy up
                        File.Delete(destinationFile);
                    }
                }

                if (successCount == pdfFiles.Length)
                {
                    WriteSuccessToConsole("\nAll files successfully processed.");
                }
                else if(successCount == 0)
                {
                    WriteErrorToConsole("\nWarning - no files were successfully processed!");
                }
                else
                {
                    WriteErrorToConsole("\nWarning - not all files were successfully processed!");
                }
            }

            Console.Out.WriteLine("\nPress any key to close...");
            Console.ReadKey();
        }

        private static void EncryptPdfWithPassword(string sourceFile, string passwordSource, string destinationFile, string passwordUser, string passwordOwner)
        {
            byte[] userPassword = Encoding.ASCII.GetBytes(passwordUser);
            byte[] ownerPassword = Encoding.ASCII.GetBytes(passwordOwner);

            PdfReader reader = null;
            if (!string.IsNullOrEmpty(passwordSource))
            {
                byte[] sourcePassword = Encoding.ASCII.GetBytes(passwordSource);
                ReaderProperties readerProperties = new ReaderProperties().SetPassword(sourcePassword);
                reader = new PdfReader(sourceFile, readerProperties);
            }
            else
            {
                reader = new PdfReader(sourceFile);
            }

            WriterProperties props = new WriterProperties()
                .SetStandardEncryption(userPassword, ownerPassword, EncryptionConstants.ALLOW_PRINTING,
                    EncryptionConstants.ENCRYPTION_AES_256 | EncryptionConstants.DO_NOT_ENCRYPT_METADATA);

            using PdfWriter writer = new PdfWriter(destinationFile, props);
            PdfDocument pdfDoc = null;
            try
            {
                pdfDoc = new PdfDocument(reader, writer);
            }
            finally
            {
                pdfDoc?.Close();
            }
        }

        /// <summary>
        /// Reads in the encrypted PDF with the supplied password to make sure it can be opened.
        /// </summary>
        private static void VerifyPdf(string pdfFile, string passwordUser)
        {
            byte[] userPassword = Encoding.ASCII.GetBytes(passwordUser);

            ReaderProperties readerProperties = new ReaderProperties().SetPassword(userPassword);
            using PdfReader reader = new PdfReader(pdfFile, readerProperties);
            PdfDocument pdfDocument = new PdfDocument(reader);
        }

        /// <summary>
        /// Gets the absolute path to the source folder. If not specified in the command line parameters it defaults to the working directory.
        /// </summary>
        private static string GetSourceFolder(AppConfiguration appConfig)
        {
            string sourceFolder = appConfig.SourceFolder ?? Directory.GetCurrentDirectory();
            return Path.GetFullPath(sourceFolder);
        }

        /// <summary>
        /// Gets the full (absolute) path to the output folder. If configured as a relative path, it will be turned into
        /// an absolute path in terms of the source folder.
        /// </summary>
        /// <param name="sourceFolder">Absolute path to the source folder</param>
        /// <param name="appConfig"></param>
        /// <returns></returns>
        private static string GetOutputFolder(string sourceFolder, AppConfiguration appConfig)
        {
            if (string.IsNullOrEmpty(sourceFolder))
            {
                throw new ArgumentException("sourceFolder not supplied", nameof(sourceFolder));
            }

            string outputFolder = appConfig.OutputFolder;
            if (outputFolder == null)
            {
                return sourceFolder;
            }

            //must have specified a path; may be absolute or relative to the source folder
            if (Path.IsPathRooted(outputFolder))
            {
                return outputFolder;
            }

            //must be relative - calculate path in terms of source folder
            return Path.Combine(sourceFolder, outputFolder);
        }

        private static void WriteErrorToConsole(string message, bool writeLine = true)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            if (writeLine)
            {
                Console.Out.WriteLine(message);
            }
            else
            {
                Console.Out.Write(message);
            }
            Console.ResetColor();
        }

        private static void WriteSuccessToConsole(string message, bool writeLine = true)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            if (writeLine)
            {
                Console.Out.WriteLine(message);
            }
            else
            {
                Console.Out.Write(message);
            }
            Console.ResetColor();
        }
    }
}
