using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Polly;
using Clipboard = System.Windows.Forms.Clipboard;
using DataFormats = System.Windows.Forms.DataFormats;
using DataObject = System.Windows.Forms.DataObject;

namespace Screenshot2PNG {

    internal static class Program {

        private const string LauncherWindowClass = "ArenaNet";
        private const string GameClientWindowClass = "ArenaNet_Dx_Window_Class";

        private static readonly object SyncLock = new();
        private static DateTime _lastWriteTime = DateTime.UnixEpoch;

        private static readonly StringBuilder WindowClass = new(1024);

        public class Options {

            [Value(0, MetaName = "input-dir", Required = true, HelpText = "Specifies the directory from which the screenshot bitmaps are read.")]
            public string InputDir { get; set; }

            [Option('o', "output-dir", HelpText = "Specifies the directory in which the converted screenshots should be saved.")]
            public string? OutputDir { get; set; }

            [Option("convert-existing", Default = true, HelpText = "Enables or disables conversion of existing screenshots.")]
            public bool ConvertExisting { get; set; }

            [Option("use-clipboard", Default = true, HelpText = "Enables or disables clipboard integration.")]
            public bool CopyToClipboard { get; set; }

        }

        private static void Main(string[] args) {
            Parser.Default.ParseArguments<Options>(args).WithParsed(options => {
                /* Validate the input directory. */
                if (!Directory.Exists(options.InputDir)) {
                    Console.WriteLine($"Input directory '{options.InputDir}' does not exist. Creating...");

                    try {
                        Directory.CreateDirectory(options.InputDir);
                    } catch (Exception e) {
                        Console.WriteLine("ERROR: Failed to create input directory!");
                        Console.WriteLine(e);
                        throw;
                    }
                }

                var outputDir = options.OutputDir ?? options.InputDir;

                /* Validate the output directory. */
                if (options.OutputDir != null) {
                    Console.WriteLine($"Output directory '{outputDir}' does not exist. Creating...");

                    try {
                        Directory.CreateDirectory(outputDir);
                    } catch (Exception e) {
                        Console.WriteLine("ERROR: Failed to create output directory!");
                        Console.WriteLine(e);
                        throw;
                    }
                }

                if (options.ConvertExisting) {
                    Console.WriteLine("Converting existing screenshots...");

                    string[] children = Directory.GetFiles(options.InputDir, "gw*.png");
                    foreach (var child in children) {
                        ProcessScreenshotCandidate(outputDir, child, false);
                    }

                    Console.WriteLine("Done!");
                }

                Console.WriteLine("Started watching for new screenshots.");

                using var watcher = new FileSystemWatcher {
                    Path = options.InputDir,
                    Filter = "gw*.bmp",
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.FileName
                };

                watcher.Created += (_, eventArgs) => {
                    var candidateFile = eventArgs.FullPath;

                    Console.WriteLine(candidateFile);

                    Policy.Handle<Exception>()
                        .WaitAndRetryForever(_ => TimeSpan.FromMilliseconds(100))
                        .Execute(() => {
                            ProcessScreenshotCandidate(outputDir, candidateFile, options.CopyToClipboard);
                            Console.WriteLine($"Finished processing {candidateFile}");
                        });

                    Policy.Handle<Exception>(_ => true)
                        .WaitAndRetry(new[] {
                            TimeSpan.FromMilliseconds(500),
                            TimeSpan.FromSeconds(1),
                            TimeSpan.FromSeconds(5)
                        })
                        .Execute(() => Task.Run(() => {
                            Console.WriteLine($"Attempting to delete {candidateFile}");
                            File.Delete(candidateFile);
                            Console.WriteLine($"Deleted {candidateFile}");
                        }));
                };

                var gw2Process = FindGw2Process();

                if (gw2Process == null) {
                    Console.WriteLine("Did not find any running GW2 process. Waiting 20s...");

                    /* Wait for 20s if no GW2 process has been found after the first attempt. */
                    Thread.Sleep(20 * 1000);
                }

                do {
                    Console.WriteLine("Searching for running GW2 process...");

                    gw2Process = FindGw2Process();
                    gw2Process?.WaitForExit();
                } while (gw2Process != null);

                Console.WriteLine("No running GW2 process found. Exiting!");
            });
        }

        private static void ProcessScreenshotCandidate(string outputDir, string candidateFile, bool copyToClipboard) {
            Console.WriteLine($"Processing {candidateFile}");

            using Bitmap screenshotBitmap = new(candidateFile);

            /*
             * Use GetLastWriteTime instead of GetCreationTime in order to avoid running into issues when file-system
             * tunneling [1] is enabled on windows. [2]
             *
             * [1] https://support.microsoft.com/?kbid=172190
             * [2] https://stackoverflow.com/questions/8804342/windows-filesystem-creation-time-of-a-file-doesnt-change-when-while-is-deleted
             */
            var creationTime = File.GetLastWriteTimeUtc(candidateFile);
            var destinationFileName = Path.Combine(outputDir, $"{creationTime:yyyy-MM-dd_HH-mm-ss.fff}.png");
            Console.WriteLine($"Saving {destinationFileName}");

            using MemoryStream pngMemoryStream = new();
            screenshotBitmap.Save(pngMemoryStream, ImageFormat.Png);

            byte[] screenshotPng = pngMemoryStream.ToArray();

            if (copyToClipboard) {
                lock (SyncLock) {
                    if (creationTime > _lastWriteTime) {
                        _lastWriteTime = creationTime;
                        CopyToClipboard(screenshotBitmap, screenshotPng);
                    }
                }
            }

            File.WriteAllBytes(destinationFileName, screenshotPng);
            File.SetCreationTimeUtc(destinationFileName, creationTime);
            File.SetLastWriteTimeUtc(destinationFileName, creationTime);
        }

        private static void CopyToClipboard(Bitmap bitmap, IEnumerable screenshotPng) {
            Thread staThread = new(() => {
                DataObject data = new();
                data.SetData(DataFormats.Bitmap, bitmap);
                data.SetData("PNG", screenshotPng);

                Clipboard.SetDataObject(data, true);
            });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }

        private static Process? FindGw2Process() {
            var processes = Process.GetProcesses();

            foreach (var process in processes) {
                var hWnd = process.MainWindowHandle;

                var result = GetClassName(hWnd, WindowClass, WindowClass.Capacity);
                if (result == 0) continue;

                var cls = WindowClass.ToString();
                if (cls.Equals(LauncherWindowClass) || cls.Equals(GameClientWindowClass)) return process;
            }

            return null;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    }

}