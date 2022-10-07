using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AudioLib;
using WpfLib;
using Path = System.IO.Path;

namespace AudioApp
{
    /// <summary>
    /// TestAudio.xaml の相互作用ロジック
    /// </summary>
    public partial class SpectrumAnalyzer : Window
    {
        private double mWindowWidth;                //  ウィンドウの高さ
        private double mWindowHeight;               //  ウィンドウ幅
        private double mPrevWindowWidth;            //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        private string mAppFolder;                  //  アプリケーションフォルダ
        private string mFileFoldir;                 //  音楽ファイルフォルダー
        private string mMusicExt = "*.flac";        //  検索拡張子
        private string[] mFileTypes = { "*.flac", "*.mp3", "*.wma", "*.wav" };
        private string mFilePath;
        private int mSelectFileIndex = 0;

        private AudioLib.AudioLib audioLib = new AudioLib.AudioLib(); //  NAudioを使用するクラス
        private FileTagReader tagReader;            //  タグ情報取得クラス

        private bool mSpectrumImageDisp = false;
        private bool mSpectrumGraphDisp = false;
        private string[] mSpectrumSize = { "256", "512", "1024", "2048", "4096", "8192" };
        private string[] mSpectrumYAxis = { "dB", "リニア" };
        private string[] mSpectrumXaxis = { "リニア", "対数" };
        private long mStartPosition;                //  信号表示開始位置
        private long mEndPosition;                  //  信号表示終了位置
        private long mSampleDataSize;               //  floatデータサイズ(片チャンネル)
        private long mPosUnit;                      //  信号データ表示間隔
        private double mXmin;                       //  グラフ表示領域
        private double mXmax;                       //  グラフ表示領域
        private double mYmin;                       //  グラフ表示領域
        private double mYmax;                       //  グラフ表示領域
        private double mSpectrumYmin;               //  グラフ表示領域
        private double mSpectrumYmax;               //  グラフ表示領域
        private int mXScreenMargin = 60;            //  X軸のマージン値(スクリーン座標値)
        private int mYScreenMargin = 2;             //  X軸のマージン値(スクリーン座標値)
        private int mLastCursorIndexL = -1;         //  演奏位置表示カーソルの要素番号
        private int mLastCursorIndexR = -1;         //  演奏位置表示カーソルの要素番号
        private int mLastTextIndex = -1;            //  演奏位置時間文字列の要素番号
        private int mSpectrumLCursorIndex = -1;
        private int mSpectrumRCursorIndex = -1;
        private int mTimerInterval = 50;            //  タイマーの間隔(m seconds)
        private double mTextSize = 12;              //  文字サイズ

        private DispatcherTimer dispatcherTimer;    //  タイマーオブジェクト
        private YWorldShapes ydrawL;                //  グラフィックライブラリ
        private YWorldShapes ydrawR;                //  グラフィックライブラリ
        private YWorldShapes ydrawSpectrumL;        //  グラフィックライブラリ
        private YWorldShapes ydrawSpectrumR;        //  グラフィックライブラリ
        private YWorldShapes ydrawSpectrum;         //  グラフィックライブラリ
        private YLib ylib = new YLib();

        public SpectrumAnalyzer()
        {
            InitializeComponent();

            mWindowWidth = Width;
            mWindowHeight = Height;
            mPrevWindowWidth = mWindowWidth;
            WindowFormLoad();

            ydrawL = new YWorldShapes(SampleL);             //  左信号グラフ
            ydrawR = new YWorldShapes(SampleR);             //  右信号グラフ
            ydrawSpectrumL = new YWorldShapes(SpectrumL);   //  左スペクトラムイメージ
            ydrawSpectrumR = new YWorldShapes(SpectrumR);   //  右スペクトラムイメージ
            ydrawSpectrum = new YWorldShapes(Spectrum);     //  スペクトラムグラフ

            mAppFolder = ylib.getAppFolderPath();   //  アプリケーションフォルダ
            //  曲ファイルパスリストの読み込み
            List<string> listData = new List<string>();
            listData = ylib.loadListData(Path.Combine(mAppFolder, "MusicList.csv"));
            if (listData != null) {
                foreach (string path in listData) {
                    string[] files = getMusicFiles(path);
                    if (files != null && 0 < files.Length)
                        CbFolderList.Items.Add(path);
                }
            }
            //  音楽ファイル種別設定
            CbFileType.ItemsSource = mFileTypes;
            CbFileType.SelectedIndex = 0;
            //  コントロールの初期化
            CbSampleRate.ItemsSource = mSpectrumSize;
            CbSampleRate.SelectedIndex = 2;
            CbSampleRate.Visibility = Visibility.Hidden;
            CbYAxis.ItemsSource = mSpectrumYAxis;
            CbYAxis.SelectedIndex = 0;
            CbYAxis.Visibility = Visibility.Hidden;
            CbXAxis.ItemsSource = mSpectrumXaxis;
            CbXAxis.SelectedIndex = 0;
            CbXAxis.Visibility = Visibility.Hidden;
            LbSampleRate.Visibility = Visibility.Hidden;
            LbYAxis.Visibility = Visibility.Hidden;
            LbXAxis.Visibility = Visibility.Hidden;

            //  タイマーインスタンスの作成
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, mTimerInterval);    //  日,時,分,秒,m秒
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            musicStop();
            //  曲ファイルリストの保存
            List<string> listData = new List<string>();
            foreach (string val in CbFolderList.Items)
                listData.Add(val);
            ylib.saveListData(Path.Combine(mAppFolder, "MusicList.csv"), listData);
            audioLib.dispose();
            WindowFormSave();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.TestAudioWindowWidth < 100 || Properties.Settings.Default.TestAudioWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.TestAudioWindowHeight) {
                Properties.Settings.Default.TestAudioWindowWidth = mWindowWidth;
                Properties.Settings.Default.TestAudioWindowHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.TestAudioWindowTop;
                Left = Properties.Settings.Default.TestAudioWindowLeft;
                Width = Properties.Settings.Default.TestAudioWindowWidth;
                Height = Properties.Settings.Default.TestAudioWindowHeight;
            }
            mFileFoldir = Properties.Settings.Default.TestAudioFileFolder;
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.TestAudioWindowTop = Top;
            Properties.Settings.Default.TestAudioWindowLeft = Left;
            Properties.Settings.Default.TestAudioWindowWidth = Width;
            Properties.Settings.Default.TestAudioWindowHeight = Height;
            Properties.Settings.Default.Save();
            Properties.Settings.Default.TestAudioFileFolder = mFileFoldir;
        }

        /// <summary>
        /// Windowサイズと位置変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            //  ウィンドウの大きさに合わせてコントロールの幅を変更する
            double dx = mWindowWidth - mPrevWindowWidth;
            mPrevWindowWidth = mWindowWidth;
            mWindowState = this.WindowState;
            //  信号グラフ表示の更新
            sampleGraphInit();
            drawSample();
        }

        /// <summary>
        /// [曲フォルダの選択の変更]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbFolderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= CbFolderList.SelectedIndex) {
                mSelectFileIndex = CbFolderList.SelectedIndex;
                string musicFolder = CbFolderList.Items[CbFolderList.SelectedIndex].ToString();
                setFileListBox(musicFolder);
            }
        }

        /// <summary>
        /// [曲ファイル選択変更]
        /// 曲リストのダブルクリックによる選択実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbFileList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            mFilePath = Path.Combine(mFileFoldir, LbFileList.Items[LbFileList.SelectedIndex].ToString());
            if (File.Exists(mFilePath)) {
                if (musicDataRead(mFilePath) && mSpectrumImageDisp)
                    spectrumImage(mStartPosition, mEndPosition);
            } else {
                MessageBox.Show(mFilePath + "\nファイルがありません");
            }
        }

        /// <summary>
        /// [操作ボタン]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (bt.Name.CompareTo("BtGOpen") == 0) {
                musicFolderOpen();
            } else if (bt.Name.CompareTo("BtGSpectraImage") == 0) {
                if (mSpectrumImageDisp) {
                    mSpectrumImageDisp = false;
                    ydrawSpectrumL.clear();
                    ydrawSpectrumR.clear();
                } else {
                    mSpectrumImageDisp = true;
                    spectrumImage(mStartPosition, mEndPosition);
                }
            } else if (bt.Name.CompareTo("BtGSpectraGraph") == 0) {
                mSpectrumGraphDisp = true;
                spectrumDispSwitch();
                spectrumGraph(audioLib.getCurPosition());
            } else if (bt.Name.CompareTo("BtGInfo") == 0) {
                tagInfo();
            } else if (bt.Name.CompareTo("BtGZoomReset") == 0) {
                graphZoom(0);
            } else if (bt.Name.CompareTo("BtGZoomDown") == 0) {
                graphZoom(0.5);
            } else if (bt.Name.CompareTo("BtGZoomUp") == 0) {
                graphZoom(2);
            } else if (bt.Name.CompareTo("BtGLeftMove") == 0) {
                graphMove((mStartPosition - mEndPosition) / 2);
            } else if (bt.Name.CompareTo("BtGRightMove") == 0) {
                graphMove((mEndPosition - mStartPosition) / 2);
            } else if (bt.Name.CompareTo("BtGStop") == 0) {
                musicStop();
            } else if (bt.Name.CompareTo("BtGPause") == 0) {
                musicPause();
            } else if (bt.Name.CompareTo("BtPrevPlay") == 0) {
                musicPrevPlay();
            } else if (bt.Name.CompareTo("BtGPlay") == 0) {
                musicPlay();
            } else if (bt.Name.CompareTo("BtNextPlay") == 0) {
                musicNextPlay();
            } else if (bt.Name.CompareTo("BtGExit") == 0) {
                Close();
            }
        }

        /// <summary>
        /// [マウスクリック操作]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Sample_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (audioLib.mSampleL == null || audioLib.mSampleR == null)
                return;

            Point pt = e.GetPosition(this);
            //  波形データグラフでのマウスクリックでカーソル位置移動
            //  マウスが左チャンネル領域
            pt.X -= SampleL.Margin.Left;
            pt.Y -= SampleL.Margin.Top;
            Point wpt = ydrawL.cnvScreen2World(pt);
            if (mXmin <= wpt.X && wpt.X <= mXmax && mYmin <= wpt.Y && wpt.Y <= mYmax) {
                setCursorPosition(wpt);
                return;
            }
            //  マウスが右チャンネル領域
            pt.X += SampleL.Margin.Left - SampleR.Margin.Left;
            pt.Y += SampleL.Margin.Top - SampleR.Margin.Top;
            wpt = ydrawR.cnvScreen2World(pt);
            if (mXmin <= wpt.X && wpt.X <= mXmax && mYmin <= wpt.Y && wpt.Y <= mYmax) {
                setCursorPosition(wpt);
                return;
            }
        }

        /// <summary>
        /// [サンプル周波数変更]
        /// スペクトラムグラフのデータサイズ設定の変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbSampleRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (mSpectrumImageDisp)
            //    spectrumImage(mStartPosition, mEndPosition);
            if (mSpectrumGraphDisp)
                spectrumGraph(audioLib.getCurPosition());
        }

        /// <summary>
        /// [信号強度表示切替]
        /// Y軸の設定変更(dBとリニア)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbYAxis_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (mSpectrumImageDisp)
            //    spectrumImage(mStartPosition, mEndPosition);
            if (mSpectrumGraphDisp)
                spectrumGraph(audioLib.getCurPosition());
        }

        /// <summary>
        /// [周波数表示切替]
        /// X軸の設定変更(リニアと対数)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbXAxis_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (mSpectrumImageDisp)
            //    spectrumImage(mStartPosition, mEndPosition);
            if (mSpectrumGraphDisp)
                spectrumGraph(audioLib.getCurPosition());
        }

        /// <summary>
        /// タイマー割込み操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (audioLib.getPlayerStat() == AudioLib.AudioLib.PLAYSTAT.STOP) {
                //musicStop();
                musicNextPlay();
            }
            drawCursor();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 外部から演奏フォルダとファイルを設定し、演奏する
        /// </summary>
        /// <param name="folder">アルバムフォルダ(folder\*.mp3,folder\*.flac)</param>
        /// <param name="fileName">演奏ファイル名</param>
        public void setFolder(string folder, string fileName)
        {
            mFileFoldir = folder;
            setFileListBox(mFileFoldir);                //  フォルダーコンボボックスに登録
            if (!CbFolderList.Items.Contains(folder)) {
                CbFolderList.Items.Add(folder);
            }
            int index = CbFolderList.Items.IndexOf(folder);
            if (0 <= index)
                CbFolderList.SelectedIndex = index;
            //  曲名があれば演奏する
            if (0 < fileName.Length) {
                int n = CbFolderList.Items.IndexOf(fileName);
                CbFolderList.SelectedIndex = 0 < n ? n : 0;
                musicListPlay();
            }
        }

        /// <summary>
        /// 曲フォルダの選択
        /// </summary>
        private void musicFolderOpen()
        {
            string musicFolder = ylib.folderSelect(mFileFoldir);
            if (0 < musicFolder.Length) {
                mMusicExt = CbFileType.Items[CbFileType.SelectedIndex].ToString();
                string filePath = Path.Combine(musicFolder, mMusicExt);
                setFileListBox(filePath);
                if (!CbFolderList.Items.Contains(filePath)) {
                    CbFolderList.Items.Add(filePath);
                }
                int index = CbFolderList.Items.IndexOf(filePath);
                if (0 <= index)
                    CbFolderList.SelectedIndex = index;
            }
        }

        /// <summary>
        /// 曲のリストボックスにファイル名を設定
        /// </summary>
        /// <param name="folder"></param>
        private void setFileListBox(string path)
        {
            string[] files = getMusicFiles(path);
            if (files != null && 0 < files.Length) {
                LbFileList.Items.Clear();
                foreach (string fileName in files)
                    LbFileList.Items.Add(Path.GetFileName(fileName));
                mFileFoldir = Path.GetDirectoryName(path); ;
            } else {
                MessageBox.Show("ファイルがありません", "警告");
            }
        }

        /// <summary>
        /// 指定されたパスからファイルパスを取り出す
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string[] getMusicFiles(string path)
        {
            try {
                string folder = Path.GetDirectoryName(path);
                string ext = Path.GetFileName(path);
                return Directory.GetFiles(folder, ext);
            } catch (Exception e) {
                return null;
            }
        }

        /// <summary>
        /// 演奏開始
        /// </summary>
        private void musicPlay()
        {
            if (audioLib.getPlayerStat() == AudioLib.AudioLib.PLAYSTAT.PAUSE) {
                musicPlayContinue();            //  演奏中断の再開
            } else {
                mSelectFileIndex = LbFileList.SelectedIndex;
                musicListPlay();
            }
        }

        /// <summary>
        /// 演奏の再開
        /// </summary>
        private void musicPlayContinue()
        {
            dispatcherTimer.Start();            //  タイマー割込み開始
            audioLib.Play(-1);
        }

        /// <summary>
        /// 演奏の一時停止
        /// </summary>
        private void musicPause()
        {
            audioLib.Pause();
        }

        /// <summary>
        /// 演奏中止
        /// </summary>
        private void musicStop()
        {
            dispatcherTimer.Stop();             //  タイマー割込み停止
            audioLib.Stop();
        }

        /// <summary>
        /// 現在選択されているファイルの前曲を演奏
        /// </summary>
        private void musicPrevPlay()
        {
            if (0 < mSelectFileIndex) {
                mSelectFileIndex--;
                LbFileList.SelectedIndex = mSelectFileIndex;
                musicListPlay();
            }
        }

        /// <summary>
        /// 現在選択されているファイルの次曲を演奏
        /// </summary>
        private void musicNextPlay()
        {
            if (mSelectFileIndex < LbFileList.Items.Count - 1) {
                mSelectFileIndex++;
                LbFileList.SelectedIndex = mSelectFileIndex;
                musicListPlay();
            }
        }

        /// <summary>
        /// 曲リストボックスで選択されているファイルを演奏する
        /// </summary>
        private void musicListPlay()
        {
            if (0 < LbFileList.Items.Count) {
                musicStop();
                mFilePath = Path.Combine(mFileFoldir, LbFileList.Items[LbFileList.SelectedIndex].ToString());
                if (File.Exists(mFilePath)) {
                    if (audioLib.mPlayFile.CompareTo(mFilePath) != 0) {
                        if (musicDataRead(mFilePath)) {
                            if (mSpectrumImageDisp)
                                spectrumImage(mStartPosition, mEndPosition);
                        } else
                            return;
                    }
                    audioLib.Play(0);
                    dispatcherTimer.Start();            //  タイマー割込み開始
                }
            }
        }

        /// <summary>
        /// 音楽データの読み込み
        /// </summary>
        /// <param name="path">ファイルパス</param>
        /// <returns></returns>
        private bool musicDataRead(string path)
        {
            dispatcherTimer.Stop();
            if (!audioLib.Open(path))
                return false;

            mSpectrumGraphDisp = false;
            spectrumDispSwitch();

            //  ストリームデータをFLOATデータに変換
            audioLib.byte2floatReaderData();
            mSampleDataSize = audioLib.mSampleL.Length; //  信号グラフデータサイズ
            mStartPosition = 0;
            mEndPosition = mSampleDataSize;
            //  ファイルデータ情報表示
            if (0 < mSampleDataSize) {
                infoDataDisp(audioLib.mWaveFormat, audioLib.mDataLength, mSampleDataSize, audioLib.mTotalTime);
                //  信号グラフ表示
                sampleGraphInit();
                drawSample();
            } else {
                MessageBox.Show("FLOATにデータ変換されていません");
            }
            this.Title = "[" + Path.GetFileNameWithoutExtension(path) + "]";

            return true;
        }

        /// <summary>
        /// 波形データ表示とスペクトラムイメージ表示゜
        /// </summary>
        private void drawSample()
        {
            removeCursor();         //  カーソル消去
            drawSampleGraph(mStartPosition, mEndPosition);
            if (mSpectrumImageDisp)
                spectrumImage(mStartPosition, mEndPosition);
            drawCursor();           //  カーソル表示
        }

        /// <summary>
        /// 波形データ表示の初期化
        /// </summary>
        private void sampleGraphInit()
        {
            ydrawL.setWindowSize(SampleL.ActualWidth, SampleL.ActualHeight);
            ydrawL.setViewArea(0, 0, SampleL.ActualWidth, SampleL.ActualHeight);
            ydrawL.setAspectFix(false);            //  アスペクト比無効
            ydrawR.setWindowSize(SampleR.ActualWidth, SampleR.ActualHeight);
            ydrawR.setViewArea(0, 0, SampleR.ActualWidth, SampleR.ActualHeight);
            ydrawR.setAspectFix(false);            //  アスペクト比無効
            //  スペクトラムのイメージ表示
            ydrawSpectrumL.setWindowSize(SpectrumL.ActualWidth, SpectrumL.ActualHeight);
            ydrawSpectrumL.setViewArea(0, 0, SpectrumL.ActualWidth, SpectrumL.ActualHeight);
            ydrawSpectrumL.setAspectFix(false);    //  アスペクト比無効
            ydrawSpectrumR.setWindowSize(SpectrumR.ActualWidth, SpectrumR.ActualHeight);
            ydrawSpectrumR.setViewArea(0, 0, SpectrumR.ActualWidth, SpectrumR.ActualHeight);
            ydrawSpectrumR.setAspectFix(false);    //  アスペクト比無効
            //  スペクトラムをグラフ表示
            ydrawSpectrum.setWindowSize(Spectrum.ActualWidth, Spectrum.ActualHeight);
            ydrawSpectrum.setViewArea(0, 0, Spectrum.ActualWidth, Spectrum.ActualHeight);
            ydrawSpectrum.setAspectFix(false);    //  アスペクト比無効

            //  カーソル要素の初期化
            mLastCursorIndexL = -1;
            mLastCursorIndexR = -1;
            mLastTextIndex = -1;
            mSpectrumLCursorIndex = -1;
            mSpectrumRCursorIndex = -1;
        }

        /// <summary>
        /// 波形データの表示(FLOATデータ)
        /// </summary>
        /// <param name="startPos">開始位置</param>
        /// <param name="endPos">終了位置</param>
        private void drawSampleGraph(long startPos, long endPos)
        {
            if (audioLib.mSampleL == null || audioLib.mSampleR == null)
                return;

            double screenWidth = SampleL.ActualWidth;
            mXmin = startPos;
            mXmax = endPos;

            sampleMinMax();                                     //  グラフの上下限を設定
            //  マージンを追加
            mYmax *= 1.1;
            mYmin *= 1.1;

            //  グラフエリアの設定
            ydrawL.setWorldWindow(mXmin, mYmax, mXmax, mYmin);  //  仮のWindow設定
            double marginX = Math.Abs(ydrawL.screen2worldXlength(mXScreenMargin));  //  Window領域のマージン
            double marginY = Math.Abs(ydrawL.screen2worldYlength(mYScreenMargin));
            //  ワールド座標の設定
            ydrawL.setWorldWindow(mXmin - marginX, mYmax + marginY, mXmax + marginX, mYmin - marginY);
            ydrawR.setWorldWindow(mXmin - marginX, mYmax + marginY, mXmax + marginX, mYmin - marginY);
            ydrawL.clear();
            ydrawR.clear();

            //  文字属性の設定
            double textSize = ydrawL.screen2worldYlength(mTextSize);
            ydrawL.setTextSize(textSize);
            ydrawR.setTextSize(textSize);
            ydrawL.setTextColor(Brushes.Black);
            ydrawR.setTextColor(Brushes.Black);

            //  枠表示
            ydrawL.setColor(Brushes.Black);
            ydrawR.setColor(Brushes.Black);
            ydrawL.drawRectangle(new Point(mXmin, mYmin), new Point(mXmax, mYmax), 0);
            ydrawR.drawRectangle(new Point(mXmin, mYmin), new Point(mXmax, mYmax), 0);
            ydrawL.drawLine(new Point(mXmin, 0), new Point(mXmax, 0));
            ydrawR.drawLine(new Point(mXmin, 0), new Point(mXmax, 0));
            ydrawL.drawText(" LEFT", new Point(mXmax, 0), 0, HorizontalAlignment.Left, VerticalAlignment.Center);
            ydrawR.drawText(" RIGHT", new Point(mXmax, 0), 0, HorizontalAlignment.Left, VerticalAlignment.Center);

            //  開始時刻と終了時刻の表示
            string startTime = ylib.second2String(audioLib.floatDataPos2Second((int)mXmin), true);
            string endTime = ylib.second2String(audioLib.floatDataPos2Second((int)mXmax), true);
            ydrawL.drawText(startTime, new Point(mXmin, mYmax), 0, HorizontalAlignment.Center, VerticalAlignment.Bottom);
            ydrawL.drawText(endTime, new Point(mXmax, mYmax), 0, HorizontalAlignment.Center, VerticalAlignment.Bottom);

            //  信号データの表示
            ydrawL.setColor(Brushes.Green);
            ydrawR.setColor(Brushes.Green);
            //  表示密度設定
            mPosUnit = Math.Max((long)((mXmax - mXmin) / screenWidth), 1);
            long posUnit = Math.Max(mPosUnit / 4, 1);
            startPos = Math.Max(startPos, 0);
            endPos = Math.Min(endPos, mSampleDataSize);

            //  波形の表示
            long prevpos = startPos;
            for (long i = startPos + mPosUnit; i < endPos - mPosUnit; i += mPosUnit) {
                if (3 < mPosUnit) {
                    float maxL = audioLib.mSampleL[i];
                    float minL = audioLib.mSampleL[i];
                    float maxR = audioLib.mSampleR[i];
                    float minR = audioLib.mSampleR[i];
                    for (long j = i + 1; j < i + mPosUnit; j++) {
                        maxL = Math.Max(maxL, audioLib.mSampleL[j]);
                        minL = Math.Min(minL, audioLib.mSampleL[j]);
                        maxR = Math.Max(maxR, audioLib.mSampleR[j]);
                        minR = Math.Min(minR, audioLib.mSampleR[j]);
                    }
                    ydrawL.drawLine(new Point(i, minL), new Point(i, maxL));
                    ydrawR.drawLine(new Point(i, minR), new Point(i, maxR));
                } else {
                    ydrawL.drawLine(new Point(prevpos, audioLib.mSampleL[prevpos]), new Point(i, audioLib.mSampleL[i]));
                    ydrawR.drawLine(new Point(prevpos, audioLib.mSampleR[prevpos]), new Point(i, audioLib.mSampleR[i]));
                    prevpos = i;
                }
            }
        }

        /// <summary>
        /// 信号波形データの最小最大値を求める
        /// </summary>
        private void sampleMinMax()
        {
            mYmax = 0;
            mYmin = 0;
            for (long i = 0; i < audioLib.mSampleL.Length; i++) {
                mYmax = Math.Max(mYmax, audioLib.mSampleL[i]);
                mYmin = Math.Min(mYmin, audioLib.mSampleL[i]);
                mYmax = Math.Max(mYmax, audioLib.mSampleR[i]);
                mYmin = Math.Min(mYmin, audioLib.mSampleR[i]);
            }
        }

        /// <summary>
        /// 信号グラフのズーム処理
        /// </summary>
        /// <param name="zoom">倍率</param>
        private void graphZoom(double zoom)
        {
            if (zoom == 0) {
                mStartPosition = 0;
                mEndPosition = mSampleDataSize;
            } else {
                long length = (long)((mEndPosition - mStartPosition) / zoom);
                long curPos = audioLib.streamPos2FloatDataPos(audioLib.getCurPosition());
                mStartPosition = curPos - length / 2;
                mEndPosition = curPos + length / 2;

                length = mEndPosition - mStartPosition;
                if (mSampleDataSize < length) {
                    mStartPosition = 0;
                    mEndPosition = mSampleDataSize;
                }
                if (mStartPosition < 0) {
                    mStartPosition = 0;
                    mEndPosition = mStartPosition + length;
                }
                if (mSampleDataSize < mEndPosition) {
                    mEndPosition = mSampleDataSize;
                    mStartPosition = mEndPosition - length;
                }
                if (mStartPosition < 0) {
                    mStartPosition = 0;
                }
            }
            drawSample();
        }


        /// <summary>
        /// 表示領域の移動
        /// </summary>
        /// <param name="length"></param>
        private void graphMove(long moveSize)
        {
            long length = mEndPosition - mStartPosition;
            if (mSampleDataSize < moveSize)
                moveSize = mSampleDataSize;
            mStartPosition += moveSize;
            mEndPosition += moveSize;
            if (mStartPosition < 0) {
                mStartPosition = 0;
                mEndPosition = mStartPosition + length;
            }
            if (mSampleDataSize < mEndPosition) {
                mEndPosition = mSampleDataSize;
                mStartPosition = mEndPosition - length;
            }
            mStartPosition = Math.Max(0, mStartPosition);
            mEndPosition = Math.Min(mSampleDataSize, mEndPosition);

            drawSample();
        }

        /// <summary>
        /// 指定位置にカーソルを表示
        /// スペクトラム表示状態であればその位置のスペクトラムを表示する
        /// </summary>
        /// <param name="wpt">マウスの位置</param>
        private void setCursorPosition(Point wpt)
        {
            audioLib.setCurPosition(audioLib.floatDataPos2StreamPos((int)wpt.X));
            drawCursor();
            if (mSpectrumGraphDisp)
                spectrumGraph(audioLib.getCurPosition());
        }

        /// <summary>
        /// 演奏位置のカーソル表示
        /// </summary>
        private void drawCursor()
        {
            if (audioLib.mDataLength <= 0)
                return;
            //  前回表示したカーソルと時間を削除
            removeCursor();
            //  現在の演奏位置
            int position = audioLib.streamPos2FloatDataPos(audioLib.getCurPosition());
            if (mXmax < position) {
                //  表示領域の変更
                long length = mEndPosition - mStartPosition;
                mStartPosition += length;
                mEndPosition += length;
                drawSample();
            } else {
                //  現在の演奏位置時間の表示
                ydrawL.setColor(Brushes.Black);
                string time = ylib.second2String(audioLib.getCurPositionSecond(), true);
                ydrawL.drawText(time, new Point(position, mYmax), 0, HorizontalAlignment.Center, VerticalAlignment.Bottom);
                mLastTextIndex = ydrawL.getLastIndex();
                //  演奏位置のカーソル表示
                ydrawL.setColor(Brushes.Red);
                ydrawR.setColor(Brushes.Red);
                ydrawL.drawLine(new Point(position, mYmin), new Point(position, mYmax));
                ydrawR.drawLine(new Point(position, mYmin), new Point(position, mYmax));
                mLastCursorIndexL = ydrawL.getLastIndex();
                mLastCursorIndexR = ydrawR.getLastIndex();
                //  スペクトラムイメージのカーソル表示(負荷が高い?)
                if (0 < ydrawSpectrumL.getItemCount()) {
                    ydrawSpectrumL.setColor(Brushes.Red);
                    ydrawSpectrumL.drawLine(new Point(position, mSpectrumYmin), new Point(position, mSpectrumYmax));
                    mSpectrumLCursorIndex = ydrawSpectrumL.getLastIndex();
                    ydrawSpectrumR.setColor(Brushes.Red);
                    ydrawSpectrumR.drawLine(new Point(position, mSpectrumYmin), new Point(position, mSpectrumYmax));
                    mSpectrumRCursorIndex = ydrawSpectrumR.getLastIndex();
                }
            }
        }

        /// <summary>
        /// カーソルの消去
        /// </summary>
        private void removeCursor()
        {
            //  最後に描画したものから順番に削除しないとインデックスNoが変わる
            if (0 <= mSpectrumLCursorIndex)
                ydrawSpectrumL.removeElement(mSpectrumLCursorIndex);
            if (0 <= mSpectrumRCursorIndex)
                ydrawSpectrumR.removeElement(mSpectrumRCursorIndex);
            if (0 <= mLastCursorIndexL && 0 <= mLastCursorIndexR) {
                ydrawL.removeElement(mLastCursorIndexL);
                ydrawR.removeElement(mLastCursorIndexR);
            }
            if (0 <= mLastTextIndex)
                ydrawL.removeElement(mLastTextIndex);
        }

        /// <summary>
        /// 音楽データの情報を表示
        /// </summary>
        /// <param name="waveFormat">WAVEフォーマット</param>
        /// <param name="streamLength">ストリームデータサイズ</param>
        /// <param name="totalTime">演奏時間</param>
        private void infoDataDisp(WaveFormat waveFormat, long streamLength, long sampleDataSize, TimeSpan totalTime)
        {
            LbInfo.Content = string.Format("{0} channels {1} {2} bit {3} block {4} Hz " +
                "ストリームサイズ {5} サンプル数 {6} 演奏時間 {7} ",
                waveFormat.Channels, waveFormat.Encoding.ToString(), waveFormat.BitsPerSample,
                waveFormat.BlockAlign, waveFormat.SampleRate.ToString("#,0"),
                streamLength.ToString("#,0"), sampleDataSize.ToString("#,0"), totalTime.ToString(@"hh\:mm\:ss"));
        }

        /// <summary>
        /// タグ情報の表示
        /// </summary>
        private void tagInfo()
        {   if (0 < LbFileList.Items.Count && 0 <= LbFileList.SelectedIndex) {
                mFilePath = Path.Combine(mFileFoldir, LbFileList.Items[LbFileList.SelectedIndex].ToString());
                tagReader = new FileTagReader(mFilePath);
                List<string> tagInfoList = tagReader.getTagList();
                TagInfo tagInfo = new TagInfo();
                tagInfo.mTagInfoList = tagInfoList;
                tagInfo.mImageData = tagReader.getImageData(0);

                tagInfo.Show();
            } else {
                MessageBox.Show("ファイルが選択されていません");
            }
        }

        /// <summary>
        /// スペクトラムのイメージ表示
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="endPos"></param>
        private void spectrumImage(long startPos, long endPos)
        {
            if (audioLib.mSampleL == null || audioLib.mSampleR == null)
                return;

            int dispBitsRate = audioLib.getSampleRate();    //  サンプリング周波数
            int sampleCount = int.Parse(CbSampleRate.Items[CbSampleRate.SelectedIndex].ToString()); //  データサイズ
            bool yaxisdB = true;    //  CbYAxis.SelectedIndex == 0;     //  信号強度 (dB/リニア表示)
            bool xaxisLog = false;  //  CbXAxis.SelectedIndex == 1;     //  周波数 (リニア/対数表示)

            //  グラフエリアの設定
            mSpectrumYmax = dispBitsRate / 2;
            mSpectrumYmin = 0;
            ydrawSpectrumL.setWorldWindow(mXmin, mSpectrumYmax, mXmax, mSpectrumYmin);      //  仮のWindow設定
            ydrawSpectrumR.setWorldWindow(mXmin, mSpectrumYmax, mXmax, mSpectrumYmin);      //  仮のWindow設定
            double marginX = Math.Abs(ydrawSpectrumL.screen2worldXlength(mXScreenMargin));  //  Window領域のマージン
            double marginY = Math.Abs(ydrawSpectrumL.screen2worldYlength(mYScreenMargin));
            ydrawSpectrumL.setWorldWindow(mXmin - marginX, mSpectrumYmax + marginY, mXmax + marginX, mSpectrumYmin - marginY);
            ydrawSpectrumL.clear();
            ydrawSpectrumR.setWorldWindow(mXmin - marginX, mSpectrumYmax + marginY, mXmax + marginX, mSpectrumYmin - marginY);
            ydrawSpectrumR.clear();
            //  信号強度の範囲
            double signalMin = val2dB(0.0001, yaxisdB);
            double signalMax = val2dB(10, yaxisdB);
            //  周波数の範囲(スクリーン座標での長さ)
            double scHeight = Math.Abs(ydrawSpectrumL.world2screenYlength(mSpectrumYmax - mSpectrumYmin));
            ydrawSpectrumL.setThickness(2);
            ydrawSpectrumR.setThickness(2);
            //double min = 0.5, max = 0.5;
            long sampleStep = mPosUnit * 3;     //  サンプリングステップ
            int freqStep = 0;                   //  周波数の表示ステップ
            for (long j = (long)mXmin + 1; j < (long)mXmax - sampleCount; j += sampleStep) {
                List<float> sampleL = new List<float>();
                List<float> sampleR = new List<float>();
                for (int i = (int)j; i < ((int)j + sampleCount) && i < audioLib.mSampleL.Length; i++) {
                    sampleL.Add(audioLib.mSampleL[i]);
                    sampleR.Add(audioLib.mSampleR[i]);
                }
                //  左チャンネルのスペクトラムイメージ
                List<Complex> spectrumL = audioLib.DoFourier(sampleL, dispBitsRate);    //  FFT
                freqStep = (int)Math.Max((spectrumL.Count / scHeight) * 2, 2);  //  周波数の表示ステップ
                for (int i = 0; i < spectrumL.Count - freqStep; i += freqStep) {
                    if (xaxisLog && spectrumL[i].Real <= 10)
                        continue;
                    //  周波数ごとの信号強度を色変換して表示
                    val2color(ydrawSpectrumL, val2dB(spectrumL[i].Imaginary, yaxisdB), signalMin, signalMax);
                    ydrawSpectrumL.drawLine(new Point(j, val2Log(spectrumL[i].Real, xaxisLog)), 
                        new Point(j, val2Log(spectrumL[i + freqStep].Real, xaxisLog)));
                }
                //  右ャンネルのスペクトラムイメージ
                List<Complex> spectrumR = audioLib.DoFourier(sampleR, dispBitsRate);
                freqStep = (int)Math.Max((spectrumR.Count / scHeight) * 2, 2);  //  周波数の表示ステップ
                for (int i = 0; i < spectrumR.Count - freqStep; i += freqStep) {
                    if (xaxisLog && spectrumR[i].Real <= 10)
                        continue;
                    //  周波数ごとの信号強度を色変換して表示
                    val2color(ydrawSpectrumR, val2dB(spectrumR[i].Imaginary, yaxisdB), signalMin, signalMax);
                    ydrawSpectrumR.drawLine(new Point(j, val2Log(spectrumR[i].Real, xaxisLog)),
                        new Point(j, val2Log(spectrumR[i + freqStep].Real, xaxisLog)));
                }
            }

            //  文字サイズ設定
            double textSize = Math.Abs(ydrawSpectrumL.screen2worldYlength(mTextSize));
            ydrawSpectrumL.setTextSize(textSize);
            ydrawSpectrumR.setTextSize(textSize);
            //  枠kと目盛の表示(左チャンネル)
            ydrawSpectrumL.setThickness(1);
            ydrawSpectrumL.setColor(Brushes.Black);
            ydrawSpectrumL.drawRectangle(new Point(mXmin, mSpectrumYmin), new Point(mXmax, mSpectrumYmax), 0);
            ydrawSpectrumL.setTextSize(ydrawSpectrumL.screen2worldYlength(mTextSize));
            for (double y = mSpectrumYmin; y < mSpectrumYmax; y += 10000) {
                ydrawSpectrumL.drawText((y / 1000).ToString("#,0KHz"), new Point(mXmin, y), 0, HorizontalAlignment.Right, VerticalAlignment.Center);
            }
            ydrawSpectrumL.drawText((mSpectrumYmax / 1000).ToString("#,0KHz"), new Point(mXmin, mSpectrumYmax), 0, HorizontalAlignment.Right, VerticalAlignment.Center);
            ydrawSpectrumL.drawText("LEFT", new Point(mXmax, mSpectrumYmax - textSize / 5), 0, HorizontalAlignment.Right, VerticalAlignment.Bottom);
            //  枠kと目盛の表示(右チャンネル)
            ydrawSpectrumR.setThickness(1);
            ydrawSpectrumR.setColor(Brushes.Black);
            ydrawSpectrumR.drawRectangle(new Point(mXmin, mSpectrumYmin), new Point(mXmax, mSpectrumYmax), 0);
            ydrawSpectrumR.setTextSize(ydrawSpectrumR.screen2worldYlength(mTextSize));
            for (double y = mSpectrumYmin; y < mSpectrumYmax; y += 10000) {
                ydrawSpectrumR.drawText((y / 1000).ToString("#,0KHz"), new Point(mXmin, y), 0, HorizontalAlignment.Right, VerticalAlignment.Center);
            }
            ydrawSpectrumR.drawText((mSpectrumYmax / 1000).ToString("#,0KHz"), new Point(mXmin, mSpectrumYmax), 0, HorizontalAlignment.Right, VerticalAlignment.Center);
            ydrawSpectrumR.drawText("RIGHT", new Point(mXmax, mSpectrumYmax - textSize / 5), 0, HorizontalAlignment.Right, VerticalAlignment.Bottom);

            //  信号強度の色見本
            double width = Math.Abs(ydrawSpectrumL.screen2worldXlength(10));
            colorLegend(mSpectrumYmin, mSpectrumYmax, width, ydrawSpectrumL);
            colorLegend(mSpectrumYmin, mSpectrumYmax, width, ydrawSpectrumR);
            //  色見本の目盛り
            ydrawSpectrumL.drawText(signalMax.ToString("#,0dB"), new Point(mXmax + width, mSpectrumYmax), 0, HorizontalAlignment.Left, VerticalAlignment.Center);
            ydrawSpectrumL.drawText(signalMin.ToString("#,0dB"), new Point(mXmax + width, mSpectrumYmin), 0, HorizontalAlignment.Left, VerticalAlignment.Center);
            ydrawSpectrumR.drawText(signalMax.ToString("#,0dB"), new Point(mXmax + width, mSpectrumYmax), 0, HorizontalAlignment.Left, VerticalAlignment.Center);
            ydrawSpectrumR.drawText(signalMin.ToString("#,0dB"), new Point(mXmax + width, mSpectrumYmin), 0, HorizontalAlignment.Left, VerticalAlignment.Center);
        }

        /// <summary>
        /// 特定位置でのスペクトラムグラフ表示
        /// </summary>
        /// <param name="pos"></param>
        private void spectrumGraph(long pos)
        {
            if (audioLib.mSampleL == null || audioLib.mSampleR == null)
                return;

            int dispBitsRate = audioLib.getSampleRate();
            int sampleCount = int.Parse(CbSampleRate.Items[CbSampleRate.SelectedIndex].ToString());
            bool yaxisdB = CbYAxis.SelectedIndex == 0;
            bool xaxisLog = CbXAxis.SelectedIndex == 1;

            double xmin=0, xmax=0;
            double ymin=0, ymax=0;

            //  フーリエ変換するためのデータ抽出
            List<float> sampleL = new List<float>();
            List<float> sampleR = new List<float>();
            int startPos = (int)audioLib.streamPos2FloatDataPos(pos);
            for (int i = startPos; i < startPos + sampleCount; i++) {
                sampleL.Add(audioLib.mSampleL[i]);
                sampleR.Add(audioLib.mSampleR[i]);
            }
            //  フーリエ変換
            List<Complex> spectrumL = audioLib.DoFourier(sampleL, dispBitsRate);
            List<Complex> spectrumR = audioLib.DoFourier(sampleR, dispBitsRate);
            //  変換データの最小最大値
            xmin = spectrumL[0].Real;
            xmax = spectrumL[0].Real;
            ymin = spectrumL[0].Imaginary;
            ymax = spectrumL[0].Imaginary;
            for (int i = 1; i < spectrumL.Count; i++) {
                xmin = Math.Min(xmin, spectrumL[i].Real);
                xmin = Math.Min(xmin, spectrumR[i].Real);
                xmax = Math.Max(xmax, spectrumL[i].Real);
                xmax = Math.Max(xmax, spectrumR[i].Real);
                ymin = Math.Min(ymin, spectrumL[i].Imaginary);
                ymin = Math.Min(ymin, spectrumR[i].Imaginary);
                ymax = Math.Max(ymax, spectrumL[i].Imaginary);
                ymax = Math.Max(ymax, spectrumR[i].Imaginary);
            }
            xmin = xaxisLog ? val2Log(10, xaxisLog) : xmin;     //  表示周波数の最小値
            xmax = val2Log(dispBitsRate / 2 + 3000, xaxisLog);  //  表示周波数の最大値(サンプリング周波数の1/2+α)
            ymin = val2dB(0.0001, yaxisdB);                     //  信号強度の最小値
            ymax = val2dB(20, yaxisdB);                         //  信号強度の最大値

            //  グラフエリアの設定
            ydrawSpectrum.setWorldWindow(xmin, ymax, xmax, ymin);   //  仮のWindow設定
            double marginX = Math.Abs(ydrawSpectrum.screen2worldXlength(20)); //  Window領域のマージン
            double marginY = Math.Abs(ydrawSpectrum.screen2worldYlength(2));
            ydrawSpectrum.setWorldWindow(xmin - marginX, ymax + marginY, xmax + marginX, ymin - marginY);
            //  表示枠
            ydrawSpectrum.clear();
            ydrawSpectrum.setColor(Brushes.Black);
            ydrawSpectrum.drawRectangle(new Point(xmin, ymin), new Point(xmax, ymax), 0);
            ydrawSpectrum.setTextSize(ydrawSpectrum.screen2worldYlength(12));
            ydrawSpectrum.setThickness(1);
            //  文字属性の設定
            double textSize = Math.Abs(ydrawSpectrum.screen2worldYlength(mTextSize));
            ydrawSpectrum.setTextSize(textSize);
            ydrawSpectrum.setTextColor(Brushes.Black);
            //  サンプル周波数の目盛り
            if (xaxisLog) {
                for (double x = xmin; x < xmax; x++) {
                    ydrawSpectrum.drawText(Math.Pow(10,x).ToString("#,0Hz"),
                        new Point(x, ymin), 0, HorizontalAlignment.Center, VerticalAlignment.Top);
                    ydrawSpectrum.drawLine(new Point(x, ymin), new Point(x, ymax));
                }
            } else {
                for (double x = xmin; x < xmax; x += 5000) {
                    ydrawSpectrum.drawText(x.ToString("#,0Hz"), new Point(x, ymin), 0, HorizontalAlignment.Center, VerticalAlignment.Top);
                    ydrawSpectrum.drawLine(new Point(x, ymin), new Point(x, ymax));
                }
            }
            //  縦軸 信号強度
            if (yaxisdB) {
                for (double y = ymin; y <= ymax; y += 20) {
                    ydrawSpectrum.drawText(y.ToString("#,0dB"), new Point(xmin, y), 0, HorizontalAlignment.Right, VerticalAlignment.Center);
                    ydrawSpectrum.drawLine(new Point(xmin, y), new Point(xmax, y));
                }
            } else {
                for (double y = ymin + 2; y < ymax; y += 2) {
                    ydrawSpectrum.drawText(y.ToString("#,0"), new Point(xmin, y), 0, HorizontalAlignment.Right, VerticalAlignment.Center);
                    ydrawSpectrum.drawLine(new Point(xmin, y), new Point(xmax, y));
                }
            }
            //  スペクトラム表示
            ydrawSpectrum.setColor(Brushes.Blue);
            for (int i = 0; i < spectrumL.Count - 1; i++) {
                if ((xaxisLog && spectrumL[i].Real <= 10) || (yaxisdB && spectrumL[i].Imaginary <= 0))
                    continue;
                ydrawSpectrum.drawLine(new Point(val2Log(spectrumL[i].Real, xaxisLog), val2dB(spectrumL[i].Imaginary, yaxisdB)), 
                    new Point(val2Log(spectrumL[i + 1].Real, xaxisLog), val2dB(spectrumL[i + 1].Imaginary, yaxisdB)));
            }
            ydrawSpectrum.setTextColor(Brushes.Blue);
            ydrawSpectrum.drawText("Left(Blue)", new Point(xmax, ymax - textSize), 0, HorizontalAlignment.Right, VerticalAlignment.Top);
            ydrawSpectrum.setColor(Brushes.Red);
            for (int i = 0; i < spectrumR.Count - 1; i++) {
                if ((xaxisLog && spectrumR[i].Real <= 10) || (yaxisdB && spectrumR[i].Imaginary <= 0))
                    continue;
                ydrawSpectrum.drawLine(new Point(val2Log(spectrumR[i].Real, xaxisLog), val2dB(spectrumR[i].Imaginary, yaxisdB)),
                    new Point(val2Log(spectrumR[i + 1].Real, xaxisLog), val2dB(spectrumR[i + 1].Imaginary, yaxisdB)));
            }
            ydrawSpectrum.setTextColor(Brushes.Red);
            ydrawSpectrum.drawText("Right(Red)", new Point(xmax, ymax - textSize * 2), 0, HorizontalAlignment.Right, VerticalAlignment.Top);
        }

        /// <summary>
        /// スペクトラム表示の切り替え設定
        /// </summary>
        private void spectrumDispSwitch()
        {
            if (mSpectrumGraphDisp) {
                //  スペクトラムグラフ表示
                CbSampleRate.Visibility = Visibility.Visible;
                CbYAxis.Visibility = Visibility.Visible;
                CbXAxis.Visibility = Visibility.Visible;
                LbSampleRate.Visibility = Visibility.Visible;
                LbYAxis.Visibility = Visibility.Visible;
                LbXAxis.Visibility = Visibility.Visible;
                //SpectrumL.Visibility = Visibility.Hidden;
                //ydrawSpectrumL.clear();
            } else {
                //  スペクトラムグラフ非表示
                CbSampleRate.Visibility = Visibility.Hidden;
                CbYAxis.Visibility = Visibility.Hidden;
                CbXAxis.Visibility = Visibility.Hidden;
                LbSampleRate.Visibility = Visibility.Hidden;
                LbYAxis.Visibility = Visibility.Hidden;
                LbXAxis.Visibility = Visibility.Hidden;
                ydrawSpectrum.clear();
                ydrawSpectrumL.clear();
            }
        }

        /// <summary>
        /// 対数値に変換
        /// </summary>
        /// <param name="val">数値</param>
        /// <param name="log">変換の有無</param>
        /// <returns>対数値</returns>
        private double val2Log(double val, bool log)
        {
            return log ? Math.Log10(val) : val;
        }

        /// <summary>
        /// デシベル値(dB)に変換
        /// </summary>
        /// <param name="val">数値</param>
        /// <param name="db">変換の有無</param>
        /// <returns>dB値</returns>
        private double val2dB(double val, bool db)
        {
            return db ? 20 * Math.Log10(val) :val;
        }

        /// <summary>
        /// 色見本
        /// </summary>
        /// <param name="ymin"></param>
        /// <param name="ymax"></param>
        private void colorLegend(double ymin, double ymax, double width, YWorldShapes ydrawSpectrum)
        {
            double xmin = mXmax;
            double xmax = mXmax + width;
            double scHeight = Math.Abs(ydrawSpectrum.world2screenYlength(ymax - ymin));
            ydrawSpectrum.setThickness(1);
            for (double y = ymin; y < ymax; y += (ymax - ymin) / scHeight) {
                val2color(ydrawSpectrum, y, ymin, ymax);
                ydrawSpectrum.drawLine(new Point(xmin, y), new Point(xmax, y));
            }
            ydrawSpectrum.setColor(Brushes.Black);
            ydrawSpectrum.drawRectangle(new Point(xmin, ymin), new Point(xmax, ymax), 0);

            System.Diagnostics.Debug.WriteLine("colorLegend 要素数 {0}", scHeight);
        }

        /// <summary>
        /// 数値を変換して色を設定する
        /// </summary>
        /// <param name="drawShaps">図形クラス</param>
        /// <param name="val">数値</param>
        /// <param name="min">数値の最小値</param>
        /// <param name="max">数値の最大値</param>
        public void val2color(YDrawingShapes drawShaps, double val, double min, double max)
        {
            float rv, gv, bv;
            float nVal = (float)((val - min) / (max - min));
            if (nVal < 0)
                nVal = 0f;
            if (1.0 < nVal)
                nVal = 1.0f;
            if (nVal < 0.5) {
                rv = 1f - nVal * 2.0f;
                gv = 1f;
                bv = 1f - nVal * 2.0f;
            } else {
                rv = nVal * 2.0f - 1f;
                gv = 2 - nVal * 2.0f;
                bv = 0f;
            }
            drawShaps.setColor(0xff, (byte)(rv * 0xff), (byte)(gv * 0xff), (byte)(bv * 0xff));
        }
    }
}
