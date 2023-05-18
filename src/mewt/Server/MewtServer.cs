/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Mewt.Server.Apis;
using Mewt.Server.Configuration;
using Mewt.Server.Models;
using Mewt.Server.Paths;
using Mewt.Server.Templating;
using Microsoft.AspNetCore.StaticFiles;
using Scriban;
using Scriban.Runtime;
using System.Diagnostics;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mewt.Server;

public class MewtServer : IMewtServer
{
    private readonly ILogger<MewtServer> _logger;
    private readonly string _workingDirectory;
    private readonly FileExtensionContentTypeProvider _extensions = new FileExtensionContentTypeProvider();
    private readonly MewtPaths _paths;
    private readonly IMewtApiProvider _apiProvider;

    public MewtServer(IConfiguration configuration, ILogger<MewtServer> logger, string? workingDirectory = null)
    {
        _logger = logger;
        _workingDirectory = workingDirectory ?? Environment.CurrentDirectory;
        var basePath = configuration.GetValue<string>($"server:basePath") ?? _workingDirectory;
        logger.LogInformation($"Mewt working directory: `{basePath}`.");
        _paths = new MewtPaths()
        {
            Apis = BuildServerPath(ConfigureServerPath(configuration, PathUsage.Apis, Path.Combine("source", "apis"), basePath, _logger)),
            Assets = BuildServerPath(ConfigureServerPath(configuration, PathUsage.Assets, Path.Combine("source", "assets"), basePath, _logger)),
            Contents = BuildServerPath(ConfigureServerPath(configuration, PathUsage.Contents, Path.Combine("source", "contents"), basePath, _logger)),
            Metadata = BuildServerPath(ConfigureServerPath(configuration, PathUsage.Metadata, Path.Combine("cache", "metadata"), basePath, _logger)),
            Pages = BuildServerPath(ConfigureServerPath(configuration, PathUsage.Pages, Path.Combine("source", "pages"), basePath, _logger)),
            Private = BuildServerPath(ConfigureServerPath(configuration, PathUsage.Private, Path.Combine("cache", "private"), basePath, _logger)),
            Public = BuildServerPath(ConfigureServerPath(configuration, PathUsage.Public, Path.Combine("cache", "public"), basePath, _logger)),
            Templates = BuildServerPath(ConfigureServerPath(configuration, PathUsage.Templates, Path.Combine("source", "templates"), basePath, _logger)),
        };
        _apiProvider = new MewtApiProvider(_paths.Private);
    }

    public MewtServer(MewtPaths paths, IMewtApiProvider apiProvider, ILogger<MewtServer> logger, string workingDirectory)
    {
        _paths = paths;
        _apiProvider = apiProvider;
        _logger = logger;
        _workingDirectory = workingDirectory;
    }

    private static ServerPathConfiguration ConfigureServerPath(IConfiguration configuration, PathUsage usage, string defaultPath, string basePath, ILogger<MewtServer> logger)
    {
        var configKey = usage.ToString();
        var provider = configuration.GetValue<FileSystemProvider>($"server:paths:{configKey}:provider", FileSystemProvider.Local);
        var path = configuration.GetValue<string>($"server:paths:{configKey}:path") ?? (provider == FileSystemProvider.Local ? defaultPath : string.Empty);
        if (provider == FileSystemProvider.Local && !Path.IsPathRooted(path))
            path = Path.Combine(basePath, path);
        var options = new Dictionary<string, string>();
        foreach (var option in configuration.GetSection($"server:paths:{configKey}:options").AsEnumerable())
            if (option.Value != null)
                options[option.Key.ToLowerInvariant()] = option.Value;
        var pathConfig = new ServerPathConfiguration()
        {
            Path = path,
            UpdateCommand = configuration.GetValue<string?>($"server:paths:{configKey}:updateCommand", null),
            Usage = usage,
            Hash = configuration.GetValue<FileHashAlgorithm>($"server:paths:{configKey}:hash", FileHashAlgorithm.SHA256),
            Provider = provider,
            Options = options,
        };
        logger.LogInformation($"Configured `{configKey}` path: {pathConfig.Path} [{pathConfig.Provider}]");
        return pathConfig;
    }

    private static IMewtPath BuildServerPath(ServerPathConfiguration pathConfiguration)
    {
        switch (pathConfiguration.Provider)
        {
            case FileSystemProvider.Local:
                return new MewtLocalFileSystemPath()
                {
                    Config = pathConfiguration
                };
            case FileSystemProvider.Memory:
                return new MewtMemoryPath(pathConfiguration);
            case FileSystemProvider.Rclone:
                return new MewtRclonePath(pathConfiguration);
        }
        throw new ArgumentException($"Provider must be one of: `Local`, `Memory`, `Rclone`; but `{pathConfiguration.Provider}` given.", "pathConfiguration.Provider");
    }

    public async ValueTask<ServerApiInfo> CheckApi(ApiType type, string relativePath)
    {
        var apiRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        if (type == ApiType.Proxy)
        {
            var sourceRelativePath = Path.Combine("proxy", apiRelativePath + ".yml");
            var privateRelativePath = Path.Combine("api", sourceRelativePath);
            var exists = await _paths.Private.DoesFileExist(privateRelativePath);
            if (!exists)
            {
                if (await _paths.Apis.DoesFileExist(sourceRelativePath = Path.Combine("proxy", apiRelativePath + ".yml")))
                    return new ServerApiProxyInfo() { PrivateFile = privateRelativePath, Exists = false, ApiProxyFile = sourceRelativePath };
                else
                    return new ServerApiInfo(ApiType.NotFound) { PrivateFile = privateRelativePath, Exists = false };
            }
            return new ServerApiProxyInfo() { PrivateFile = privateRelativePath, Exists = true, ApiProxyFile = sourceRelativePath };
        }
        return new ServerApiInfo(ApiType.Unknown) { PrivateFile = apiRelativePath, Exists = false };
    }

    public async ValueTask<ServerFileInfo> CheckFile(string relativePath)
    {
        var publicRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var exists = await _paths.Public.DoesFileExist(publicRelativePath);
        var sourceRelativePath = string.Empty;
        if (!exists)
        {
            if (await _paths.Pages.DoesFileExist(sourceRelativePath = publicRelativePath + ".yml"))
                return new ServerPageInfo() { PublicFile = publicRelativePath, Exists = false, PageDescriptor = sourceRelativePath };
            else if (await _paths.Assets.DoesFileExist(sourceRelativePath = publicRelativePath))
                return new ServerAssetInfo() { PublicFile = publicRelativePath, Exists = false, Asset = sourceRelativePath };
            else
                return new ServerFileInfo(FileType.NotFound) { PublicFile = publicRelativePath, Exists = false };
        }
        return new ServerFileInfo(FileType.Unknown) { PublicFile = publicRelativePath, Exists = true };
    }

    public async ValueTask<bool> CopyApi(ServerApiInfo api)
    {
        ServerApiProxyInfo? apiProxyInfo;
        if (api.Type != ApiType.Proxy || (apiProxyInfo = api as ServerApiProxyInfo) == null)
        {
            _logger.LogWarning($"`{api.PrivateFile}` is not a valid api name.");
            return false;
        }
        try
        {
            await _paths.Private.CopyFileFrom(apiProxyInfo.ApiProxyFile, _paths.Apis, apiProxyInfo.PrivateFile);
            _ = Task.Run(async () => WriteMetadata(await ComputeMetadata(apiProxyInfo)));
            return true;
        }
        catch (Exception e) { _logger.LogError($"Cannot copy api `{apiProxyInfo.ApiProxyFile}`: {e.Message}"); }
        return false;
    }

    public async ValueTask<bool> CopyAsset(ServerFileInfo asset)
    {
        ServerAssetInfo? assetInfo;
        if (asset.Type != FileType.Asset || (assetInfo = asset as ServerAssetInfo) == null)
        {
            _logger.LogWarning($"`{asset.PublicFile}` is not a valid asset name.");
            return false;
        }
        try
        {
            await _paths.Public.CopyFileFrom(assetInfo.Asset, _paths.Assets);
            _ = Task.Run(async () => WriteMetadata(await ComputeMetadata(assetInfo)));
            return true;
        }
        catch (Exception e) { _logger.LogError($"Cannot copy asset `{assetInfo.Asset}`: {e.Message}"); }
        return false;
    }

    public async ValueTask<bool> GeneratePage(ServerFileInfo page)
    {
        ServerPageInfo? pageInfo;
        if (page.Type != FileType.Page || (pageInfo = page as ServerPageInfo) == null)
        {
            _logger.LogWarning($"`{page.PublicFile}` is not a valid page name.");
            return false;
        }
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .Build();
            var pd = deserializer.Deserialize<PageDescriptor>(await _paths.Pages.ReadFile(pageInfo.PageDescriptor));

            var context = new TemplateContext();
            string absolutePath = string.Empty;
            if (pd.ContentFiles != null)
            {
                foreach (var file in pd.ContentFiles)
                {
                    if (await _paths.Contents.DoesFileExist(file))
                    {
                        var content = deserializer.Deserialize<IDictionary<string, object>>(await _paths.Contents.ReadFile(file));
                        var script = new ScriptObject();
                        script.Import(content);
                        context.PushGlobal(script);
                    }
                    else
                    {
                        _logger.LogWarning($"Cannot find content file `{file}` for page `{pageInfo.PageDescriptor}`.");
                    }
                }
            }
            if (pd.Content != null)
            {
                var script = new ScriptObject();
                script.Import(pd.Content);
                context.PushGlobal(script);
            }
            var templateText = new StringBuilder();
            foreach (var file in pd.TemplateFiles)
            {
                if (await _paths.Templates.DoesFileExist(file))
                    templateText.AppendLine(await _paths.Templates.ReadFile(file));
                else
                    _logger.LogWarning($"Cannot find template file `{file}` for page `{pageInfo.PageDescriptor}`.");
            }
            context.TemplateLoader = new TemplateLoader()
            {
                AvailableTemplates = pd.TemplateIncludes,
                TemplatesPath = _paths.Templates
            };
            var template = Template.Parse(templateText.ToString());
            var result = await template.RenderAsync(context);
            await _paths.Public.WriteFile(pageInfo.PublicFile, result);
            _ = Task.Run(async () => WriteMetadata(await ComputeMetadata(pageInfo, pd.ContentFiles, pd.TemplateFiles.Concat(pd.TemplateIncludes ?? new List<string>().Distinct()))));
            return true;
        }
        catch (Exception e) { _logger.LogError($"Cannot generate page `{pageInfo.PageDescriptor}`: {e.Message}"); }
        return false;
    }

    public string ComputeFileContentType(ServerFileInfo file)
    {
        if (!_extensions.TryGetContentType(file.PublicFile, out string? contentType) || contentType == null)
        {
            contentType = "application/octet-stream";
            _logger.LogWarning($"Cannot compute Content-Type for file: `{file.PublicFile}`.");
        }
        return contentType;
    }

    public async ValueTask<(ApiStatus status, object? body)> ExecuteApi(ServerApiInfo apiInfo, HttpRequest request, HttpResponse response)
    {
        try
        {
            var api = await _apiProvider.GetApi(apiInfo);
            if (api == null)
            {
                _logger.LogWarning($"Cannot get API for: `{apiInfo.PrivateFile}`.");
                return (ApiStatus.Error, null);
            }
            var status = await api.Execute(request, response);
            if (status.exception != null)
            {
                _logger.LogWarning($"Exception thrown when executing API for `{apiInfo.PrivateFile}` with status `{status.status}`: {status.exception.Message}");
            }
            return (status.status, status.body);
        }
        catch (Exception e) { _logger.LogError($"Error when getting API for `{apiInfo.PrivateFile}`: {e.Message}"); }
        return (ApiStatus.Error, null);
    }

    public async ValueTask<(bool isPhysical, string? absolutePath, string? content)> GetPublicFile(ServerFileInfo file)
    {
        if (_paths.Public.Config.Provider == FileSystemProvider.Local)
            return (isPhysical: true, absolutePath: Path.Combine(_paths.Public.Config.Path, file.PublicFile), content: null);
        if (_paths.Public.Config.Provider == FileSystemProvider.Memory && file.Type == FileType.Asset && _paths.Assets.Config.Provider == FileSystemProvider.Local)
            return (isPhysical: true, absolutePath: Path.Combine(_paths.Assets.Config.Path, file.PublicFile), content: null);
        return (isPhysical: false, absolutePath: null, content: await _paths.Public.ReadFile(file.PublicFile));
    }

    private async ValueTask<FileMetadata?> ComputeMetadata(ServerApiProxyInfo apiProxy)
    {
        if (apiProxy.Type != ApiType.Proxy)
            return null;
        try
        {
            return new FileMetadata()
            {
                Private = await _paths.Private.ComputeFileVersion(apiProxy.PrivateFile),
                Type = FileType.API,
                Api = await _paths.Apis.ComputeFileVersion(apiProxy.ApiProxyFile),
            };
        }
        catch (Exception e) { _logger.LogError($"Cannot compute metadata for file `{apiProxy.PrivateFile}`: {e.Message}"); }
        return null;
    }

    private async ValueTask<FileMetadata?> ComputeMetadata(ServerAssetInfo asset)
    {
        if (_paths.Public.Config.Provider == FileSystemProvider.Memory)
            return null; // Don't generate metadata for assets in case of Memory public path because they are not copied in memory
        if (asset.Type != FileType.Asset)
            return null;
        try
        {
            return new FileMetadata()
            {
                Public = await _paths.Public.ComputeFileVersion(asset.PublicFile),
                Type = FileType.Asset,
                Asset = await _paths.Assets.ComputeFileVersion(asset.Asset),
            };
        }
        catch (Exception e) { _logger.LogError($"Cannot compute metadata for file `{asset.PublicFile}`: {e.Message}"); }
        return null;
    }

    private async ValueTask<FileMetadata?> ComputeMetadata(ServerPageInfo page, IEnumerable<string>? contents, IEnumerable<string>? templates)
    {
        if (page.Type != FileType.Page)
            return null;
        if (templates == null || !templates.Any())
            return null;
        try
        {
            var metadata = new FileMetadata()
            {
                Public = await _paths.Public.ComputeFileVersion(page.PublicFile),
                Type = FileType.Page,
                Page = await _paths.Pages.ComputeFileVersion(page.PageDescriptor),
            };
            if (contents != null && contents.Any())
            {
                metadata.Contents = new List<FileVersion>();
                foreach (var content in contents)
                {
                    metadata.Contents.Add(await _paths.Contents.ComputeFileVersion(content));
                }
            }
            metadata.Templates = new List<FileVersion>();
            foreach (var template in templates)
            {
                metadata.Templates.Add(await _paths.Templates.ComputeFileVersion(template));
            }
            return metadata;
        }
        catch (Exception e) { _logger.LogError($"Cannot compute metadata for file `{page.PublicFile}`: {e.Message}"); }
        return null;
    }

    private async ValueTask<bool> WriteMetadata(FileMetadata? metadata)
    {
        if (metadata == null)
            return false;
        try
        {
            var filename = (metadata.Public?.Name ?? metadata.Private?.Name) + ".yml";
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .Build();
            var yaml = serializer.Serialize(metadata);
            await _paths.Metadata.WriteFile(filename, yaml);
            return true;
        }
        catch (Exception e) { _logger.LogError($"Cannot generate metadata for file `{(metadata.Public?.Name ?? metadata.Private?.Name)}`: {e.Message}"); }
        return false;
    }

    private async ValueTask<FileStatus> GetGeneratedFileStatus(FileMetadata metadata, bool hash)
    {
        try
        {
            if (metadata.Type == FileType.API)
            {
                if (metadata.Api == null || metadata.Private == null)
                    return FileStatus.MetadataCorrupted;
                if (!await _paths.Private.DoesFileExist(metadata.Private.Name))
                    return FileStatus.Removed;
                if (!await _paths.Apis.DoesFileExist(metadata.Api.Name))
                    return FileStatus.ApiRemoved;
                if (!await _paths.Apis.CompareFileVersion(metadata.Api, hash))
                    return FileStatus.ApiModified;
            }
            if (metadata.Type == FileType.Asset)
            {
                if (metadata.Asset == null || metadata.Public == null)
                    return FileStatus.MetadataCorrupted;
                if (!await _paths.Public.DoesFileExist(metadata.Public.Name))
                    return FileStatus.Removed;
                if (!await _paths.Assets.DoesFileExist(metadata.Asset.Name))
                    return FileStatus.AssetRemoved;
                if (!await _paths.Assets.CompareFileVersion(metadata.Asset, hash))
                    return FileStatus.AssetModified;
            }
            if (metadata.Type == FileType.Page)
            {
                if (metadata.Page == null || metadata.Public == null || metadata.Templates == null || !metadata.Templates.Any())
                    return FileStatus.MetadataCorrupted;
                if (!await _paths.Public.DoesFileExist(metadata.Public.Name))
                    return FileStatus.Removed;
                if (!await _paths.Public.CompareFileVersion(metadata.Public, hash))
                    return FileStatus.Modified;
                if (!await _paths.Pages.DoesFileExist(metadata.Page.Name))
                    return FileStatus.PageRemoved;
                if (!await _paths.Pages.CompareFileVersion(metadata.Page, hash))
                    return FileStatus.PageModified;
                if (metadata.Contents != null)
                {
                    foreach (var content in metadata.Contents)
                    {
                        if (!await _paths.Contents.DoesFileExist(content.Name))
                            return FileStatus.ContentRemoved;
                        if (!await _paths.Contents.CompareFileVersion(content, hash))
                            return FileStatus.ContentModified;
                    }
                }
                foreach (var template in metadata.Templates)
                {
                    if (!await _paths.Templates.DoesFileExist(template.Name))
                        return FileStatus.TemplateRemoved;
                    if (!await _paths.Templates.CompareFileVersion(template, hash))
                        return FileStatus.TemplateModified;
                }
            }
            return FileStatus.UpToDate;
        }
        catch (Exception e) { _logger.LogError($"Cannot check status for metadata `{(metadata.Public?.Name ?? metadata.Private?.Name)}`: {e.Message}"); }
        return FileStatus.Error;
    }

    public async ValueTask<IEnumerable<FileStatusInfo>> ListGeneratedFilesStatus(bool hash)
    {
        var generated = new List<FileStatusInfo>();
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        try
        {
            // Check files from their metadata
            foreach (var file in await _paths.Metadata.ListFiles("*.yml"))
            {
                try
                {
                    var metadata = deserializer.Deserialize<FileMetadata>(await _paths.Metadata.ReadFile(file));
                    if (metadata.Public != null)
                        generated.Add(new FileStatusInfo(metadata.Type, metadata.Public.Name, await GetGeneratedFileStatus(metadata, hash)));
                    if (metadata.Private != null)
                        generated.Add(new FileStatusInfo(metadata.Type, metadata.Private.Name, await GetGeneratedFileStatus(metadata, hash)));
                }
                catch (Exception e)
                {
                    _logger.LogError($"Cannot read metadata file `{file}`: {e.Message}");
                    generated.Add(new FileStatusInfo(FileType.Unknown, file, FileStatus.MetadataCorrupted));
                }
            }
            // Check orphan files, without metadata
            var orphans = new List<(FileType type, string file)>();
            foreach (var file in await _paths.Private.ListFiles("*"))
                if (!generated.Any(fsi => fsi.Name == file && (fsi.Type == FileType.API || fsi.Type == FileType.Private)))
                    generated.Add(new FileStatusInfo(FileType.Private, file, FileStatus.Orphan));
            foreach (var file in await _paths.Public.ListFiles("*"))
                if (!generated.Any(fsi => fsi.Name == file && (fsi.Type == FileType.Asset || fsi.Type == FileType.Page || fsi.Type == FileType.Public)))
                    generated.Add(new FileStatusInfo(FileType.Public, file, FileStatus.Orphan));
        }
        catch (Exception e) { _logger.LogError($"Cannot list metadata files: {e.Message}"); }
        return generated;
    }

    public async ValueTask<bool> DeleteFile(FileType type, string file)
    {
        if (type == FileType.Unknown || type == FileType.NotFound)
            return false;
        try
        {
            if ((type == FileType.API || type == FileType.Private) && await _paths.Private.DoesFileExist(file))
                await _paths.Private.DeleteFile(file);
            if ((type == FileType.Asset || type == FileType.Page || type == FileType.Public) && await _paths.Public.DoesFileExist(file))
                await _paths.Public.DeleteFile(file);
            if (await _paths.Metadata.DoesFileExist(file + ".yml"))
                await _paths.Metadata.DeleteFile(file + ".yml");
            return true;
        }
        catch (Exception e) { _logger.LogError($"Cannot cannot delete {type} file `{file}`: {e.Message}"); }
        return false;
    }

    public async ValueTask<IEnumerable<string>> DeleteAll()
    {
        var errors = new List<string>();
        var metadataErrors = await _paths.Metadata.DeleteAll();
        var privateErrors = await _paths.Private.DeleteAll();
        var publicErrors = await _paths.Public.DeleteAll();
        _apiProvider.Clear();
        errors.AddRange(metadataErrors.Select<(string file, Exception exception), string>((error) => error.file));
        errors.AddRange(privateErrors.Select<(string file, Exception exception), string>((error) => error.file));
        errors.AddRange(publicErrors.Select<(string file, Exception exception), string>((error) => error.file));
        return errors;
    }

    public async ValueTask<IDictionary<string, HealthCheck>> HealthCheck()
    {
        var isMetadataPathWriteable = await _paths.Metadata.IsPathWritable();
        var isPrivatePathWriteable = await _paths.Private.IsPathWritable();
        var isPublicPathWriteable = await _paths.Public.IsPathWritable();
        return new Dictionary<string, HealthCheck>(3)
        {
            { "IsMetadataPathWriteable", new HealthCheck(isMetadataPathWriteable.writable, isMetadataPathWriteable.exception?.ToString()) },
            { "IsPrivatePathWriteable", new HealthCheck(isPrivatePathWriteable.writable, isPrivatePathWriteable.exception?.ToString()) },
            { "IsPublicPathWriteable", new HealthCheck(isPublicPathWriteable.writable, isPublicPathWriteable.exception?.ToString()) },
        };
    }

    public async ValueTask<IDictionary<string, object>> GetMetrics()
    {
        IDictionary<string, object> metrics = new Dictionary<string, object>();
        // Files
        foreach (var path in _paths.Values)
        {
            var pathMetrics = await path.GetMetrics("*");
            metrics.Add(path.Config.Usage + "Files", pathMetrics.files);
            metrics.Add(path.Config.Usage + "Bytes", pathMetrics.size);
        }
        // Resources
        var process = Process.GetCurrentProcess();
        metrics.Add("ResidentMemoryBytes", process.WorkingSet64);
        metrics.Add("VirtualMemoryBytes", process.VirtualMemorySize64);
        try
        {
            metrics.Add("TotalProcessorTimeSeconds", process.TotalProcessorTime.TotalSeconds);
            metrics.Add("ProcessStartTimeSeconds", ((DateTimeOffset)process.StartTime.ToUniversalTime()).ToUnixTimeSeconds());
        }
        catch (Exception e) { _logger.LogWarning($"Cannot get `*Process*` metrics: {e.Message}"); }
        return metrics;
    }

    public async ValueTask<string> UpdatePath(PathUsage path, HttpRequest request, HttpResponse response)
    {
        try
        {
            var commandTemplate = _paths[path].Config.UpdateCommand;
            if (commandTemplate == null)
            {
                response.StatusCode = StatusCodes.Status501NotImplemented;
                return $"server:paths:{path.ToString().ToLowerInvariant()}:updateCommand is not defined.";
            }
            var command = Template.Parse(String.Concat("{{ ", commandTemplate, " }}"));
            if (command.HasErrors)
            {
                response.StatusCode = StatusCodes.Status500InternalServerError;
                var message = new StringBuilder("Update command template contains errors:");
                message.AppendLine();
                foreach (var error in command.Messages)
                    message.AppendFormat("  - Span: `{0}`, Type: `{1}`, Message: `{2}`.", error.Span, error.Type, error.Message).AppendLine();
                return message.ToString();
            }
            var context = new TemplateContext();
            context.PushGlobal(new ApiBuiltinFunctions());
            var data = new ScriptObject();
            data.SetValue("path", _paths[path], true);
            data.SetValue("pwd", Directory.Exists(_paths[path].Config.Path) ? _paths[path].Config.Path : _workingDirectory, true);
            data.SetValue("request", request, true);
            data.SetValue("response", response, true);
            context.PushGlobal(data);
            return await command.EvaluateAsync(context) as string ?? string.Empty;
        }
        catch (Exception e)
        {
            response.StatusCode = StatusCodes.Status500InternalServerError;
            return e.Message;
        }
    }
}