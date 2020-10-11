using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TListen
{
    class Program
    {
        private static string _fullname;
        private static string _progname;
        private static void Main(string[] args)
        {
            try
            {
                _fullname = System.Reflection.Assembly.GetEntryAssembly().Location;
                _progname = Path.GetFileNameWithoutExtension(_fullname);

                try
                {
                    var helpArgs = new HashSet<string>() { "--help", "-help", "/help", "/?" };
                    if (args.Any() && helpArgs.Contains(args[0]))
                    {
                        PrintUsageAndExit();
                    }

                    var ordinaryArgs = args.Where(x => x[0] != '-');
                    var optionArgs = args.Where(x => x[0] == '-').ToHashSet();
                    var regex = optionArgs.Any() && optionArgs.Contains("-e");
                    var excludeName = !(optionArgs.Any() && optionArgs.Contains("-x"));

                    var listener = new DebugListener(args, regex, excludeName);
                    listener.Listen();
                }
                catch (Exception e)
                {
                    const int ERROR_ALREADY_EXISTS = unchecked((int)0x800700b7);
                    if (e.HResult != ERROR_ALREADY_EXISTS)
                        throw;

                    Console.WriteLine("There's already an OutputDebugString Trace Listener running on the machine.");
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{_progname} Error: {ex.Message}");
            }
        }

        private static void PrintUsageAndExit()
        {
            Console.WriteLine($"{_progname}: A Very Simple Windows Trace Listener.");
            Console.WriteLine($"Usage:");
            Console.WriteLine($"   {_progname} (listen to all processes)");
            Console.WriteLine($"   {_progname} <pid1> [<pid2>]... listen to the processes specified by the given process IDs.");
            Console.WriteLine($"   {_progname} <name1> [<name2>]... listen to the processes specified by the given process names (do not include .exe)");
            Console.WriteLine($"   {_progname} -e <regex1> [<regex2>]... listen to the processes whose names match any of the regexes given.");
            Console.WriteLine($"   use -x to exclude the name of the process from the output.");
            Environment.Exit(-1);
        }
    }
}
