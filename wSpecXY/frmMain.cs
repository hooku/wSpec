using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;

namespace wSpecXY
{
    public enum LOG_TYPE
    {
        LOG_INFO,
        LOG_WARN,
        LOG_ERROR,
    }

    public enum WORK_TYPE
    {
        WORK_BIDU,
        WORK_IBOT,
        WORK_SIMI,
        WORK_LOCA,
        WORK_MSMS,
    }

    public struct WORK_PARA
    {
        public WORK_TYPE    work_type;
        public object       work_data;
    }

    public partial class frmMain : Form
    {
        private const string TEST_QUESTION = "今天是星期几？";
        private const string TEST_SPEAKING = "天上飘着些微云，" + "地上吹着些微风。" + "啊！" + "微风吹动了我头发，" + "教我如何不想她？";

        Thread bg_thread;
        BlockingCollection<WORK_PARA> bg_queue = new BlockingCollection<WORK_PARA>();

        ImageList image_icon;
        ContextMenu menu_local;

        int current_row, current_col;

        public frmMain()
        {
            InitializeComponent();
        }

        private void refresh_local()
        {
            this.listView_local.Items.Clear();

            foreach (sxy_local.LocalRegExItem intrp_item in sxy_local.intrp_table)
            {
                ListViewItem item_local = new ListViewItem();
                item_local.Text = intrp_item.request;
                item_local.SubItems.Add(intrp_item.url);
                item_local.SubItems.Add(intrp_item.parser);
                item_local.SubItems.Add(intrp_item.reply);

                this.listView_local.Items.Add(item_local);
            }
        }

        private void bg_worker_do(WORK_TYPE work_type, object work_data)
        {
            this.toolstrip_progress.Style = ProgressBarStyle.Marquee;
            this.toolstrip_status.Text = "Working..";

            WORK_PARA work_para;
            work_para.work_type = work_type;
            work_para.work_data = work_data;

            bg_queue.Add(work_para);
        }

        private void bg_worker_thread()
        {
            WORK_PARA work_para;
            object result = null;

            logger.log(LOG_TYPE.LOG_INFO, "Background Worker created");

            while (true)
            {
                work_para = bg_queue.Take();

                switch (work_para.work_type)
                {
                    case WORK_TYPE.WORK_BIDU:
                        result = sxy_bidu.exec((byte[])work_para.work_data);
                        break;
                    case WORK_TYPE.WORK_LOCA:
                        result = sxy_local.exec((string)work_para.work_data);
                        break;
                    case WORK_TYPE.WORK_SIMI:
                        result = sxy_simsimi.exec((string)work_para.work_data);
                        break;
                    case WORK_TYPE.WORK_IBOT:
                        result = sxy_ibot.exec((string)work_para.work_data);
                        break;
                    case WORK_TYPE.WORK_MSMS:
                        sxy_ms.speak((string)work_para.work_data);
                        break;
                }

                // work done:
                bg_worker_done(result);
            }
        }

        delegate void Bg_Worker_Cb(object result);

        private void bg_worker_done(object result)
        {
            if (this.status_strip.InvokeRequired)
            {
                Bg_Worker_Cb d = new Bg_Worker_Cb(bg_worker_done);
                this.status_strip.Invoke(d, new object[] { result });
            }
            else
            {
                this.toolstrip_progress.Style = ProgressBarStyle.Blocks;
                this.toolstrip_status.Text = "Ready";
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            this.Icon = Properties.Resources.wspecxy;

            this.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + " " +
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor + " Build " +
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Build;

            /* log list */
            // create a imagelist:
            image_icon = new ImageList();
            image_icon.ColorDepth = ColorDepth.Depth32Bit;
            image_icon.Images.Add("INFO"    , Properties.Resources.info);
            image_icon.Images.Add("WARN"    , Properties.Resources.warn);
            image_icon.Images.Add("ERROR"   , Properties.Resources.error);
            image_icon.Images.Add("CONFIG"  , Properties.Resources.config);
            image_icon.Images.Add("GRAPH"   , Properties.Resources.graph);
            image_icon.Images.Add("REC"     , Properties.Resources.rec);
            image_icon.Images.Add("INTER"   , Properties.Resources.inter);
            image_icon.Images.Add("SYN"     , Properties.Resources.syn);

            this.list_log.SmallImageList = image_icon;
            logger.init(list_log);

            this.tabControl_wspecxy.ImageList = image_icon;
            this.tab_status.ImageKey    = "GRAPH";
            this.tab_bidu.ImageKey      = "REC";
            this.tab_local.ImageKey     = "INTER";
            this.tab_ibot.ImageKey      = "INTER";
            this.tab_simsimi.ImageKey   = "INTER";
            this.tab_ms.ImageKey        = "SYN";

            /* status tab */
            this.button_mic.Image       = Properties.Resources.mic.ToBitmap();
            this.picture_arrow0.Image   = Properties.Resources.right1.ToBitmap();
            this.button_arrow1.Image    = Properties.Resources.right2.ToBitmap();
            this.button_arrow2.Image    = Properties.Resources.right2.ToBitmap();
            this.picture_arrow1.Image   = Properties.Resources.right1.ToBitmap();
            this.button_speaker.Image   = Properties.Resources.speaker.ToBitmap();

            this.combo_wave_fmt.SelectedIndex = Properties.Settings.Default.cfg_wave_fmt;
            combo_wave_fmt_Validating(this, new CancelEventArgs());

            this.radio_bidu.Checked = true;
            this.radio_iflytek.Enabled = false;

            this.check_local.Checked = true;
            this.check_local.Enabled = false;
            this.check_ibot.Checked = Properties.Settings.Default.cfg_intrp_ibot;
            this.check_simsimi.Checked = Properties.Settings.Default.cfg_intrp_simsimi;

            this.check_ms.Checked = true;
            this.check_ms.Enabled = false;

            /* bidu tab */
            this.text_bidu_granttype.Text       = Properties.Settings.Default.bidu_grant_type;
            this.text_bidu_clientid.Text        = Properties.Settings.Default.bidu_client_id;
            this.text_bidu_clientsecret.Text    = Properties.Settings.Default.bidu_client_secret;
            this.text_bidu_accesstoken.Text     = Properties.Settings.Default.bidu_access_token;
            this.text_bidu_cuid.Text            = Properties.Settings.Default.bidu_cuid;
            this.text_bidu_language.Text        = Properties.Settings.Default.bidu_lan;
            bidu_validating(this, new CancelEventArgs());

            /* local tab */
            refresh_local();

            menu_local = new ContextMenu();
            menu_local.MenuItems.Add("&Add Entry"   , new EventHandler(menu_local_add));
            menu_local.MenuItems.Add("&Delete Entry", new EventHandler(menu_local_del));
            menu_local.MenuItems.Add("-");
            menu_local.MenuItems.Add("&Test Selected");

            this.listView_local.ContextMenu = menu_local;

            /* ibot tab */
            this.text_ibot_appkey.Text          = Properties.Settings.Default.ibot_app_key;
            this.text_ibot_appsecret.Text       = Properties.Settings.Default.ibot_app_secret;
            this.text_ibot_userid.Text          = Properties.Settings.Default.ibot_user_id;
            ibot_validating(this, new CancelEventArgs());

            /* ms tab */
            foreach (string voice in sxy_ms.get_voice())
            {
                this.combo_syn_voices.Items.Add(voice);
            }
            this.combo_syn_voices.SelectedIndex = Properties.Settings.Default.syn_voice_id;
            this.track_syn_speed.Value          = Properties.Settings.Default.syn_voice_speed;

            base.OnLoad(e);

            logger.log(LOG_TYPE.LOG_INFO, "Server startup");

            /* create background thread (for ui only) */
            ThreadStart thread_d = new ThreadStart(bg_worker_thread);
            bg_thread = new Thread(thread_d);
            bg_thread.IsBackground = true;  // auto terminates when app exit
            bg_thread.Start();              // our worker thread should persist

            /* create sxy server */
            sxy_server sxy_server = new sxy_server(3442);

            /* create sxy control */
            sxy_control sxy_control = new sxy_control();
        }

        private void menu_local_add(object sender, EventArgs e)
        {
            int sel_index = this.listView_local.Items.Count;
            if (this.listView_local.SelectedItems.Count > 0)
            {
                sel_index = this.listView_local.SelectedItems[0].Index + 1;
            }
            sxy_local.LocalRegExItem intrp_item = new sxy_local.LocalRegExItem();
            intrp_item.request  = "New Request RegEx";
            intrp_item.url      = "http://example.com";
            intrp_item.parser   = "New Parser RegEx";
            intrp_item.reply    = "New Reply RegEx";

            sxy_local.intrp_table.Insert(sel_index, intrp_item);

            refresh_local();
        }

        private void menu_local_del(object sender, EventArgs e)
        {
            sxy_local.intrp_table.RemoveAt(this.listView_local.SelectedItems[0].Index);

            refresh_local();
        }

        private void button_arrow2_Click(object sender, EventArgs e)
        {
            bg_worker_do(WORK_TYPE.WORK_MSMS, TEST_SPEAKING);
        }

        private void button_arrow1_Click(object sender, EventArgs e)
        {
            bg_worker_do(WORK_TYPE.WORK_LOCA, TEST_QUESTION);
            bg_worker_do(WORK_TYPE.WORK_SIMI, TEST_QUESTION);
            bg_worker_do(WORK_TYPE.WORK_IBOT, TEST_QUESTION);
        }

        private void tabControl_wspecxy_DoubleClick(object sender, EventArgs e)
        {
            string input;

            switch (this.tabControl_wspecxy.SelectedIndex)
            {
                case 1: // bidu
                    if (this.text_bidu_accesstoken.Text == string.Empty)
                    {
                        button_update_token_Click(this, e);
                    }

                    OpenFileDialog open_wav = new OpenFileDialog();

                    open_wav.Filter = "Wav Files (*.wav)|*.wav";
                    open_wav.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

                    if (open_wav.ShowDialog() == DialogResult.OK)
                    {
                        byte[] data_byte = File.ReadAllBytes(open_wav.FileName);
                        bg_worker_do(WORK_TYPE.WORK_BIDU, data_byte);
                    }

                    break;
                case 2: // loca
                    input = Microsoft.VisualBasic.Interaction.InputBox(
                        "Enter text to interpret:", "", TEST_QUESTION, -1, -1);
                    bg_worker_do(WORK_TYPE.WORK_LOCA, input);
                    break;
                case 3: // simi
                    bg_worker_do(WORK_TYPE.WORK_SIMI, TEST_QUESTION);
                    break;
                case 4: // ibot
                    input = Microsoft.VisualBasic.Interaction.InputBox(
                        "Enter text to interpret:", "", TEST_QUESTION, -1, -1);
                    bg_worker_do(WORK_TYPE.WORK_IBOT, input);
                    break;
                case 5: // msms
                    input = Microsoft.VisualBasic.Interaction.InputBox(
                        "Enter text to speech:", "", "轻轻的我走了，正如我轻轻的来；我轻轻的招手，作别西天的云彩。", -1, -1); // 欢迎光临 www.EM lab点net
                    bg_worker_do(WORK_TYPE.WORK_MSMS, input);
                    break;
                default:
                    break;
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // save settings:
            Properties.Settings.Default.cfg_wave_fmt        = this.combo_wave_fmt.SelectedIndex;
            Properties.Settings.Default.cfg_intrp_ibot      = this.check_ibot.Checked;
            Properties.Settings.Default.cfg_intrp_simsimi   = this.check_simsimi.Checked;

            Properties.Settings.Default.bidu_grant_type     = this.text_bidu_granttype.Text;
            Properties.Settings.Default.bidu_client_id      = this.text_bidu_clientid.Text;
            Properties.Settings.Default.bidu_client_secret  = this.text_bidu_clientsecret.Text;
            Properties.Settings.Default.bidu_access_token   = this.text_bidu_accesstoken.Text;
            Properties.Settings.Default.bidu_cuid           = this.text_bidu_cuid.Text;
            Properties.Settings.Default.bidu_lan            = this.text_bidu_language.Text;

            Properties.Settings.Default.ibot_app_key        = this.text_ibot_appkey.Text;
            Properties.Settings.Default.ibot_app_secret     = this.text_ibot_appsecret.Text;
            Properties.Settings.Default.ibot_user_id        = this.text_ibot_userid.Text;

            Properties.Settings.Default.syn_voice_id        = this.combo_syn_voices.SelectedIndex;
            Properties.Settings.Default.syn_voice_speed     = this.track_syn_speed.Value;

            Properties.Settings.Default.Save();
        }

        private void text_local_edit_Leave(object sender, EventArgs e)
        {
            switch (current_col)
            {
                case 0:
                    sxy_local.intrp_table[current_row].request = this.text_local_edit.Text;
                    break;
                case 1:
                    sxy_local.intrp_table[current_row].url = this.text_local_edit.Text;
                    break;
                case 2:
                    sxy_local.intrp_table[current_row].parser = this.text_local_edit.Text;
                    break;
                case 3:
                    sxy_local.intrp_table[current_row].reply = this.text_local_edit.Text;
                    break;
                default:
                    break;
            }

            refresh_local();
            this.text_local_edit.Hide();
        }

        private void listView_local_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit_test_item = this.listView_local.HitTest(e.X, e.Y);

            current_row = hit_test_item.Item.Index;
            current_col = hit_test_item.Item.SubItems.IndexOf(hit_test_item.SubItem);
            this.text_local_edit.Top = hit_test_item.Item.Bounds.Y;

            this.text_local_edit.Text   = hit_test_item.SubItem.Text;
            this.text_local_edit.Left   = this.listView_local.Left + hit_test_item.SubItem.Bounds.Left + 5;
            this.text_local_edit.Top    = this.listView_local.Top + hit_test_item.SubItem.Bounds.Top + 1;
            this.text_local_edit.Width  = this.listView_local.Columns[current_col].Width - 5;

            this.text_local_edit.SelectAll();
            this.text_local_edit.Show();
            this.text_local_edit.Focus();
        }

        private void text_local_edit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                text_local_edit_Leave(this, new EventArgs());
            }
        }

        private void list_log_DoubleClick(object sender, EventArgs e)
        {
            Clipboard.SetText(this.list_log.SelectedItems[0].SubItems[1].Text);
        }

        private void ibot_validating(object sender, CancelEventArgs e)
        {
            sxy_ibot.config(this.text_ibot_appkey.Text, this.text_ibot_appsecret.Text, this.text_ibot_userid.Text);
        }

        private void msms_validating(object sender, CancelEventArgs e)
        {
            sxy_ms.set_voice(this.combo_syn_voices.Text, this.track_syn_speed.Value);
        }

        private void bidu_validating(object sender, CancelEventArgs e)
        {
            sxy_bidu.config(this.text_bidu_granttype.Text, this.text_bidu_clientid.Text, this.text_bidu_clientsecret.Text,
                this.text_bidu_accesstoken.Text, this.text_bidu_cuid.Text, this.text_bidu_language.Text);
        }

        private void button_update_token_Click(object sender, EventArgs e)
        {
            this.text_bidu_accesstoken.Text = sxy_bidu.do_update_access_token();
            bidu_validating(this, new CancelEventArgs());
        }

        private void combo_wave_fmt_Validating(object sender, CancelEventArgs e)
        {
            sxy_server.set_rate(this.combo_wave_fmt.SelectedIndex);
        }
    }

    public class logger
    {
        static ListView list_log;

        public static void init(ListView list)
        {
            list_log = list;
        }

        delegate void Log_Cb(LOG_TYPE log_type, string text);

        public static void log(LOG_TYPE log_type, string text)
        {
            if (list_log.InvokeRequired)
            {
                Log_Cb d = new Log_Cb(log);
                list_log.Invoke(d, new object[] { log_type, text });
            }
            else
            {
                ListViewItem item_log = new ListViewItem();

                item_log.ImageIndex = (int)log_type;
                item_log.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                item_log.SubItems.Add(text);

                list_log.Items.Insert(0, item_log);
                list_log.EnsureVisible(0);

                Application.DoEvents();
            }
        }
    }
}
