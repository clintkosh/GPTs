using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuotaWatch
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class QuotaConfig
    {
        public const string CsvVersion = "1";
        public string Version = CsvVersion;
        public string Profile = "Work quota";
        public string Unit = "messages";
        public double Total = 100;
        public double Used = 0;
        public string Accuracy = "estimated";
        public string Source = "Local configured limit";
        public DateTime? ResetAt = DateTime.Now.AddHours(24);
        public double ResetHours = 24;
        public double LowThreshold = 20;
        public double CriticalThreshold = 5;
        public bool Notifications = false;

        public double? PercentRemaining
        {
            get
            {
                if (Accuracy == "unavailable" || Total <= 0) return null;
                return Math.Max(0, Math.Min(100, ((Total - Used) / Total) * 100.0));
            }
        }

        public string State
        {
            get
            {
                var p = PercentRemaining;
                if (!p.HasValue) return "unavailable";
                if (p.Value <= 0) return "exhausted";
                if (p.Value <= CriticalThreshold) return "critical";
                if (p.Value <= LowThreshold) return "low";
                if (p.Value <= 50) return "moderate";
                return "healthy";
            }
        }

        public void Normalize()
        {
            Total = Math.Max(0, Total);
            Used = Math.Max(0, Used);
            ResetHours = Math.Max(0, ResetHours);
            LowThreshold = Math.Max(1, Math.Min(100, LowThreshold));
            CriticalThreshold = Math.Max(1, Math.Min(LowThreshold, CriticalThreshold));
            if (!new[] { "exact", "estimated", "unavailable" }.Contains(Accuracy)) Accuracy = "unavailable";
            if (string.IsNullOrWhiteSpace(Profile)) Profile = "Work quota";
            if (string.IsNullOrWhiteSpace(Unit)) Unit = "work_units";
            if (string.IsNullOrWhiteSpace(Source)) Source = "Local configured limit";
        }

        private static readonly string[] Fields = { "version", "profile", "unit", "total", "used", "accuracy", "source", "resetAt", "resetHours", "lowThreshold", "criticalThreshold", "notifications" };

        public string ToCsv()
        {
            var values = new[]
            {
                Version, Profile, Unit,
                Total.ToString(CultureInfo.InvariantCulture), Used.ToString(CultureInfo.InvariantCulture), Accuracy, Source,
                ResetAt.HasValue ? ResetAt.Value.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture) : "",
                ResetHours.ToString(CultureInfo.InvariantCulture), LowThreshold.ToString(CultureInfo.InvariantCulture), CriticalThreshold.ToString(CultureInfo.InvariantCulture),
                Notifications ? "on" : "off"
            };
            return string.Join(",", Fields) + Environment.NewLine + string.Join(",", values.Select(EscapeCsv)) + Environment.NewLine;
        }

        public static QuotaConfig FromCsv(string text)
        {
            var lines = text.Replace("\r", "").Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (lines.Length < 2) throw new InvalidDataException("CSV must contain a header and one configuration row.");
            var headers = ParseCsvLine(lines[0]);
            var values = ParseCsvLine(lines[1]);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++) map[headers[i]] = i < values.Count ? values[i] : "";
            foreach (var field in Fields) if (!map.ContainsKey(field)) throw new InvalidDataException("Missing column: " + field);
            if (map["version"] != CsvVersion) throw new InvalidDataException("Unsupported CSV version: " + map["version"]);
            var c = new QuotaConfig
            {
                Version = map["version"], Profile = map["profile"], Unit = map["unit"],
                Total = ParseDouble(map["total"]), Used = ParseDouble(map["used"]), Accuracy = map["accuracy"], Source = map["source"],
                ResetAt = ParseDate(map["resetAt"]), ResetHours = ParseDouble(map["resetHours"]), LowThreshold = ParseDouble(map["lowThreshold"]),
                CriticalThreshold = ParseDouble(map["criticalThreshold"]), Notifications = string.Equals(map["notifications"], "on", StringComparison.OrdinalIgnoreCase)
            };
            c.Normalize();
            return c;
        }

        private static string EscapeCsv(string value)
        {
            value = value ?? "";
            return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (quoted)
                {
                    if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else if (ch == '"') quoted = false;
                    else sb.Append(ch);
                }
                else if (ch == '"') quoted = true;
                else if (ch == ',') { result.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(ch);
            }
            result.Add(sb.ToString());
            return result;
        }

        private static double ParseDouble(string value)
        {
            double n;
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out n) ? n : 0;
        }

        private static DateTime? ParseDate(string value)
        {
            DateTime d;
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out d) ? d : (DateTime?)null;
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clintware", "QuotaWatch");
        private string ConfigPath { get { return Path.Combine(dataDir, "config.csv"); } }
        private QuotaConfig config;
        private NotifyIcon tray;
        private Timer timer;
        private bool allowExit;
        private string lastNotified;

        private Label percentLabel, stateLabel, remainingLabel, sourceLabel, resetLabel;
        private TextBox profileBox, sourceBox;
        private ComboBox unitBox, accuracyBox;
        private NumericUpDown totalBox, usedBox, resetHoursBox, lowBox, criticalBox;
        private DateTimePicker resetPicker;
        private CheckBox resetEnabled, notificationsBox;
        private ProgressBar progress;

        public MainForm()
        {
            Text = "QuotaWatch by Clintware";
            Width = 620; Height = 560; MinimumSize = new Size(570, 520); StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F); BackColor = Color.FromArgb(13, 17, 23); ForeColor = Color.White;
            Directory.CreateDirectory(dataDir);
            config = LoadConfig();
            BuildUi(); BuildTray();
            timer = new Timer { Interval = 60000 }; timer.Tick += (s, e) => RefreshQuota(); timer.Start();
            RefreshQuota();
            Resize += (s, e) => { if (WindowState == FormWindowState.Minimized) Hide(); };
            FormClosing += OnFormClosing;
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 9, BackColor = BackColor };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            Controls.Add(root);
            var title = new Label { Text = "QuotaWatch", AutoSize = true, Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = Color.FromArgb(103,232,249), Margin = new Padding(0,0,0,8) };
            root.Controls.Add(title,0,0); root.SetColumnSpan(title,2);
            var statusPanel = new Panel { Height = 95, Dock = DockStyle.Top, BackColor = Color.FromArgb(17,24,39), Padding = new Padding(12) };
            percentLabel = new Label { Text = "—", Font = new Font("Segoe UI", 24F, FontStyle.Bold), AutoSize = true, Location = new Point(12,10) };
            stateLabel = new Label { Text = "Quota unavailable", AutoSize = true, Location = new Point(16,58), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            remainingLabel = new Label { AutoSize = true, Location = new Point(130,18) }; sourceLabel = new Label { AutoSize = true, Location = new Point(130,42), ForeColor = Color.Silver }; resetLabel = new Label { AutoSize = true, Location = new Point(130,64), ForeColor = Color.Silver };
            progress = new ProgressBar { Location = new Point(420,18), Width = 120, Height = 18, Minimum = 0, Maximum = 100 };
            statusPanel.Controls.AddRange(new Control[] { percentLabel,stateLabel,remainingLabel,sourceLabel,resetLabel,progress }); root.Controls.Add(statusPanel,0,1); root.SetColumnSpan(statusPanel,2);
            profileBox = MakeTextBox(); sourceBox = MakeTextBox();
            unitBox = MakeCombo(new[] { "messages","tokens","tasks","credits","minutes","work_units" }); accuracyBox = MakeCombo(new[] { "estimated","exact","unavailable" });
            totalBox = MakeNumber(0,100000000,1); usedBox = MakeNumber(0,100000000,1); resetHoursBox=MakeNumber(0,8760,1); lowBox=MakeNumber(1,100,1); criticalBox=MakeNumber(1,100,1);
            resetPicker = new DateTimePicker { Format=DateTimePickerFormat.Custom, CustomFormat="yyyy-MM-dd HH:mm", Width=230, CalendarForeColor=Color.Black };
            resetEnabled = new CheckBox { Text="Scheduled reset enabled", AutoSize=true, ForeColor=ForeColor }; notificationsBox = new CheckBox { Text="Desktop threshold notifications", AutoSize=true, ForeColor=ForeColor };
            AddField(root,2,0,"Profile",profileBox); AddField(root,2,1,"Unit",unitBox); AddField(root,3,0,"Total quota",totalBox); AddField(root,3,1,"Used",usedBox); AddField(root,4,0,"Accuracy",accuracyBox); AddField(root,4,1,"Data source",sourceBox);
            AddField(root,5,0,"Reset time",resetPicker); AddField(root,5,1,"Repeat reset every hours",resetHoursBox); AddField(root,6,0,"Low warning %",lowBox); AddField(root,6,1,"Critical warning %",criticalBox);
            var options = new FlowLayoutPanel { Dock=DockStyle.Fill, AutoSize=true }; options.Controls.Add(resetEnabled); options.Controls.Add(notificationsBox); root.Controls.Add(options,0,7); root.SetColumnSpan(options,2);
            var buttons = new FlowLayoutPanel { Dock=DockStyle.Fill, AutoSize=true };
            Button save=MakeButton("Save"), add1=MakeButton("+1"), addCustom=MakeButton("+5"), reset=MakeButton("Reset usage"), import=MakeButton("Import CSV"), export=MakeButton("Export CSV"), hide=MakeButton("Hide to tray");
            save.Click+=(s,e)=>SaveFromUi(); add1.Click+=(s,e)=>RecordUsage(1); addCustom.Click+=(s,e)=>RecordUsage(5); reset.Click+=(s,e)=>{config.Used=0;lastNotified=null;SaveConfig();RefreshQuota();}; import.Click+=(s,e)=>ImportCsv(); export.Click+=(s,e)=>ExportCsv(); hide.Click+=(s,e)=>Hide();
            buttons.Controls.AddRange(new Control[]{save,add1,addCustom,reset,import,export,hide}); root.Controls.Add(buttons,0,8); root.SetColumnSpan(buttons,2);
            LoadUi();
        }

        private void BuildTray()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Open",null,(s,e)=>ShowMain()); menu.Items.Add("Record +1",null,(s,e)=>RecordUsage(1)); menu.Items.Add("Refresh",null,(s,e)=>RefreshQuota());
            menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("Import CSV",null,(s,e)=>ImportCsv()); menu.Items.Add("Export CSV",null,(s,e)=>ExportCsv()); menu.Items.Add("Reset usage",null,(s,e)=>{config.Used=0;lastNotified=null;SaveConfig();RefreshQuota();});
            menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("Exit",null,(s,e)=>{allowExit=true;tray.Visible=false;Close();});
            tray = new NotifyIcon { Icon=SystemIcons.Information, Visible=true, Text="QuotaWatch", ContextMenuStrip=menu };
            tray.DoubleClick += (s,e)=>ShowMain();
        }

        private void RefreshQuota()
        {
            ApplyScheduledReset(); config.Normalize(); var p=config.PercentRemaining; string st=config.State;
            percentLabel.Text=p.HasValue?Math.Round(p.Value)+"%":"—"; stateLabel.Text=LabelFor(st); stateLabel.ForeColor=ColorFor(st); progress.Value=p.HasValue?(int)Math.Max(0,Math.Min(100,Math.Round(p.Value))):0;
            remainingLabel.Text=p.HasValue?"Remaining: "+Math.Max(0,config.Total-config.Used).ToString("0.##")+" "+config.Unit:"Remaining: unavailable";
            sourceLabel.Text="Source: "+config.Source+" ("+Cap(config.Accuracy)+")"; resetLabel.Text=config.ResetAt.HasValue?"Resets: "+config.ResetAt.Value.ToString("g"):"No reset scheduled";
            var tip=(p.HasValue?Math.Round(p.Value)+"% left":"Quota unavailable")+" · "+config.Profile; tray.Text=tip.Length<=63?tip:tip.Substring(0,63); MaybeNotify(st,p);
        }

        private void RecordUsage(double amount)
        {
            if(amount<=0)return; if(config.Accuracy=="unavailable"){config.Accuracy="estimated";config.Source="Local configured limit";} else if(config.Accuracy=="exact"){config.Accuracy="estimated";config.Source="Local usage added to prior exact snapshot";}
            config.Used+=amount; SaveConfig(); LoadUi(); RefreshQuota();
        }

        private void ApplyScheduledReset()
        {
            if(!config.ResetAt.HasValue||config.ResetHours<=0||config.ResetAt.Value>DateTime.Now)return;
            var r=config.ResetAt.Value; config.Used=0; do r=r.AddHours(config.ResetHours); while(r<=DateTime.Now); config.ResetAt=r; lastNotified=null; SaveConfig(); LoadUi();
        }

        private void MaybeNotify(string state,double? percent)
        {
            if(!config.Notifications)return; string bucket=state=="exhausted"?"0":state=="critical"?"critical":state=="low"?"low":null;
            if(bucket==null){if(state=="healthy"||state=="moderate")lastNotified=null;return;} if(bucket==lastNotified)return; lastNotified=bucket;
            tray.BalloonTipTitle=config.Profile+": "+LabelFor(state); tray.BalloonTipText=percent.HasValue?Math.Round(percent.Value)+"% remains"+(config.ResetAt.HasValue?". Resets "+config.ResetAt.Value.ToString("g")+".":"."):"Quota unavailable."; tray.ShowBalloonTip(6000);
        }

        private void SaveFromUi()
        {
            config.Profile=profileBox.Text; config.Unit=(string)unitBox.SelectedItem; config.Total=(double)totalBox.Value; config.Used=(double)usedBox.Value; config.Accuracy=(string)accuracyBox.SelectedItem; config.Source=sourceBox.Text; config.ResetAt=resetEnabled.Checked?(DateTime?)resetPicker.Value:null; config.ResetHours=(double)resetHoursBox.Value; config.LowThreshold=(double)lowBox.Value; config.CriticalThreshold=(double)criticalBox.Value; config.Notifications=notificationsBox.Checked; config.Normalize(); SaveConfig(); RefreshQuota();
        }

        private void LoadUi()
        {
            config.Normalize(); profileBox.Text=config.Profile; unitBox.SelectedItem=config.Unit; if(unitBox.SelectedIndex<0)unitBox.SelectedIndex=0; totalBox.Value=ClampDecimal(config.Total,totalBox.Maximum); usedBox.Value=ClampDecimal(config.Used,usedBox.Maximum); accuracyBox.SelectedItem=config.Accuracy; if(accuracyBox.SelectedIndex<0)accuracyBox.SelectedItem="unavailable"; sourceBox.Text=config.Source; resetEnabled.Checked=config.ResetAt.HasValue; resetPicker.Value=config.ResetAt.HasValue&&config.ResetAt.Value>DateTimePicker.MinimumDateTime?config.ResetAt.Value:DateTime.Now.AddHours(24); resetHoursBox.Value=ClampDecimal(config.ResetHours,resetHoursBox.Maximum); lowBox.Value=ClampDecimal(config.LowThreshold,100); criticalBox.Value=ClampDecimal(config.CriticalThreshold,100); notificationsBox.Checked=config.Notifications;
        }

        private QuotaConfig LoadConfig()
        {
            try{if(File.Exists(ConfigPath))return QuotaConfig.FromCsv(File.ReadAllText(ConfigPath));}catch(Exception ex){MessageBox.Show("Existing configuration could not be loaded. A safe default will be used.\n\n"+ex.Message,"QuotaWatch",MessageBoxButtons.OK,MessageBoxIcon.Warning);} return new QuotaConfig();
        }
        private void SaveConfig(){Directory.CreateDirectory(dataDir);File.WriteAllText(ConfigPath,config.ToCsv(),new UTF8Encoding(false));}
        private void ImportCsv(){using(var d=new OpenFileDialog{Filter="CSV configuration (*.csv)|*.csv|All files (*.*)|*.*"})if(d.ShowDialog()==DialogResult.OK){try{var next=QuotaConfig.FromCsv(File.ReadAllText(d.FileName));config=next;lastNotified=null;SaveConfig();LoadUi();RefreshQuota();MessageBox.Show("Configuration imported and validated.","QuotaWatch");}catch(Exception ex){MessageBox.Show("Import failed:\n"+ex.Message,"QuotaWatch",MessageBoxButtons.OK,MessageBoxIcon.Error);}}}
        private void ExportCsv(){using(var d=new SaveFileDialog{Filter="CSV configuration (*.csv)|*.csv",FileName="quotawatch-config.csv"})if(d.ShowDialog()==DialogResult.OK)File.WriteAllText(d.FileName,config.ToCsv(),new UTF8Encoding(false));}
        private void ShowMain(){Show();WindowState=FormWindowState.Normal;Activate();}
        private void OnFormClosing(object sender,FormClosingEventArgs e){if(!allowExit&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();tray.ShowBalloonTip(2500,"QuotaWatch","Still running in the notification area.",ToolTipIcon.Info);}}

        private static TextBox MakeTextBox(){return new TextBox{Dock=DockStyle.Fill,BackColor=Color.FromArgb(9,13,18),ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle};}
        private static ComboBox MakeCombo(string[] items){var c=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Color.FromArgb(9,13,18),ForeColor=Color.White};c.Items.AddRange(items);return c;}
        private static NumericUpDown MakeNumber(decimal min,decimal max,decimal inc){return new NumericUpDown{Dock=DockStyle.Fill,Minimum=min,Maximum=max,Increment=inc,BackColor=Color.FromArgb(9,13,18),ForeColor=Color.White,DecimalPlaces=0};}
        private static Button MakeButton(string text){return new Button{Text=text,AutoSize=true,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(24,32,43),ForeColor=Color.White,Padding=new Padding(5)};}
        private static void AddField(TableLayoutPanel root,int row,int col,string label,Control control){var p=new Panel{Dock=DockStyle.Fill,Padding=new Padding(0,4,8,4)};var l=new Label{Text=label,Dock=DockStyle.Top,Height=19,ForeColor=Color.Silver};control.Dock=DockStyle.Top;p.Controls.Add(control);p.Controls.Add(l);root.Controls.Add(p,col,row);}
        private static decimal ClampDecimal(double value,decimal max){var v=(decimal)Math.Max(0,value);return Math.Min(v,max);}
        private static string Cap(string s){return string.IsNullOrEmpty(s)?s:char.ToUpperInvariant(s[0])+s.Substring(1);}
        private static string LabelFor(string state){switch(state){case"healthy":return"Healthy";case"moderate":return"Moderate";case"low":return"Low quota";case"critical":return"Critical quota";case"exhausted":return"Quota exhausted";default:return"Quota unavailable";}}
        private static Color ColorFor(string state){switch(state){case"healthy":return Color.LightGreen;case"moderate":return Color.Khaki;case"low":return Color.SandyBrown;case"critical":return Color.LightCoral;case"exhausted":return Color.HotPink;default:return Color.LightSlateGray;}}
    }
}
