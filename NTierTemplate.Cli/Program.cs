using System.CommandLine;

namespace NTierTemplate.Cli;

/// <summary>
/// Entry point for the NTierTemplate CLI tool.
/// </summary>
public class Program
{
    /// <summary>
    /// Parse command-line arguments and invoke the appropriate subcommand.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the program.</param>
    /// <returns>The exit code (0 for success, non-zero for failure).</returns>
    public static int Main(string[] args)
    {
        var rootCommand = new RootCommand("NTierTemplate command-line tools.");
        rootCommand.Subcommands.Add(new UserCreateAdminCommand().Build());

        return rootCommand.Parse(args).Invoke();
    }
}
