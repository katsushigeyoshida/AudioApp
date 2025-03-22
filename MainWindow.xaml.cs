using System.Windows;
using System.Windows.Input;

namespace AudioApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private string[] mProgramTitle = {                      //  プログラムタイトルリスト
            "マイク録音",
            "オーディオプレイヤ",
            "スピーカ出力キャプチャー",
            "音楽ファイル管理",
            "スペクトラム表示",
            "ミュージシャン検索",
        };

        public MainWindow()
        {
            InitializeComponent();

            mWindowWidth = this.Width;
            mWindowHeight = this.Height;
            mPrevWindowWidth = mWindowWidth;

            WindowFormLoad();

            ProgramList.Items.Clear();
            foreach (string name in mProgramTitle)
                ProgramList.Items.Add(name);
        }

        private void SelectWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //  Windowの状態を保存
            WindowFormSave();
        }


        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.MainWindowWidth < 100 || Properties.Settings.Default.MainWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.MainWindowHeight) {
                Properties.Settings.Default.MainWindowWidth = mWindowWidth;
                Properties.Settings.Default.MainWindowHeight = mWindowHeight;
            } else {
                this.Top = Properties.Settings.Default.MainWindowTop;
                this.Left = Properties.Settings.Default.MainWindowLeft;
                this.Width = Properties.Settings.Default.MainWindowWidth;
                this.Height = Properties.Settings.Default.MainWindowHeight;
            }

            ProgramList.Focus();
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.MainWindowTop = this.Top;
            Properties.Settings.Default.MainWindowLeft = this.Left;
            Properties.Settings.Default.MainWindowWidth = this.Width;
            Properties.Settings.Default.MainWindowHeight = this.Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// プログラムリストをダブルクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProgramList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            executeProg();
        }

        /// <summary>
        /// プログラムリストでのキー入力
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProgramList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) {
                executeProg();
            }
        }

        /// <summary>
        /// 選択されたプログラムリストの実行
        /// </summary>
        private void executeProg()
        {
            Window programDlg = null;
            switch (ProgramList.SelectedIndex) {
                case 0: programDlg = new MicRecord(); break;
                case 1: programDlg = new AudioPlay(); break;
                case 2: programDlg = new LoopbackCapture(); break;
                case 3: programDlg = new MusicExplorer(); break;
                case 4: programDlg = new SpectrumAnalyzer(); break;
                case 5: programDlg = new FindPlayer(); break;
            }
            if (programDlg != null)
                programDlg.Show();
            //programDlg.ShowDialog();
        }
    }
}
