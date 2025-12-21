using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace remote_window
{
    public class RdpSettings
    {
        public string RemoteAddress { get; set; } = string.Empty;
        public string Port { get; set; } = "3389";
        public string Username { get; set; } = string.Empty;
        public string Resolution { get; set; } = "1920x1080";
        public bool Fullscreen { get; set; } = true;
        public bool MultiScreen { get; set; } = false;
        public string ColorDepth { get; set; } = "32";
        public int AudioMode { get; set; } = 0;
        public int AudioCaptureMode { get; set; } = 1;
        public bool RedirectClipboard { get; set; } = true;
        public bool RedirectPrinters { get; set; } = true;
        public bool RedirectSmartcards { get; set; } = true;
        public bool RedirectComports { get; set; } = false;
        public bool RedirectDrives { get; set; } = false;
        public string Password { get; set; } = string.Empty;
    }

    public class RdpFileManager
    {
        private readonly Func<string, string> buildPasswordBlob;

        public RdpFileManager(Func<string, string> buildPasswordBlob)
        {
            this.buildPasswordBlob = buildPasswordBlob ?? (_ => string.Empty);
        }

        public string BuildRdpFileContent(RdpSettings settings, bool includePassword)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            string fullAddress = BuildFullAddress(settings.RemoteAddress, settings.Port);
            string[] resParts = (settings.Resolution ?? "1920x1080").Split('x');
            string width = resParts.Length > 0 ? resParts[0] : "1920";
            string height = resParts.Length > 1 ? resParts[1] : "1080";

            var lines = new List<string>
            {
                $"full address:s:{fullAddress}",
                $"username:s:{settings.Username}",
                $"screen mode id:i:{(settings.Fullscreen ? 2 : 1)}",
                $"use multimon:i:{(settings.MultiScreen ? 1 : 0)}",
                $"desktopwidth:i:{width}",
                $"desktopheight:i:{height}",
                $"session bpp:i:{settings.ColorDepth}",
                $"smart sizing:i:{(settings.Fullscreen ? 0 : 1)}",
                $"audiomode:i:{settings.AudioMode}",
                $"audiocapturemode:i:{settings.AudioCaptureMode}",
                $"redirectclipboard:i:{(settings.RedirectClipboard ? 1 : 0)}",
                $"redirectprinters:i:{(settings.RedirectPrinters ? 1 : 0)}",
                $"redirectcomports:i:{(settings.RedirectComports ? 1 : 0)}",
                $"redirectsmartcards:i:{(settings.RedirectSmartcards ? 1 : 0)}",
                settings.RedirectDrives ? "drivestoredirect:s:*" : "redirectdrives:i:0",
                "autoreconnection enabled:i:1",
                "displayconnectionbar:i:1",
                "compression:i:1",
                "bitmapcachepersistenable:i:1",
                "authentication level:i:2",
                "enablecredsspsupport:i:1"
            };

            if (includePassword && !string.IsNullOrEmpty(settings.Password))
            {
                string blob = buildPasswordBlob(settings.Password);
                if (!string.IsNullOrEmpty(blob))
                {
                    lines.Add($"password 51:b:{blob}");
                }
            }

            return string.Join("\r\n", lines) + "\r\n";
        }

        public void SaveRdpFile(string filePath, RdpSettings settings, bool includePassword)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("檔案路徑不可為空白。", nameof(filePath));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            string content = BuildRdpFileContent(settings, includePassword);
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(filePath, content, Encoding.Unicode);

            if (!ValidateRdpFile(filePath, settings.Port ?? "3389"))
            {
                throw new InvalidOperationException("產生的 RDP 檔案內容不完整或無法使用。請檢查輸入的主機與設定。");
            }
        }

        public bool ValidateRdpFile(string filePath, string portFallback)
        {
            try
            {
                if (!File.Exists(filePath)) return false;
                var lines = File.ReadAllLines(filePath, Encoding.Unicode);
                string addressLine = null;
                string userLine = null;
                foreach (var line in lines)
                {
                    if (line.StartsWith("full address:s:", StringComparison.OrdinalIgnoreCase)) addressLine = line.Substring("full address:s:".Length);
                    if (line.StartsWith("username:s:", StringComparison.OrdinalIgnoreCase)) userLine = line.Substring("username:s:".Length);
                }
                if (string.IsNullOrWhiteSpace(addressLine)) return false;
                if (string.IsNullOrWhiteSpace(userLine)) return false;

                BuildFullAddress(addressLine, portFallback);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string BuildFullAddress(string remoteAddress, string port)
        {
            string address = (remoteAddress ?? string.Empty).Trim();
            string portText = (port ?? "3389").Trim();
            if (string.IsNullOrEmpty(address))
            {
                throw new InvalidOperationException("請輸入遠端主機位址。");
            }

            if (address.Contains(":"))
            {
                return address;
            }

            if (string.IsNullOrEmpty(portText))
            {
                return address;
            }

            return $"{address}:{portText}";
        }
    }
}
