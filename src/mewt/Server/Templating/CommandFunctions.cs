/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using System.Diagnostics;
using Mewt.Server.Models;
using Scriban.Runtime;

namespace Mewt.Server.Templating;

public class CommandFunctions : ScriptObject
{
    public static async ValueTask<CommandResult?> Exec(string workingDirectory, string command, string? args = null, bool shell = false)
    {
        var startInfo = new ProcessStartInfo(command, args ?? string.Empty)
        {
            RedirectStandardError = !shell,
            RedirectStandardOutput = !shell,
            UseShellExecute = shell,
            WorkingDirectory = workingDirectory,
        };
        using (var process = Process.Start(startInfo))
        {
            if (process == null)
                return null;
            await process.WaitForExitAsync();
            return new CommandResult()
            {
                ExitCode = process.ExitCode,
                StandardError = shell ? string.Empty : await process.StandardError.ReadToEndAsync(),
                StandardOutput = shell ? string.Empty : await process.StandardOutput.ReadToEndAsync()
            };
        }
    }

    public static void Debug(string message)
    {
        Console.WriteLine(message);
    }
}