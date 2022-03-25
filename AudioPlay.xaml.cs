using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AudioLib;
using WpfLib;
using Path = System.IO.Path;

namespace AudioApp
{
    /// <summary>
    /// AudioPlay.xaml の相互作用ロジック
    /// 
    /// 参考 : NAudio C# プログラミング解説
    ///         https://so-zou.jp/software/tech/programming/c-sharp/media/audio/naudio/
    ///     C#で音声波形を表示する音楽プレーヤーを作る
    ///         https://qiita.com/lenna_kun/items/a0f03447bb893c9ab937
    /// </summary>
    public partial class AudioPlay : Window
    {
        private double mWindowWidth;                //  ウィンドウの高さ
        private double mWindowHeight;               //  ウィンドウ幅

        //  OxyPlot(LeftWave)
        private PlotModel mLeftPlot = new PlotModel();
        private LinearAxis mLeftLinearAxisX = new LinearAxis {  //  時間軸
            Minimum = 0,
            Maximum = mXRange,
            Position = AxisPosition.Bottom
        };
        private LinearAxis mLeftLinearAxisY = new LinearAxis {  //  レベル軸
            Minimum = -0.2,
            Maximum = 0.2,
            Position = AxisPosition.Left
        };
        private LineSeries mLeftLineSeries = new LineSeries();
        //  OxyPlot(RightWave)
        private PlotModel mRightPlot = new PlotModel();
        private LinearAxis mRightLinearAxisX = new LinearAxis {  //  時間軸
            Minimum = 0,
            Maximum = mXRange,
            Position = AxisPosition.Bottom
        };
        private LinearAxis mRightLinearAxisY = new LinearAxis {  //  レベル軸
            Minimum = -0.2,
            Maximum = 0.2,
            Position = AxisPosition.Left
        };
        private LineSeries mRightLineSeries = new LineSeries();

        //  OxyPlot(LeftFFT)
        private PlotModel mLeftPlotFFT = new PlotModel();
        private LineSeries mLeftLineSeriesFFT = new LineSeries();
        private LinearAxis mLeftFFTAxisY = new LinearAxis {     //  Y軸
            Maximum = mFftVolMax,
            Minimum = 0,
            Position = AxisPosition.Left
        };
        private LinearAxis mLeftFFTAxisX = new LinearAxis {     //  X軸 周波数
            Maximum = 4000,
            Minimum = 0,
            Position = AxisPosition.Bottom
        };
        //  OxyPlot(RightFFT)
        private PlotModel mRightPlotFFT = new PlotModel();
        private LineSeries mRightLineSeriesFFT = new LineSeries();
        private LinearAxis mRightFFTAxisY = new LinearAxis {     //  Y軸
            Maximum = mFftVolMax,
            Minimum = 0,
            Position = AxisPosition.Left
        };
        private LinearAxis mRightFFTAxisX = new LinearAxis {     //  X軸 周波数
            Maximum = 4000,
            Minimum = 0,
            Position = AxisPosition.Bottom
        };

        private double mMaxLevel = 1.0;                 //  音量の最大レベル
        private static double mXRange = 400;            //  1フレーム分のデータ数
        private static double mFftVolMax = 30;          //  FFTグラフの最大値
        private int mFrameInterval;                     //  表示フレームの間隔(m秒)
        private int mIntevalRate = 2000;                //  表示フレーム間隔比率
        private int mDispBitsRate = 8000;               //  表示用のサンプルレート

        private string mPlayLength;                     //  演奏時間の文字列
        private long mPlayPosition = 0;                 //  演奏位置
        private double mSlPrevPosition = 0;             //  スライダーの演奏位置

        private string mPlayFile = "";                  //  演奏中のファイル
        private string mDataFolder;                     //  曲フォルダ
        private SortedSet<string> mDataFolderList;      //  曲フォルダーリスト
        private string mAppFolder;                      //  アプリフォルダ
        private string mFolderListFileName = "FolderList.csv";  //  曲フォルダーリストファイル名
        private string[] mFileTypes = { "*.mp3", "*.flac", "*.wma", "*.wav" };
        private string mMusicExt = "*.mp3";             //  検索拡張子
        private float mLowPassFriq = 0;                 //  ローパスフィルタのカット周波数
        private string[] mLowPassFriqList = {           //  ローパスフィルタメニュータイトル
            "なし","100","200","300","500","800" ,
            "1000","2000","3000","5000","8000","10000","20000"};

        private DispatcherTimer dispatcherTimer;    // タイマーオブジェクト

        private AudioLib.AudioLib audioLib;           //  NAudioを処するオーディオライブラリ
        private YLib ylib;

        public AudioPlay()
        {
            InitializeComponent();

            mWindowWidth = Width;
            mWindowHeight = Height;
            WindowFormLoad();
            ylib = new YLib();
            audioLib = new AudioLib.AudioLib();
            ExtensionList.ItemsSource = mFileTypes;
            ExtensionList.SelectedIndex = 0;

            //  タイマーインスタンスの作成
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);

            //  グラフ表示の初期化
            InitPlot();

            //  データファイルフォルダ
            mDataFolder = Properties.Settings.Default.AudioPlayFolder;
            mDataFolderList = new SortedSet<string>();
            loadFolderList();
        }

        private void AudioPlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            setFileListBox(mDataFolder);
            FolderList.SelectedIndex = FolderList.Items.IndexOf(mDataFolder);
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Stop();
            Properties.Settings.Default.AudioPlayFolder = mDataFolder;
            saveFolderList();
            WindowFormSave();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.AudioPlayWindowWidth < 100 || Properties.Settings.Default.AudioPlayWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.AudioPlayWindowHeight) {
                Properties.Settings.Default.AudioPlayWindowWidth = mWindowWidth;
                Properties.Settings.Default.AudioPlayWindowHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.AudioPlayWindowTop;
                Left = Properties.Settings.Default.AudioPlayWindowLeft;
                Width = Properties.Settings.Default.AudioPlayWindowWidth;
                //AudioPlayWindow.Height = Properties.Settings.Default.AudioPlayWindowHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.AudioPlayWindowTop = Top;
            Properties.Settings.Default.AudioPlayWindowLeft = Left;
            Properties.Settings.Default.AudioPlayWindowWidth = Width;
            Properties.Settings.Default.AudioPlayWindowHeight = Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// [ボタン]処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (bt.Name.CompareTo("BtPlay") == 0) {
                if (0 <= FileList.SelectedIndex) {
                    //  ファイルが選択されている場合
                    PlayFile(FileList.SelectedItem.ToString());
                } else if (0 < FileList.Items.Count) {
                    //  ファイルが選択されていない場合
                    FileList.SelectedIndex = 0;
                    PlayFile(FileList.Items[0].ToString());
                }
            } else if (bt.Name.CompareTo("BtPause") == 0) {
                Pause();
            } else if (bt.Name.CompareTo("BtStop") == 0) {
                Stop();
            } else if (bt.Name.CompareTo("BtOpen") == 0) {
                setFileList();
            } else if (bt.Name.CompareTo("BtExit") == 0) {
                Close();
            }
        }

        /// <summary>
        /// [ファイルリストのダブルクリック]
        /// 選択したファイルの演奏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PlayFile(FileList.SelectedItem.ToString());
        }

        /// <summary>
        /// [フォルダリストのコンボボックスの選択変更]
        /// ファイルリストの表示更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FolderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= FolderList.SelectedIndex) {
                mDataFolder = FolderList.SelectedItem.ToString();
                setFileListBox(mDataFolder);
            }
        }

        /// <summary>
        /// [ローパスフィルタのコンボボックスの選択変更]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LowPassList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LowPassList.SelectedIndex == 0) {
                mLowPassFriq = 0;
            } else {
                mLowPassFriq = float.Parse(LowPassList.Items[LowPassList.SelectedIndex].ToString());
            }
        }

        /// <summary>
        /// 外部から演奏フォルダとファイルを設定し、演奏する
        /// </summary>
        /// <param name="folder">アルバムフォルダ(folder\*.mp3,folder\*.flac)</param>
        /// <param name="fileName">演奏ファイル名</param>
        public void setFolder(string folder, string fileName)
        {
            mDataFolder = folder;
            setFileListBox(mDataFolder);                //  フォルダーコンボボックスに登録
            setFolderList();                            //  曲リストに登録
            //  曲名があれば演奏する
            if (0 < fileName.Length) {
                int n = FileList.Items.IndexOf(fileName);
                FileList.SelectedIndex = 0 < n ? n : 0;
                PlayFile(FileList.Items[0].ToString());
            }
        }

        /// <summary>
        /// 指定したファイルの演奏を開始する
        /// </summary>
        /// <param name="filename">音楽ファイル名</param>
        private void PlayFile(string filename)
        {
            if (audioLib.IsStopped() || mPlayFile.CompareTo(filename) != 0) {
                if (!OpenFile(Path.GetDirectoryName(mDataFolder) + "\\" + filename))
                    return;
            }
            mPlayFile = filename;
            Play();     //  演奏開始/再開
        }

        /// <summary>
        /// 演奏するファイルの設定
        /// </summary>
        /// <param name="fileName">ファイル名(WAV/MP3)</param>
        private bool OpenFile(string fileName)
        {
            if (fileName.Length == 0 || !File.Exists(fileName))
                return false;

            Stop();                             //  既存データの停止

            string folder = Path.GetDirectoryName(fileName);  //  音楽データのフォルダの抽出
            Title = "AudioPlay [" + Path.GetFileNameWithoutExtension(fileName) + "][" + folder + "]";
            dispTagData(fileName);              //  TAGデータの表示

            try {
                if (!audioLib.Open(fileName))   //  音楽データファイルのオープン
                    return false;
                mFrameInterval = ((int)mXRange * mIntevalRate) / mDispBitsRate;     //  データ表示間隔(fps)
                dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, mFrameInterval);//  日,時,分,秒,m秒

                dispSampleFrame();              //  サンプル周波数、量子ビット、チャンネル数を表示
                dispGraphData();                //  表示用のサンプル周波数、表示フレーム間隔

                audioLib.byte2floatReaderData();    //  WaveInで読み込んだbyte配列データをfloatに変換する
            } catch (Exception e) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 演奏開始処理
        /// </summary>
        private void Play()
        {
            if (audioLib.Play(mPlayPosition)) {     //  開始位置を指定して演奏開始
                SlPlayPostion.Maximum = audioLib.mTotalTime.TotalSeconds;   //  演奏時間スライダの最大値を設定
                SlPlayPostion.LargeChange = audioLib.mTotalTime.TotalSeconds / 20;
                mPlayLength = ylib.second2String(audioLib.mTotalTime.TotalSeconds, true);   //  演奏時間を文字列化
                dispPosionTime();                   //  曲の演奏時間表示
                dispatcherTimer.Start();            //  タイマー割込み開始
            }
        }

        /// <summary>
        /// 一時停止処理
        /// </summary>
        private void Pause()
        {
            if (audioLib.IsPlaying()) {
                dispatcherTimer.Stop();             //  タイマー割込み停止
                mPlayPosition = audioLib.Pause();   //  演奏中断
            }
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        private void Stop()
        {
            if (audioLib.Stop()) {                  //  演奏停止
                dispatcherTimer.Stop();             //  タイマー割込み停止
                mPlayPosition = 0;                  //  演奏位置初期化
                SlPlayPostion.Value = 0;
                dispPosionTime();
            }
        }

        /// <summary>
        /// タイマー割込みでスライダの位置と経過時間を表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //  演奏位置の表示
            dispPosionTime();
            //  ボリュームの調整
            audioLib.setVolume((float)SlPlayVolume.Value);
            //  演奏位置の調整
            if (SlPlayPostion.Value != mSlPrevPosition) {
                //  スライダーの位置が前回と異なるときスライダーの位置に演奏位置を合わせる
                audioLib.setCurPositionSecond((long)SlPlayPostion.Value);
            } else {
                //  スライダーの位置を演奏位置に合わせる
                SlPlayPostion.Value = audioLib.getCurPositionSecond();
            }
            //  演奏が終わっているか
            if (audioLib.IsStopped()) {
                Stop();
                if (FileList.Items.Count - 1 > FileList.SelectedIndex) {
                    //次の曲を演奏
                    FileList.SelectedIndex += 1;
                    OpenFile(Path.GetDirectoryName(mDataFolder) + "\\" + FileList.SelectedItem.ToString());
                    Play();
                }
            }
            //  表示データの更新
            if (0 < (SlPlayPostion.Value - mSlPrevPosition)) {
                setSampleData();    //  表示用のデータ作成
                dispVolLevel();     //  レベル表示
                ProcessSample();    //  波形表示
                Spectram();         //  スペクトラム表示
            }
            mSlPrevPosition = SlPlayPostion.Value;
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 表示用データの作成
        /// 表示用のビットレートに合わせてデータを間引きする
        /// </summary>
        private void setSampleData()
        {
            //  表示用のデータを作成
            audioLib.setSampleData(mDispBitsRate, mXRange);
            //  ローパスフィルタ
            if (0 < mLowPassFriq) {
                //  LowPassFilter
                audioLib.LowPassfilter(audioLib.mLeftRecord, mDispBitsRate, mLowPassFriq);
                audioLib.LowPassfilter(audioLib.mRightRecord, mDispBitsRate, mLowPassFriq);
            }

        }

        /// <summary>
        /// 音量レベルの表示
        /// 測定期間の平均を音量レベルとする
        /// </summary>
        private void dispVolLevel()
        {
            if (0 < audioLib.mLeftRecord.Count) {
                double leftVol = 0;
                double rightVol = 0;
                for (int i = 0; i < audioLib.mLeftRecord.Count; i++) {
                    leftVol += Math.Abs(audioLib.mLeftRecord[i]);
                    rightVol += Math.Abs(audioLib.mRightRecord[i]);
                }
                LeftVol.Value = leftVol / audioLib.mLeftRecord.Count;
                RightVol.Value = rightVol / audioLib.mRightRecord.Count;
            }
        }

        /// <summary>
        /// 波形グラフ表示
        /// </summary>
        private void ProcessSample()
        {
            if (0 < audioLib.mLeftRecord.Count) {
                mLeftLineSeries.Points.Clear();
                mRightLineSeries.Points.Clear();
                //  DataPointのListに変換
                var leftPoints = audioLib.mLeftRecord.Select((v, indexer) =>
                        new DataPoint((double)indexer, v)).ToList();
                var rightPoints = audioLib.mRightRecord.Select((v, indexer) =>
                        new DataPoint((double)indexer, v)).ToList();
                //  グラフ表示
                mLeftLineSeries.Points.AddRange(leftPoints);
                mRightLineSeries.Points.AddRange(rightPoints);
                mLeftPlot.InvalidatePlot(true);
                mRightPlot.InvalidatePlot(true);
            }
        }

        /// <summary>
        /// FFTによるスペクトラムグラフ表示
        /// </summary>
        private void Spectram()
        {
            if (0 < audioLib.mLeftRecord.Count) {
                //  データクリア
                mLeftLineSeriesFFT.Points.Clear();
                mRightLineSeriesFFT.Points.Clear();
                //  高速フーリエ変換(MathNet.Numericsを使用)
                mLeftLineSeriesFFT.Points.AddRange(FftTrandForms(audioLib.mLeftRecord, mDispBitsRate));
                mRightLineSeriesFFT.Points.AddRange(FftTrandForms(audioLib.mRightRecord, mDispBitsRate));
                //  高速フーリエ変換(NAudio.Dspを使用)
                //mLeftLineSeriesFFT.Points.AddRange(audioLib.DoFourier(audioLib.mLeftRecord, mDispBitsRate));
                //mRightLineSeriesFFT.Points.AddRange(audioLib.DoFourier(audioLib.mRightRecord, mDispBitsRate));
                //  グラフ表示
                mLeftPlotFFT.InvalidatePlot(true);
                mRightPlotFFT.InvalidatePlot(true);
            }
        }

        /// <summary>
        /// 高速フーリエ変換(MathNet.Numericsを使用)
        /// </summary>
        /// <param name="sampleRecord">音データ</param>
        /// <param name="dispBitsRate">サンプリング周波数</param>
        /// <returns></returns>
        public List<DataPoint> FftTrandForms(List<float> sampleRecord, int dispBitsRate)
        {
            //  ハミング窓の設定(w(x)=0.54-0.5*cos(2*PI*x) 0<=x<=1
            var window = MathNet.Numerics.Window.Hamming(sampleRecord.Count);
            sampleRecord = sampleRecord.Select((v, i) => v * (float)window[i]).ToList();
            //  高速フーリエ変換
            System.Numerics.Complex[] complexData = sampleRecord.Select(v => new System.Numerics.Complex(v, 0.0)).ToArray();
            MathNet.Numerics.IntegralTransforms.Fourier.Forward(complexData,
                        MathNet.Numerics.IntegralTransforms.FourierOptions.Matlab);
            var s = sampleRecord.Count * (1.0 / dispBitsRate);
            var complexPoint = complexData.Take(complexData.Count() / 2).Select(
                        (v, index) => new DataPoint((double)index / s,
                        Math.Sqrt(v.Real * v.Real + v.Imaginary * v.Imaginary))).ToList();
            return complexPoint;
        }

        /// <summary>
        /// グラフ表示の初期化
        /// </summary>
        private void InitPlot()
        {
            //  レベルメータ
            LeftVol.Maximum = 0.5;
            RightVol.Maximum = 0.5;
            //  音量スライダ
            SlPlayVolume.Maximum = 1.0;
            SlPlayVolume.Value = 0.8;
            SlPlayVolume.SmallChange = 0.01;
            SlPlayVolume.LargeChange = 0.1;

            //  波形グラフ
            mLeftLinearAxisY.Maximum = mMaxLevel;
            mLeftLinearAxisY.Minimum = -mMaxLevel;
            mLeftPlot.Axes.Add(mLeftLinearAxisX);
            mLeftPlot.Axes.Add(mLeftLinearAxisY);
            mLeftPlot.Series.Add(mLeftLineSeries);
            this.LeftWave.Model = mLeftPlot;

            mRightLinearAxisY.Maximum = mMaxLevel;
            mRightLinearAxisY.Minimum = -mMaxLevel;
            mRightPlot.Axes.Add(mRightLinearAxisX);
            mRightPlot.Axes.Add(mRightLinearAxisY);
            mRightPlot.Series.Add(mRightLineSeries);
            this.RightWave.Model = mRightPlot;

            //  FFTスペクトラムグラフ
            mLeftPlotFFT.Axes.Add(mLeftFFTAxisX);
            mLeftPlotFFT.Axes.Add(mLeftFFTAxisY);
            mLeftPlotFFT.Series.Add(mLeftLineSeriesFFT);
            this.LeftFFT.Model = mLeftPlotFFT;

            mRightPlotFFT.Axes.Add(mRightFFTAxisX);
            mRightPlotFFT.Axes.Add(mRightFFTAxisY);
            mRightPlotFFT.Series.Add(mRightLineSeriesFFT);
            this.RightFFT.Model = mRightPlotFFT;

            LowPassList.Items.Clear();
            foreach (string text in mLowPassFriqList)
                LowPassList.Items.Add(text);
            LowPassList.SelectedIndex = 0;
        }

        /// <summary>
        /// サンプル周波数、量子ビット、チャンネル数を表示
        /// </summary>
        private void dispSampleFrame()
        {
            string sampleFrame = String.Format("Sample Rate {0}Hz\nSample Bits {1}bit\n" +
                "Channels {2}\nEncording {3}",
                audioLib.getSampleRate(), audioLib.getBitsPerSample(), audioLib.getChannels(),
                audioLib.getEncording());
            fileFormat.Text = sampleFrame;
        }

        /// <summary>
        /// 表示グラフ用のデータ表示
        /// </summary>
        private void dispGraphData()
        {
            string dispForm = string.Format("DispSampleRate　{0}Hz\nFrameDispInterval {1}ms",
                mDispBitsRate, mFrameInterval);
            dispFormat.Text = dispForm;
        }

        /// <summary>
        /// 曲の長さと経過時間を表示
        /// </summary>
        private void dispPosionTime()
        {
            RecordPosition.Text = ylib.second2String(audioLib.getCurPositionSecond(), true) +
                "/" + ylib.second2String(audioLib.mTotalTime.TotalSeconds, true);
        }

        /// <summary>
        /// MP3ファイルのタグ情報を取得しListBoxに表示する
        /// </summary>
        /// <param name="filename">MP3ファイル名</param>
        private void dispTagData(string filename)
        {
            //List<string> tagList = audioLib.getTagData(filename);
            FileTagReader fileTagReader = new FileTagReader(filename);
            List<string> tagList = fileTagReader.getTagList();
            if (tagList != null) {
                TagListBox.Items.Clear();
                foreach (string tag in tagList)
                    TagListBox.Items.Add(tag);
            }
        }

        /// <summary>
        /// 曲ファイルのフォルダを選択しリストボックスに表示する
        /// </summary>
        private void setFileList()
        {
            mDataFolder = ylib.folderSelect(mDataFolder);
            if (0 < mDataFolder.Length) {
                mMusicExt = ExtensionList.Items[ExtensionList.SelectedIndex].ToString();
                string filePath = Path.Combine(mDataFolder, mMusicExt);
                setFileListBox(filePath);
                setFolderList();
                FolderList.SelectedIndex = FolderList.Items.IndexOf(mDataFolder);
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
                FileList.Items.Clear();
                foreach (string fileName in files)
                    FileList.Items.Add(Path.GetFileName(fileName));
                mDataFolder = path;
                mDataFolderList.Add(mDataFolder);
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
        /// 曲フォルダのリストをコンボボックスにとろくする
        /// </summary>
        private void setFolderList()
        {
            FolderList.Items.Clear();
            foreach (string folder in mDataFolderList)
                FolderList.Items.Add(folder);
            if (0 < FolderList.Items.Count && mDataFolder.Length == 0)
                FolderList.SelectedIndex = 0;
        }

        /// <summary>
        /// フォルダリストをファイルから読み込んでコンボボックスに登録する
        /// </summary>
        private void loadFolderList()
        {
            mAppFolder = ylib.getAppFolderPath();
            List<string> folderList = ylib.loadListData(mAppFolder + "\\" + mFolderListFileName);
            if (folderList != null) {
                mDataFolderList.Clear();
                foreach (string val in folderList) {
                    string[] files = getMusicFiles(val);
                    if (files != null && 0 < files.Length)
                    //if (Directory.Exists(val))
                        mDataFolderList.Add(val);
                }
                setFolderList();
            }
        }

        /// <summary>
        /// フォルダリストをファイルに保存する
        /// </summary>
        private void saveFolderList()
        {
            mAppFolder = ylib.getAppFolderPath();
            List<string> folderList = new List<string>();
            foreach (string folder in mDataFolderList)
                folderList.Add(folder);
            ylib.saveListData(mAppFolder + "\\" + mFolderListFileName, folderList);
        }
    }
}
