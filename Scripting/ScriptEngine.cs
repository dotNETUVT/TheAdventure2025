using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;

namespace TheAdventure.Scripting;

public class ScriptEngine
{
    private PortableExecutableReference[] _scriptReferences;
    private Dictionary<string, IScript> _scripts = new Dictionary<string, IScript>();
    private FileSystemWatcher? _watcher;

    public ScriptEngine()
    {
        var rtPath = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? "";
        // Ensure rtPath ends with a directory separator character for reliable Path.Combine
        if (!string.IsNullOrEmpty(rtPath) && !rtPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            rtPath += Path.DirectorySeparatorChar;
        }

        var references = new List<string>
        {
            Path.Combine(rtPath, "System.Private.CoreLib.dll"),
            Path.Combine(rtPath, "System.Runtime.dll"),
            Path.Combine(rtPath, "System.Console.dll"),
            Path.Combine(rtPath, "netstandard.dll"),
            Path.Combine(rtPath, "System.Text.RegularExpressions.dll"),
            Path.Combine(rtPath, "System.Linq.dll"),
            Path.Combine(rtPath, "System.Linq.Expressions.dll"),
            Path.Combine(rtPath, "System.IO.dll"),
            Path.Combine(rtPath, "System.Net.Primitives.dll"),
            Path.Combine(rtPath, "System.Net.Http.dll"),
            Path.Combine(rtPath, "System.Private.Uri.dll"),
            Path.Combine(rtPath, "System.Reflection.dll"),
            Path.Combine(rtPath, "System.ComponentModel.Primitives.dll"),
            Path.Combine(rtPath, "System.Globalization.dll"),
            Path.Combine(rtPath, "System.Collections.Concurrent.dll"),
            Path.Combine(rtPath, "System.Collections.NonGeneric.dll"),
            Path.Combine(rtPath, "System.Collections.dll"),
            Path.Combine(rtPath, "Microsoft.CSharp.dll"),
        };

        // Add reference to the assembly containing IScript (TheAdventure itself)
        // and the Engine type (also TheAdventure).
        // This assumes IScript and Engine are in the currently executing assembly.
        var mainAssemblyLocation = typeof(IScript).Assembly.Location;
        if (!string.IsNullOrEmpty(mainAssemblyLocation) && !references.Contains(mainAssemblyLocation))
        {
            references.Add(mainAssemblyLocation);
        }

        _scriptReferences = references
            .Where(File.Exists)
            .Select(x => MetadataReference.CreateFromFile(x))
            .ToArray();

        if (_scriptReferences.Length < references.Count - 5)
        {
            Console.WriteLine("Warning: Some core .NET references for scripting might be missing.");
        }
    }

    public void LoadAll(string scriptFolder)
    {
        string fullScriptFolderPath = Path.GetFullPath(scriptFolder); // Ensure we have a full path
        AttachWatcher(fullScriptFolderPath);
        var dirInfo = new DirectoryInfo(fullScriptFolderPath);
        if (!dirInfo.Exists)
        {
            Console.WriteLine($"Script folder not found: {fullScriptFolderPath}");
            return;
        }

        foreach (var file in dirInfo.GetFiles("*.script.cs"))
        {
            try
            {
                Load(file.FullName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception trying to load {file.FullName}: {ex.Message}");
            }
        }
    }

    public void ReinitializeAllScripts(Engine engineContext)
    {
        // Use ToList() to create a copy of the keys or values if Load might modify _scripts
        foreach (var scriptEntry in _scripts.ToList())
        {
            try
            {
                Console.WriteLine($"Reinitializing script: {scriptEntry.Key}");
                scriptEntry.Value.Initialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reinitializing script {scriptEntry.Key}: {ex.Message}");
            }
        }
    }

    public void ExecuteAll(Engine engine)
    {
        foreach (var script in _scripts.Values)
        {
            try
            {
                script.Execute(engine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing script ({script.GetType().FullName}): {ex.Message}");
            }
        }
    }

    private void AttachWatcher(string path)
    {
        try
        {
            _watcher = new FileSystemWatcher(path);
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            _watcher.Changed += OnScriptChanged;
            _watcher.Created += OnScriptChanged;
            _watcher.Deleted += OnScriptChanged;
            _watcher.Renamed += OnScriptRenamed;
            _watcher.EnableRaisingEvents = true;
            Console.WriteLine($"Watching for script changes in: {path}");
        }
        catch (ArgumentException ex) // Can happen if path is invalid
        {
            Console.WriteLine($"Error attaching script watcher to '{path}': {ex.Message}. Ensure the path is valid and accessible.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error attaching script watcher to {path}: {ex.Message}");
        }
    }

    private void OnScriptRenamed(object source, RenamedEventArgs e)
    {
        Console.WriteLine($"Script renamed: {e.OldFullPath} to {e.FullPath}");
        if (_scripts.ContainsKey(e.OldFullPath))
        {
            _scripts.Remove(e.OldFullPath);
        }
        if (e.FullPath.EndsWith(".script.cs", StringComparison.OrdinalIgnoreCase))
        {
            Load(e.FullPath);
        }
    }

    private void OnScriptChanged(object source, FileSystemEventArgs e)
    {
        if (!e.Name.EndsWith(".script.cs", StringComparison.OrdinalIgnoreCase)) return;

        Console.WriteLine($"Script change detected ({e.ChangeType}): {e.FullPath}");
        // Use a brief delay to handle rapid saves or IDE temp files
        System.Threading.Tasks.Task.Delay(100).ContinueWith(t =>
        {
            if (!File.Exists(e.FullPath) && e.ChangeType != WatcherChangeTypes.Deleted)
            {
                // File might be a temporary one that got deleted quickly
                Console.WriteLine($"Script file {e.FullPath} no longer exists, change ignored.");
                return;
            }

            lock (_scripts) // Synchronize access to _scripts
            {
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Changed:
                    case WatcherChangeTypes.Created:
                        if (_scripts.ContainsKey(e.FullPath))
                        {
                            _scripts.Remove(e.FullPath);
                            Console.WriteLine($"Preparing to reload script: {e.FullPath}");
                        }
                        Load(e.FullPath);
                        break;
                    case WatcherChangeTypes.Deleted:
                        if (_scripts.ContainsKey(e.FullPath))
                        {
                            _scripts.Remove(e.FullPath);
                            Console.WriteLine($"Unloaded script: {e.FullPath}");
                        }
                        break;
                }
            }
        });
    }

    private IScript? Load(string filePath)
    {
        Console.WriteLine($"Attempting to load script: {filePath}");
        FileInfo fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists) // Check again after potential delay
        {
            Console.WriteLine($"Script file confirmed not found: {filePath}");
            return null;
        }

        var assemblyName = Path.GetFileNameWithoutExtension(fileInfo.Name) + "_" + Guid.NewGuid().ToString("N");
        var tempDir = Path.Combine(Path.GetTempPath(), "TheAdventureScripts"); // Use a subfolder in temp
        Directory.CreateDirectory(tempDir); // Ensure temp directory exists
        var dllPath = Path.Combine(tempDir, assemblyName + ".dll");

        string code;
        try
        {
            code = File.ReadAllText(filePath);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error reading script file {filePath}: {ex.Message}. Retrying in a moment...");
            System.Threading.Thread.Sleep(200);
            try { code = File.ReadAllText(filePath); }
            catch (IOException innerEx)
            {
                Console.WriteLine($"Still couldn't read script file {filePath} after retry: {innerEx.Message}");
                return null;
            }
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)); // Use latest preview
        var compilation = CSharpCompilation.Create(assemblyName,
            new[] { syntaxTree },
            _scriptReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug, platform: Platform.AnyCpu)
        );

        if (File.Exists(dllPath))
        {
            try { File.Delete(dllPath); }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not delete old DLL {dllPath}: {ex.Message}");
            }
        }

        Microsoft.CodeAnalysis.Emit.EmitResult result;
        try
        {
            using (var dllStream = new FileStream(dllPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                result = compilation.Emit(dllStream);
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IOException during Emit for {dllPath}: {ex.Message}. Check file locks or permissions.");
            return null;
        }


        if (!result.Success)
        {
            Console.WriteLine($"Compilation failed for {filePath}:");
            foreach (var diag in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning))
            {
                Console.WriteLine($"- {diag.GetMessage()} (at {diag.Location.GetLineSpan().StartLinePosition})");
            }
            return null;
        }

        Assembly scriptAssembly;
        try
        {
            byte[] assemblyBytes = File.ReadAllBytes(dllPath);
            scriptAssembly = Assembly.Load(assemblyBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load compiled assembly {dllPath} for script {filePath}: {ex.Message}");
            return null;
        }
        finally
        {
            // Clean up the temporary DLL after loading (optional, but good practice)
            // try { if(File.Exists(dllPath)) File.Delete(dllPath); } catch { /* ignore */ }
        }


        foreach (var type in scriptAssembly.GetTypes())
        {
            if (typeof(IScript).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            {
                try
                {
                    var instance = (IScript?)Activator.CreateInstance(type);
                    if (instance != null)
                    {
                        instance.Initialize();
                        // Ensure thread-safe update if OnScriptChanged can be rapid
                        lock (_scripts) { _scripts[filePath] = instance; }
                        Console.WriteLine($"Successfully loaded and initialized script: {type.FullName} from {filePath}");
                        return instance;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to instantiate or initialize script {type.FullName} from {filePath}: {ex.Message}");
                }
            }
        }
        Console.WriteLine($"No IScript compatible type found in {filePath}");
        return null;
    }
}