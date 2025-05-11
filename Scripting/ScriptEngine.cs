using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TheAdventure.Scripting;

public class ScriptEngine
{
    private PortableExecutableReference[] _scriptReferences;
    private Dictionary<string, IScript> _scripts = new Dictionary<string, IScript>();
    private FileSystemWatcher? _watcher;

    public ScriptEngine()
    {
        var rtPath = Path.GetDirectoryName(typeof(object).Assembly.Location) +
                     Path.DirectorySeparatorChar;
        var references = new string[]
        {
            #region .Net SDK

            rtPath + "System.Private.CoreLib.dll",
            rtPath + "System.Runtime.dll",
            rtPath + "System.Console.dll",
            rtPath + "netstandard.dll",
            rtPath + "System.Text.RegularExpressions.dll", // IMPORTANT!
            rtPath + "System.Linq.dll",
            rtPath + "System.Linq.Expressions.dll", // IMPORTANT!
            rtPath + "System.IO.dll",
            rtPath + "System.Net.Primitives.dll",
            rtPath + "System.Net.Http.dll",
            rtPath + "System.Private.Uri.dll",
            rtPath + "System.Reflection.dll",
            rtPath + "System.ComponentModel.Primitives.dll",
            rtPath + "System.Globalization.dll",
            rtPath + "System.Collections.Concurrent.dll",
            rtPath + "System.Collections.NonGeneric.dll",
            rtPath + "Microsoft.CSharp.dll",

            #endregion
            
            typeof(IScript).Assembly.Location
        };
        _scriptReferences = references.Select(x => MetadataReference.CreateFromFile(x)).ToArray();
    }

    public void LoadAll(string scriptFolder)
    {
        AttachWatcher(scriptFolder);
        var dirInfo = new DirectoryInfo(scriptFolder);
        if (!dirInfo.Exists)
        {
            return;
        }

        foreach (var file in dirInfo.GetFiles())
        {
            if (!file.Name.EndsWith(".script.cs"))
            {
                continue;
            }

            try
            {
                Load(file.FullName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception trying to load {file.FullName}");
                Console.WriteLine(ex);
            }
        }
    }

    public void ExecuteAll(Engine engine)
    {
        foreach (var script in _scripts)
        {
            script.Value.Execute(engine);
        }
    }

    private void AttachWatcher(string path)
    {
        _watcher = new FileSystemWatcher(path);
        _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.LastAccess |
                                NotifyFilters.Size;
        _watcher.Changed += OnScriptChanged;
        _watcher.Deleted += OnScriptChanged;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnScriptChanged(object source, FileSystemEventArgs e)
    {
        if (!_scripts.ContainsKey(e.FullPath))
        {
            return;
        }
        
        Console.WriteLine($"Change detected for: {e.FullPath}");
        switch (e.ChangeType)
        {
            case WatcherChangeTypes.Changed:
                Task.Delay(100).Wait();
                _scripts.Remove(e.FullPath, out _);
                Load(e.FullPath);
                break;
            case WatcherChangeTypes.Deleted:
                _scripts.Remove(e.FullPath, out _);
                break;
        }
    }

    private IScript? Load(string file)
    {
        Console.WriteLine($"Loading script {file}");
        FileInfo fileInfo = new FileInfo(file);
        var fileOutput = fileInfo.FullName.Replace(fileInfo.Extension, ".dll");
        var code = File.ReadAllText(fileInfo.FullName);
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(fileInfo.Name.Replace(fileInfo.Extension, string.Empty),
            new[] { syntaxTree },
            _scriptReferences, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            foreach (var diag in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                Console.WriteLine($"{diag.GetMessage()} at {diag.Location}");
            }
            throw new FileLoadException(file);
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAssignableTo(typeof(IScript)))
            {
                var instance = (IScript?)type.GetConstructor(Type.EmptyTypes)?.Invoke(null);
                if (instance != null)
                {
                    instance.Initialize();
                    _scripts[file] = instance;
                }

                return instance;
            }
        }

        return null;
    }
}