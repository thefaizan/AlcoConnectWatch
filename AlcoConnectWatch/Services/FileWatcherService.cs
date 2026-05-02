using System;
using System.IO;
using System.Linq;
using System.Threading;
using AlcoConnectWatch.Data;
using AlcoConnectWatch.Models;

namespace AlcoConnectWatch.Services
{
    public class FileWatcherService
    {
        private static readonly Lazy<FileWatcherService> _instance =
            new Lazy<FileWatcherService>(() => new FileWatcherService());

        public static FileWatcherService Instance => _instance.Value;

        private Timer _timer;
        private bool _isRunning;
        private string _watchFolder;
        private int _scanIntervalMinutes;
        private DateTime? _lastScan;

        public bool IsRunning => _isRunning;
        public string WatchFolder => _watchFolder;
        public int ScanIntervalMinutes => _scanIntervalMinutes;
        public DateTime? LastScan => _lastScan;

        private FileWatcherService() { }

        public void Start()
        {
            LoadSettings();

            if (string.IsNullOrEmpty(_watchFolder))
            {
                _isRunning = false;
                return;
            }

            if (!Directory.Exists(_watchFolder))
            {
                try { Directory.CreateDirectory(_watchFolder); }
                catch { _isRunning = false; return; }
            }

            _isRunning = true;
            var interval = TimeSpan.FromMinutes(_scanIntervalMinutes);
            _timer = new Timer(ScanFolder, null, TimeSpan.FromSeconds(10), interval);
        }

        public void Stop()
        {
            _isRunning = false;
            _timer?.Dispose();
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        private void LoadSettings()
        {
            try
            {
                using (var db = new AlcoConnectWatchContext())
                {
                    var watchFolderSetting = db.AppSettings.Find("WatchFolderPath");
                    _watchFolder = watchFolderSetting?.SettingValue
                        ?? System.Configuration.ConfigurationManager.AppSettings["WatchFolderPath"]
                        ?? "Files";

                    // Resolve relative paths to application base directory
                    _watchFolder = ResolvePath(_watchFolder);

                    var intervalSetting = db.AppSettings.Find("ScanIntervalMinutes");
                    int interval;
                    if (intervalSetting != null && int.TryParse(intervalSetting.SettingValue, out interval))
                        _scanIntervalMinutes = interval;
                    else
                        _scanIntervalMinutes = 5;
                }
            }
            catch
            {
                _watchFolder = System.Configuration.ConfigurationManager.AppSettings["WatchFolderPath"]
                    ?? "Files";
                _watchFolder = ResolvePath(_watchFolder);
                _scanIntervalMinutes = 5;
            }
        }

        private string ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Skip URLs
            if (path.StartsWith("http://") || path.StartsWith("https://"))
                return null;

            // Handle ~/path format
            if (path.StartsWith("~/"))
                path = path.Substring(2);

            // If already absolute path, return as-is
            if (Path.IsPathRooted(path))
                return path;

            // Resolve relative path from application base directory
            string basePath = AppDomain.CurrentDomain.BaseDirectory;

            // For web apps, try to get the actual web root
            try
            {
                if (System.Web.Hosting.HostingEnvironment.IsHosted)
                    basePath = System.Web.Hosting.HostingEnvironment.MapPath("~") ?? basePath;
            }
            catch { }

            return Path.Combine(basePath, path);
        }

        private void ScanFolder(object state)
        {
            _lastScan = DateTime.Now;

            if (!Directory.Exists(_watchFolder)) return;

            var files = Directory.GetFiles(_watchFolder, "*.xlsx")
                .OrderBy(f => File.GetCreationTime(f))
                .ToArray();

            foreach (var filePath in files)
            {
                ProcessFile(filePath);
            }
        }

        private void ProcessFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);

            using (var db = new AlcoConnectWatchContext())
            {
                var alreadyImported = db.FileImportLogs
                    .Any(f => f.FileName == fileName && f.Status == "Success");

                if (alreadyImported) return;

                var fileType = ExcelParserService.DetectFileType(fileName);
                if (fileType == null)
                {
                    LogImport(db, fileName, "Unknown", 0, 0, "Error", "Unrecognized file type");
                    return;
                }

                if (fileType == "Evac")
                    ProcessEvacFile(db, filePath, fileName);
                else
                    ProcessAlcoConnectFile(db, filePath, fileName);
            }
        }

        private void ProcessEvacFile(AlcoConnectWatchContext db, string filePath, string fileName)
        {
            string error;
            var records = ExcelParserService.ParseEvacFile(filePath, out error);

            if (records == null)
            {
                LogImport(db, fileName, "Evac", 0, 0, "Error", error ?? "Failed to parse file");
                return;
            }

            int imported = 0;
            foreach (var record in records)
            {
                db.EvacRecords.Add(record);
                imported++;
            }

            db.SaveChanges();
            LogImport(db, fileName, "Evac", imported, 0, "Success", null);
            MoveToProcessed(filePath);
        }

        private void ProcessAlcoConnectFile(AlcoConnectWatchContext db, string filePath, string fileName)
        {
            string error;
            var records = ExcelParserService.ParseAlcoConnectFile(filePath, out error);

            if (records == null)
            {
                LogImport(db, fileName, "AlcoConnect", 0, 0, "Error", error ?? "Failed to parse file");
                return;
            }

            int imported = 0;
            int duplicatesSkipped = 0;

            foreach (var record in records)
            {
                var staffIdNormalized = record.StaffId.TrimStart('0');
                var isDuplicate = db.AlcoConnectRecords.Any(a =>
                    a.StaffId == record.StaffId &&
                    a.TestDate == record.TestDate &&
                    a.Site == record.Site &&
                    a.TestTime == record.TestTime);

                if (isDuplicate)
                {
                    duplicatesSkipped++;
                    continue;
                }

                db.AlcoConnectRecords.Add(record);
                imported++;
            }

            db.SaveChanges();
            LogImport(db, fileName, "AlcoConnect", imported, duplicatesSkipped, "Success", null);
            MoveToProcessed(filePath);
        }

        private void LogImport(AlcoConnectWatchContext db, string fileName, string fileType,
            int rowCount, int duplicatesSkipped, string status, string errorMessage)
        {
            db.FileImportLogs.Add(new FileImportLog
            {
                FileName = fileName,
                FileType = fileType,
                RowCount = rowCount,
                DuplicatesSkipped = duplicatesSkipped,
                Status = status,
                ErrorMessage = errorMessage,
                ImportedAt = DateTime.Now
            });
            db.SaveChanges();
        }

        private void MoveToProcessed(string filePath)
        {
            try
            {
                var processedDir = Path.Combine(Path.GetDirectoryName(filePath), "Processed");
                if (!Directory.Exists(processedDir))
                    Directory.CreateDirectory(processedDir);

                var destPath = Path.Combine(processedDir, Path.GetFileName(filePath));
                if (File.Exists(destPath))
                    destPath = Path.Combine(processedDir,
                        Path.GetFileNameWithoutExtension(filePath) + "_" +
                        DateTime.Now.ToString("yyyyMMddHHmmss") +
                        Path.GetExtension(filePath));

                File.Move(filePath, destPath);
            }
            catch { }
        }
    }
}
