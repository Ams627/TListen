using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace TListen
{
    internal class DebugListener
    {
        private string[] args;
        private const int OdsBufferSize = 4096;
        private readonly bool _regex;
        private readonly bool _printName;

        public DebugListener(string[] args, bool regex = false, bool printName = true)
        {
            this.args = args;
            this._regex = regex;
            this._printName = printName;
        }

        internal void Listen()
        {
            var bufferReadyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "DBWIN_BUFFER_READY");
            var dataReadyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "DBWIN_DATA_READY");
            var buffer = MemoryMappedFile.CreateNew("DBWIN_BUFFER", OdsBufferSize);
            var bufferStream = buffer.CreateViewStream(0, OdsBufferSize);

            var pidBytes = new byte[4];
            var strBytes = new byte[OdsBufferSize - 4];

            // filter on anything at all:
            var filter = args.Any();

            // filter on processIds - if all the arguments are all-digit arguments then filter on process ID
            // otherwise filter on name:
            var filterOnPids = args.Any() && args.All(x => x.All(char.IsDigit));
            var pidList = filterOnPids ? args.Select(int.Parse).ToHashSet() : new HashSet<int>();
            var processNames = filterOnPids ? new HashSet<string>() : args.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var regexes = _regex && args.Any() ? args.Select(x => new Regex(x, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToArray() : new Regex[] { };

            string processName;

            while (true)
            {
                bufferReadyEvent.Set();
                var eventResult = dataReadyEvent.WaitOne();
                if (eventResult)
                {
                    bufferStream.Position = 0;
                    bufferStream.Read(pidBytes, 0, pidBytes.Length);
                    int pid = BitConverter.ToInt32(pidBytes, 0);
                    processName = Process.GetProcessById(pid).ProcessName;

                    // any arguments to the program means some kind of filter - either by name, PID or regex matches on the process name:
                    if (filter)
                    {
                        if (_regex)
                        {
                            
                            if (!regexes.Any(x=>x.Match(processName).Success))
                            {
                                continue;
                            }
                        }
                        else if (filterOnPids)
                        {
                            if (!pidList.Contains(pid))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (!processNames.Contains(processName))
                            {
                                continue;
                            }
                        }
                    }
                    bufferStream.Read(strBytes, 0, strBytes.Length);
                    string message = GetNullTerminatedString(strBytes).Trim();

                    if (string.IsNullOrWhiteSpace(message))
                    {
                        continue;
                    }

                    var nameToPrint = _printName ? $" {processName}" : "";
                    Console.WriteLine($"{pid}{nameToPrint} {message}");
                }
            }
        }

        private static string GetNullTerminatedString(byte[] bytes)
        {
            var chars = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0)
                {
                    if (i == 0)
                        return string.Empty;

                    return new string(chars, 0, i);
                }
                chars[i] = (char)bytes[i];
            }

            return Encoding.Default.GetString(bytes);
        }
    }
}