using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfLib;
using Path = System.IO.Path;

namespace AudioApp
{
    /// <summary>
    /// LoopbackCapture.xaml の相互作用ロジック
    /// 
    //  WaspiLoopbackCapture によるオーディオ出力のファイル化
    /// https://github.com/naudio/NAudio/blob/master/Docs/WasapiLoopbackCapture.md
    /// https://taktak.jp/2017/03/07/1800
    /// </summary>
    public partial class LoopbackCapture : Window
    {
        private double mWindowWidth;
        private double mWindowHeight;

        //  OxyPlot(LeftWave)
        private PlotModel mLeftPlot = new PlotModel();
        private LinearAxis mLeftLinearAxisX = new LinearAxis {  //  時間軸
            Minimum = 0,
            Maximum = mXRange,
            Position = AxisPosition.Bottom
        };
        private LinearAxis mLeftLinearAxisY = new LinearAxis {  //  レベル軸
            Minimum = -mMaxLevel,
            Maximum = mMaxLevel,
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
            Minimum = -mMaxLevel,
            Maximum = mMaxLevel,
            Position = AxisPosition.Left
        };
        private LineSeries mRightLineSeries = new LineSeries();

        //  OxyPlot(LeftFFT)
        private PlotModel mLeftPlotFFT = new PlotModel();
        private LineSeries mLeftLineSeriesFFT = new LineSeries();
        private LinearAxis mLeftFFTAxisY = new LinearAxis {     //  Y軸
            Maximum = mMaxLevelFft,
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
            Maximum = mMaxLevelFft,
            Minimum = 0,
            Position = AxisPosition.Left
        };
        private LinearAxis mRightFFTAxisX = new LinearAxis {     //  X軸 周波数
            Maximum = 4000,
            Minimum = 0,
            Position = AxisPosition.Bottom
        };

        private string mFileName;                           //  WAVデータ保存ファイル名
        private string mOutFolder;                          //  ファイル出力先フォルダ
        private WasapiLoopbackCapture mLoopBackWaveIn;      //  LoopbackCaptureクラス
        private Stream mStream;                             //  出力データストリーム
        private WaveFileWriter mWaveFileWriter;             //  出力ファイル
        private MMDevice mDevice;                           //  出力デバイスデータ

        private bool IsRecording { get; set; }              //  録音中
        private bool mRecording = false;                    //  データ保存中
        private bool mPause = false;                        //  一時停止中
        private bool mGraph = true;                         //  グラフ表示

        private static double mMaxLevel = 0.8;              //  音量の最大レベル
        private static double mMaxLevelFft = 40;            //  FFTグラフの最大値
        private static double mXRange = 400;                //  1フレーム分のデータ数
        private static int mDispBitsRate = 8000;            //  表示用のサンプルレート

        private int mChannels;                              //  チャンネル数
        private int mBlockAlign;                            //  ブロックサイズ(32bitx2Channels/8=8
        private int mBitsPerSample;                         //  1秒あたりのバイト数(48000Hzx4bytex2Channels=384000)
        private int mSampleRate;                            //  サンプリング周波数
        private float[] mSampleL;                           //  左データ
        private float[] mSampleR;                           //  右データ
        private List<float> mLeftRecord = new List<float>();    //  音声データ
        private List<float> mRightRecord = new List<float>();   //  音声データ
        private long mStartTime;                            //  録音開始時間
        private long mLapTime;                              //  録音時間

        private float mLowPassFriq = 0;                     //  ローパスフィルタのカット周波数
        private string[] mLowPassFriqList = {               //  ローパスフィルタのカット周波数
            "なし","100","200","300","500","800" ,
            "1000","2000","3000","5000","8000"};
        BiQuadFilter mLeftFilter;
        BiQuadFilter mRightFilter;

        private event EventHandler<WaveInEventArgs> DataAvailable;
        private YLib ylib = new YLib();

        public LoopbackCapture()
        {
            InitializeComponent();

            mWindowWidth = Width;
            mWindowHeight = Height;
            WindowFormLoad();

            //  ボタンの状態設定
            setButton();

            //  OxyPlotの初期化
            InitPlot();
            //  保存ファイルの表示
            mOutFolder = Path.Combine(ylib.getAppFolderPath(), "SoundData");
            if (!Directory.Exists(mOutFolder))
                Directory.CreateDirectory(mOutFolder);
            dispFileList();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //  出力デバイスの取得と表示
            LoopBackDevice();
            //  LoopbackWaveInの初期化
            LoopbackWaveInInit();
            LoopbackStart();
        }

        private void LoopbackCaptureWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowFormSave();
            Dispose();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.LoopbackCaptureWindowWidth < 100 || Properties.Settings.Default.LoopbackCaptureWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.LoopbackCaptureWindowHeight) {
                Properties.Settings.Default.LoopbackCaptureWindowWidth = mWindowWidth;
                Properties.Settings.Default.LoopbackCaptureWindowHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.LoopbackCaptureWindowTop;
                Left = Properties.Settings.Default.LoopbackCaptureWindowLeft;
                //LoopbackCaptureWindow.Width = Properties.Settings.Default.LoopbackCaptureWindowWidth;
                //LoopbackCaptureWindow.Height = Properties.Settings.Default.LoopbackCaptureWindowHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.LoopbackCaptureWindowTop = Top;
            Properties.Settings.Default.LoopbackCaptureWindowLeft = Left;
            Properties.Settings.Default.LoopbackCaptureWindowWidth = Width;
            Properties.Settings.Default.LoopbackCaptureWindowHeight = Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// 入力デバイスリストをダブルクリックした入力デバイスの変更処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //  LoopbackWaveInのクローズ
            LoopbackWainClose();
            //  LoopbackWaveInの初期化
            LoopbackWaveInInit();
            LoopbackStart();
        }

        /// <summary>
        /// 保存ファイルリストのダブルクリック処理(ファイルの実行)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            selectFilePlay();
        }

        /// <summary>
        /// 保存ファイルリストのコンテキストメニュー処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("PlayMenu") == 0) {         //  ファイルの実行
                selectFilePlay();
            } else if (menuItem.Name.CompareTo("RenameMenu") == 0) {//  ファイル名変更
                selectFileRename();
            } else if (menuItem.Name.CompareTo("DeleteMenu") == 0) {//  ファイル削除
                selectFileDelete();
            }
        }

        /// <summary>
        /// ローパスフィルタのカット周波数選択処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LowPassList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LowPassList.SelectedIndex == 0) {
                mLowPassFriq = 0;
            } else {
                mLowPassFriq = float.Parse(LowPassList.Items[LowPassList.SelectedIndex].ToString());
                //  フィルタのパラメータを設定
                mLeftFilter = BiQuadFilter.LowPassFilter(mSampleRate, mLowPassFriq, 1);
                mRightFilter = BiQuadFilter.LowPassFilter(mSampleRate, mLowPassFriq, 1);
            }
        }

        /// <summary>
        ///         /// ボタンのクリック処理

        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (bt.Name.CompareTo("StartButton") == 0) {        //  録音開始
                if (!mPause || mLoopBackWaveIn == null) {
                    SaveFileInit();
                } else {
                    mStartTime = DateTime.Now.Ticks / 10000000 - mLapTime;
                }
                mPause = false;
                mRecording = true;
                //LoopbackStart();
            } else if (bt.Name.CompareTo("PauseButton") == 0) { //  一時停止
                mPause = true;
                mRecording = false;
                //LoopbackStop();
            } else if (bt.Name.CompareTo("EndButton") == 0) {   //  録音終了
                mPause = false;
                mRecording = false;
                SaveFileClose();
                //LoopbackStop();
                dispFileList();
            } else if (bt.Name.CompareTo("ExitButton") == 0) {   //  プログラム終了
                Close();
            } else if (bt.Name.CompareTo("PlayButton") == 0) {  //  Waveファイルの実行
                selectFilePlay();
            } else if (bt.Name.CompareTo("DeleteButton") == 0) {//  Waveファイルの削除
                selectFileDelete();
            } else if (bt.Name.CompareTo("RenameButton") == 0) {//  Waveファイル名の変更
                selectFileRename();
            }
            setButton();
        }

        /// <summary>
        /// 表示グラフの初期化
        /// </summary>
        private void InitPlot()
        {
            //  波形レベルSliderの設定
            volGraphSlider.Minimum = 0.0;
            volGraphSlider.Maximum = mMaxLevel * 1.5;
            volGraphSlider.Value = mMaxLevel;
            volGraphSlider.SmallChange = mMaxLevel / 20;
            volGraphSlider.LargeChange = mMaxLevel / 10;
            //  FFTレベル Sliderの設定
            fftGraphSlider.Minimum = 0.0;
            fftGraphSlider.Maximum = mMaxLevelFft * 1.5;
            fftGraphSlider.Value = mMaxLevelFft;
            fftGraphSlider.SmallChange = mMaxLevelFft / 20;
            fftGraphSlider.LargeChange = mMaxLevelFft / 10;

            //  波形グラフ
            mLeftLinearAxisY.Maximum = mMaxLevel;
            mLeftLinearAxisY.Minimum = -mMaxLevel;
            mLeftPlot.Axes.Add(mLeftLinearAxisX);
            mLeftPlot.Axes.Add(mLeftLinearAxisY);
            mLeftPlot.Series.Add(mLeftLineSeries);
            this.LeftWavePlot.Model = mLeftPlot;
            //  FFTによるスペクトラムグラフ
            mLeftPlotFFT.Axes.Add(mLeftFFTAxisX);
            mLeftPlotFFT.Axes.Add(mLeftFFTAxisY);
            mLeftPlotFFT.Series.Add(mLeftLineSeriesFFT);
            this.LeftFFTPlot.Model = mLeftPlotFFT;

            //  波形グラフ
            mRightLinearAxisY.Maximum = mMaxLevel;
            mRightLinearAxisY.Minimum = -mMaxLevel;
            mRightPlot.Axes.Add(mRightLinearAxisX);
            mRightPlot.Axes.Add(mRightLinearAxisY);
            mRightPlot.Series.Add(mRightLineSeries);
            this.RightWavePlot.Model = mRightPlot;
            //  FFTによるスペクトラムグラフ
            mRightPlotFFT.Axes.Add(mRightFFTAxisX);
            mRightPlotFFT.Axes.Add(mRightFFTAxisY);
            mRightPlotFFT.Series.Add(mRightLineSeriesFFT);
            this.RightFFTPlot.Model = mRightPlotFFT;

            //  グラフデータの初期化
            mLeftRecord.Clear();
            mRightRecord.Clear();

            //  ローパスフィルタのカット周波数をリストボックスに登録
            LowPassList.Items.Clear();
            foreach (string text in mLowPassFriqList)
                LowPassList.Items.Add(text);
            LowPassList.SelectedIndex = 0;
        }

        /// <summary>
        /// 音声出力デバイスの表示
        /// https://taktak.jp/2017/03/07/1800
        /// </summary>
        private void LoopBackDevice()
        {
            var collection = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            DeviceList.Items.Clear();
            for (int i = 0; i < collection.Count; i++) {
                DeviceList.Items.Add(string.Format("{0} Channels {1}" + "\nSampleRate {2:#,0} BitsPerSample {3:#,0} BlockAlign {4}",
                    collection[i].FriendlyName, collection[i].AudioClient.MixFormat.Channels,
                    collection[i].AudioClient.MixFormat.SampleRate,
                    collection[i].AudioClient.MixFormat.BitsPerSample,
                    collection[i].AudioClient.MixFormat.BlockAlign));
            }
            if (0 < DeviceList.Items.Count)
                DeviceList.SelectedIndex = 0;
        }

        /// <summary>
        /// LoopbackCaptureの初期化
        /// </summary>
        private void LoopbackWaveInInit()
        {
            //  出力デバイスデータの取得
            var collection = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            if (0 < collection.Count && 0 <= DeviceList.SelectedIndex)
                mDevice = collection[DeviceList.SelectedIndex];
            else
                return;

            //  デバイスデータ
            mChannels = mDevice.AudioClient.MixFormat.Channels;             //  チャンネル数
            mBlockAlign = mDevice.AudioClient.MixFormat.BlockAlign;         //  ブロックサイズ
            mBitsPerSample = mDevice.AudioClient.MixFormat.BitsPerSample;   //  サンプリングのビット数
            mSampleRate = mDevice.AudioClient.MixFormat.SampleRate;         //  サンプリング周波数

            //  LoopbackCaptureの設定(入力デバイスとイベントの追加)
            mLoopBackWaveIn = new WasapiLoopbackCapture(mDevice);
            mLoopBackWaveIn.DataAvailable += LoopbackWaveInOnDataAvailable;         //  データ取得イベント
            mLoopBackWaveIn.RecordingStopped += LoopbackWaveInOnRecordingStopped;   //  レコーディング中止イベント

            //  表示用データの設定値表示
            comment.Text = string.Format("表示SampleRate {0:#,0}", mDispBitsRate);  //  サンプリング周波数
        }

        /// <summary>
        /// LoopbackWaveInのクローズ
        /// </summary>
        private void LoopbackWainClose()
        {
            if (mLoopBackWaveIn != null) {
                mLoopBackWaveIn.StopRecording();
                mLoopBackWaveIn.DataAvailable -= LoopbackWaveInOnDataAvailable;
                mLoopBackWaveIn.RecordingStopped -= LoopbackWaveInOnRecordingStopped;
            }
        }

        /// <summary>
        /// ファイル保存の初期化
        /// ファイル名には日時を入れる
        /// </summary>
        private void SaveFileInit()
        {
            //  保存ファイル設定
            DateTime dt = DateTime.Now;
            mStartTime = dt.Ticks / 10000000;       //  秒(元は100n秒)
            mLapTime = 0;
            LapTime.Text = ylib.second2String(0.0, true);
            mFileName = Path.Combine(mOutFolder, "AudioCapture" + dt.ToString("yyyyMMdd-HHmmss") + ".wav");
            mStream = new FileStream(mFileName, FileMode.Create, FileAccess.Write, FileShare.Write);
            mWaveFileWriter = new WaveFileWriter(mStream, mLoopBackWaveIn.WaveFormat);
        }

        /// <summary>
        /// データファイルのクローズ処理
        /// </summary>
        private void SaveFileClose()
        {
            if (mWaveFileWriter != null) {
                mWaveFileWriter.Close();
                mWaveFileWriter = null;
            }
            if (mStream != null) {
                mStream.Close();
                mStream = null;
            }
            Wav2Mp3(mFileName);
        }

        /// <summary>
        /// Capture開始
        /// </summary>
        public void LoopbackStart()
        {
            IsRecording = true;
            mPause = false;
            if (mLoopBackWaveIn != null)
                mLoopBackWaveIn.StartRecording();
        }

        /// <summary>
        /// Capture終了
        /// </summary>
        public void LoopbackStop()
        {
            IsRecording = false;
            if (mLoopBackWaveIn != null)
                mLoopBackWaveIn.StopRecording();
        }

        public void Dispose()
        {
            if (mLoopBackWaveIn != null) {
                mLoopBackWaveIn.StopRecording();
                mLoopBackWaveIn.DataAvailable -= LoopbackWaveInOnDataAvailable;
                mLoopBackWaveIn.RecordingStopped -= LoopbackWaveInOnRecordingStopped;
                //  Disposeしようとすると終了できない
                //mLoopBackWaveIn?.Dispose();
                //mWaveFileWriter?.Dispose();
                //mStream?.Dispose();
            }
        }

        /// <summary>
        /// Capture終了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoopbackWaveInOnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (!mPause) {
                SaveFileClose();
            }
            //Dispose();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Capture処理、データ保存と波形とスペクトラム表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoopbackWaveInOnDataAvailable(object sender, WaveInEventArgs e)
        {
            //  ローパスフィルタ
            if (0 < mLowPassFriq)
                filteringConvert(e.Buffer, e.BytesRecorded, mBitsPerSample, mBlockAlign, mLowPassFriq);
            //  ファイル保存
            if (!mPause && mRecording) {
                mWaveFileWriter.Write(e.Buffer, 0, e.BytesRecorded);    //  データのファイル保存
                setRecordingTime();             //  
            }
            //  波形・スペクトラム表示
            if (mGraph) {
                getSampleFrame(e.Buffer);       //  byteデータをfloatデータに変換
                if (mLeftRecord.Count < mXRange) {
                    setSampleData();            //  mSample → mReacord データを間引きしてビットレートを下げる
                    setGraphRange();            //  グラフのレベル設定
                    dispGraph();                //  グラフ表示
                }
            }
            DataAvailable?.Invoke(this, e);
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 表示用データの作成
        /// 表示用のビットレートに合わせてデータを間引きする
        /// </summary>
        private void setSampleData()
        {
            int interval = mSampleRate / mDispBitsRate;
            for (long i = 0; i < mSampleL.Length; i += interval) {
                mLeftRecord.Add(mSampleL[i]);
                mRightRecord.Add(mSampleR[i]);
            }
            //  ローパスフィルタ
            if (0 < mLowPassFriq) {
                //  LowPassFilter
                //LowPassfilter(mLeftRecord, mDispBitsRate, mLowPassFriq);
                //LowPassfilter(mRightRecord, mDispBitsRate, mLowPassFriq);
            }
        }

        /// <summary>
        /// 波形グラフとスペクトラムの表示
        /// </summary>
        private void dispGraph()
        {
            if (mXRange <= mLeftRecord.Count) {
                ProcessSample();    //  波形グラフ表示
                Spectram();         //  FFT処理によるスペクトラム表示
                mLeftRecord.Clear();
                mRightRecord.Clear();
            }
        }

        /// <summary>
        /// 録音時間の表示
        /// スレッドの衝突を防ぐためDispatcher.Invokeメソッドで回避
        /// </summary>
        private void setRecordingTime()
        {
            DateTime dt = DateTime.Now;
            long lapTime = dt.Ticks / 10000000 - mStartTime;
            if (mLapTime < lapTime) {
                mLapTime = lapTime;
                //スレッド内ではコントロールのオブジェクトにアクセスできないのでDispatcherを使う
                this.Dispatcher.Invoke((Action)(() => {
                    LapTime.Text = ylib.second2String(mLapTime, true);
                }));
            }
        }

        /// <summary>
        /// グラフのY軸レベルをスライダに合わせて変更する
        /// スレッドの衝突を防ぐためDispatcher.Invokeメソッドで回避
        /// </summary>
        private void setGraphRange()
        {
            this.Dispatcher.Invoke((Action)(() => {
                //  Y軸(音レベル)の最大値を設定
                if (volGraphSlider != null && 0 <= volGraphSlider.Value) {
                    mMaxLevel = 0 < volGraphSlider.Value ? volGraphSlider.Value : mMaxLevel;
                    mLeftLinearAxisY.Maximum = mMaxLevel;
                    mLeftLinearAxisY.Minimum = -mMaxLevel;
                    mRightLinearAxisY.Maximum = mMaxLevel;
                    mRightLinearAxisY.Minimum = -mMaxLevel;
                }
                if (fftGraphSlider != null && 0 <= fftGraphSlider.Value) {
                    mMaxLevelFft = 0 < fftGraphSlider.Value ? fftGraphSlider.Value : mMaxLevelFft;
                    mLeftFFTAxisY.Maximum = mMaxLevelFft;
                    mRightFFTAxisY.Maximum = mMaxLevelFft;
                }
            }));
        }

        /// <summary>
        /// 波形データの設定と表示
        /// </summary>
        private void ProcessSample()
        {
            //  DataPoint型に変換(index → X, value →Y)
            var leftPoints = mLeftRecord.Select((v, indexer) => new DataPoint((double)indexer, v)).ToList();
            var rightPoints = mRightRecord.Select((v, indexer) => new DataPoint((double)indexer, v)).ToList();

            //  データの表示
            mLeftLineSeries.Points.Clear();
            mRightLineSeries.Points.Clear();
            mLeftLineSeries.Points.AddRange(leftPoints);
            mRightLineSeries.Points.AddRange(rightPoints);

            LeftWavePlot.InvalidatePlot(true);
            RightWavePlot.InvalidatePlot(true);
        }

        /// <summary>
        /// 音声データをフーリエ変換しグラフに表示
        /// </summary>
        private void Spectram()
        {
            //  FFTデータ変換
            mLeftLineSeriesFFT.Points.Clear();
            mRightLineSeriesFFT.Points.Clear();
            //  MathNet.NumericsのFFTを使用
            mLeftLineSeriesFFT.Points.AddRange(FftTrandForms(mLeftRecord, mDispBitsRate));
            mRightLineSeriesFFT.Points.AddRange(FftTrandForms(mRightRecord, mDispBitsRate));
            //  NAudioのFFTを使用
            //mLeftLineSeriesFFT.Points.AddRange(DoFourier(mLeftRecord, mDispBitsRate));
            //mRightLineSeriesFFT.Points.AddRange(DoFourier(mRightRecord, mDispBitsRate));
            //  グラフ表示
            this.LeftFFTPlot.InvalidatePlot(true);
            this.RightFFTPlot.InvalidatePlot(true);
        }

        /// <summary>
        /// ローパスフィルタ(双二次フィルタ)
        /// </summary>
        /// <param name="sampleRecord">サンプリングデータ</param>
        /// <param name="dispBitsRate">サンプリング周波数</param>
        /// <param name="cutFreq">カット周波数</param>
        private void LowPassfilter(List<float> sampleRecord, int dispBitsRate, float cutFreq)
        {
            //  BiQuadFilter LowPassFilter(float sampleRate, float cutoffFrequency, float q);
            BiQuadFilter filter = BiQuadFilter.LowPassFilter(dispBitsRate, cutFreq, 1);
            for (int i = 0; i < sampleRecord.Count; i++)
                sampleRecord[i] = filter.Transform(sampleRecord[i]);
        }

        /// <summary>
        /// 高速フーリエ変換(MathNet.Numericsを使用
        /// </summary>
        /// <param name="sampleRecord"></param>
        /// <param name="dispBitsRate"></param>
        /// <returns></returns>
        private List<DataPoint> FftTrandForms(List<float> sampleRecord, int dispBitsRate)
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
        /// 高速フーリエ変換(NAudio.Dspを使用)
        /// </summary>
        /// <param name="sampleRecord"></param>
        /// <param name="dispBitsRate"></param>
        /// <returns></returns>
        public static List<DataPoint> DoFourier(List<float> sampleRecord, int dispBitsRate)
        {
            var fftsample = new Complex[sampleRecord.Count];

            //ハミング窓をかける
            for (int i = 0; i < sampleRecord.Count; i++) {
                fftsample[i].X = (float)(sampleRecord[i] * FastFourierTransform.HammingWindow(i, sampleRecord.Count));
                fftsample[i].Y = 0.0f;
            }

            //サンプル数のlogを取る
            var m = (int)Math.Log(fftsample.Length, 2);
            //FFT
            FastFourierTransform.FFT(true, m, fftsample);

            //結果を出力
            //FFTSamplenum / 2なのは標本化定理から半分は冗長だから
            List<DataPoint> dataPoints = new List<DataPoint>();
            var s = sampleRecord.Count * (1.0 / dispBitsRate);
            for (int k = 0; k < fftsample.Length / 2; k++) {
                //複素数の大きさを計算
                double diagonal = Math.Sqrt(fftsample[k].X * fftsample[k].X + fftsample[k].Y * fftsample[k].Y);
                dataPoints.Add(new DataPoint((double)k / s, diagonal * 500.0));
            }

            return dataPoints;
        }

        /// <summary>
        /// byteデータをfloatデータに変換
        /// </summary>
        /// <param name="buffer">byteデータ</param>
        private void getSampleFrame(byte[] buffer)
        {
            mSampleL = new float[buffer.Length / mBlockAlign];
            mSampleR = new float[buffer.Length / mBlockAlign];
            switch (mBitsPerSample) {
                case 8:
                    for (int i = 0; i < mSampleL.Length; i++) {
                        mSampleL[i] = (buffer[i * mBlockAlign] - 128) / 128f;
                        mSampleR[i] = (buffer[i * mBlockAlign + mBlockAlign / 2] - 128) / 128f;
                    }
                    break;
                case 16:
                    for (int i = 0; i < mSampleL.Length; i++) {
                        mSampleL[i] = BitConverter.ToInt16(buffer, i * mBlockAlign) / 32768f;
                        mSampleR[i] = BitConverter.ToInt16(buffer, i * mBlockAlign + mBlockAlign / 2) / 32768f;
                    }
                    break;
                case 32:        //  BlockAlign = 8
                    for (int i = 0; i < mSampleL.Length; i++) {
                        mSampleL[i] = BitConverter.ToSingle(buffer, i * mBlockAlign);
                        mSampleR[i] = BitConverter.ToSingle(buffer, i * mBlockAlign + mBlockAlign / 2);
                    }
                    break;
            }
        }

        /// <summary>
        /// ローパスフィルタ処理
        /// byte配列→float変換→ローパスフィルタ→byte配列変換
        /// </summary>
        /// <param name="buffer">データのbyte配列</param>
        /// <param name="dispBitsRate">サンプルレート(mSampleRate)</param>
        /// <param name="bitsPerSample">量子ビット(mBitsPerSample)</param>
        /// <param name="blockAlign">ブロックサイズ(mBlockAlign)</param>
        /// <param name="cutFreq">カットオフ周波数(mLowPassFriq)</param>
        private void filteringConvert(byte[] buffer, int dispBitsRate, int bitsPerSample, int blockAlign, float cutFreq)
        {
            //  関数内部でフィルタのパラメータを設定
            //BiQuadFilter leftFilter = BiQuadFilter.LowPassFilter(dispBitsRate, cutFreq, 1);
            //BiQuadFilter rightFilter = BiQuadFilter.LowPassFilter(dispBitsRate, cutFreq, 1);

            int bytesRead = buffer.Length;
            float leftFloat = 0, rightFloat = 0;
            byte[] transformedLeft, transformedRight;
            for (int i = 0; i < bytesRead / blockAlign; i++) {
                switch (bitsPerSample) {
                    case 8:
                        leftFloat = (buffer[i * blockAlign] - 128) / 128f;
                        rightFloat = (buffer[i * blockAlign + blockAlign / 2] - 128) / 128f;
                        break;
                    case 16:
                        leftFloat = BitConverter.ToInt16(buffer, i * blockAlign) / 32768f;
                        rightFloat = BitConverter.ToInt16(buffer, i * blockAlign + blockAlign / 2) / 32768f;
                        break;
                    case 32:        //  BlockAlign = 8
                        leftFloat = BitConverter.ToSingle(buffer, i * blockAlign);
                        rightFloat = BitConverter.ToSingle(buffer, i * blockAlign + blockAlign / 2);
                        break;
                }
                //  内部設定したフィルタを使用
                //leftFloat = leftFilter.Transform(leftFloat);
                //rightFloat = rightFilter.Transform(rightFloat);
                //  外部設定したフィルタを使用
                leftFloat = mLeftFilter.Transform(leftFloat);
                rightFloat = mRightFilter.Transform(rightFloat);
                switch (bitsPerSample) {
                    case 8:
                        transformedLeft = BitConverter.GetBytes((byte)(leftFloat * 128f + 128f));
                        Buffer.BlockCopy(transformedLeft, 0, buffer, i * blockAlign, 1);
                        transformedRight = BitConverter.GetBytes((byte)(rightFloat * 128f + 128f));
                        Buffer.BlockCopy(transformedRight, 0, buffer, i * blockAlign + blockAlign / 2, 1);
                        break;
                    case 16:
                        transformedLeft = BitConverter.GetBytes((short)(leftFloat * 32768f));
                        Buffer.BlockCopy(transformedLeft, 0, buffer, i * blockAlign, 2);
                        transformedRight = BitConverter.GetBytes((short)(rightFloat * 32768f));
                        Buffer.BlockCopy(transformedRight, 0, buffer, i * blockAlign + blockAlign, 2);
                        break;
                    case 32:        //  BlockAlign = 8
                        transformedLeft = BitConverter.GetBytes(leftFloat);
                        Buffer.BlockCopy(transformedLeft, 0, buffer, i * blockAlign, 4);
                        transformedRight = BitConverter.GetBytes(rightFloat);
                        Buffer.BlockCopy(transformedRight, 0, buffer, i * blockAlign + blockAlign / 2, 4);
                        break;
                }
            }
            //reader.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 保存ファイルリストの表示
        /// </summary>
        private void dispFileList()
        {
            FileList.Items.Clear();
            foreach (var filename in Directory.GetFiles(mOutFolder, "*.wav")) {
                FileList.Items.Add(Path.GetFileName(filename));
            }
            foreach (var filename in Directory.GetFiles(mOutFolder, "*.mp3")) {
                FileList.Items.Add(Path.GetFileName(filename));
            }
        }

        /// <summary>
        /// ボタンの状態設定
        /// </summary>
        private void setButton()
        {
            StartButton.IsEnabled = !mRecording;
            PauseButton.IsEnabled = mRecording;
            EndButton.IsEnabled = mRecording;
            //PlayButton.IsEnabled = !mRecording;
            //DeleteButton.IsEnabled = !mRecording;
            //RenameButton.IsEnabled = !mRecording;
            if (mPause) {
                //StartButton.Content = "再開";
                EndButton.IsEnabled = true;
            } else {
                //StartButton.Content = "録音";
            }
            //volGraphSlider.IsEnabled = !IsRecording;
            //fftGraphSlider.IsEnabled = !IsRecording;
        }

        /// <summary>
        /// 選択したファイルを実行する
        /// </summary>
        private void selectFilePlay()
        {
            if (!mRecording && FileList.SelectedItem != null)
                Process.Start(Path.Combine(mOutFolder, FileList.SelectedItem.ToString()));
        }

        /// <summary>
        /// 選択したファイルを削除する
        /// </summary>
        private void selectFileDelete()
        {
            if (!mRecording && FileList.SelectedItem != null) {
                File.Delete(Path.Combine(mOutFolder, FileList.SelectedItem.ToString()));
                dispFileList();
            }
        }

        /// <summary>
        /// 選択したファイルの名称変更
        /// </summary>
        private void selectFileRename()
        {
            if (!mRecording && FileList.SelectedItem != null) {
                InputBox dlg = new InputBox();
                dlg.Title = "ファイル名変更";
                dlg.mEditText = FileList.SelectedItem.ToString();
                var result = dlg.ShowDialog();
                if (result == true) {
                    File.Move(
                        Path.Combine(mOutFolder, FileList.SelectedItem.ToString()),
                        Path.Combine(mOutFolder, dlg.mEditText));
                    dispFileList();
                }
            }
        }

        /// <summary>
        /// WAVEファイルをMP3ファイルに変換する
        /// </summary>
        /// <param name="fileName">WAVEファイル名</param>
        private void Wav2Mp3(string fileName)
        {
            if (!File.Exists(fileName) || Path.GetExtension(fileName).CompareTo(".wav") != 0)
                return;

            MediaFoundationReader reader = new MediaFoundationReader(fileName);
            string destFile = Path.Combine(Path.GetDirectoryName(fileName),
                                Path.GetFileNameWithoutExtension(fileName)) + ".mp3";
            NAudio.MediaFoundation.MediaType mediaType = MediaFoundationEncoder.SelectMediaType(
                NAudio.MediaFoundation.AudioSubtypes.MFAudioFormat_MP3,     //  変換フォーマット
                reader.WaveFormat,
                reader.WaveFormat.BitsPerSample);
            using (MediaFoundationEncoder encorder = new MediaFoundationEncoder(mediaType)) {
                encorder.Encode(destFile, reader);
            }
        }



        /// <summary>
        /// 参考
        /// https://stackoverflow.com/questions/30601078/using-equalizer-in-naudio-loopback/30601718#30601718
        /// </summary>
        private void WaspiLoopback()
        {
            WasapiLoopbackCapture waveIn = new WasapiLoopbackCapture();
            BufferedWaveProvider bufferedWaveProvider = new BufferedWaveProvider(waveIn.WaveFormat);
            VolumeSampleProvider volumeProvider = new VolumeSampleProvider(bufferedWaveProvider.ToSampleProvider());
            WasapiOut wasapiOut = new WasapiOut(AudioClientShareMode.Shared, 0);
            BiQuadFilter filter = BiQuadFilter.HighPassFilter(44000, 200, 1);   //  ハイパスフィルター

            wasapiOut.Init(volumeProvider);
            wasapiOut.Play();
            waveIn.StartRecording();

            waveIn.DataAvailable += delegate (object sender, WaveInEventArgs e) {
                for (int i = 0; i < e.BytesRecorded; i += 4) {
                    byte[] transformed = BitConverter.GetBytes(filter.Transform(BitConverter.ToSingle(e.Buffer, i)));
                    Buffer.BlockCopy(transformed, 0, e.Buffer, i, 4);
                    //float sample32 = BitConverter.ToSingle(e.Buffer, 0);
                    //ProcessSample(sample32);    //  グラフ表示
                }
                bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
                //volumeProvider.Volume = .8f * ReverbIntensity;
            };
        }
    }
}
