using System;
using System.CommandLine;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;
using Eruru.CSharp.Api;
using Newtonsoft.Json.Linq;

namespace JetbrainsPluginVersionModifier
{
    internal class Program
    {
        static readonly CommandLineWriter CommandLineWriter = new();

        static async Task Main(string[] args)
        {
            var pathOrDirectoryOption = new Option<string>("--path", "路径或目录批量修改");
            var sinceVersion = new Option<string>("--sinceVersion", () => string.Empty);
            var isWebApiOption = new Option<bool>("--isWebApi", () => false, "WebApi方式打印结果");
            var isIndentOption = new Option<bool>("--isIndent", () => false, "缩进");

            var rootCommand = new RootCommand("修改Jetbrains插件版本号")
            {
                pathOrDirectoryOption,
                sinceVersion,
                isWebApiOption,
                isIndentOption
            };

            rootCommand.SetHandler(ModifyAsync, pathOrDirectoryOption, sinceVersion, isWebApiOption, isIndentOption);

            try
            {
                await rootCommand.InvokeAsync(args);
            }
            finally
            {
                CommandLineWriter.Dispose();
            }
        }

        static async Task ModifyAsync(string path, string sinceVersion, bool isWebApi, bool isIndent)
        {
            try
            {
                CommandLineWriter.IsWebApi = isWebApi;

                if (isIndent)
                    CommandLineWriter.Formatting = Newtonsoft.Json.Formatting.Indented;

                string[] paths;

                if (Directory.Exists(path))
                    paths = Directory.GetFiles(path, "*.jar", SearchOption.AllDirectories);
                else
                    paths = new[] { path };

                var jsonArray = new JArray();

                foreach (var jarPath in paths)
                {
                    var fileInfo = new FileInfo(jarPath);

                    string oldSinceVersion = "";
                    string newSinceVersion = "";
                    string originVersion = "";
                    string newName = "";

                    byte[] newJarBytes;

                    using (var originalStream = fileInfo.OpenRead())
                    using (var originalZip = new ZipArchive(originalStream, ZipArchiveMode.Read))
                    using (var memory = new MemoryStream())
                    {
                        var xmlDocument = new XmlDocument();

                        var pluginEntry = originalZip.GetEntry("META-INF/plugin.xml");

                        if (pluginEntry == null)
                            throw new FileNotFoundException("jar内未找到 META-INF/plugin.xml 文件");

                        using (var xmlStream = pluginEntry.Open())
                        using (var reader = new StreamReader(xmlStream))
                        {
                            var xmlText = await reader.ReadToEndAsync();
                            xmlDocument.LoadXml(xmlText);
                        }

                        var root = xmlDocument.DocumentElement;

                        var ideaVersionNode = root?.SelectSingleNode("idea-version");

                        if (ideaVersionNode == null)
                            throw new Exception("plugin.xml 内未找到 idea-version 节点");

                        var versionAttribute = ideaVersionNode.Attributes?["since-build"];
                        var untilBuildAttribute = ideaVersionNode.Attributes?["until-build"];

                        if (versionAttribute == null)
                            throw new Exception("idea-version 节点没有 since-build 属性");

                        if (untilBuildAttribute == null)
                        {
                            untilBuildAttribute = xmlDocument.CreateAttribute("until-build");
                            ideaVersionNode.Attributes.Append(untilBuildAttribute);
                        }

                        oldSinceVersion = versionAttribute.Value;

                        untilBuildAttribute.Value = "1012.2407.50";

                        if (!string.IsNullOrEmpty(sinceVersion))
                        {
                            newSinceVersion = sinceVersion;
                        }
                        else
                        {
                            var versions = versionAttribute.Value.Split('.');

                            for (var n = 1; n < versions.Length; n++)
                                versions[n] = "0";

                            newSinceVersion = string.Join('.', versions);
                        }

                        versionAttribute.Value = newSinceVersion;

                        var versionNode = root.SelectSingleNode("version");

                        if (versionNode != null)
                            originVersion = versionNode.InnerText;

                        var nameNode = root.SelectSingleNode("name");

                        if (nameNode != null)
                        {
                            newName = $"{nameNode.InnerText} [Origin: ideaIU {originVersion}]";
                            nameNode.InnerText = newName;
                        }

                        using (var newZip = new ZipArchive(memory, ZipArchiveMode.Create, true))
                        {
                            foreach (var entry in originalZip.Entries)
                            {
                                if (entry.FullName == "__index__")
                                    continue;

                                if (entry.FullName.Equals("META-INF/plugin.xml", StringComparison.OrdinalIgnoreCase))
                                    continue;

                                if (entry.FullName.StartsWith("META-INF/", StringComparison.OrdinalIgnoreCase) &&
                                   (entry.FullName.EndsWith(".SF", StringComparison.OrdinalIgnoreCase) ||
                                    entry.FullName.EndsWith(".RSA", StringComparison.OrdinalIgnoreCase) ||
                                    entry.FullName.EndsWith(".DSA", StringComparison.OrdinalIgnoreCase)))
                                    continue;

                                var newEntry = newZip.CreateEntry(entry.FullName, CompressionLevel.Optimal);

                                using var inStream = entry.Open();
                                using var outStream = newEntry.Open();

                                await inStream.CopyToAsync(outStream);
                            }

                            var newPluginEntry = newZip.CreateEntry("META-INF/plugin.xml", CompressionLevel.Optimal);

                            using var writer = new StreamWriter(newPluginEntry.Open());

                            xmlDocument.Save(writer);
                        }

                        newJarBytes = memory.ToArray();
                    }

                    await File.WriteAllBytesAsync(fileInfo.FullName, newJarBytes);

                    using var sha256 = SHA256.Create();
                    using var stream = fileInfo.OpenRead();

                    var hash = Convert.ToHexString(sha256.ComputeHash(stream)).ToLowerInvariant();

                    CommandLineWriter.WriteLine($"Since版本：{oldSinceVersion} 修改后：{newSinceVersion} Sha256：{hash} 已修改原文件：{fileInfo}");

                    jsonArray.Add(new JObject
                    {
                        { "sinceVersion", oldSinceVersion },
                        { "newSinceVersion", newSinceVersion },
                        { "sha256", hash },
                        { "newName", newName },
                        { "path", fileInfo.FullName }
                    });
                }

                CommandLineWriter.SetData(() => jsonArray);
            }
            catch (Exception exception)
            {
                CommandLineWriter.SetException(exception);
            }
        }
    }
}
