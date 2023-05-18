/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Mewt.Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace Mewt.Server.Controllers;

[ApiController]
[Route("/", Order = 2)]
public class MetricsController : BaseController<MetricsController>
{
    public MetricsController(ILogger<MetricsController> logger, IMewtServer mewt)
        : base(logger, mewt) { }

    [HttpGet("healthcheck")]
    [Produces("application/json")]
    public async ValueTask<IDictionary<string, HealthCheck>> HealthCheck()
    {
        var checks = await GetMewt().HealthCheck();
        Response.StatusCode = checks.Any(check => !check.Value.Success) ? 503 : 200;
        return checks;
    }

    [HttpGet("metrics")]
    [Produces("text/plain")]
    public async ValueTask<string> Metrics()
    {
        var customCulture = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
        customCulture.NumberFormat.NumberDecimalSeparator = ".";
        Thread.CurrentThread.CurrentCulture = customCulture;
        var prometheus = string.Empty;
        var metrics = await GetMewt().GetMetrics();
        prometheus = $$"""
        # HELP mewt_filesystem_files Mewt filesystem total files per path.
        # TYPE mewt_filesystem_files gauge
        mewt_filesystem_files{path="apis"} {{metrics["ApisFiles"]}}
        mewt_filesystem_files{path="assets"} {{metrics["AssetsFiles"]}}
        mewt_filesystem_files{path="contents"} {{metrics["ContentsFiles"]}}
        mewt_filesystem_files{path="metadata"} {{metrics["MetadataFiles"]}}
        mewt_filesystem_files{path="pages"} {{metrics["PagesFiles"]}}
        mewt_filesystem_files{path="private"} {{metrics["PrivateFiles"]}}
        mewt_filesystem_files{path="public"} {{metrics["PublicFiles"]}}
        mewt_filesystem_files{path="templates"} {{metrics["TemplatesFiles"]}}
        # HELP mewt_filesystem_size_bytes Mewt filesystem size in bytes per path.
        # TYPE mewt_filesystem_size_bytes gauge
        mewt_filesystem_size_bytes{path="apis"} {{metrics["ApisBytes"]}}
        mewt_filesystem_size_bytes{path="assets"} {{metrics["AssetsBytes"]}}
        mewt_filesystem_size_bytes{path="contents"} {{metrics["ContentsBytes"]}}
        mewt_filesystem_size_bytes{path="metadata"} {{metrics["MetadataBytes"]}}
        mewt_filesystem_size_bytes{path="pages"} {{metrics["PagesBytes"]}}
        mewt_filesystem_size_bytes{path="private"} {{metrics["PrivateBytes"]}}
        mewt_filesystem_size_bytes{path="public"} {{metrics["PublicBytes"]}}
        mewt_filesystem_size_bytes{path="templates"} {{metrics["TemplatesBytes"]}}
        # HELP process_cpu_seconds_total Total user and system CPU time spent in seconds.
        # TYPE process_cpu_seconds_total counter
        process_cpu_seconds_total {{metrics["TotalProcessorTimeSeconds"]}}
        # HELP process_resident_memory_bytes Resident memory size in bytes.
        # TYPE process_resident_memory_bytes gauge
        process_resident_memory_bytes {{metrics["ResidentMemoryBytes"]}}
        # HELP process_start_time_seconds Start time of the process since unix epoch in seconds.
        # TYPE process_start_time_seconds gauge
        process_start_time_seconds {{metrics["ProcessStartTimeSeconds"]}}
        # HELP process_virtual_memory_bytes Virtual memory size in bytes.
        # TYPE process_virtual_memory_bytes gauge
        process_virtual_memory_bytes {{metrics["VirtualMemoryBytes"]}}
        """;
        return prometheus.ToString();
    }
}
