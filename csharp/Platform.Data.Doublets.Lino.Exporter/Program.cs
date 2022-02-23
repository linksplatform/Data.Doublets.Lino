using TLinkAddress = System.UInt64;

namespace Platform.Data.Doublets.Lino.Exporter;

internal static class Program
{
    /// <summary>
    /// <para>
    /// Main the args.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <param name="args">
    /// <para>The args.</para>
    /// <para></para>
    /// </param>
    private static void Main(params string?[] args)
    {
        new LinoExporterCli<TLinkAddress>().Run(args);
    }
}
