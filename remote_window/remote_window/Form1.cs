using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace remote_window
{
    public partial class Form1 : Form
    {
        
        private string rdpDir;
        private Dictionary<string, string> rdpFileMap = new Dictionary<string, string>();
        private Dictionary<string, string> savedAccountPasswords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private const string DefaultListEntry = "新增的連線...";

        public Form1()
        {
            InitializeComponent();
        }

        private string ReadTextFileAuto(string path)
        {
            // Read first bytes to detect BOM for UTF-8/UTF-16LE/UTF-16BE. If no BOM, assume UTF-8.
            try
            {
                using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
                {
                    byte[] bom = new byte[4];
                    int read = fs.Read(bom, 0, bom.Length);
                    // UTF-8 BOM
                    if (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                    {
                        fs.Position = 3;
                        using (var sr = new System.IO.StreamReader(fs, Encoding.UTF8))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                    // UTF-16 LE BOM
                    if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
                    {
                        fs.Position = 2;
                        using (var sr = new System.IO.StreamReader(fs, Encoding.Unicode))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                    // UTF-16 BE BOM
                    if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
                    {
                        fs.Position = 2;
                        using (var sr = new System.IO.StreamReader(fs, Encoding.BigEndianUnicode))
                        {
                            return sr.ReadToEnd();
                        }
                    }

                    // No BOM -> assume UTF8 without BOM (common for checked-in text)
                    fs.Position = 0;
                    using (var sr = new System.IO.StreamReader(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                try { return System.IO.File.ReadAllText(path, Encoding.Default); } catch { return string.Empty; }
            }
        }

        // Move file to recycle bin using SHFileOperation
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
            public string pFrom;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
            public string pTo;
            public ushort fFlags;
            public int fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public IntPtr lpszProgressTitle;
        }

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

        private bool MoveToRecycleBin(string path)
        {
            try
            {
                var fs = new SHFILEOPSTRUCT();
                fs.wFunc = 3; // FO_DELETE
                // double-null terminated
                fs.pFrom = path + "\0\0";
                fs.fFlags = 0x0001 | 0x0040; // FOF_ALLOWUNDO | FOF_NOCONFIRMATION
                int result = SHFileOperation(ref fs);
                return result == 0 && fs.fAnyOperationsAborted == 0;
            }
            catch
            {
                return false;
            }
        }

        private RdpFileManager.RdpSettings CollectRdpSettings()
        {
            var settings = new RdpFileManager.RdpSettings();

            settings.RemoteAddress = this.textRemoteAddress?.Text ?? string.Empty;
            settings.Port = this.numRemotePort?.Value.ToString() ?? "3389";
            settings.Username = this.comboUserAccount?.Text ?? string.Empty;
            settings.Resolution = this.lblNowRes?.Text ?? "1920x1080";
            settings.Fullscreen = this.chkFullscreen?.Checked ?? true;
            settings.MultiScreen = this.chkMultiScreen?.Checked ?? false;
            settings.ColorDepth = (this.radColordepth32 != null && this.radColordepth32.Checked) ? "32" : "16";
            settings.Password = this.textPassword?.Text ?? string.Empty;

            settings.AudioMode = 0;
            if (this.radAudio_remote != null && this.radAudio_remote.Checked) settings.AudioMode = 1;
            else if (this.radAudio_noplay != null && this.radAudio_noplay.Checked) settings.AudioMode = 2;

            settings.AudioCaptureMode = (this.radRecord_local != null && this.radRecord_local.Checked) ? 1 : 0;

            if (this.treeDeviceList != null)
            {
                foreach (TreeNode node in this.treeDeviceList.Nodes)
                {
                    if (node.Name == "Clipboard") settings.RedirectClipboard = node.Checked;
                    if (node.Name == "Printer") settings.RedirectPrinters = node.Checked;
                    if (node.Name == "SmartCard") settings.RedirectSmartcards = node.Checked;
                    if (node.Name == "Ports") settings.RedirectComports = node.Checked;
                    if (node.Name == "Drive") settings.RedirectDrives = node.Checked;
                }
            }

            return settings;
        }

        private void SaveRdpFile(string filePath, bool includePassword)
        {
            var settings = CollectRdpSettings();
            RdpFileManager.SaveRdpFile(filePath, settings, includePassword);
        }

        private void RefreshPresetList()
        {
            // 掃描 ~/RDP/*.rdp 並顯示於 listSavedPreset
            rdpDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "RDP");
            listSavedPreset.Items.Clear();
            rdpFileMap.Clear();
            comboUserAccount.Items.Clear();
            savedAccountPasswords.Clear();

            var collectedUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var collectedUserList = new List<string>();

            // Always have default first entry for creating a new connection
            listSavedPreset.Items.Add(DefaultListEntry);
            if (System.IO.Directory.Exists(rdpDir))
            {
                var files = System.IO.Directory.GetFiles(rdpDir, "*.rdp");
                foreach (var file in files)
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(file);
                    listSavedPreset.Items.Add(name);
                    rdpFileMap[name] = file;

                    try
                    {
                        var lines = System.IO.File.ReadAllLines(file, System.Text.Encoding.Unicode);
                        string username = string.Empty;
                        string password = string.Empty;
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("username:s:", StringComparison.OrdinalIgnoreCase))
                            {
                                username = line.Substring("username:s:".Length);
                            }
                            else if (line.StartsWith("password 51:b:", StringComparison.OrdinalIgnoreCase))
                            {
                                var blob = line.Substring("password 51:b:".Length);
                                password = RdpFileManager.DecodePasswordFromBlob(blob);
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(username) && collectedUsers.Add(username))
                        {
                            collectedUserList.Add(username);
                        }

                        if (!string.IsNullOrWhiteSpace(username))
                        {
                            if (!savedAccountPasswords.ContainsKey(username) || (!string.IsNullOrEmpty(password) && string.IsNullOrEmpty(savedAccountPasswords[username])))
                            {
                                savedAccountPasswords[username] = password;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore malformed RDP files while populating presets and accounts
                    }
                }
            }
            // select default
            RefreshComboUserAccount(collectedUserList);
            listSavedPreset.SelectedIndex = 0;
        }

        private void RefreshComboUserAccount(List<string> users)
        {
            comboUserAccount.Items.Clear();
            if (users == null || users.Count == 0) return;

            // Bubble sort with nested loops (case-insensitive)
            for (int i = 0; i < users.Count - 1; i++)
            {
                for (int j = 0; j < users.Count - 1 - i; j++)
                {
                    if (string.Compare(users[j], users[j + 1], StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        string tmp = users[j];
                        users[j] = users[j + 1];
                        users[j + 1] = tmp;
                    }
                }
            }

            foreach (var user in users)
            {
                comboUserAccount.Items.Add(user);
            }
        }

        private void comboUserAccount_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboUserAccount.SelectedItem is string username)
            {
                if (savedAccountPasswords.TryGetValue(username, out var password))
                {
                    this.textPassword.Text = password;
                }
            }
        }

        private string FindFramesDirectory()
        {
            // Try to locate a folder named 'frames' or 'frame' starting from the app base directory
            try
            {
                string[] candidates = new[] { "frames", "frame" };
                string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? Application.StartupPath;

                // check immediate
                foreach (var c in candidates)
                {
                    var p = System.IO.Path.Combine(baseDir, c);
                    if (System.IO.Directory.Exists(p)) return p;
                }

                // ascend folders up to reasonable depth (to support running from bin\Debug and repo layout)
                string cur = baseDir;
                for (int i = 0; i < 6; i++)
                {
                    cur = System.IO.Path.GetFullPath(System.IO.Path.Combine(cur, ".."));
                    foreach (var c in candidates)
                    {
                        var p = System.IO.Path.Combine(cur, c);
                        if (System.IO.Directory.Exists(p)) return p;
                    }
                }

                // also try repo-specific path: looking for remote_window\frame under parents
                cur = baseDir;
                for (int i = 0; i < 6; i++)
                {
                    cur = System.IO.Path.GetFullPath(System.IO.Path.Combine(cur, ".."));
                    var p = System.IO.Path.Combine(cur, "remote_window", "frame");
                    if (System.IO.Directory.Exists(p)) return p;
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        // About player state
        private int aboutFrameIndex = 1;
        private int aboutMissCount = 0;
        private const int AboutMissThreshold = 250;
        private void Form1_Load(object sender, EventArgs e)
        {
            RefreshPresetList();

            // Ensure connect button gets initial focus so Enter activates it
            try
            {
                this.btnConnect?.Focus();
            }
            catch { }
            // initialize About frame player state
            aboutFrameIndex = 1;
            aboutMissCount = 0;
        }
        private void tabAllSettings_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                bool isAbout = false;
                if (this.tabAllSettings.SelectedTab != null)
                {
                    var sel = this.tabAllSettings.SelectedTab;
                    if (sel == this.About) isAbout = true;
                    else if (!string.IsNullOrEmpty(sel.Text) && (sel.Text.Equals("About", StringComparison.OrdinalIgnoreCase) || sel.Text.Equals("關於"))) isAbout = true;
                    else if (this.groupBox9 != null && sel.Controls.Contains(this.groupBox9)) isAbout = true;
                }

                if (isAbout)
                {
                    //try { this.AAAApple.Font = new Font("Consolas", 9F); } catch { }
                    try { this.AAAApple.WordWrap = false; } catch { }
                    try { if (!this.timer1.Enabled) this.timer1.Start(); } catch { }
                }
                else
                {
                    try { if (this.timer1.Enabled) this.timer1.Stop(); } catch { }
                }
            }
            catch { }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {

            // This part uses code from the "bad-apple" project.
            // The "bad-apple" project is licensed under the GPL-3.0 License.
            // Source: https://github.com/simotmm/bad-apple
            //
            // The code from "bad-apple" is used here in compliance with the terms of the GPL-3.0 License.
            // You may modify, distribute, and use the code according to the terms outlined in the GPL-3.0 License.
            // For more details, please refer to the LICENSE file in this repository and the original "bad-apple" repository.

            // Each tick: find next available frame and display it. Use nested loops per requirement.
            string framesDir = FindFramesDirectory();
            if (string.IsNullOrEmpty(framesDir) || !System.IO.Directory.Exists(framesDir))
            {
                // frames directory not found; nothing to play
                return;
            }

            bool displayed = false;

            // Outer loop caps attempts per tick to avoid infinite loops
            for (int outer = 0; outer < 1000 && !displayed; outer++)
            {
                // Inner loop: try to find next available frame; break when one displayed
                while (!displayed)
                {
                    string fileName = $"BA{aboutFrameIndex}.txt";
                    string path = System.IO.Path.Combine(framesDir, fileName);
                    try
                    {
                        if (System.IO.File.Exists(path))
                        {
                            string content = ReadTextFileAuto(path);
                            this.AAAApple.Text = content;
                            aboutFrameIndex++;
                            aboutMissCount = 0;
                            displayed = true;
                            break; // found frame, exit inner loop
                        }

        
                        else
                        {
                            // missing frame -> skip
                            aboutFrameIndex++;
                            aboutMissCount++;
                            if (aboutMissCount >= AboutMissThreshold)
                            {
                                // considered end; reset to start
                                aboutFrameIndex = 1;
                                aboutMissCount = 0;
                                // continue searching from start in same tick
                                continue;
                            }
                            // continue inner while to try next index
                            continue;
                        }
                    }
                    catch
                    {
                        // treat as miss
                        aboutFrameIndex++;
                        aboutMissCount++;
                        if (aboutMissCount >= AboutMissThreshold)
                        {
                            aboutFrameIndex = 1;
                            aboutMissCount = 0;
                            continue;
                        }
                        continue;
                    }
                }
            }
        }

        private void listSavedPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listSavedPreset.SelectedItem == null) return;
            string name = listSavedPreset.SelectedItem.ToString();
            lblPresetName.Text = name;
            if (name == DefaultListEntry)
            {
                ResetFormToDefaults();
                this.btnSave.Enabled = false;
                this.btnDelete.Enabled = false;
                return;
            }
            if (!rdpFileMap.ContainsKey(name)) return;
            string file = rdpFileMap[name];
            try
            {
                var lines = System.IO.File.ReadAllLines(file, System.Text.Encoding.Unicode);
                foreach (var line in lines)
                {
                    if (line.StartsWith("full address:s:"))
                    {
                        var val = line.Substring("full address:s:".Length);
                        var parts = val.Split(':');
                        this.textRemoteAddress.Text = parts[0];
                        if (parts.Length > 1) this.numRemotePort.Value = int.TryParse(parts[1], out int p) ? p : 3389;
                    }
                    else if (line.StartsWith("username:s:"))
                    {
                        this.comboUserAccount.Text = line.Substring("username:s:".Length);
                    }
                    else if (line.StartsWith("screen mode id:i:"))
                    {
                        this.chkFullscreen.Checked = line.EndsWith("2");
                    }
                    else if (line.StartsWith("use multimon:i:"))
                    {
                        this.chkMultiScreen.Checked = line.EndsWith("1");
                    }
                    else if (line.StartsWith("desktopwidth:i:"))
                    {
                        string w = line.Substring("desktopwidth:i:".Length);
                        // 設定 trackRes
                        SetTrackResByWidth(w);
                    }
                    else if (line.StartsWith("session bpp:i:"))
                    {
                        string bpp = line.Substring("session bpp:i:".Length);
                        if (bpp == "32") this.radColordepth32.Checked = true;
                        else this.radColordepth16.Checked = true;
                    }
                    else if (line.StartsWith("audiomode:i:"))
                    {
                        string v = line.Substring("audiomode:i:".Length);
                        if (v == "0") this.radAudio_local.Checked = true;
                        else if (v == "1") this.radAudio_remote.Checked = true;
                        else if (v == "2") this.radAudio_noplay.Checked = true;
                    }
                    else if (line.StartsWith("audiocapturemode:i:"))
                    {
                        string v = line.Substring("audiocapturemode:i:".Length);
                        this.radRecord_local.Checked = (v == "1");
                        this.radRecord_norec.Checked = (v != "1");
                    }
                    else if (line.StartsWith("redirectclipboard:i:"))
                    {
                        SetTreeDeviceChecked("Clipboard", line.EndsWith("1"));
                    }
                    else if (line.StartsWith("redirectprinters:i:"))
                    {
                        SetTreeDeviceChecked("Printer", line.EndsWith("1"));
                    }
                    else if (line.StartsWith("redirectsmartcards:i:"))
                    {
                        SetTreeDeviceChecked("SmartCard", line.EndsWith("1"));
                    }
                    else if (line.StartsWith("redirectcomports:i:"))
                    {
                        SetTreeDeviceChecked("Ports", line.EndsWith("1"));
                    }
                    else if (line.StartsWith("drivestoredirect:s:"))
                    {
                        var value = line.Substring("drivestoredirect:s:".Length);
                        SetTreeDeviceChecked("Drive", !string.IsNullOrEmpty(value));
                    }
                    else if (line.StartsWith("redirectdrives:i:"))
                    {
                        SetTreeDeviceChecked("Drive", line.EndsWith("1"));
                    }
                    else if (line.StartsWith("password 51:b:"))
                    {
                        var blob = line.Substring("password 51:b:".Length);
                        this.textPassword.Text = RdpFileManager.DecodePasswordFromBlob(blob);
                    }
                }
            }
            catch { }
            // enable save/delete for actual presets
            this.btnSave.Enabled = true;
            this.btnDelete.Enabled = true;
        }

        private void ResetFormToDefaults()
        {
            // Basic fields
            this.lblPresetName.Text = DefaultListEntry;
            this.textRemoteAddress.Text = string.Empty;
            this.textPassword.Text = string.Empty;
            this.comboUserAccount.Text = string.Empty;
            this.numRemotePort.Value = 3389;
            this.chkIsAdmin.Checked = false;

            // Display defaults
            this.chkFullscreen.Checked = true;
            this.chkMultiScreen.Checked = false;
            this.trackRes.Value = 11;
            this.lblNowRes.Text = "1920x1080";
            this.radColordepth32.Checked = true;

            // Audio defaults
            this.radAudio_local.Checked = true;
            this.radRecord_local.Checked = true;

            // Resources defaults (match designer defaults)
            if (this.treeDeviceList != null)
            {
                foreach (TreeNode node in this.treeDeviceList.Nodes)
                {
                    if (node.Name == "Clipboard" || node.Name == "Printer" || node.Name == "SmartCard" || node.Name == "WebAuth")
                        node.Checked = true;
                    else
                        node.Checked = false;
                }
            }
        }

        private void SetTrackResByWidth(string width)
        {
            string[] resolutions = new string[]
            {
                "640x480",
                "800x600",
                "1024x768",
                "1280x720",
                "1280x768",
                "1280x800",
                "1280x1024",
                "1366x768",
                "1440x900",
                "1400x1050",
                "1680x1050",
                "1920x1080"
            };
            for (int i = 0; i < resolutions.Length; i++)
            {
                if (resolutions[i].StartsWith(width + "x"))
                {
                    this.trackRes.Value = i;
                    this.lblNowRes.Text = resolutions[i];
                    break;
                }
            }
        }

        private void SetTreeDeviceChecked(string nodeName, bool isChecked)
        {
            if (this.treeDeviceList == null) return;
            foreach (TreeNode node in this.treeDeviceList.Nodes)
            {
                if (node.Name == nodeName)
                {
                    node.Checked = isChecked;
                    break;
                }
            }
        }

        private void chkToggleVisibleRemoteAddress_CheckedChanged(object sender, EventArgs e)
        {
            // Toggle masking for the remote address textbox (show/hide text like password)
            try
            {
                var chk = sender as CheckBox;
                if (chk != null && this.textRemoteAddress != null)
                {
                    // when checked -> show the address (no mask)
                    // when unchecked -> mask the address
                    this.textRemoteAddress.UseSystemPasswordChar = !chk.Checked;
                }
            }
            catch
            {
                // safe fail
            }
        }

        private void chkVisibleTogglePassword_CheckedChanged(object sender, EventArgs e)
        {
            // Toggle password masking for the password textbox
            try
            {
                var chk = sender as CheckBox;
                if (chk != null && this.textPassword != null)
                {
                    this.textPassword.UseSystemPasswordChar = !chk.Checked;
                }
            }
            catch
            {
                // ignore
            }

        }

        private void chkFullscreen_CheckedChanged(object sender, EventArgs e)
        {
            // When fullscreen enabled: allow multi-screen, disable resolution trackbar
            // and dim the resolution labels. Reverse when disabled.
            try
            {
                var chk = sender as CheckBox;
                if (chk == null) return;

                if (this.chkMultiScreen != null)
                    this.chkMultiScreen.Enabled = chk.Checked;

                if (this.trackRes != null)
                    this.trackRes.Enabled = !chk.Checked;

                var gray = SystemColors.GrayText;
                var normal = SystemColors.ControlText;
                if (this.lblNowRes != null)
                    this.lblNowRes.ForeColor = chk.Checked ? gray : normal;
                if (this.lblHintChoosingRes != null)
                    this.lblHintChoosingRes.ForeColor = chk.Checked ? gray : normal;
            }
            catch
            {
                // ignore
            }
        }

        private void trackRes_Scroll(object sender, EventArgs e)
        {
            // update the resolution label "lblNowRes" when the trackbar is scrolled
            string[] resolutions = new string[]
            {
                "640x480",
                "800x600",
                "1024x768",
                "1280x720",
                "1280x768",
                "1280x800",
                "1280x1024",
                "1366x768",
                "1440x900",
                "1400x1050",
                "1680x1050",
                "1920x1080"
            };

            if (this.trackRes != null && this.lblNowRes != null)
            {
                int idx = this.trackRes.Value;
                if (idx >= 0 && idx < resolutions.Length)
                {
                    this.lblNowRes.Text = resolutions[idx];
                }
            }
        }

        private void btnSaveNew_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dlg = new SaveRdpDialog())
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.FileName))
                        return;
                    string inputName = dlg.FileName;
                    foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                        inputName = inputName.Replace(c.ToString(), "_");
                    string fileName = inputName + ".rdp";
                    bool savePassword = dlg.SavePassword;

                    // Ensure directory exists
                    string rdpDir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "RDP");
                    if (!System.IO.Directory.Exists(rdpDir))
                        System.IO.Directory.CreateDirectory(rdpDir);

                    string filePath = System.IO.Path.Combine(rdpDir, fileName);
                    SaveRdpFile(filePath, savePassword);

                    if (!rdpFileMap.ContainsKey(inputName))
                    {
                        rdpFileMap[inputName] = filePath;
                        listSavedPreset.Items.Add(inputName);
                    }

                    MessageBox.Show($"RDP 設定已儲存至: {filePath}", "儲存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    RefreshPresetList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"儲存 RDP 設定時發生錯誤: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.listSavedPreset.SelectedItem == null)
                {
                    MessageBox.Show("請先選擇要儲存的設定檔。", "未選取", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string name = this.listSavedPreset.SelectedItem.ToString();
                if (!rdpFileMap.ContainsKey(name))
                {
                    MessageBox.Show("找不到對應的檔案路徑。", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string filePath = rdpFileMap[name];

                SaveRdpFile(filePath, includePassword: true);
                MessageBox.Show($"已將變更儲存至: {filePath}", "儲存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshPresetList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"儲存失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.listSavedPreset.SelectedItem == null)
                {
                    MessageBox.Show("請先選擇要刪除的設定檔。", "未選取", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string name = this.listSavedPreset.SelectedItem.ToString();
                if (!rdpFileMap.ContainsKey(name))
                {
                    MessageBox.Show("找不到對應的檔案路徑。", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string filePath = rdpFileMap[name];

                // Move to recycle bin using SHFileOperation
                bool ok = MoveToRecycleBin(filePath);
                if (!ok) throw new Exception("無法移動至資源回收筒。請確認檔案權限。");

                MessageBox.Show($"已將檔案移至資源回收筒: {filePath}", "刪除成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshPresetList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刪除失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"BetterRDP_{Guid.NewGuid():N}.rdp");
                SaveRdpFile(tempPath, includePassword: true);

                var psi = new ProcessStartInfo
                {
                    FileName = "mstsc.exe",
                    Arguments = $"\"{tempPath}\"",
                    UseShellExecute = true
                };
                Process.Start(psi);

                Task.Run(() =>
                {
                    try
                    {
                        Thread.Sleep(5000);
                        if (System.IO.File.Exists(tempPath))
                        {
                            System.IO.File.Delete(tempPath);
                        }
                    }
                    catch { }
                });

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"無法啟動遠端連線: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Dao-you/BetterRDPLauncher",
                UseShellExecute = true
            });
        }

        private void buttonShowLicense_Click(object sender, EventArgs e)
        {
            // 讀取 LICENSE.txt 文件內容
            string licenseFilePath = Path.Combine(Application.StartupPath, "LICENSE_INFO.txt");

            if (File.Exists(licenseFilePath))
            {
                // 讀取授權資訊並顯示在 MessageBox 中
                string licenseContent = File.ReadAllText(licenseFilePath, Encoding.UTF8);
                MessageBox.Show(licenseContent, "開放原始碼授權", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("授權文件未找到。", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdateTreeSavedList_Click(object sender, EventArgs e)
        {
            RefreshPresetList();
        }
    }
}
