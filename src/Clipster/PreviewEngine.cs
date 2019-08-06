using MediaToolkit;
using MediaToolkit.Model;
using System;
using System.Diagnostics;
using System.IO;

namespace Clipster
{
    public enum AspectRatio
    {
        FullScreen, // 4:3
        WideScreen  // 16:9
        
    }
    public static class PreviewEngine
    {
        private static string _workingDir;
        private static string _snippetsDir;
        private static string _listFile;
        private static string _sourceFile;
        private static string _outputFile;
        private static string _outputFileExt;
        private static string _ffmpeg = @".\ffmpeg\ffmpeg.exe";

        public static void Run(PreviewAttribute attribute)
        {
            SetWorkingDir(attribute.Source, attribute.Output);
            DisplayHero(attribute);

            double totalSeconds;
            AspectRatio ratio;
            GetVideoData(attribute.Source, out totalSeconds, out ratio);

            // Get video length in seconds
            var length = (int)Math.Round(totalSeconds, 0);

            // Ensure the video is long enough to even bother previewing
            var minLength = attribute.SnippetLengthInSeconds * attribute.DesiredSnippets;

            // Display and check video length
            if (length < minLength)
            {
                Console.WriteLine("Video is too short. Skipping");
                Environment.Exit(0);
            }

            // Video dimension            
            var dimensions = ratio == AspectRatio.FullScreen
                ? @"(iw*sar)*min(427/(iw*sar)\,240/ih):ih*min(427/(iw*sar)\,240/ih), pad=427:240:(427-iw*min(427/iw\,240/ih))/2:(240-ih*min(427/iw\,240/ih))/2"
                : "426x240";
            var interval = ((length - attribute.BeginSeconds) / attribute.DesiredSnippets);
            string arguments;
            for (var i = 1; i <= attribute.DesiredSnippets; i++)
            {
                var start = ((i * interval) + attribute.BeginSeconds);
                // only create snippet if the start time is not at the end of the video
                // or if the start time +  snippet length does not exceed the length of the video
                if (start < length && start + attribute.SnippetLengthInSeconds <= length)
                {
                    var formattedStart = string.Format("{0}:{1}:{2}",
                                            (start / 3600).ToString("D2"),
                                            ((start % 3600) / 60).ToString("D2"),
                                            (start % 60).ToString("D2"));
                    Console.WriteLine($"Generating preview part {i}{_outputFileExt} at {formattedStart}");

                    // Generating the snippet at calculated time                
                    arguments = $@"-i {_sourceFile} -vf ""scale={dimensions}"" -an -preset fast -qmin 1 -qmax 1 -ss {
                        formattedStart} -t {attribute.SnippetLengthInSeconds} {_snippetsDir}\\{i}{_outputFileExt}";
                    RunVideoTool(arguments, attribute.Verbose);
                }
            }

            // Concat videos
            Console.WriteLine("Generating final preview file");
            CreateListFile();

            // Generate a text file with one snippet video location per line
            // (https://trac.ffmpeg.org/wiki/Concatenate)
            arguments = $@"-y -f concat -safe 0 -i {_listFile} -c copy {_outputFile}";
            RunVideoTool(arguments, attribute.Verbose);

            File.Copy(_outputFile, attribute.Output, true);

            Console.WriteLine($"Clip creation completed! File is located at {attribute.Output}");
        }

        private static void GetVideoData(string filename, out double totalSeconds, out AspectRatio ratio)
        {
            var mediaFile = new MediaFile { Filename = filename };

            using (var engine = new Engine(@".\ffmpeg\ffmpeg.exe"))
            {
                engine.GetMetadata(mediaFile);
                totalSeconds = mediaFile.Metadata.Duration.TotalSeconds;

                var frameSize = mediaFile.Metadata.VideoData.FrameSize;
                var sizeArr = frameSize.Split('x');
                var width = int.Parse(sizeArr[0]);
                var height = int.Parse(sizeArr[1]);

                var result = Math.Round((height * 16m) / width, 0);

                switch (result)
                {
                    case 12:
                        ratio = AspectRatio.FullScreen;
                        break;
                    case 9:
                        ratio = AspectRatio.WideScreen;
                        break;
                    default:
                        throw new ApplicationException($"Unsupported aspect ratio {frameSize}");
                }
            }
        }

        private static void SetWorkingDir(string source, string output)
        {
            _workingDir = Path.Combine(Path.GetTempPath(), "clipster");
            _snippetsDir = Path.Combine(_workingDir, "snippets");
            _listFile = Path.Combine(_workingDir, "list.txt");

            // clear out snippets dir if exists, else create it
            if (Directory.Exists(_snippetsDir))
            {
                var snippetsDirInfo = new DirectoryInfo(_snippetsDir);
                foreach (FileInfo file in snippetsDirInfo.GetFiles())
                {
                    file.Delete();
                }
            }
            else
            {
                Directory.CreateDirectory(_snippetsDir);
            }

            // remove all files in the working dir
            if (Directory.Exists(_workingDir))
            {
                var workingDirInfo = new DirectoryInfo(_workingDir);
                foreach (FileInfo file in workingDirInfo.GetFiles())
                {
                    file.Delete();
                }
            }

            // copy source file into working dir. Removing all spaces in file name since ffmpeg has a hard time with
            // processing files with spaces.
            var sourceExt = Path.GetExtension(source);
            _outputFileExt = Path.GetExtension(output);
            _sourceFile = Path.Combine(_workingDir, $"source{sourceExt}");
            File.Copy(source, _sourceFile, true);

            _outputFile = Path.Combine(_workingDir, $"output{_outputFileExt}");
        }

        private static void RunVideoTool(string args, bool verbose)
        {
            var startInfo = new ProcessStartInfo();

            startInfo.CreateNoWindow = !verbose;
            startInfo.UseShellExecute = false;
            startInfo.FileName = _ffmpeg;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = args;

            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
        }

        private static void CreateListFile()
        {
            if (Directory.Exists(_workingDir))
            {
                using (var writeFile = new StreamWriter(_listFile))
                {
                    var files = Directory.GetFiles(_snippetsDir);
                    foreach (var file in files)
                    {
                        writeFile.WriteLine($"file {file.Replace(@"\", @"\\")}");
                    }
                }
            }
        }

        private static void DisplayHero(PreviewAttribute attribute)
        {
            Console.WriteLine("Clipster running with the following arguments:");
            Console.WriteLine("");                       
            Console.WriteLine($"   Begin capture at:  {attribute.BeginSeconds} second(s)");
            Console.WriteLine($"   Desired Snippets:  {attribute.DesiredSnippets}");
            Console.WriteLine($"   Output File:       {attribute.Output}");
            Console.WriteLine($"   Snippet Length:    {attribute.SnippetLengthInSeconds} second(s)");
            Console.WriteLine($"   Source File:       {attribute.Source}");
            Console.WriteLine($"   Verbose Logging:   {attribute.Verbose}");
            Console.WriteLine($"   Video Height:      {attribute.VideoHeightInPixels} pixel(s)");
            Console.WriteLine($"   Video Width:       {attribute.VideoWidthInPixels} pixel(s)");
            Console.WriteLine("");
            Console.WriteLine("");
        }
    }
}
