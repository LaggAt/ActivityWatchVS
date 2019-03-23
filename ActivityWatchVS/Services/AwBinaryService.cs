using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ActivityWatchVS.Services
{
    internal class AwBinaryService
    {
        private const string AW_EXE = "aw-qt.exe";

        private AWPackage _package;
        private bool _startedAwServer = false;
        private volatile object _lock = new object();

        internal AwBinaryService(AWPackage package)
        {
            _package = package;
        }

        internal void TryStartAwServer()
        {
            // start aw_server only once
            if (!_startedAwServer)
            {
                lock (_lock)
                {
                    if (!_startedAwServer)
                    {
                        _startedAwServer = true; // we try that only once!
                        _package.LogService.Log("AW-Server not running, I try to start it.", LogService.EErrorLevel.Debug);
                        Task task = new Task(() => startAwServerThread());
                        task.Start();
                    }
                }
            }
        }

        private string findAwServerExe()
        {
            // H:\Program Files\activitywatch
            foreach (var drive in DriveInfo.GetDrives().Select(di => di.Name))
            {
                foreach (var folder1 in new List<string>()
                {
                    "Program Files",
                    "",
                    "Program Files (x86)",
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                })
                {
                    foreach (var folder2 in new List<string>()
                    {
                        "activitywatch",
                        "activity-watch",
                        "aw"
                    })
                    {
                        var testPath = Path.Combine(drive, stripPathRoot(folder1), folder2, AW_EXE);
                        try
                        {
                            if (File.Exists(testPath))
                            {
                                return testPath;
                            }
                        }
                        catch
                        { }
                    }
                }
            }
            return null;
        }

        private string stripPathRoot(string p)
        {
            try
            { 
                if (p.StartsWith(Path.GetPathRoot(p)))
                {
                    return p.Substring(Path.GetPathRoot(p).Length);
                }
            }
            catch { }
            return p;
        }

        private void startAwServerThread()
        {
            try
            {
                var exePath = findAwServerExe();
                if (exePath == null)
                {
                    _package.LogService.Log($"AW-Server not running, I cannot find the executable '{AW_EXE}'.", LogService.EErrorLevel.Error);
                    return;
                }
                var arguments = string.Empty;
                if (!_package.AwOptions.IsProductive)
                {
                    arguments = "--testing";
                }
                Process.Start(exePath, arguments);
                _package.LogService.Log($"AW-Server not running, started '{exePath} {arguments}'.", LogService.EErrorLevel.Info);
            }
            catch (Exception ex)
            {
                _package.LogService.Log(ex, $"I failed to start {AW_EXE}, this is a bug. Please report it.", activate: true);
            }
        }
    }
}
