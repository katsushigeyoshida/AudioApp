using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace AudioApp
{
    /// <summary>
    /// CoverView.xaml の相互作用ロジック
    /// </summary>
    public partial class CoverView : Window
    {
        private double mWindowWidth;            //  ウィンドウの高さ
        private double mWindowHeight;           //  ウィンドウ幅
        public string mComment;                 //  コメント表示
        public BitmapSource mBitmapSource;      //  トリミングする画像データ
        public bool mFullScreen = true;         //  全画面表示
        public double mFontSize = 12;           //  文字サイズ

        public CoverView()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (mFullScreen) {
                // タイトルバーと境界線を表示しない
                this.WindowStyle = WindowStyle.None;
                // 最大化表示
                this.WindowState = WindowState.Maximized;
            } else {
                WindowFormLoad();
            }
            imScreen.Source = mBitmapSource;
            tbComment.Text = mComment;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowFormSave();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.CoverViewWidth < 100 ||
                Properties.Settings.Default.CoverViewHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.CoverViewHeight) {
                Properties.Settings.Default.CoverViewWidth = mWindowWidth;
                Properties.Settings.Default.CoverViewHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.CoverViewTop;
                Left = Properties.Settings.Default.CoverViewLeft;
                Width = Properties.Settings.Default.CoverViewWidth;
                Height = Properties.Settings.Default.CoverViewHeight;
            }
            if (0 < Properties.Settings.Default.CoverViewFontSize) {
                mFontSize = Properties.Settings.Default.CoverViewFontSize;
                tbComment.FontSize = mFontSize;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            Properties.Settings.Default.CoverViewFontSize = mFontSize;
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.CoverViewTop = Top;
            Properties.Settings.Default.CoverViewLeft = Left;
            Properties.Settings.Default.CoverViewWidth = Width;
            Properties.Settings.Default.CoverViewHeight = Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// キー入力処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool control = false;
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                control = true;
            switch (e.Key) {
                case Key.Escape:        //  ESCキーで終了
                    Close();
                    break;
                case Key.Down:           //  1行スクロールダウン
                    tbComment.LineDown();
                    break;
                case Key.Up:            //  1行スクロールアップ
                    tbComment.LineUp();
                    break;
                case Key.Add:           //  文字サイズ拡大
                    mFontSize++;
                    tbComment.FontSize = mFontSize;
                    break;
                case Key.Subtract:      //  文字サイズ縮小
                    mFontSize--;
                    tbComment.FontSize = mFontSize;
                    break;
            }
        }
    }
}
