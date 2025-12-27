using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace remote_window
{
    public static class RdpFileManager
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

        // DPAPI P/Invoke wrappers to avoid dependency issues with ProtectedData in some build setups
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct DATA_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        [System.Runtime.InteropServices.DllImport("crypt32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool CryptProtectData(ref DATA_BLOB pDataIn, string szDataDescr, IntPtr pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, out DATA_BLOB pDataOut);

        [System.Runtime.InteropServices.DllImport("crypt32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool CryptUnprotectData(ref DATA_BLOB pDataIn, StringBuilder ppszDataDescr, IntPtr pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, out DATA_BLOB pDataOut);

        private static byte[] DpapiProtect(byte[] plain)
        {
            var inBlob = new DATA_BLOB();
            inBlob.cbData = plain.Length;
            inBlob.pbData = System.Runtime.InteropServices.Marshal.AllocHGlobal(plain.Length);
            try
            {
                System.Runtime.InteropServices.Marshal.Copy(plain, 0, inBlob.pbData, plain.Length);
                DATA_BLOB outBlob;
                const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;
                bool ok = CryptProtectData(ref inBlob, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, out outBlob);
                if (!ok) throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                byte[] encrypted = new byte[outBlob.cbData];
                System.Runtime.InteropServices.Marshal.Copy(outBlob.pbData, encrypted, 0, outBlob.cbData);
                // free outBlob.pbData allocated by API
                System.Runtime.InteropServices.Marshal.FreeHGlobal(outBlob.pbData);
                return encrypted;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(inBlob.pbData);
            }
        }

        private static byte[] DpapiUnprotect(byte[] encrypted)
        {
            var inBlob = new DATA_BLOB();
            inBlob.cbData = encrypted.Length;
            inBlob.pbData = System.Runtime.InteropServices.Marshal.AllocHGlobal(encrypted.Length);
            try
            {
                System.Runtime.InteropServices.Marshal.Copy(encrypted, 0, inBlob.pbData, encrypted.Length);
                DATA_BLOB outBlob;
                StringBuilder desc = new StringBuilder();
                const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;
                bool ok = CryptUnprotectData(ref inBlob, desc, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, out outBlob);
                if (!ok) throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                byte[] plain = new byte[outBlob.cbData];
                System.Runtime.InteropServices.Marshal.Copy(outBlob.pbData, plain, 0, outBlob.cbData);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(outBlob.pbData);
                return plain;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(inBlob.pbData);
            }
        }
        public static string BuildRdpFileContent(RdpSettings settings, bool includePassword)
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
                string blob = BuildPasswordBlob(settings.Password);
                if (!string.IsNullOrEmpty(blob))
                {
                    lines.Add($"password 51:b:{blob}");
                }
            }

            return string.Join("\r\n", lines) + "\r\n";
        }

        public static void SaveRdpFile(string filePath, RdpSettings settings, bool includePassword)
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

        public static bool ValidateRdpFile(string filePath, string portFallback)
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

        private static string BuildPasswordBlob(string password)
        {
            // Build DPAPI-encrypted blob and return as HEX (uppercase) string
            // MSTSC expects DPAPI encrypted bytes represented as hex for "password 51:b:".
            if (string.IsNullOrEmpty(password)) return string.Empty;
            byte[] pwBytes = Encoding.Unicode.GetBytes(password);
            byte[] enc = DpapiProtect(pwBytes);
            var sb = new StringBuilder(enc.Length * 2);
            foreach (byte b in enc)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }

        public static string DecodePasswordFromBlob(string hexBlob)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hexBlob)) return string.Empty;

                // First, if looks like hex, try DPAPI unprotect (the intended format)
                if (hexBlob.Length % 2 == 0)
                {
                    bool isHex = true;
                    for (int i = 0; i < hexBlob.Length; i++)
                    {
                        char c = hexBlob[i];
                        if (!Uri.IsHexDigit(c)) { isHex = false; break; }
                    }
                    if (isHex)
                    {
                        int len = hexBlob.Length / 2;
                        byte[] data = new byte[len];
                        for (int i = 0; i < len; i++)
                        {
                            data[i] = Convert.ToByte(hexBlob.Substring(i * 2, 2), 16);
                        }
                        try
                        {
                            var decrypted = DpapiUnprotect(data);
                            return Encoding.Unicode.GetString(decrypted);
                        }
                        catch
                        {
                            // If DPAPI unprotect fails, fall through to other fallbacks
                        }
                        // If DPAPI failed, as a last resort try interpreting hex as raw unicode bytes
                        try
                        {
                            return Encoding.Unicode.GetString(data);
                        }
                        catch { }
                    }
                }

                // Next, try base64 (legacy cases where base64 was stored)
                try
                {
                    var bytes = Convert.FromBase64String(hexBlob);
                    return Encoding.Unicode.GetString(bytes);
                }
                catch { }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string BuildFullAddress(string remoteAddress, string port)
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
