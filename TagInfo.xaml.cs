using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AudioApp
{
    /// <summary>
    /// TagInfo.xaml の相互作用ロジック
    /// 
    /// 音楽ファイルデータのタグ表情報表示
    /// </summary>
    public partial class TagInfo : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        public List<string> mTagInfoList;
        public byte[] mImageData;

        public TagInfo()
        {
            InitializeComponent();

            mWindowWidth = Width;
            mWindowHeight = Height;
            mPrevWindowWidth = mWindowWidth;
            WindowFormLoad();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (mTagInfoList != null) {
                foreach (string data in mTagInfoList) {
                    LbTagInfo.Items.Add(data);
                }
            }
            if (mImageData != null) {
                ImTagImage.Visibility = Visibility.Visible;
                //  ファイルから解放可能なBitmapImageを読み込む
                //  http://neareal.net/index.php?Programming%2F.NetFramework%2FWPF%2FWriteableBitmap%2FLoadReleaseableBitmapImage

                //  イメージデータをStream化してBitmapImageに使用
                MemoryStream stream = new MemoryStream(mImageData);
                BitmapImage bitmap = new BitmapImage();
                try {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;  //  作成に使用されたストリームを閉じる
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    stream.Close();
                    ImTagImage.Source = bitmap;
                    ImTagImage.Stretch = Stretch.Uniform;
                } catch (Exception err) {
                    MessageBox.Show(err.Message);
                }
            } else {
                //  イメージデータがない場合は非表示にする
                ImTagImage.Visibility = Visibility.Hidden;
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowFormSave();
        }

        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            if (this.WindowState != mWindowState &&
                this.WindowState == WindowState.Maximized) {
                //  ウィンドウの最大化時
                mWindowWidth = System.Windows.SystemParameters.WorkArea.Width;
                mWindowHeight = System.Windows.SystemParameters.WorkArea.Height;
            } else if (this.WindowState != mWindowState ||
                mWindowWidth != this.Width ||
                mWindowHeight != this.Height) {
                //  ウィンドウサイズが変わった時
                mWindowWidth = this.Width;
                mWindowHeight = this.Height;
            } else {
                //  ウィンドウサイズが変わらない時は何もしない
                mWindowState = this.WindowState;
                return;
            }
            mWindowState = this.WindowState;
            //  ウィンドウの大きさに合わせてコントロールの幅を変更する
            double dx = mWindowWidth - mPrevWindowWidth;
            mPrevWindowWidth = mWindowWidth;
            //  表示の更新
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.TabInfoWidth < 100 || Properties.Settings.Default.TabInfoHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.TabInfoHeight) {
                Properties.Settings.Default.TabInfoWidth = mWindowWidth;
                Properties.Settings.Default.TabInfoHeight = mWindowHeight;
            } else {
                this.Top = Properties.Settings.Default.TabInfoTop;
                this.Left = Properties.Settings.Default.TabInfoLeft;
                this.Width = Properties.Settings.Default.TabInfoWidth;
                this.Height = Properties.Settings.Default.TabInfoHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.TabInfoTop = this.Top;
            Properties.Settings.Default.TabInfoLeft = this.Left;
            Properties.Settings.Default.TabInfoWidth = this.Width;
            Properties.Settings.Default.TabInfoHeight = this.Height;
            Properties.Settings.Default.Save();
        }
    }
}
