using System;
using System.IO;
using System.Linq;
using System.Threading;
using AlcoConnectWatch.Data;
using AlcoConnectWatch.Models;
using AlcoConnectWatch.Models.DTOs;

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
                        ?? @"C:\AlcoConnectWatch\WatchFolder";

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
                    ?? @"C:\AlcoConnectWatch\WatchFolder";
                _scanIntervalMinutes = 5;
            }
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
                    LogImport(db, fileName, "Unknown", 0, 0, "Error", "The imported file format must be 'Evac Report' or 'breathalyser_activity_report_'");
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
            var result = ExcelParserService.ParseEvacFileDetailed(filePath);

            if (!result.Success)
            {
                LogImportDetailed(db, fileName, "Evac", 0, 0, result, "Error");
                return;
            }

            int imported = 0;
            foreach (var record in result.Records)
            {
                db.EvacRecords.Add(record);
                imported++;
            }

            db.SaveChanges();
            LogImportDetailed(db, fileName, "Evac", imported, 0, result, "Success");
            MoveToProcessed(filePath);
        }

        private void ProcessAlcoConnectFile(AlcoConnectWatchContext db, string filePath, string fileName)
        {
            var result = ExcelParserService.ParseAlcoConnectFileDetailed(filePath);

            if (!result.Success)
            {
                LogImportDetailed(db, fileName, "AlcoConnect", 0, 0, result, "Error");
                return;
            }

            int imported = 0;
            int duplicatesSkipped = 0;

            foreach (var record in result.Records)
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
            LogImportDetailed(db, fileName, "AlcoConnect", imported, duplicatesSkipped, result, "Success");
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

        private void LogImportDetailed<T>(AlcoConnectWatchContext db, string fileName, string fileType,
            int rowCount, int duplicatesSkipped, ParseResult<T> result, string status)
        {
            db.FileImportLogs.Add(new FileImportLog
            {
                FileName = fileName,
                FileType = fileType,
                RowCount = rowCount,
                DuplicatesSkipped = duplicatesSkipped,
                TotalRowsInFile = result.TotalRowsInFile,
                SkippedEmptyRows = result.SkippedEmptyRows,
                SkippedInvalidDate = result.SkippedInvalidDate,
                SkippedOtherErrors = result.SkippedOtherErrors,
                Status = status,
                ErrorMessage = result.GetDetailedMessage(),
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
