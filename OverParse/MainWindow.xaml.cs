﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using HotKeyFrame;
using System.Net;

namespace OverParse
{
    public partial class MainWindow : Window
    {
        public static Dictionary<string, string> skillDict = new Dictionary<string, string>();
        public static string[] jaignoreskill;
        public static string[] critignoreskill;
        public DispatcherTimer damageTimer = new DispatcherTimer();
        private Log encounterlog;
        private List<Combatant> lastCombatants = new List<Combatant>();
        private List<string> sessionLogFilenames = new List<string>();
        private string lastStatus = "";
        private HotKey hotkey1;
        private HotKey hotkey2;
        private HotKey hotkey3;
        private IntPtr hwndcontainer;
        private string updatemsg = " - Update check Error.";
        List<Combatant> workingList = new List<Combatant>();
        Process thisProcess = Process.GetCurrentProcess();
        

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;  // Get this window's handle
            hwndcontainer = hwnd;
        }

        public MainWindow()
        {
            InitializeComponent();

            Dispatcher.UnhandledException += Panic;
            Abouttext.Text = "OverParse v3.0.3";
            LowResources.IsChecked = Properties.Settings.Default.LowResources;
            CPUdraw.IsChecked = Properties.Settings.Default.CPUdraw;
            if (Properties.Settings.Default.LowResources) { thisProcess.PriorityClass = ProcessPriorityClass.Idle; }
            if (Properties.Settings.Default.CPUdraw) { RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly; }

            try { Directory.CreateDirectory("Logs"); }
            catch
            {
                MessageBox.Show("OverParseにアクセス権が無く、ログの保存が出来ません！\n管理者としてOverParseを実行してみるか、システムのアクセス権を確認して下さい！\nOverParseを別のフォルダーに移動してみるのも良いかも知れません。", "OverParse Setup", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            if (Properties.Settings.Default.UpgradeRequired && !Properties.Settings.Default.ResetInvoked)
            {
                //Console.WriteLine("Upgrading settings");
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
            }

            Properties.Settings.Default.ResetInvoked = false;

            Top = Properties.Settings.Default.Top;
            Left = Properties.Settings.Default.Left;
            Height = Properties.Settings.Default.Height;
            Width = Properties.Settings.Default.Width;

            //Console.WriteLine("Applying UI settings");
            //Console.WriteLine(this.Top = Properties.Settings.Default.Top);
            //Console.WriteLine(this.Left = Properties.Settings.Default.Left);
            //Console.WriteLine(this.Height = Properties.Settings.Default.Height);
            //Console.WriteLine(this.Width = Properties.Settings.Default.Width);

            bool outOfBounds = (Left <= SystemParameters.VirtualScreenLeft - Width) ||
                (Top <= SystemParameters.VirtualScreenTop - Height) ||
                (SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth <= Left) ||
                (SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight <= Top);

            if (outOfBounds)
            {
                //Console.WriteLine("Window's off-screen, resetting");
                Top = 50;
                Left = 50;
            }

            AutoEndEncounters.IsChecked = Properties.Settings.Default.AutoEndEncounters;
            SetEncounterTimeout.IsEnabled = AutoEndEncounters.IsChecked;
            SeparateZanverse.IsChecked = Properties.Settings.Default.SeparateZanverse;
            SeparateFinish.IsChecked = Properties.Settings.Default.SeparateFinish;
            SeparateAIS.IsChecked = Properties.Settings.Default.SeparateAIS;
            SeparateDB.IsChecked = Properties.Settings.Default.SeparateDB;
            SeparateRide.IsChecked = Properties.Settings.Default.SeparateRide;
            SeparatePwp.IsChecked = Properties.Settings.Default.SeparatePwp;
            SeparateLsw.IsChecked = Properties.Settings.Default.SeparateLsw;
            //NoMyName.IsChecked = Properties.Settings.Default.NomyName;
            Onlyme.IsChecked = Properties.Settings.Default.Onlyme;
            DPSFormat.IsChecked = Properties.Settings.Default.DPSformat;
            Nodecimal.IsChecked = Properties.Settings.Default.Nodecimal;
            ClickthroughMode.IsChecked = Properties.Settings.Default.ClickthroughEnabled;
            LogToClipboard.IsChecked = Properties.Settings.Default.LogToClipboard;
            AlwaysOnTop.IsChecked = Properties.Settings.Default.AlwaysOnTop;
            AutoHideWindow.IsChecked = Properties.Settings.Default.AutoHideWindow;

            ShowDamageGraph.IsChecked = Properties.Settings.Default.ShowDamageGraph; ShowDamageGraph_Click(null, null);
            JAcfg.IsChecked = Properties.Settings.Default.JAcfg; JA_Click(null, null);
            Cricfg.IsChecked = Properties.Settings.Default.Criticalcfg; Critical_Click(null, null);
            CompactMode.IsChecked = Properties.Settings.Default.CompactMode; CompactMode_Click(null, null);
            AnonymizeNames.IsChecked = Properties.Settings.Default.AnonymizeNames; AnonymizeNames_Click(null, null);
            HighlightYourDamage.IsChecked = Properties.Settings.Default.HighlightYourDamage; HighlightYourDamage_Click(null, null);
            Clock.IsChecked = Properties.Settings.Default.Clock; Clock_Click(null, null);
            HandleWindowOpacity(); HandleListOpacity(); SeparateAIS_Click(null, null);
            HandleWindowOpacity(); HandleListOpacity(); SeparateDB_Click(null, null);
            HandleWindowOpacity(); HandleListOpacity(); SeparateRide_Click(null, null);
            HandleWindowOpacity(); HandleListOpacity(); SeparatePwp_Click(null, null);
            HandleWindowOpacity(); HandleListOpacity(); SeparateLsw_Click(null, null);

            //Console.WriteLine($"Launch method: {Properties.Settings.Default.LaunchMethod}");

            if (Properties.Settings.Default.Maximized)
            {
                WindowState = WindowState.Maximized;
            }

            try
            {
                hotkey1 = new HotKey(this);
                hotkey2 = new HotKey(this);
                hotkey3 = new HotKey(this);
                hotkey1.Regist(ModifierKeys.Control | ModifierKeys.Shift, Key.E, new EventHandler(EndEncounter_Key), 0x0071);
                hotkey2.Regist(ModifierKeys.Control | ModifierKeys.Shift, Key.R, new EventHandler(EndEncounterNoLog_Key), 0x0072);
                hotkey3.Regist(ModifierKeys.Control | ModifierKeys.Shift, Key.D, new EventHandler(DefaultWindowSize_Key), 0x0073);
            } catch {
                MessageBox.Show("OverParseはホットキーを初期化出来ませんでした。　多重起動していないか確認して下さい！\nプログラムは引き続き使用できますが、ホットキーは反応しません。", "OverParse Setup", MessageBoxButton.OK, MessageBoxImage.Information);
            }


            //new_version_check
            try
            {
                const string url = "https://api.github.com/repos/remon-7l/overparse/releases/latest";
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "Mozilla / 5.0 OverParse / 3.0.2";
                request.GetResponseAsync().ContinueWith(task => {
                    var response = task.Result;
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        string content = reader.ReadToEnd();
                        var m = Regex.Match(content, @"tag_name.........");
                        var v = Regex.Match(m.Value, @"\d.\d.\d");
                        var newVersion = Version.Parse(v.ToString());
                        var nowVersion = Version.Parse("3.0.3");
                        if (newVersion <= nowVersion) { updatemsg = ""; }
                        if (nowVersion < newVersion) { updatemsg = " - New version available(" + v.ToString() + ")"; }
                    }
                });
            } catch {
            }

        //skills.csv
        string[] skills;
            try
            {
                skills = File.ReadAllLines("skills.csv");
            } catch {
                //Console.WriteLine($"skills.csv update failed: {ex.ToString()}");
                    MessageBox.Show("skills.csvが存在しません。\n全ての最大ダメージはUnknownとなります。", "OverParse Setup", MessageBoxButton.OK, MessageBoxImage.Information);
                    skills = new string[0];
            }

            try {
                jaignoreskill = File.ReadAllLines("jaignoreskills.csv");
            } catch (Exception e) {
                MessageBox.Show(e.ToString());
                jaignoreskill = new string[] { "12345678900" }; //nullだとエラーが出るので適当な値
            }

            try {
                critignoreskill = File.ReadAllLines("critignoreskills.csv");
            } catch (Exception e) {
                MessageBox.Show(e.ToString());
                critignoreskill = new string[] { "12345678900" }; //nullだとエラーが出るので適当な値
            }

            foreach (string s in skills)
            {
                string[] split = s.Split(',');
                if (split.Length > 1)
                {
                    skillDict.Add(split[1], split[0]);
                }
            }

            //Initializing default log
            //and installing...
            encounterlog = new Log(Properties.Settings.Default.Path);
            UpdateForm(null, null);

            //Initializing damageTimer
            damageTimer.Tick += new EventHandler(UpdateForm);
            damageTimer.Interval = new TimeSpan(0, 0, 0, 0, Properties.Settings.Default.Updateinv);
            damageTimer.Start();

            //Initializing inactiveTimer
            DispatcherTimer inactiveTimer = new DispatcherTimer();
            inactiveTimer.Tick += new EventHandler(HideIfInactive);
            inactiveTimer.Interval = new TimeSpan(0, 0, 1);
            inactiveTimer.Start();

            //Initializing logCheckTimer
            DispatcherTimer logCheckTimer = new DispatcherTimer();
            logCheckTimer.Tick += new EventHandler(CheckForNewLog);
            logCheckTimer.Interval = new TimeSpan(0, 0, 1);
            logCheckTimer.Start();
        }

        private void HideIfInactive(object sender, EventArgs e)
        {
            if (!Properties.Settings.Default.AutoHideWindow)
                return;

            string title = WindowsServices.GetActiveWindowTitle();
            string[] relevant = { "OverParse", "OverParse Setup", "OverParse Error", "Encounter Timeout", "Phantasy Star Online 2" };

            if (!relevant.Contains(title))
            {
                Opacity = 0;
            } else {
                HandleWindowOpacity();
            }
        }

        private void CheckForNewLog(object sender, EventArgs e)
        {
            DirectoryInfo directory = encounterlog.logDirectory;
            if (!directory.Exists) { return; }
            if (directory.GetFiles().Count() == 0) { return; }

            FileInfo log = directory.GetFiles().Where(f => Regex.IsMatch(f.Name, @"\d+\.csv")).OrderByDescending(f => f.Name).First();

            if (log.Name != encounterlog.filename)
            {
                //Console.WriteLine($"Found a new log file ({log.Name}), switching...");
                encounterlog = new Log(Properties.Settings.Default.Path);
            }
        }

        private void Panic(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try { Directory.CreateDirectory("ErrorLogs"); }
            catch { MessageBox.Show("OverParseはDirectory<ErrorLogs>の作成に失敗しました。"); }
            string datetime = string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now);
            string filename = $"ErrorLogs/ErrorLogs - {datetime}.txt";
            string errorMessage1 = string.Format("{0}", e.Exception.Source);
            string errorMessage2 = string.Format("{0}", e.Exception.StackTrace);
            string errorMessage3 = string.Format("{0}", e.Exception.TargetSite);
            string errorMessage4 = string.Format("{0}", e.Exception.InnerException);
            string errorMessage5 = string.Format("{0}", e.Exception.Message);
            //=== UNHANDLED EXCEPTION ===
            //e.Exception.ToString()
            string elog = (errorMessage1 + "\n" + errorMessage2 + "\n" + errorMessage3 + "\n" + errorMessage4 + "\n" + errorMessage5);
            File.WriteAllText(filename, elog);
        }

        public void HandleWindowOpacity()
        {
            TheWindow.Opacity = Properties.Settings.Default.WindowOpacity;
            // ACHTUNG ACHTUNG ACHTUNG ACHTUNG ACHTUNG ACHTUNG ACHTUNG ACHTUNG
            WinOpacity_0.IsChecked = false;
            WinOpacity_25.IsChecked = false;
            Winopacity_50.IsChecked = false;
            WinOpacity_75.IsChecked = false;
            WinOpacity_100.IsChecked = false;

            if (Properties.Settings.Default.WindowOpacity == 0) { WinOpacity_0.IsChecked = true; }
            else if (Properties.Settings.Default.WindowOpacity == .25) { WinOpacity_25.IsChecked = true; }
            else if (Properties.Settings.Default.WindowOpacity == .50) { Winopacity_50.IsChecked = true; }
            else if (Properties.Settings.Default.WindowOpacity == .75) { WinOpacity_75.IsChecked = true; }
            else if (Properties.Settings.Default.WindowOpacity == 1) { WinOpacity_100.IsChecked = true; }
        }

        public void HandleListOpacity()
        {
            MainBack.Opacity = Properties.Settings.Default.ListOpacity;
            ListOpacity_0.IsChecked = false;
            ListOpacity_25.IsChecked = false;
            Listopacity_50.IsChecked = false;
            ListOpacity_75.IsChecked = false;
            ListOpacity_100.IsChecked = false;

            if (Properties.Settings.Default.ListOpacity == 0) { ListOpacity_0.IsChecked = true; }
            else if (Properties.Settings.Default.ListOpacity == .25) { ListOpacity_25.IsChecked = true; }
            else if (Properties.Settings.Default.ListOpacity == .50) { Listopacity_50.IsChecked = true; }
            else if (Properties.Settings.Default.ListOpacity == .75) { ListOpacity_75.IsChecked = true; }
            else if (Properties.Settings.Default.ListOpacity == 1) { ListOpacity_100.IsChecked = true; }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = AlwaysOnTop.IsChecked;
            if (Properties.Settings.Default.ClickthroughEnabled)
            {
                int extendedStyle = WindowsServices.GetWindowLong(hwndcontainer, WindowsServices.GWL_EXSTYLE);
                WindowsServices.SetWindowLong(hwndcontainer, WindowsServices.GWL_EXSTYLE, extendedStyle | WindowsServices.WS_EX_TRANSPARENT);
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            HandleWindowOpacity();
            Window window = (Window)sender;
            window.Topmost = AlwaysOnTop.IsChecked;
            if (Properties.Settings.Default.ClickthroughEnabled)
            {
                int extendedStyle = WindowsServices.GetWindowLong(hwndcontainer, WindowsServices.GWL_EXSTYLE);
                WindowsServices.SetWindowLong(hwndcontainer, WindowsServices.GWL_EXSTYLE, extendedStyle & ~WindowsServices.WS_EX_TRANSPARENT);
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized) { WindowState = WindowState.Normal; }
        }

        public void UpdateForm(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.Clock) { Datetime.Content = DateTime.Now.ToString("HH:mm:ss.ff"); }
            if (encounterlog == null) { return; }

            encounterlog.UpdateLog(this, null);
            EncounterStatus.Content = encounterlog.LogStatus();

            // every part of this section is fucking stupid

            // get a copy of the right combatants
            List<Combatant> targetList = (encounterlog.running ? encounterlog.combatants : lastCombatants);
            //workingList = new List<Combatant>();
            workingList.Clear();
            foreach (Combatant c in targetList)
            {
                Combatant temp = new Combatant(c.ID, c.Name, c.isTemporary);
                foreach (Attack a in c.Attacks)
                    temp.Attacks.Add(new Attack(a.ID, a.Damage, a.Timestamp,a.JA,a.Cri,a.Dmgd));
                temp.ActiveTime = c.ActiveTime;
                workingList.Add(temp);
            }

            // clear out the list
            CombatantData.Items.Clear();
            AllyData.Items.Clear();
            DBData.Items.Clear();
            LswData.Items.Clear();
            PwpData.Items.Clear();
            AisData.Items.Clear();
            RideData.Items.Clear();
            //workingList.RemoveAll(c => c.isTemporary != "no");

            // for zanverse dummy and status bar because WHAT IS GOOD STRUCTURE
            int elapsed = 0;
            Combatant stealActiveTimeDummy = workingList.FirstOrDefault();
            if (stealActiveTimeDummy != null) { elapsed = stealActiveTimeDummy.ActiveTime; }

            //Separate Part
            if (Properties.Settings.Default.SeparateAIS)
            {
                List<Combatant> pendingCombatants = new List<Combatant>();

                foreach (Combatant c in workingList)
                {
                    if (!c.IsAlly)
                        continue;
                    if (c.AisDamage > 0)
                    {
                        Combatant AISHolder = new Combatant(c.ID, "AIS|" + c.Name, "AIS");
                        List<Attack> targetAttacks = c.Attacks.Where(a => Combatant.AISAttackIDs.Contains(a.ID)).ToList();
                        c.Attacks = c.Attacks.Except(targetAttacks).ToList();
                        AISHolder.Attacks.AddRange(targetAttacks);
                        AISHolder.ActiveTime = elapsed;
                        pendingCombatants.Add(AISHolder);
                    }
                }
                workingList.AddRange(pendingCombatants);
            }

            if (Properties.Settings.Default.SeparateDB)
            {
                List<Combatant> pendingDBCombatants = new List<Combatant>();

                foreach (Combatant c in workingList)
                {
                    if (!c.IsAlly)
                        continue;
                    if (c.DBDamage > 0)
                    {
                        Combatant DBHolder = new Combatant(c.ID, "DB|" + c.Name, "DB");
                        List<Attack> targetAttacks = c.Attacks.Where(a => Combatant.DBAttackIDs.Contains(a.ID)).ToList();
                        c.Attacks = c.Attacks.Except(targetAttacks).ToList();
                        DBHolder.Attacks.AddRange(targetAttacks);
                        DBHolder.ActiveTime = elapsed;
                        pendingDBCombatants.Add(DBHolder);
                    }
                }
                workingList.AddRange(pendingDBCombatants);
            }

            if (Properties.Settings.Default.SeparateRide)
            {
                List<Combatant> pendingRideCombatants = new List<Combatant>();

                foreach (Combatant c in workingList)
                {
                    if (!c.IsAlly)
                        continue;
                    if (c.RideDamage > 0)
                        {
                        Combatant RideHolder = new Combatant(c.ID, "Ride|" + c.Name, "Ride");
                        List<Attack> targetAttacks = c.Attacks.Where(a => Combatant.RideAttackIDs.Contains(a.ID)).ToList();
                        c.Attacks = c.Attacks.Except(targetAttacks).ToList();
                        RideHolder.Attacks.AddRange(targetAttacks);
                        RideHolder.ActiveTime = elapsed;
                        pendingRideCombatants.Add(RideHolder);
                    }
                }
                workingList.AddRange(pendingRideCombatants);
            }

            if (Properties.Settings.Default.SeparatePwp)
            {
                List<Combatant> pendingPwpCombatants = new List<Combatant>();

                foreach (Combatant c in workingList)
                {
                    if (!c.IsAlly)
                        continue;
                    if (c.PwpDamage > 0)
                    {
                        Combatant PhotonHolder = new Combatant(c.ID, "Pwp|" + c.Name, "Pwp");
                        List<Attack> targetAttacks = c.Attacks.Where(a => Combatant.PhotonAttackIDs.Contains(a.ID)).ToList();
                        c.Attacks = c.Attacks.Except(targetAttacks).ToList();
                        PhotonHolder.Attacks.AddRange(targetAttacks);
                        PhotonHolder.ActiveTime = elapsed;
                        pendingPwpCombatants.Add(PhotonHolder);
                    }
                }
                workingList.AddRange(pendingPwpCombatants);
            }

            if (Properties.Settings.Default.SeparateLsw)
            {
                List<Combatant> pendingLswCombatants = new List<Combatant>();

                foreach (Combatant c in workingList)
                {
                    if (!c.IsAlly)
                        continue;
                    if (c.LswDamage > 0)
                    {
                        Combatant LswHolder = new Combatant(c.ID, "Lsw|" + c.Name, "Lsw");
                        List<Attack> targetAttacks = c.Attacks.Where(a => Combatant.LaconiumAttackIDs.Contains(a.ID)).ToList();
                        c.Attacks = c.Attacks.Except(targetAttacks).ToList();
                        LswHolder.Attacks.AddRange(targetAttacks);
                        LswHolder.ActiveTime = elapsed;
                        pendingLswCombatants.Add(LswHolder);
                    }
                }
                workingList.AddRange(pendingLswCombatants);
            }

            //分けたものを含めて再ソート
            if (SeparateTab.SelectedIndex == 0) { workingList.Sort((x, y) => y.ReadDamage.CompareTo(x.ReadDamage)); }

            //ザンバースをプレイヤーとして認識させ、元の計算から除外
            Int64 totalZanverse = workingList.Sum(x => x.GetZanverseDamage);
            Int64 totalFinish = workingList.Sum(x => x.GetFinishDamage);

            if (Properties.Settings.Default.SeparateFinish && 0 < totalFinish)
            {
                Combatant finishHolder = new Combatant("99999998", "HTF Attacks", "HTF Attacks");
                foreach (Combatant c in workingList)
                {
                    List<Attack> targetAttacks = c.Attacks.Where(a => Combatant.FinishAttackIDs.Contains(a.ID)).ToList();
                    finishHolder.Attacks.AddRange(targetAttacks);
                    c.Attacks = c.Attacks.Except(targetAttacks).ToList();
                }
                finishHolder.ActiveTime = elapsed;
                workingList.Add(finishHolder);
            }

            if (Properties.Settings.Default.SeparateZanverse && 0 < totalZanverse)
            {
                Combatant zanverseHolder = new Combatant("99999999", "Zanverse", "Zanverse");
                foreach (Combatant c in workingList)
                {
                    List<Attack> targetAttacks = c.Attacks.Where(a => a.ID == "2106601422").ToList();
                    zanverseHolder.Attacks.AddRange(targetAttacks);
                    c.Attacks = c.Attacks.Except(targetAttacks).ToList();
                }
                zanverseHolder.ActiveTime = elapsed;
                workingList.Add(zanverseHolder);
            }

            // get group damage totals
            Int64 totalReadDamage = workingList.Sum(x => x.Damage);
            Int64 totalAllyDamage = workingList.Where(c => c.IsAlly).Sum(x => x.ReadDamage);
            Int64 totalDBDamage = workingList.Sum(x => x.DBDamage);
            Int64 totalLswDamage = workingList.Sum(x => x.LswDamage);
            Int64 totalPwpDamage = workingList.Sum(x => x.PwpDamage);
            Int64 totalAisDamage = workingList.Sum(x => x.AisDamage);
            Int64 totalRideDamage = workingList.Sum(x => x.RideDamage);

            // dps calcs!
            foreach (Combatant c in workingList)
            {
                c.PercentReadDPS = c.ReadDamage / (float)totalReadDamage * 100;
                c.AllyPct = c.AllyDamage / (float)totalAllyDamage * 100;
                c.DBPct = c.DBDamage / (float)totalDBDamage * 100;
                c.LswPct = c.LswDamage / (float)totalReadDamage * 100;
                c.PwpPct = c.PwpDamage / (float)totalPwpDamage * 100;
                c.AisPct = c.AisDamage / (float)totalReadDamage * 100;
                c.RidePct = c.RideDamage / (float)totalReadDamage * 100;
            }

            // damage graph stuff
            Combatant.maxShare = 0;

            foreach (Combatant c in workingList)
            {
                if ((c.IsAlly) && c.ReadDamage > Combatant.maxShare)
                    Combatant.maxShare = c.ReadDamage;

                bool filtered = true;
                if (Properties.Settings.Default.SeparateAIS || Properties.Settings.Default.SeparateDB || Properties.Settings.Default.SeparateRide || Properties.Settings.Default.SeparatePwp || Properties.Settings.Default.SeparateLsw)
                {
                    if (c.IsAlly && c.isTemporary == "no" && !HidePlayers.IsChecked)
                        filtered = false;
                    if (c.IsAlly && c.isTemporary == "AIS" && !HideAIS.IsChecked)
                        filtered = false;
                    if (c.IsAlly && c.isTemporary == "DB" && !HideDB.IsChecked)
                        filtered = false;
                    if (c.IsAlly && c.isTemporary == "Ride" && !HideRide.IsChecked)
                        filtered = false;
                    if (c.IsAlly && c.isTemporary == "Pwp" && !HidePwp.IsChecked)
                        filtered = false;
                    if (c.IsAlly && c.isTemporary == "Lsw" && !HideLsw.IsChecked)
                        filtered = false;
                    if (c.IsZanverse)
                        filtered = false;
                    if (c.IsFinish)
                        filtered = false;
                }
                else
                {
                    if ((c.IsAlly || c.IsZanverse || c.IsFinish || !FilterPlayers.IsChecked) && (c.Damage > 0))
                        filtered = false;
                }

                if (!filtered && (c.Damage > 0) && (SeparateTab.SelectedIndex == 0)) { CombatantData.Items.Add(c); }
                if ((c.AllyDamage > 0) && (SeparateTab.SelectedIndex == 1)) { workingList.Sort((x, y) => y.AllyDamage.CompareTo(x.AllyDamage)); AllyData.Items.Add(c); }
                if ((c.DBDamage > 0) && (SeparateTab.SelectedIndex == 2) ) { workingList.Sort((x, y) => y.DBDamage.CompareTo(x.DBDamage)); DBData.Items.Add(c); }
                if ((c.LswDamage > 0) && (SeparateTab.SelectedIndex == 3)) { workingList.Sort((x, y) => y.LswDamage.CompareTo(x.LswDamage)); LswData.Items.Add(c); }
                if ((c.PwpDamage > 0) && (SeparateTab.SelectedIndex == 4)) { workingList.Sort((x, y) => y.PwpDamage.CompareTo(x.PwpDamage)); PwpData.Items.Add(c); }
                if ((c.AisDamage > 0) && (SeparateTab.SelectedIndex == 5)) { workingList.Sort((x, y) => y.AisDamage.CompareTo(x.AisDamage)); AisData.Items.Add(c); }
                if ((c.RideDamage > 0) && (SeparateTab.SelectedIndex == 6)) { workingList.Sort((x, y) => y.RideDamage.CompareTo(x.RideDamage)); RideData.Items.Add(c); }
            }


            // status pane updates
            EncounterIndicator.Fill = new SolidColorBrush(Color.FromArgb(192, 255, 128, 128));
            EncounterStatus.Content = encounterlog.LogStatus();

            if (encounterlog.valid && encounterlog.notEmpty)
            {
                EncounterIndicator.Fill = new SolidColorBrush(Color.FromArgb(192, 64, 192, 64));
                EncounterStatus.Content = $"Waiting - {lastStatus}";
                if (lastStatus == "")
                    EncounterStatus.Content = "Waiting... - " + encounterlog.filename + updatemsg;

                CombatantData.Items.Refresh();
                AllyData.Items.Refresh();
                DBData.Items.Refresh();
                LswData.Items.Refresh();
                PwpData.Items.Refresh();
                AisData.Items.Refresh();
                RideData.Items.Refresh();
            }

            if (encounterlog.running)
            {
                EncounterIndicator.Fill = new SolidColorBrush(Color.FromArgb(192, 0, 192, 255));

                TimeSpan timespan = TimeSpan.FromSeconds(elapsed);
                string timer = timespan.ToString(@"h\:mm\:ss");
                EncounterStatus.Content = $"{timer}";

                float totalDPS = totalReadDamage / (float)elapsed;

                if (totalDPS > 0)
                    EncounterStatus.Content += $" - Total : {totalReadDamage.ToString("N0")}" + $" - {totalDPS.ToString("N0")} DPS";

                if (!Properties.Settings.Default.SeparateZanverse)
                    EncounterStatus.Content += $" - Zanverse : {totalZanverse.ToString("N0")}";

                lastStatus = EncounterStatus.Content.ToString();
            }

            // autoend
            if (encounterlog.running && Properties.Settings.Default.AutoEndEncounters)
            {
                int unixTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                if ((unixTimestamp - encounterlog.newTimestamp) >= Properties.Settings.Default.EncounterTimeout) { EndEncounter_Click(null, null); }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Closing...

            if (!Properties.Settings.Default.ResetInvoked)
            {
                if (WindowState == WindowState.Maximized)
                {
                    Properties.Settings.Default.Top = RestoreBounds.Top;
                    Properties.Settings.Default.Left = RestoreBounds.Left;
                    Properties.Settings.Default.Height = RestoreBounds.Height;
                    Properties.Settings.Default.Width = RestoreBounds.Width;
                    Properties.Settings.Default.Maximized = true;
                }
                else
                {
                    Properties.Settings.Default.Top = Top;
                    Properties.Settings.Default.Left = Left;
                    Properties.Settings.Default.Height = Height;
                    Properties.Settings.Default.Width = Width;
                    Properties.Settings.Default.Maximized = false;
                }
            }

            encounterlog.WriteLog();

            Properties.Settings.Default.Save();
        }


        private void OpenRecentLog_Click(object sender, RoutedEventArgs e)
        {
            string filename = sessionLogFilenames[SessionLogs.Items.IndexOf((e.OriginalSource as MenuItem))];
            //attempting to open
            Process.Start(Directory.GetCurrentDirectory() + "\\" + filename);
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            Application.Current.Shutdown();
        }

        /*private void WindowStats_Click(object sender, RoutedEventArgs e)
        {
            AisData.Items.Add(workingList);
            string result = "";
            result += $"Name: {AisNameColumn.Width.ToString()}  Percent: {AisPercentColumn.Width.ToString()}";
            result += $"Name: {DmgHC.ActualWidth.ToString()}  Percent: {DPSHC.ActualWidth.ToString()}";
            result += $"Name: {JAHC.ActualWidth.ToString()}  Percent: {CriHC.ActualWidth.ToString()}";
            result += $"maxdmg: {MdmgHC.ActualWidth.ToString()}  Atk: {AtkHC.ActualWidth.ToString()}";
            result += $"Tab: {TabHC.ActualWidth.ToString()}  Percent: {PercentHC.ActualWidth.ToString()}";
            result += $"menu bar: {MenuBar.Width.ToString()} width {MenuBar.Height.ToString()} height\n";
            result += $"menu bar: {MenuBar.Padding} padding {MenuBar.Margin} margin\n";
            result += $"menu item: {AutoEndEncounters.Foreground} fg {AutoEndEncounters.Background} bg\n";
            MessageBox.Show(result);
        }*/

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); }
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var data = (ListViewItem)sender;
            var item = CombatantData.ContainerFromElement((DependencyObject)e.OriginalSource) as ListViewItem;
            Detalis f = new Detalis(data.ToString(), "value") { Owner = this };
            f.Show();
        }
    }
}
