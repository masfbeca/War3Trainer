using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace War3Trainer
{
    public partial class test : Form
    {
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }
        uint WM_LBUTTONDOWN = 0x201;
        uint WM_LBUTTONUP = 0x202;
 
        protected override CreateParams CreateParams
        {
            get
            {
                //make sure Top Most property on form is set to false
                //otherwise this doesn't work
                int WS_EX_TOPMOST = 0x00000008;
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOPMOST;
                return cp;
            }
        }
        List<HeroInfo> _heroList = new List<HeroInfo>();
        MyTrainer _mt = new MyTrainer();
        private int _hotKey;
        private int _hotKeyF3;
        Graphics _graphics;
        Font ft = new Font("宋体", 8, FontStyle.Regular);
        SolidBrush brush = new SolidBrush(Color.Lime);
        List<NameMapped> _nameDic = new List<NameMapped>();
        uint dwStyle = 0x94000044;
        uint dwExStyle = 0x50000;
        uint WM_ACTIVATE = 0x0006;
        bool isNeed = false;
        IntPtr hwd;
        public test()
        {
            InitializeComponent();
            this.Location = new Point(15, 585);
            StreamReader sr = new StreamReader("1.txt", Encoding.GetEncoding("gb2312"));

            string key = "";
            string values = "";
            while (!sr.EndOfStream)
            {
                string str = sr.ReadLine();
                if (string.IsNullOrEmpty(str.Trim()))
                {
                    continue;
                }
                if (string.IsNullOrEmpty(key))
                {
                    key = str.Trim();
                }
                else
                {
                    values = str.Trim();
                }
                if (!string.IsNullOrEmpty(key.Trim())
                    && !string.IsNullOrEmpty(values.Trim()))
                {
                    NameMapped nm = new NameMapped();
                    nm.Abs = key;
                    nm.Name = values;

                    _nameDic.Add(nm);
                    key = "";
                    values = "";
                }
            }


            timer1.Tick += TimerTick;
            timer1.Interval = 1000;
            
            Hotkey hotkey;
            hotkey = new Hotkey(this.Handle);
            _hotKey = hotkey.RegisterHotkey(System.Windows.Forms.Keys.F2, 0);
            _hotKeyF3 = hotkey.RegisterHotkey(System.Windows.Forms.Keys.F3,0);
            hotkey.OnHotkey += new HotkeyEventHandler(OnHotkey);

            timer1.Start();



             hwd = Win32.FindWindow("Warcraft III", null);
            Win32.SetWindowLong(hwd, -16, dwStyle);
            Win32.SetWindowLong(hwd, -20, dwExStyle);
            Win32.ShowWindow(hwd, 3);
            
        }

        private bool _needMin = true;
        public void OnHotkey(int HotkeyID)
        {
            isNeed = true;
            if (HotkeyID == _hotKey)
            {
                _mt.FindGame();
                HeroInfo hero = new HeroInfo();
                _mt.GetCurrentHeroAddress(ref hero);
                if (hero.Address != 0)
                {
                    HeroInfo hi = _heroList.Find(i => i.Address == hero.Address);
                    if (hi==null)
                    {
                        _heroList.Add(hero);
                    }
                }
            }
            if (HotkeyID==_hotKeyF3)
            {
                if (_needMin)
                {
                    this.WindowState = FormWindowState.Minimized;
                    _needMin = false;
                }
                else
                {
                    this.WindowState = FormWindowState.Normal;
                    _needMin = true;
                }
            }
        }

        private void TimerTick(object sender, EventArgs e)
        {
            _graphics.Clear(this.BackColor);
            int length = _heroList.Count;
            int times = 0;
            for (int i = 0; i < length; i++)
            {
                HeroInfo item = _heroList[i];
                _mt.GetInfo(item);
                if (item.X==0&&item.Y==0)
                {
                    _mt.FindGame();
                    if (times++<4)
                    {
                        i = 0;
                    }
                }
            }
            DrawHeros();

        }
        private void DrawHeros()
        {
            foreach (HeroInfo item in _heroList)
            {
                _mt.GetInfo(item);
                foreach (NameMapped nextName in _nameDic)
                {
                    if (nextName.Abs.ToLower().Contains(item.Name.ToLower()))
                    {
                        item.Name = nextName.Name;
                        break;
                    }
                }
                float a = (((float)item.X) / 490);
                float b = (((float)item.Y) / 474);
                float X = a * 240;
                float Y = 175 - b * 175;
                _graphics.DrawString(item.Name, ft, brush, X - 17, Y - 3);
            }
        }
        private void test_Load(object sender, EventArgs e)
        {
            _graphics = this.CreateGraphics();
            _graphics.SmoothingMode = SmoothingMode.AntiAlias;
        }
       
        bool HideTaskBar(bool Hide)
        {

            int ABM_SETSTATE = 0x0000000a;
            IntPtr hWnd = Win32.FindWindow("Shell_TrayWnd", null);

            if (hWnd == null)
            {
                return false;
            }

            uint nCmdShow;
            APPBARDATA apBar = new APPBARDATA();

            apBar.hWnd = hWnd;


            if (Hide)
            {
                nCmdShow = 0;
                apBar.lParam = (IntPtr)0x0000001;
            }
            else
            {
                nCmdShow = 5;
                apBar.lParam = (IntPtr)0x0000002;
            }


            Win32.SHAppBarMessage(ABM_SETSTATE, ref apBar);
            Win32.ShowWindow(hWnd, nCmdShow);
            return true;
        }
        private void test_Click(object sender, EventArgs e)
        {

        }

        private void test_MouseEnter(object sender, EventArgs e)
        {

        }

        private void test_MouseDown(object sender, MouseEventArgs e)
        {
            if (_needMin)
            {
                this.WindowState = FormWindowState.Minimized;
                _needMin = false;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                _needMin = true;
            }
        }

    }
}
