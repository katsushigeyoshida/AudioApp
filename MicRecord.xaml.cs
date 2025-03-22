using NAudio.Dsp;
using NAudio.Wave;
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
using WpfLib;

namespace AudioApp
{
    /// <summary>
    /// MicRecord.xaml の相互作用ロジック
    /// 
    /// NuGetでインストールするライブラリ
    /// NAudio, NAudio.DspでComplexを使用
    /// OxyPlot,OxyPlot.Wpf
    /// MathNet.Numerics
    /// </summary>
    public partial class MicRecord : Window
    {
        private double mWindowWidth;
        private double mWindowHeight;

        //  NAudio
        private WaveIn waveIn;

        //  ファイル保存
        private Stream mStream;                             //  出力データストリーム
        private WaveFileWriter mWaveFileWriter;             //  出力ファイル
        private string mFileName;                           //  WAVデータ保存ファイル名
        private string mOutFolder;                          //  ファイル出力先フォルダ
        private long mStartTime;                            //  録音開始時間
        private long mLapTime;                              //  録音時間
        private bool mRecording = false;                    //  録音中
        private bool mPause = false;                        //  一時停止中

        //  OxyPlot
        private PlotModel mPlotModel = new PlotModel();
        private LinearAxis mLinearAxis1 = new LinearAxis {  //  時間軸
            Position = AxisPosition.Bottom
        };
        private LinearAxis mLinearAxis2 = new LinearAxis {  //  レベル軸
            Minimum = -0.2,
            Maximum = 0.2,
            Position = AxisPosition.Left
        };
        private LineSeries mLineSeries = new LineSeries();

        private PlotModel mPlotModelFFT = new PlotModel();
        private LineSeries mLineSeriesFFT = new LineSeries();
        private LinearAxis mFFTAxis1 = new LinearAxis {     //  Y軸
            Maximum = 1,
            Minimum = 0,
            Position = AxisPosition.Left
        };
        private LinearAxis mFFTAxis2 = new LinearAxis {     //  X軸 周波数
            Maximum = 4000,
            Minimum = 0,
            Position = AxisPosition.Bottom
        };

        private List<float> mRecord = new List<float>();    //  音声データ
        private double mMaxLevel = 0.1;                     //  音量の最大レベル
        private int mBufferSize = 1024;                     //  1回のデータ点数
        private int mSampleRate = 8000;                     //  サンプリング周波数
        private double mMaxLevelFFt = 1.0;

        private YLib ylib = new YLib();


        public MicRecord()
        {
            InitializeComponent();

            mWindowWidth = Width;
            mWindowHeight = Height;
            WindowFormLoad();

            //  Sliderの設定
            VolSlider.Minimum = 0.0;
            VolSlider.Maximum = 0.5;
            VolSlider.Value = 0.2;
            VolSlider.SmallChange = 0.01;
            VolSlider.LargeChange = 0.02;
            FftSlider.Minimum = 0.0;
            FftSlider.Maximum = 1.0;
            FftSlider.Value = 0.5;
            FftSlider.LargeChange = 0.02;

            //  OxyPlotの初期化
            InitPlot();

            //  保存ファイルの表示
            mOutFolder = Path.Combine(ylib.getAppFolderPath(), "SoundData");
            if (!Directory.Exists(mOutFolder))
                Directory.CreateDirectory(mOutFolder);
            dispFileList();

            //  WaveInの確認
            WaveInCheck();
            //  WaveInの設定
            WaveInInit();
            //  音声入力開始
            waveIn.StartRecording();

            setButtonStat();
        }

        private void MicRecordWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //  音声入力終了
            waveIn.StopRecording();
            WaveInDispose();
            //  ウインドウ位置保存
            WindowFormSave();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.MicRecordWindowWidth < 100 || Properties.Settings.Default.MicRecordWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.MicRecordWindowHeight) {
                Properties.Settings.Default.MicRecordWindowWidth = mWindowWidth;
                Properties.Settings.Default.MicRecordWindowHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.MicRecordWindowTop;
                Left = Properties.Settings.Default.MicRecordWindowLeft;
                Width = Properties.Settings.Default.MicRecordWindowWidth;
                Height = Properties.Settings.Default.MicRecordWindowHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.MicRecordWindowTop = Top;
            Properties.Settings.Default.MicRecordWindowLeft = Left;
            Properties.Settings.Default.MicRecordWindowWidth = Width;
            Properties.Settings.Default.MicRecordWindowHeight = Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// 音声取得開始/終了
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void recordButton_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (bt.Name.CompareTo("StartButton") == 0) {
                if (mRecording && mPause) {
                    mStartTime = DateTime.Now.Ticks / 10000000 - mLapTime;
                } else {
                    SaveFileInit();
                }
                mRecording = true;
                mPause = false;
            } else if (bt.Name.CompareTo("PauseButton") == 0) {
                mPause = true;
            } else if (bt.Name.CompareTo("EndButton") == 0) {
                SaveFileClose();
                dispFileList();
                mRecording = false;
                mPause = false;
            }
            setButtonStat();
        }

        /// <summary>
        /// 表示グラフの初期化
        /// </summary>
        private void InitPlot()
        {
            //  波形グラフ
            mLinearAxis2.Maximum = mMaxLevel;
            mLinearAxis2.Minimum = -mMaxLevel;
            mPlotModel.Axes.Add(mLinearAxis1);
            mPlotModel.Axes.Add(mLinearAxis2);
            mPlotModel.Series.Add(mLineSeries);
            this.PlotView.Model = mPlotModel;
            //  FFTによるスペクトラムグラフ
            mPlotModelFFT.Axes.Add(mFFTAxis1);
            mPlotModelFFT.Axes.Add(mFFTAxis2);
            mPlotModelFFT.Series.Add(mLineSeriesFFT);
            this.PlotViewFFT.Model = mPlotModelFFT;
        }

        /// <summary>
        /// 入力デバイス表示
        /// </summary>
        private void WaveInCheck()
        {
            DeviceText.Items.Clear();
            for (int i = 0; i < WaveIn.DeviceCount; i++) {
                var deviceInfo = WaveIn.GetCapabilities(i);
                DeviceText.Items.Add(string.Format("Device {0}: {1} {2} channnels\n",
                    i, deviceInfo.ProductName, deviceInfo.Channels));
            }
        }

        //  入力デバイスの初期化
        private void WaveInInit()
        {
            waveIn = new WaveIn() {
                DeviceNumber = DeviceText.SelectedIndex < 0 ? 0 : DeviceText.SelectedIndex, //  Default
            };
            waveIn.DataAvailable += WaveIn_DataAvaible;
            waveIn.WaveFormat = new WaveFormat(sampleRate: mSampleRate, channels: 1);
        }

        /// <summary>
        /// 入力デバイスの終了
        /// </summary>
        private void WaveInDispose()
        {
            if (waveIn != null) {
                waveIn.DataAvailable -= WaveIn_DataAvaible;
                waveIn.WaveFormat = null;
                waveIn.Dispose();
            }
        }


        /// <summary>
        /// 音声データの取得と処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">音声データ</param>
        private void WaveIn_DataAvaible(object sender, WaveInEventArgs e)
        {
            if (mStream != null && mWaveFileWriter != null &&
                mRecording && !mPause) {
                mWaveFileWriter.Write(e.Buffer, 0, e.BytesRecorded);    //  データのファイル保存
                setRecordingTime();                                     //  録音時間表示
            }

            mMaxLevel = 0 < VolSlider.Value ? VolSlider.Value : mMaxLevel;
            //  Y軸(音レベル)の最大値を設定
            mLinearAxis2.Maximum = mMaxLevel;
            mLinearAxis2.Minimum = -mMaxLevel;

            //  32bitで最大値を1.0fにする
            for (int index = 0; index < e.BytesRecorded; index += 2) {
                short sample = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index + 0]);
                float sample32 = sample / 32768f;
                //System.Diagnostics.Debug.WriteLine(index+": "+sample32);
                ProcessSample(sample32);    //  グラフ表示
            }
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 波形データの設定と表示
        /// </summary>
        /// <param name="sample"></param>
        private void ProcessSample(float sample)
        {
            mRecord.Add(sample);
            if (mRecord.Count == mBufferSize) {
                var points = mRecord.Select((v, indexer) =>
                        new DataPoint((double)indexer, v)).ToList();
                mLineSeries.Points.Clear();
                mLineSeries.Points.AddRange(points);
                this.PlotView.InvalidatePlot(true);
                Spectram();     //  FFT処理によるスペクトラム表示
                mRecord.Clear();
            }
        }

        /// <summary>
        /// 音声データをフーリエ変換しグラフに表示
        /// </summary>
        private void Spectram()
        {
            //  ハミング窓の設定
            var window = MathNet.Numerics.Window.Hamming(mBufferSize);
            mRecord = mRecord.Select((v, i) => v * (float)window[i]).ToList();
            //  高速フーリエ変換
            System.Numerics.Complex[] complexData = mRecord.Select(v => new System.Numerics.Complex(v, 0.0)).ToArray();
            MathNet.Numerics.IntegralTransforms.Fourier.Forward(complexData,
                MathNet.Numerics.IntegralTransforms.FourierOptions.Matlab);

            var s = mBufferSize * (1.0 / mSampleRate);
            var point = complexData.Take(complexData.Count() / 2).Select(
                        (v, index) => new DataPoint((double)index / s,
                        Math.Sqrt(v.Real * v.Real + v.Imaginary * v.Imaginary))).ToList();

            mMaxLevelFFt = 0 < FftSlider.Value ? FftSlider.Value : mMaxLevelFFt;
            mFFTAxis1.Maximum = mMaxLevelFFt;
            mLineSeriesFFT.Points.Clear();
            mLineSeriesFFT.Points.AddRange(point);
            this.PlotViewFFT.InvalidatePlot(true);
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
        /// ファイル保存の初期化
        /// ファイル名には日時を入れる
        /// </summary>
        private void SaveFileInit()
        {
            //  保存ファイル設定
            DateTime dt = DateTime.Now;
            mStartTime = dt.Ticks / 10000000;       //  秒(元は100n秒)
            LapTime.Text = ylib.second2String(0.0, true);
            mFileName = Path.Combine(mOutFolder, "AudioRecord" + dt.ToString("yyyyMMdd-HHmmss") + ".wav");
            mStream = new FileStream(mFileName, FileMode.Create, FileAccess.Write, FileShare.Write);
            mWaveFileWriter = new WaveFileWriter(mStream, waveIn.WaveFormat);
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
        }

        private void FileList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            selectFilePlay();
        }

        private void setButtonStat()
        {
            StartButton.IsEnabled = !mRecording || mPause;
            PauseButton.IsEnabled = !mPause && mRecording;
            EndButton.IsEnabled = mRecording;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("PlayMenu") == 0) {
                selectFilePlay();
            } else if (menuItem.Name.CompareTo("RenameMenu") == 0) {
                selectFileRename();
            } else if (menuItem.Name.CompareTo("DeleteMenu") == 0) {
                selectFileDelete();
            }
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

        //  参考
        //  NAudioの関数を用いたFFT処理
        //
        // ファイル名の拡張子によって、異なるストリームを生成

        AudioFileReader audioStream;// = new AudioFileReader("fileName");

        /// <summary>
        /// 音楽の波形データにハミング窓をかけ、高速フーリエ変換する
        /// https://qiita.com/lenna_kun/items/a0f03447bb893c9ab937
        /// </summary>
        /// <returns>フーリエ変換後の音楽データ</returns>
        private float[,] FFT_HammingWindow_ver1()
        {
            // 波形データを配列samplesに格納
            float[] samples = new float[audioStream.Length / audioStream.BlockAlign * audioStream.WaveFormat.Channels];
            audioStream.Read(samples, 0, samples.Length);

            //1サンプルのデータ数
            int fftLength = 256;
            //1サンプルごとに実行するためのイテレータ用変数
            int fftPos = 0;

            // フーリエ変換後の音楽データを格納する配列 (標本化定理より、半分は冗長)
            float[,] result = new float[samples.Length / fftLength, fftLength / 2];

            // 波形データにハミング窓をかけたデータを格納する配列
            Complex[] buffer = new Complex[fftLength];
            for (int i = 0; i < samples.Length; i++) {
                // ハミング窓をかける
                buffer[fftPos].X = (float)(samples[i] * FastFourierTransform.HammingWindow(fftPos, fftLength));
                buffer[fftPos].Y = 0.0f;
                fftPos++;

                // 1サンプル分のデータが溜まったとき
                if (fftLength <= fftPos) {
                    fftPos = 0;

                    // サンプル数の対数をとる (高速フーリエ変換に使用)
                    int m = (int)Math.Log(fftLength, 2.0);
                    // 高速フーリエ変換
                    FastFourierTransform.FFT(true, m, buffer);

                    for (int k = 0; k < result.GetLength(1); k++) {
                        // 複素数の大きさを計算
                        double diagonal = Math.Sqrt(buffer[k].X * buffer[k].X + buffer[k].Y * buffer[k].Y);
                        // デシベルの値を計算
                        double intensityDB = 10.0 * Math.Log10(diagonal);

                        const double minDB = -60.0;

                        // 音の大きさを百分率に変換
                        double percent = (intensityDB < minDB) ? 1.0 : intensityDB / minDB;
                        // 結果を代入
                        result[i / fftLength, k] = (float)diagonal;
                    }
                }
            }

            return result;
        }
    }
}
