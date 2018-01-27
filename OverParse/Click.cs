﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace OverParse
{
    public partial class MainWindow : Window
    {

        private void EndEncounter_Click(object sender, RoutedEventArgs e)
        {
            //Ending encounter
            bool temp = Properties.Settings.Default.AutoEndEncounters;
            Properties.Settings.Default.AutoEndEncounters = false;
            UpdateForm(null, null); // I'M FUCKING STUPID
            Properties.Settings.Default.AutoEndEncounters = temp;
            encounterlog.backupCombatants = encounterlog.combatants;


            List<Combatant> workingListCopy = new List<Combatant>();
            foreach (Combatant c in workingList)
            {
                Combatant temp2 = new Combatant(c.ID, c.Name, c.isTemporary);
                foreach (Attack a in c.Attacks)
                {
                    temp2.Attacks.Add(new Attack(a.ID, a.Damage, a.Timestamp, a.JA, a.Cri, a.Dmgd,a.TargetID));
                }
                temp2.ActiveTime = c.ActiveTime;
                temp2.PercentReadDPS = c.PercentReadDPS;
                workingListCopy.Add(temp2);
            }
            //Saving last combatant list"
            lastCombatants = encounterlog.combatants;
            encounterlog.combatants = workingListCopy;
            string filename = encounterlog.WriteLog();
            if (filename != null)
            {
                if ((SessionLogs.Items[0] as MenuItem).Name == "SessionLogPlaceholder")
                    SessionLogs.Items.Clear();
                int items = SessionLogs.Items.Count;

                string prettyName = filename.Split('/').LastOrDefault();

                sessionLogFilenames.Add(filename);

                var menuItem = new MenuItem() { Name = "SessionLog_" + items.ToString(), Header = prettyName };
                menuItem.Click += OpenRecentLog_Click;
                SessionLogs.Items.Add(menuItem);
            }
            if (Properties.Settings.Default.LogToClipboard)
            {
                encounterlog.WriteClipboard();
            }

            encounterlog = new Log(Properties.Settings.Default.Path);
            UpdateForm(null, null);
        }

        public void EndEncounter_Key(object sender, EventArgs e)
        {
            //Encounter hotkey pressed
            EndEncounter_Click(null, null);
        }

        private void EndEncounterNoLog_Click(object sender, RoutedEventArgs e)
        {
            //Ending encounter (no log)
            bool temp = Properties.Settings.Default.AutoEndEncounters;
            Properties.Settings.Default.AutoEndEncounters = false;
            UpdateForm(null, null);
            Properties.Settings.Default.AutoEndEncounters = temp;
            //Reinitializing log
            encounterlog = new Log(Properties.Settings.Default.Path);
            UpdateForm(null, null);
        }

        private void EndEncounterNoLog_Key(object sender, EventArgs e)
        {
            //Encounter hotkey (no log) pressed
            EndEncounterNoLog_Click(null, null);
        }

        /*private void Questcheck_Click(object sender, RoutedEventArgs e)
        {
            QuestName quest = new QuestName();
            quest.Show();
        }*/

        private void AutoEndEncounters_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoEndEncounters = AutoEndEncounters.IsChecked;
            SetEncounterTimeout.IsEnabled = AutoEndEncounters.IsChecked;
        }

        private void SetEncounterTimeout_Click(object sender, RoutedEventArgs e)
        {
            AlwaysOnTop.IsChecked = false;
            Inputbox input = new Inputbox("Encounter Timeout", "何秒経過すればエンカウントを終了させますか？", Properties.Settings.Default.EncounterTimeout.ToString()) { Owner = this };
            input.ShowDialog();
            if (Int32.TryParse(input.ResultText, out int x))
            {
                if (x > 0) { Properties.Settings.Default.EncounterTimeout = x; }
                else { MessageBox.Show("Error"); }
            }
            else
            {
                if (input.ResultText.Length > 0) { MessageBox.Show("Couldn't parse your input. Enter only a number."); }
            }

            AlwaysOnTop.IsChecked = Properties.Settings.Default.AlwaysOnTop;
        }

        private void LogToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.LogToClipboard = LogToClipboard.IsChecked;
        }

        private void OpenLogsFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Directory.GetCurrentDirectory() + "\\Logs");
        }

        private void FilterPlayers_Click(object sender, RoutedEventArgs e)
        {
            UpdateForm(null, null);
        }

        private void SeparateZanverse_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SeparateZanverse = SeparateZanverse.IsChecked;
            UpdateForm(null, null);
        }

        private void SeparateFinish_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SeparateFinish = SeparateFinish.IsChecked;
            UpdateForm(null, null);
        }

        private void SeparateAIS_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SeparateAIS = SeparateAIS.IsChecked;
            HideAIS.IsEnabled = SeparateAIS.IsChecked;
            HidePlayers.IsEnabled = (SeparateAIS.IsChecked || SeparateDB.IsChecked || SeparateRide.IsChecked || SeparatePwp.IsChecked || SeparateLsw.IsChecked);
            UpdateForm(null, null);
        }

        private void SeparateDB_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SeparateDB = SeparateDB.IsChecked;
            HideDB.IsEnabled = SeparateDB.IsChecked;
            HidePlayers.IsEnabled = (SeparateAIS.IsChecked || SeparateDB.IsChecked || SeparateRide.IsChecked || SeparatePwp.IsChecked || SeparateLsw.IsChecked);
            UpdateForm(null, null);
        }

        private void SeparateRide_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SeparateRide = SeparateRide.IsChecked;
            HideRide.IsEnabled = SeparateRide.IsChecked;
            HidePlayers.IsEnabled = (SeparateAIS.IsChecked || SeparateDB.IsChecked || SeparateRide.IsChecked || SeparatePwp.IsChecked || SeparateLsw.IsChecked);
            UpdateForm(null, null);
        }

        private void SeparatePwp_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SeparatePwp = SeparatePwp.IsChecked;
            HidePwp.IsEnabled = SeparatePwp.IsChecked;
            HidePlayers.IsEnabled = (SeparateAIS.IsChecked || SeparateDB.IsChecked || SeparateRide.IsChecked || SeparatePwp.IsChecked || SeparateLsw.IsChecked);
            UpdateForm(null, null);
        }

        private void SeparateLsw_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SeparateLsw = SeparateLsw.IsChecked;
            HideLsw.IsEnabled = SeparateLsw.IsChecked;
            HidePlayers.IsEnabled = (SeparateAIS.IsChecked || SeparateDB.IsChecked || SeparateRide.IsChecked || SeparatePwp.IsChecked || SeparateLsw.IsChecked);
            UpdateForm(null, null);
        }

        private void HidePlayers_Click(object sender, RoutedEventArgs e)
        {
            if (HidePlayers.IsChecked)
            {
                HideAIS.IsChecked = false;
                HideDB.IsChecked = false;
                HideRide.IsChecked = false;
                HidePwp.IsChecked = false;
            }
            UpdateForm(null, null);
        }

        private void HideAIS_Click(object sender, RoutedEventArgs e)
        {
            if (HideAIS.IsChecked) { HidePlayers.IsChecked = false; }
            UpdateForm(null, null);
        }

        private void HideDB_Click(object sender, RoutedEventArgs e)
        {
            if (HideDB.IsChecked) { HidePlayers.IsChecked = false; }
            UpdateForm(null, null);
        }

        private void HideRide_Click(object sender, RoutedEventArgs e)
        {
            if (HideRide.IsChecked) { HidePlayers.IsChecked = false; }
            UpdateForm(null, null);
        }

        private void HidePwp_Click(object sender, RoutedEventArgs e)
        {
            if (HidePwp.IsChecked) { HidePlayers.IsChecked = false; }
            UpdateForm(null, null);
        }

        private void HideLsw_Click(object sender, RoutedEventArgs e)
        {
            if (HideLsw.IsChecked) { HidePlayers.IsChecked = false; }
            UpdateForm(null, null);
        }

        private void AnonymizeNames_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AnonymizeNames = AnonymizeNames.IsChecked;
            UpdateForm(null, null);
        }

        private void Onlyme_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Onlyme = Onlyme.IsChecked;
            UpdateForm(null, null);
        }

        private void DPSFormat_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DPSformat = DPSFormat.IsChecked;
            UpdateForm(null, null);
        }

        private void Nodecimal_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Nodecimal = Nodecimal.IsChecked;
            UpdateForm(null, null);
        }

        private void ChangeInterval_Click(object sender, RoutedEventArgs e)
        {
            AlwaysOnTop.IsChecked = false;
            Inputbox input = new Inputbox("OverParse", ".csvファイルの読み取り間隔を変更します... (単位:ms)", Properties.Settings.Default.Updateinv.ToString()) { Owner = this };
            input.ShowDialog();
            if (Int32.TryParse(input.ResultText, out int x))
            {
                if (x > 50) { damageTimer.Interval = new TimeSpan(0, 0, 0, 0, x); Properties.Settings.Default.Updateinv = x;}
                else { MessageBox.Show("Error"); }
            } else {
                if (input.ResultText.Length > 0) { MessageBox.Show("Couldn't parse your input. Enter only a number."); }
            }

            AlwaysOnTop.IsChecked = Properties.Settings.Default.AlwaysOnTop;
        }

        private void DefaultWindowSize_Click(object sender, RoutedEventArgs e)
        {
            Height = 275;
            Width = 670;
        }

        private void DefaultWindowSize_Key(object sender, EventArgs e)
        {
            Height = 275;
            Width = 670;
        }

        private void Japanese_Click(object sender, RoutedEventArgs e) => Properties.Settings.Default.Language = "ja-JP";

        private void English_Click(object sender, RoutedEventArgs e) => Properties.Settings.Default.Language = "en-US";

        private void ChangeFont_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new emanual.Wpf.Dialogs.FontDialogEx() { Owner = this };

            var font = new emanual.Wpf.Dialogs.ToolFont
            {
                FontFamily = CombatantData.FontFamily,
                FontSize = CombatantData.FontSize,
                FontWeight = CombatantData.FontWeight,
                FontStyle = CombatantData.FontStyle,
                FontStretch = CombatantData.FontStretch
            };

            // 言語情報を保存するため、textBox1 の Tag プロパティに設定した
            if (CombatantData.Tag == null)
                font.FontLanguage = "ja-jp";
            else
                font.FontLanguage = (string)CombatantData.Tag;

            dlg.Font = font;
            dlg.SampleText = "Sample サンプル\nPSO2\nOverParse";

            if (dlg.ShowDialog().Value)
            {
                dlg.SetPropertyToTargetObject(CombatantData);
                CombatantData.Tag = dlg.Font.FontLanguage;
            }
        }

        private void JA_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.JAcfg = JAcfg.IsChecked;
            if (Properties.Settings.Default.JAcfg)
            {
                CombatantView.Columns.Remove(JAColumn);
                JAHC.Width = new GridLength(0);
            }
            else
            {
                CombatantView.Columns.Remove(JAColumn);
                CombatantView.Columns.Remove(CriColumn);
                CombatantView.Columns.Remove(HColumn);
                CombatantView.Columns.Remove(MaxHitColumn);
                CombatantView.Columns.Add(JAColumn);
                if (!Properties.Settings.Default.Criticalcfg) { CombatantView.Columns.Add(CriColumn); }
                CombatantView.Columns.Add(HColumn);
                if (!Properties.Settings.Default.CompactMode) { CombatantView.Columns.Add(MaxHitColumn); }
                if (Properties.Settings.Default.Variable) { JAHC.Width = new GridLength(0.4, GridUnitType.Star); } else { JAHC.Width = new GridLength(39); }
            }
            UpdateForm(null, null);
        }

        private void Critical_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Criticalcfg = Cricfg.IsChecked;
            if (Cricfg.IsChecked)
            {
                CombatantView.Columns.Remove(CriColumn);
                CriHC.Width = new GridLength(0);
            }
            else
            {
                CombatantView.Columns.Remove(CriColumn);
                CombatantView.Columns.Remove(HColumn);
                CombatantView.Columns.Remove(MaxHitColumn);
                CombatantView.Columns.Add(CriColumn);
                CombatantView.Columns.Add(HColumn);
                if (!Properties.Settings.Default.CompactMode) { CombatantView.Columns.Add(MaxHitColumn); }
                if (Properties.Settings.Default.Variable) { CriHC.Width = new GridLength(0.4, GridUnitType.Star); } else { CriHC.Width = new GridLength(39); }
            }
            UpdateForm(null, null);
        }

        private void CompactMode_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CompactMode = CompactMode.IsChecked;
            if (CompactMode.IsChecked)
            {
                CombatantView.Columns.Remove(MaxHitColumn);
                AtkHC.Width = new GridLength(0);
            }
            else
            {
                CombatantView.Columns.Remove(MaxHitColumn);
                CombatantView.Columns.Add(MaxHitColumn);
                AtkHC.Width = new GridLength(1.7, GridUnitType.Star);
            }
            UpdateForm(null, null);
        }

        private void VariableColumn_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Variable = Variable.IsChecked;
            if (Variable.IsChecked)
            {
                PercentHC.Width = new GridLength(0.4, GridUnitType.Star);
                DmgHC.Width = new GridLength(0.8, GridUnitType.Star);
                DmgDHC.Width = new GridLength(0.6, GridUnitType.Star);
                DPSHC.Width = new GridLength(0.6, GridUnitType.Star);
                if (Properties.Settings.Default.JAcfg) { JAHC.Width = new GridLength(0); } else { JAHC.Width = new GridLength(0.4, GridUnitType.Star); }
                if (Properties.Settings.Default.Criticalcfg) { CriHC.Width = new GridLength(0); } else { CriHC.Width = new GridLength(0.4, GridUnitType.Star); }
                MdmgHC.Width = new GridLength(0.6, GridUnitType.Star);
            } else
            {
                PercentHC.Width = new GridLength(39);
                DmgHC.Width = new GridLength(78);
                DmgDHC.Width = new GridLength(56);
                DPSHC.Width = new GridLength(56);
                if (Properties.Settings.Default.JAcfg) { JAHC.Width = new GridLength(0); } else { JAHC.Width = new GridLength(39); }
                if (Properties.Settings.Default.Criticalcfg) { CriHC.Width = new GridLength(0); } else { CriHC.Width = new GridLength(39); }
                MdmgHC.Width = new GridLength(62);
            }
        }

        private void ShowDamageGraph_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ShowDamageGraph = ShowDamageGraph.IsChecked;
            UpdateForm(null, null);
        }

        private void HighlightYourDamage_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.HighlightYourDamage = HighlightYourDamage.IsChecked;
            UpdateForm(null, null);
        }

        private void WindowOpacity_0_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.WindowOpacity = 0;
            HandleWindowOpacity();
        }

        private void WindowOpacity_25_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.WindowOpacity = .25;
            HandleWindowOpacity();
        }

        private void WindowOpacity_50_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.WindowOpacity = .50;
            HandleWindowOpacity();
        }

        private void WindowOpacity_75_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.WindowOpacity = .75;
            HandleWindowOpacity();
        }

        private void WindowOpacity_100_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.WindowOpacity = 1;
            HandleWindowOpacity();
        }

        private void ListOpacity_0_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ListOpacity = 0;
            HandleListOpacity();
        }

        private void ListOpacity_25_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ListOpacity = .25;
            HandleListOpacity();
        }

        private void ListOpacity_50_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ListOpacity = .50;
            HandleListOpacity();
        }

        private void ListOpacity_75_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ListOpacity = .75;
            HandleListOpacity();
        }

        private void ListOpacity_100_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ListOpacity = 1;
            HandleListOpacity();
        }

        private void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AlwaysOnTop = AlwaysOnTop.IsChecked;
            OnActivated(e);
        }

        private void AutoHideWindow_Click(object sender, RoutedEventArgs e)
        {
            if (AutoHideWindow.IsChecked && Properties.Settings.Default.AutoHideWindowWarning)
            {
                MessageBox.Show("これにより、PSO2またはOverParseがフォアグラウンドにない時は、OverParseのウィンドゥが非表示になります。\nウィンドゥを表示するには、Alt+TabでOverParseにするか、タスクバーのアイコンをクリックします。", "OverParse Setup", MessageBoxButton.OK, MessageBoxImage.Information);
                Properties.Settings.Default.AutoHideWindowWarning = false;
            }
            Properties.Settings.Default.AutoHideWindow = AutoHideWindow.IsChecked;
        }

        private void ClickthroughToggle(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ClickthroughEnabled = ClickthroughMode.IsChecked;
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            MessageBox.Show($"OverParse v{version}\n簡易的な自己監視ツール。\n\nShoutouts to WaifuDfnseForce.\nAdditional shoutouts to Variant, AIDA, and everyone else who makes the Tweaker plugin possible.\n\nPlease use damage information responsibly.", "OverParse");
        }

        private void LowResources_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.LowResources = LowResources.IsChecked;
            if (Properties.Settings.Default.LowResources)
            {
                thisProcess.PriorityClass = ProcessPriorityClass.Idle;
                MessageBox.Show("OverParseの基本優先度を低に設定しました。\n殆どのCPUではあまり影響ありませんが、CPU使用率が100%になるようなPCスペックの場合にOverParseの動作を止め、PSO2や画面キャプチャ等の他のプログラムを優先させます。\nOverParseが応答不能になる可能性があることを覚えておいて下さい。","OverParse");
            } else {
                thisProcess.PriorityClass = ProcessPriorityClass.Normal;
            }
        }

        private void CPUdraw_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CPUdraw = CPUdraw.IsChecked;
            if (Properties.Settings.Default.CPUdraw)
            {
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                MessageBox.Show("OverParseの画面描画をCPU処理(ソフトウェアレンダリング)に変更しました。\nグラフィックボード搭載のPCでは逆効果ですが、CPUのみ高性能で内蔵GPUを使用する大部分の日本メーカー製ノートPCではある程度効果があります。\nIntel HD Graphicsを使用している場合や0.1%でもGPUの負荷を減らしたい場合これを有効にして下さい。", "OverParse");
            } else {
                RenderOptions.ProcessRenderMode = RenderMode.Default;
            }
        }

        private void Clock_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Clock = Clock.IsChecked;
            if (Properties.Settings.Default.Clock) { Datetime.Visibility = Visibility.Visible; }
            else { Datetime.Visibility = Visibility.Collapsed; }
        }

        private void Discord_Click(object sender, RoutedEventArgs e) => Process.Start("https://discord.gg/pTCq443");

        private void Github_Click(object sender, RoutedEventArgs e) => Process.Start("https://github.com/Remon-7L/OverParse");

        private void SkipPlugin_Click(object sender, RoutedEventArgs e) => Properties.Settings.Default.InstalledPluginVersion = 5;

        private void ResetLogFolder_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Path = "A://BROKEN/FILE/PATH";
            EndEncounterNoLog_Click(this, null);
        }

        private void UpdatePlugin_Click(object sender, RoutedEventArgs e)
        {
            encounterlog.UpdatePlugin(Properties.Settings.Default.Path);
            EndEncounterNoLog_Click(this, null);
        }

        private void Updateskills_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    Stream stream = client.OpenRead("https://remon-7l.github.io/skills.csv");
                    StreamReader streamReader = new StreamReader(stream);
                    String content = streamReader.ReadToEnd();
                    File.WriteAllText("skills.csv", content);
                }
                } catch {
                MessageBox.Show("skills.csvの取得に失敗しました。");
            }
        }

        private void ResetOverParse(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("OverParseをリセットしますか？\n設定は消去されますが、ログは消去されません。", "OverParse Setup", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result != MessageBoxResult.Yes)
                return;

            //Resetting
            Properties.Settings.Default.Reset();
            Properties.Settings.Default.ResetInvoked = true;
            Properties.Settings.Default.Save();

            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

    }
}
