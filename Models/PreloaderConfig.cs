using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using ReactiveUI;

namespace PreloaderConfigurator.Models;

/// <summary>
/// Represents a process item with a name and a flag indicating whether it is allowed.
/// </summary>
public class ProcessItem(string name, bool isAllowed) : ReactiveObject
{
    private string _name = name;
    private bool _isAllowed = isAllowed;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public bool IsAllowed
    {
        get => _isAllowed;
        set => this.RaiseAndSetIfChanged(ref _isAllowed, value);
    }
}

/// <summary>
/// Represents the configuration model for a preloader system, providing properties
/// and behaviors related to the loading process, including library imports, exception handling,
/// timing configurations, and associated processes.
/// </summary>
public class PreloaderConfig : ReactiveObject
{
    private string _loadMethod = "ImportAddressHook";
    private string _originalLibrary = string.Empty;
    private string _importLibrary = "MSVCR110.dll";
    private string _importFunction = "_initterm_e";
    private string _threadNumber = "2";
    private bool _installExceptionHandler = true;
    private bool _keepExceptionHandler;
    private int _loadDelay;
    private int _hookDelay;
    // ReSharper disable once UseCollectionExpression
    private ObservableCollection<ProcessItem> _processes = new()
    {
        new ProcessItem("Fallout3.exe", false),
        new ProcessItem("FalloutNV.exe", false),
        new ProcessItem("Fallout4.exe", true),
        new ProcessItem("Fallout4VR.exe", true),
        new ProcessItem("Morrowind.exe", false),
        new ProcessItem("Oblivion.exe", false),
        new ProcessItem("TESV.exe", true),
        new ProcessItem("SkyrimSE.exe", true),
        new ProcessItem("SkyrimVR.exe", true),
        new ProcessItem("TESConstructionSet.exe", false),
        new ProcessItem("CreationKit.exe", false)
    };
    private bool _isLoaded;

    public bool IsLoaded
    {
        get => _isLoaded;
        private set => this.RaiseAndSetIfChanged(ref _isLoaded, value);
    }

    public string LoadMethod
    {
        get => _loadMethod;
        set => this.RaiseAndSetIfChanged(ref _loadMethod, value);
    }

    public string OriginalLibrary
    {
        get => _originalLibrary;
        set => this.RaiseAndSetIfChanged(ref _originalLibrary, value);
    }

    public string ImportLibrary
    {
        get => _importLibrary;
        set => this.RaiseAndSetIfChanged(ref _importLibrary, value);
    }

    public string ImportFunction
    {
        get => _importFunction;
        set => this.RaiseAndSetIfChanged(ref _importFunction, value);
    }

    public string ThreadNumber
    {
        get => _threadNumber;
        set => this.RaiseAndSetIfChanged(ref _threadNumber, value);
    }

    public bool InstallExceptionHandler
    {
        get => _installExceptionHandler;
        set => this.RaiseAndSetIfChanged(ref _installExceptionHandler, value);
    }

    public bool KeepExceptionHandler
    {
        get => _keepExceptionHandler;
        set => this.RaiseAndSetIfChanged(ref _keepExceptionHandler, value);
    }

    public int LoadDelay
    {
        get => _loadDelay;
        set => this.RaiseAndSetIfChanged(ref _loadDelay, value);
    }

    public int HookDelay
    {
        get => _hookDelay;
        set => this.RaiseAndSetIfChanged(ref _hookDelay, value);
    }

    public ObservableCollection<ProcessItem> Processes
    {
        get => _processes;
        // ReSharper disable once UnusedMember.Local
        private set => this.RaiseAndSetIfChanged(ref _processes, value);
    }

    /// Parses the provided XML content and creates a new instance of PreloaderConfig with the data
    /// extracted from the XML. The method reads various configuration values from the XML and
    /// populates the corresponding properties of the PreloaderConfig instance.
    /// <param name="xmlContent">
    /// A string containing the XML content to be parsed. The XML must have a root element
    /// with a structure defined for the "PluginPreloader" configuration.
    /// </param>
    /// <returns>
    /// A new instance of PreloaderConfig populated with the configuration data from the XML. If the
    /// XML does not contain valid or expected elements, an instance with default values will be returned.
    /// </returns>
    public static PreloaderConfig FromXml(string xmlContent)
    {
        var config = new PreloaderConfig();
            
        var doc = XDocument.Parse(xmlContent);
        var root = doc.Root?.Element("PluginPreloader");

        if (root == null) return config;

        config.OriginalLibrary = root.Element("OriginalLibrary")?.Value ?? string.Empty;
        config.LoadMethod = root.Element("LoadMethod")?.Attribute("Name")?.Value ?? "ImportAddressHook";

        var importHook = root.Element("LoadMethod")?.Element("ImportAddressHook");
        if (importHook != null)
        {
            config.ImportLibrary = importHook.Element("LibraryName")?.Value ?? "MSVCR110.dll";
            config.ImportFunction = importHook.Element("FunctionName")?.Value ?? "_initterm_e";
        }

        var threadAttach = root.Element("LoadMethod")?.Element("OnThreadAttach");
        if (threadAttach != null)
        {
            config.ThreadNumber = threadAttach.Element("ThreadNumber")?.Value ?? "2";
        }

        config.InstallExceptionHandler = bool.Parse(root.Element("InstallExceptionHandler")?.Value ?? "true");
        config.KeepExceptionHandler = bool.Parse(root.Element("KeepExceptionHandler")?.Value ?? "false");
        config.LoadDelay = int.Parse(root.Element("LoadDelay")?.Value ?? "0");
        config.HookDelay = int.Parse(root.Element("HookDelay")?.Value ?? "0");

        var processesElement = root.Element("Processes");
        if (processesElement != null)
        {
            foreach (var process in config.Processes)
            {
                var item = processesElement.Elements("Item")
                    .FirstOrDefault(x => x.Attribute("Name")?.Value.Equals(process.Name, StringComparison.OrdinalIgnoreCase) == true);
                if (item != null)
                {
                    process.IsAllowed = bool.Parse(item.Attribute("Allow")?.Value ?? "false");
                }
            }
        }

        config.IsLoaded = true;
        return config;
    }

    /// Converts the current PreloaderConfig instance into an XML representation.
    /// The method serializes the properties of the PreloaderConfig object into an XML format
    /// that follows the structure defined for the "PluginPreloader" configuration.
    /// <returns>
    /// A string containing the serialized XML representation of the PreloaderConfig instance.
    /// If the serialization fails, an empty string or a default XML structure may be returned.
    /// </returns>
    public string SaveToXml()
    {
        try 
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace,
                Encoding = new System.Text.UTF8Encoding(true),
                OmitXmlDeclaration = false
            };

            var doc = new XDocument(
                new XElement("xSE",
                    new XElement("PluginPreloader",
                        new XComment(PreloaderDocs.OriginalLibraryComment),
                        new XElement("OriginalLibrary", OriginalLibrary),
                        new XComment(PreloaderDocs.LoadMethodComment),
                        new XElement("LoadMethod", 
                            new XAttribute("Name", LoadMethod),
                            new XComment(PreloaderDocs.ImportAddressHookComment),
                            new XElement("ImportAddressHook",
                                new XElement("LibraryName", ImportLibrary),
                                new XElement("FunctionName", ImportFunction)
                            ),
                            new XComment(PreloaderDocs.OnThreadAttachComment),
                            new XElement("OnThreadAttach",
                                new XElement("ThreadNumber", ThreadNumber)
                            ),
                            new XComment(PreloaderDocs.OnProcessAttachComment),
                            new XElement("OnProcessAttach")
                        ),
                        new XComment(PreloaderDocs.InstallExceptionHandlerComment + Environment.NewLine + 
                                     Environment.NewLine + PreloaderDocs.KeepExceptionHandlerComment),
                        new XElement("InstallExceptionHandler", InstallExceptionHandler.ToString().ToLower()),
                        new XElement("KeepExceptionHandler", KeepExceptionHandler.ToString().ToLower()),
                        new XComment(PreloaderDocs.LoadDelayComment + Environment.NewLine + 
                                     Environment.NewLine + PreloaderDocs.HookDelayComment),
                        new XElement("LoadDelay", LoadDelay),
                        new XElement("HookDelay", HookDelay),
                        new XComment(PreloaderDocs.ProcessesComment),
                        new XElement("Processes",
                            from process in Processes
                            select new XElement("Item",
                                new XAttribute("Name", process.Name),
                                new XAttribute("Allow", process.IsAllowed.ToString().ToLower())
                            )
                        )
                    )
                )
            );

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);
            doc.Save(xmlWriter);
            xmlWriter.Flush();
            return $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n{stringWriter.ToString().Substring(stringWriter.ToString().IndexOf('\n') + 1)}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving XML: {ex}");
            throw;
        }
    }
}