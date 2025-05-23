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

        var code = File.ReadAllText(file);
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(file) + "_" + Guid.NewGuid().ToString("N"),
            new[] { syntaxTree },
            _scriptReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            foreach (var d in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                Console.WriteLine(d.ToString());
            return null;
        }

        ms.Position = 0;
        var asm = Assembly.Load(ms.ToArray());

        _scripts.Remove(file, out _);

        foreach (var t in asm.GetTypes())
        {
            if (!typeof(IScript).IsAssignableFrom(t)) continue;
            if (Activator.CreateInstance(t) is IScript script)
            {
                script.Initialize();
                _scripts[file] = script;
                return script;
            }
        }

        return null;
    }
}