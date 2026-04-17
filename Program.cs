using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using FMP.FMP7;

namespace FMP7CLI
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding  = System.Text.Encoding.UTF8;

            RootCommand rootCommand = new RootCommand("FMP7 CLI - FMP7 Controler on CLI");

            // Option<bool> APIOption = new("--api")
            // {
            //     Description = "API Mode",
            //     Recursive = true
            // };
            // rootCommand.Options.Add(APIOption);


            Command playCommand = new Command("play", "Play a music file");
            Option<string> pathOption = new("--path"){
                Description = "The path to the music file to play",
                Required = true
            };
            playCommand.Add(pathOption);
            playCommand.SetAction(parseResult =>
            {
                if (!CheckRunning())
                    return;
                Play(parseResult.GetValue(pathOption));
            });
            rootCommand.Add(playCommand);

            Command stopCommand = new Command("stop", "Stop playing");
            stopCommand.SetAction(parseResult => 
            {
                if (!CheckRunning())
                    return;
                Stop();
            });
            rootCommand.Add(stopCommand);

            Command pauseCommand = new Command("pause", "Pause playing");
            pauseCommand.SetAction(parseResult => 
            {
                if (!CheckRunning())
                    return;
                Pause();
            });
            rootCommand.Add(pauseCommand);

            Command fadeCommand = new Command("fade", "Fade playing");
            Option<int?> fadeTimeOption = new("--time")
            {
                Description = "The time to fade in seconds (default: 10, range: 1-255)",
            };
            fadeCommand.Add(fadeTimeOption);
            fadeCommand.SetAction(parseResult =>
            {
                if (!CheckRunning())
                    return;
                if (parseResult.GetValue(fadeTimeOption) == null || parseResult.GetValue(fadeTimeOption) < 1 || parseResult.GetValue(fadeTimeOption) > 255)
                    Fade(10);
                else
                    Fade(parseResult.GetValue(fadeTimeOption).Value);
            });
            rootCommand.Add(fadeCommand);

            Command getExtsCommand = new Command("get-exts", "Get supported file extensions");
            getExtsCommand.SetAction(parseResult =>
            {
                GetExts();
            });
            rootCommand.Add(getExtsCommand);
        

            ParseResult parseResult = rootCommand.Parse(args);
            if (parseResult.Errors.Count > 0)
            {
                rootCommand.Parse("-h").Invoke();
                foreach (ParseError parseError in parseResult.Errors)
                {
                    Console.WriteLine("ERROR: " + parseError.Message);
                }
                Environment.Exit(1);
            }

            Environment.Exit(parseResult.Invoke());
        }

        /// <summary>
        /// Checks if FMP7 is running.
        /// </summary>
        /// <returns>True if FMP7 is running, false otherwise.</returns>
        public static bool CheckRunning()
        {
            if (FMPControl.CheckAvailableFMP() == false)
            {
                Console.WriteLine("ERROR: " + "FMP is not Available. Please start or restart FMP7.");
                return false;
            }

            try
            {
                FMPInfo info = FMPControl.GetFMPInfo();
                if (info == null)
                {
                    Console.WriteLine("ERROR: " + "FMPInfo is null - shared memory not properly mapped");
                    try
                    {

                    }
                    catch { }
                    return false;
                }

                //Console.WriteLine($"FMP initialized. Version: {info.Version}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + $"{ex.Message}");
            }

            return true;
        }
        
        static FMPWork GetFMPWork()
        {
            FMPInfo m_info = FMPControl.GetFMPInfo();
            FMPWork m_work = new FMPWork();

            m_work.Open(
                m_info,
                FMP.FMP7.AddOn.DriverType.FMP4 |
                FMP.FMP7.AddOn.DriverType.PMD |
                FMP.FMP7.AddOn.DriverType.MXDRV);
            
            return m_work;
        }

        static bool CheckPlaying(bool silent = false)
        {
            var gwork = GetFMPWork().GetGlobalWork();

            if ((gwork.Status & FMPStat.Play) == FMPStat.Play)
            {
                return true;
            }
            if (!silent)
                Console.WriteLine("ERROR: " + "Not playing.");
            return false;
        }


        static void Play(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("ERROR: " + $"File not found: {path}");
                return;
            }

            FMPControl.MusicLoad(path, FMPMusicLoadAction.LoadAndPlay);
            Console.WriteLine($"FMP7 Playing: {FMPControl.GetTextData(FMPText.Title)} / {FMPControl.GetTextData(FMPText.Creator)}");
        }

        static void Stop()
        {
            FMPControl.MusicStop();
            Console.WriteLine("FMP7 Stopped.");
        }
        
        static void Pause()
        {
            FMPControl.MusicPause();
            Console.WriteLine("FMP7 Paused.");
        }

        static void Fade(int fadeTime)
        {
            FMPControl.MusicFadeOut(fadeTime);
            Console.WriteLine("FMP7 Faded.");
        }

        static void GetExts()
        {
            string extList = FMPControl.GetTextData(FMPText.ExtList);  
            Console.WriteLine("Supported file extensions:");
            Console.WriteLine(extList);
        }
    }
}
