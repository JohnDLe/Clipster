using Clipster.Validators;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Clipster
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            var argDefaults = configuration.GetSection("ArgumentDefaults");
            var valDefaults = configuration.GetSection("ValidatorDefaults");

            var idx = args.Length > 0 ? Array.FindIndex(args, s => s.Equals("-s") || s.Equals("--source")) + 1 : -1;
            var app = new CommandLineApplication();

            app.HelpOption();
            var optionSource = app.Option("-s|--source <Source>", "Source video file",
                CommandOptionType.SingleValue)
                    .IsRequired()
                    .Accepts(v => v.ExistingFile());


            var optionOutput = app.Option("-o|--output <Output>", "Output of the generated preview file",
                CommandOptionType.SingleValue)                    
                    .Accepts(v => v.LegalFilePath());
            if (idx > -1)
            {
                optionOutput.Validators.Add(new OutputFileValidator(args[idx]));
            }

            var optionBeginSeconds = app.Option("-b|--begin-seconds <Seconds>", $"When to start the preview - default {argDefaults["BEGIN_SECONDS"]} seconds",
                CommandOptionType.SingleValue);
            optionBeginSeconds.Validators.Add(new RangeValidator(int.Parse(valDefaults["BEGIN_SECONDS_LOWER"]), int.Parse(valDefaults["BEGIN_SECONDS_UPPER"])));

            var optionLengthSeconds = app.Option("-l|--length-seconds <Seconds>", $"Length of snippet - default {argDefaults["LENGTH_SECONDS"]} seconds",
                CommandOptionType.SingleValue);
            optionLengthSeconds.Validators.Add(new RangeValidator(int.Parse(valDefaults["LENGTH_SECONDS_LOWER"]), int.Parse(valDefaults["LENGTH_SECONDS_UPPER"])));

            var optionDesiredSnippets = app.Option("-d|--desired-snippets <Snippets>", $"Number of desired snippet - default {argDefaults["DESIRED_SNIPPETS"]} snippets",
                CommandOptionType.SingleValue);
            optionDesiredSnippets.Validators.Add(new RangeValidator(int.Parse(valDefaults["DESIRED_SNIPPETS_LOWER"]), int.Parse(valDefaults["DESIRED_SNIPPETS_UPPER"])));

            var optionVideoHeight = app.Option("-h|--video-height <Pixels>", $"Video height dimension - default {argDefaults["VIDEO_HEIGHT"]} pixels",
                CommandOptionType.SingleValue);
            optionVideoHeight.Validators.Add(new RangeValidator(int.Parse(valDefaults["VIDEO_HEIGHT_LOWER"]), int.Parse(valDefaults["VIDEO_HEIGHT_UPPER"])));

            var optionVideoWidth = app.Option("-w|--video-width <Pixels>", $"Video height dimension - default {argDefaults["VIDEO_WIDTH"]} pixels",
                CommandOptionType.SingleValue);
            optionVideoWidth.Validators.Add(new RangeValidator(int.Parse(valDefaults["VIDEO_WIDTH_LOWER"]), int.Parse(valDefaults["VIDEO_WIDTH_UPPER"])));

            var optionVerbose = app.Option("-v|--verbose", "Level of logging",
                CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                var verbose = optionVerbose.HasValue();
                var beginSeconds = optionBeginSeconds.HasValue()
                    ? int.Parse(optionBeginSeconds.Value())
                    : int.Parse(argDefaults["BEGIN_SECONDS"]);
                var lengthSeconds = optionLengthSeconds.HasValue()
                    ? int.Parse(optionLengthSeconds.Value())
                    : int.Parse(argDefaults["LENGTH_SECONDS"]);
                var desiredSnippets = optionDesiredSnippets.HasValue()
                    ? int.Parse(optionDesiredSnippets.Value())
                    : int.Parse(argDefaults["DESIRED_SNIPPETS"]);
                var videoHeight = optionVideoHeight.HasValue()
                   ? int.Parse(optionVideoHeight.Value())
                   : int.Parse(argDefaults["VIDEO_HEIGHT"]);
                var videoWidth = optionVideoWidth.HasValue()
                   ? int.Parse(optionVideoWidth.Value())
                   : int.Parse(argDefaults["VIDEO_WIDTH"]);

                var attribute = new PreviewAttribute()
                {
                    Source = optionSource.Value(),
                    Output = optionOutput.Value(),
                    BeginSeconds = beginSeconds,
                    DesiredSnippets = desiredSnippets,
                    SnippetLengthInSeconds = lengthSeconds,
                    VideoHeightInPixels = videoHeight,
                    VideoWidthInPixels = videoWidth,
                    Verbose = verbose
                };
                
                PreviewEngine.Run(attribute);
            });

            app.Execute(args);
        }        
    }
}
