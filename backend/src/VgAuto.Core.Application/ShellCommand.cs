using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VgAuto.Core.Application
{
    /// <summary>Runs an external program without a shell. Arguments are passed as a list, never concatenated.</summary>
    public class ShellCommand
    {
        public async Task<string> Run(string program, IEnumerable<string> arguments, IDictionary<string, string> environment = null)
        {
            var startInfo = new ProcessStartInfo(program)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
            if (environment != null)
            {
                foreach (var (key, value) in environment) startInfo.Environment[key] = value;
            }

            using var p = new Process { StartInfo = startInfo };
            p.Start();

            var standardOutputReader = ConsumeReader(p.StandardOutput);
            var standardErrorReader = ConsumeReader(p.StandardError);

            await p.WaitForExitAsync();

            var outputString = await standardOutputReader;
            var errorString = await standardErrorReader;
            if (p.ExitCode == 0)
            {
                return outputString;
            }
            throw new Exception($"{Path.GetFileName(program)} exited with code {p.ExitCode}: {errorString}");
        }

        static async Task<string> ConsumeReader(TextReader reader)
        {
            var strings = new StringBuilder();
            string text;
            while ((text = await reader.ReadLineAsync()) != null)
            {
                strings.AppendLine(text);
            }
            return strings.ToString();
        }
    }
}
