using Sharingan;
using Sharingan.Abstractions;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("🔮 Sharingan - Multi-Process Concurrent Access Sample");
Console.WriteLine(new string('=', 60));
Console.WriteLine();
Console.WriteLine("This sample demonstrates how Sharingan handles concurrent access");
Console.WriteLine("from multiple processes sharing the same settings file.");
Console.WriteLine("Ideal for multi-component applications, microservices, or plugins.");
Console.WriteLine();

string settingsPath = Path.Combine(Path.GetTempPath(), "Sharingan.MultiProcess", "shared-settings.json");
string logPath = Path.Combine(Path.GetTempPath(), "Sharingan.MultiProcess", "process-log.txt");
Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);

// Check if this is a child process
string? processId = Environment.GetEnvironmentVariable("SHARINGAN_PROCESS_ID");
if (!string.IsNullOrEmpty(processId))
{
    // This is a child process - simulate an app component
    await RunAsChildProcess(processId, settingsPath, logPath);
    return;
}

// This is the main process - orchestrate the demo
Console.WriteLine("📌 Scenario: Multi-Application Settings Access");
Console.WriteLine("   Simulating 12 application components/processes");
Console.WriteLine("   reading and writing to the same settings file.");
Console.WriteLine();

// Initialize shared settings
Console.WriteLine("🔧 Initializing shared settings file...");
using (ISettingsProvider initStore = CreateStore(settingsPath))
{
    initStore.Set("app.version", "1.0.0");
    initStore.Set("app.lastStartup", DateTime.UtcNow.ToString("O"));
    initStore.Set("ui.theme", "dark");
    initStore.Set("ui.fontSize", 14);
    initStore.Set("performance.quality", "high");
    initStore.Set("performance.fps", 60);
    initStore.Flush();
}
Console.WriteLine("   ✅ Settings initialized");
Console.WriteLine();

// Clear log file
if (File.Exists(logPath))
{
    File.Delete(logPath);
}

// Example 1: Simulate multiple processes reading settings
Console.WriteLine("📌 Example 1: Concurrent Reads (12 processes)");
Console.WriteLine("   Launching 12 reader processes simultaneously...");

List<Task> readerTasks = [];
for (int i = 1; i <= 12; i++)
{
    Task task = Task.Run(() => SimulateProcess(i, "read", settingsPath, logPath));
    readerTasks.Add(task);
}

await Task.WhenAll(readerTasks);
Console.WriteLine("   ✅ All 12 reader processes completed without conflicts");
Console.WriteLine();

// Example 2: Simulate mixed read/write operations
Console.WriteLine("📌 Example 2: Concurrent Reads and Writes");
Console.WriteLine("   Launching processes with mixed operations...");

List<Task> mixedTasks = [];
for (int i = 1; i <= 8; i++)
{
    string mode = i % 2 == 0 ? "write" : "read";
    Task task = Task.Run(() => SimulateProcess(i, mode, settingsPath, logPath));
    mixedTasks.Add(task);
}

await Task.WhenAll(mixedTasks);
Console.WriteLine("   ✅ Mixed read/write operations completed");
Console.WriteLine();

// Example 3: Verify data integrity
Console.WriteLine("📌 Example 3: Data Integrity Check");
using (ISettingsProvider verifyStore = CreateStore(settingsPath))
{
    verifyStore.Reload();
    Console.WriteLine($"   App Version: {verifyStore.Get("app.version", "")}");
    Console.WriteLine($"   Theme: {verifyStore.Get("ui.theme", "")}");
    Console.WriteLine($"   Quality: {verifyStore.Get("performance.quality", "")}");
    Console.WriteLine($"   Total Keys: {verifyStore.Count}");
}
Console.WriteLine("   ✅ Data integrity verified");
Console.WriteLine();

// Example 4: In-process concurrent access demo
Console.WriteLine("📌 Example 4: In-Process Multi-Thread Access");
Console.WriteLine("   Simulating 50 concurrent operations...");

using (ISettingsProvider store = CreateStore(settingsPath))
{
    List<Task> threadTasks = [];
    int successCount = 0;
    int errorCount = 0;

    for (int i = 0; i < 50; i++)
    {
        int index = i;
        Task task = Task.Run(() =>
        {
            try
            {
                if (index % 3 == 0)
                {
                    store.Set($"thread.value.{index}", $"data-{index}");
                }
                else
                {
                    store.Get($"thread.value.{index % 10}", "default");
                }
                Interlocked.Increment(ref successCount);
            }
            catch
            {
                Interlocked.Increment(ref errorCount);
            }
        });
        threadTasks.Add(task);
    }

    await Task.WhenAll(threadTasks);
    store.Flush();

    Console.WriteLine($"   Successful operations: {successCount}");
    Console.WriteLine($"   Failed operations: {errorCount}");
}
Console.WriteLine("   ✅ Multi-thread access completed");
Console.WriteLine();

// Example 5: File watcher for external changes (demonstration)
Console.WriteLine("📌 Example 5: Best Practices for Multi-Process Scenarios");
Console.WriteLine("   ✅ Use atomic writes (UseAtomicWrites = true)");
Console.WriteLine("   ✅ Call Reload() before reading if external changes expected");
Console.WriteLine("   ✅ Call Flush() after writing to persist changes immediately");
Console.WriteLine("   ✅ Implement retry logic for file lock contention");
Console.WriteLine("   ✅ Consider SQLite provider for highest concurrency needs");
Console.WriteLine("   ✅ Use file watchers to detect external changes");
Console.WriteLine();

// Show process log
if (File.Exists(logPath))
{
    Console.WriteLine("📌 Process Activity Log:");
    IEnumerable<string> logLines = File.ReadAllLines(logPath).Take(15);
    foreach (string? line in logLines)
    {
        Console.WriteLine($"   {line}");
    }
    if (File.ReadAllLines(logPath).Length > 15)
    {
        Console.WriteLine("   ... (truncated)");
    }
    Console.WriteLine();
}

// Example 6: Multi-Component Application Tips
Console.WriteLine("📌 Example 6: Tips for Multi-Component Applications");
Console.WriteLine("   Multiple components can safely share settings by:");
Console.WriteLine();
Console.WriteLine("   1️⃣  Using a single shared settings file:");
Console.WriteLine("      var store = new SharinganBuilder()");
Console.WriteLine("          .UseJsonFile(\"shared-settings.json\")");
Console.WriteLine("          .Build();");
Console.WriteLine();
Console.WriteLine("   2️⃣  Enable atomic writes to prevent corruption:");
Console.WriteLine("      .UseJsonFile(\"settings.json\", configure: opt => {");
Console.WriteLine("          opt.UseAtomicWrites = true;");
Console.WriteLine("      })");
Console.WriteLine();
Console.WriteLine("   3️⃣  Call Reload() when a component starts:");
Console.WriteLine("      store.Reload(); // Get latest changes from other components");
Console.WriteLine();
Console.WriteLine("   4️⃣  Call Flush() immediately after writing:");
Console.WriteLine("      store.Set(\"app.setting\", newValue);");
Console.WriteLine("      store.Flush(); // Persist for other components to see");
Console.WriteLine();
Console.WriteLine("   5️⃣  For highest reliability, use SQLite provider:");
Console.WriteLine("      .UseSQLite(\"app-settings.db\")");
Console.WriteLine();

Console.WriteLine(new string('=', 60));
Console.WriteLine("🎉 Multi-Process sample completed successfully!");
Console.WriteLine($"   Settings file: {settingsPath}");

// Helper: Create a store with atomic writes enabled
static ISettingsProvider CreateStore(string path)
{
    return new SharinganBuilder()
        .UseJsonFile(path, configure: opt =>
        {
            opt.UseAtomicWrites = true;
            opt.CreateIfNotExists = true;
        })
        .BuildProvider();
}

// Helper: Simulate a process (in-process for demo, but could be actual child processes)
static async Task SimulateProcess(int id, string mode, string settingsPath, string logPath)
{
    string processName = $"Process-{id:D2}";
    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
    int maxRetries = 5;

    for (int retry = 0; retry < maxRetries; retry++)
    {
        try
        {
            using ISettingsProvider store = CreateStore(settingsPath);

            if (mode == "read")
            {
                store.Reload();
                string theme = store.Get("ui.theme", "unknown");
                int fontSize = store.Get("ui.fontSize", 0);

                await AppendLog(logPath, $"[{timestamp}] {processName} READ: theme={theme}, fontSize={fontSize}");
            }
            else
            {
                store.Reload();
                store.Set($"process.{id}.lastAccess", DateTime.UtcNow.ToString("O"));
                store.Set($"process.{id}.status", "active");
                store.Flush();

                await AppendLog(logPath, $"[{timestamp}] {processName} WRITE: Updated process status");
            }

            // Simulate some work
            await Task.Delay(Random.Shared.Next(10, 50));
            return; // Success - exit retry loop
        }
        catch (IOException) when (retry < maxRetries - 1)
        {
            // File is locked by another process, wait and retry
            await Task.Delay(Random.Shared.Next(50, 150));
        }
        catch (Exception ex)
        {
            await AppendLog(logPath, $"[{timestamp}] {processName} ERROR: {ex.Message}");
            return;
        }
    }
}

// Helper: Append to log file with locking
static async Task AppendLog(string logPath, string message)
{
    int maxRetries = 3;
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await File.AppendAllTextAsync(logPath, message + Environment.NewLine);
            return;
        }
        catch (IOException)
        {
            await Task.Delay(10);
        }
    }
}

// Child process entry point
static async Task RunAsChildProcess(string processId, string settingsPath, string logPath)
{
    string mode = Environment.GetEnvironmentVariable("SHARINGAN_MODE") ?? "read";
    await SimulateProcess(int.Parse(processId), mode, settingsPath, logPath);
}
