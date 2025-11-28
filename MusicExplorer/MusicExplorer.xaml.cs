using AudioLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfLib;

namespace AudioApp
{
    /// <summary>
    /// MusicExplore.xaml の相互作用ロジック
    /// </summary>
    public partial class MusicExplorer : Window
    {
        private double mWindowWidth;
        private double mWindowHeight;
        private double mSearchPathWidth;
        private double mFileListDataHeight;

        //  システムメニューに追加(https://ameblo.jp/kani-tarou/entry-10240156672.html)
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, int bRevert);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int AppendMenu(IntPtr hMenu, int Flagsw, int IDNewItem, string lpNewItem);
        private HwndSource hwndSource = null;
        private const int WM_SYSCOMMAND = 0x112;
        private const int MF_SEPARATOR = 0x0800;
        private const int APP_SETTING_MENU = 100;               //  変更不可セルの背景色の設定

        private Dictionary<string, MusicFileData> mDataList;    //  音楽データファイルリスト(key=ファイルパス)
        private Dictionary<string, AlbumData> mAlbumList;       //  アルバムデータリスト(key=ファイル拡張子+アルバムフォルダ)
        private Dictionary<string, ArtistData> mArtistList;     //  アーティストリスト(key=アーティスト名大文字)
        private List<string> mYearList;                         //  年代リスト
        private List<string> mGenreList;                        //  ジャンルリスト
        private List<string> mUserGenreList;                    //  ユーザージャンルリスト
        private List<string> mUserStyleList;                    //  ユーザースタイルリスト
        private List<string> mOriginalMediaList;                //  元メディアのリスト
        private List<MusicFileData> mDispFileList;              //  表示用ファイルデータ(フィルタ後のデータ)
        private List<AlbumData> mDispAlbumList;                 //  表示用アルバムデータ(フィルタ後のデータ)
        private List<ArtistData> mDispArtistList;               //  表示用アーティストデータ(フィルタ後のデータ)

        private string[] mMusicDataTitle;           //  CSV音楽データファイルのタイトル

        private string mFolderSelectMessage = "フォルダの一覧を表示する時はここをダブルクリックしてください。";
        private string mDataFolder;                 //  曲フォルダ
        private string mAppFolder;                  //  アプリケーションフォルダ
        private string mMusicFileCategory;          //  音楽ファイルリスト名
        private string mMusicFileListPath;          //  音楽ファイルリストファイル名
        private string mAlbumListPath;              //  アルバムリストファイル名
        private string mArtistInfoListPath;         //  アーティスト情報リストファイルパス
        private string mSearchPathListPath;         //  検索ファイルパスリストのファイル名
        private string mSearchWordListPath;         //  検索ワードリストのファイル名
        private bool mFileAdding = false;           //  音楽ファイルをリスト登録中フラグ
        private bool mFileAddStop = false;          //  音楽ファイルのリスト登録中断フラグ
        private bool mAlbumFileUse = true;          //  アルバムファイルの使用の有無
        private int mSearchPathListMax = 100;       //  検索ファイルパスの最大登録数
        private int mSearchWordListMax = 100;       //  検索ワードの最大登録数
        private bool mAlbumUnDisp = false;          //  重複アルバムの非表示
        enum DISPARTIST { ARTIST, ALBUMARTIST, USERARTIST };
        private int mDispArtistType = 1;            //  アーティスト項目に表示選択(0:Artist 1:AlbumArtist 2:UserArtist)
        private string mSearchWord = "";            //  検索ワード
        private string[] mFileFormat = { "MP3", "FLAC", "WMA", "WAV" };

        enum PLAYORDER { NORMALORDER, RANDOM_ALBUM, RANDOM_SONG }
        private PLAYORDER mPlayOrder = PLAYORDER.NORMALORDER;   //  演奏順

        private ArtistInfoList mArtistInfoList;     //  アーティスト情報リスト
        private AlbumData[] mSelectAlbumData;       //  選択されたアルバムリスト
        private MusicFileData[] mSelectMusicData;   //  選択した音楽ファイルのリスト
        private MusicFileData mCurMusicData;        //  演奏中の音楽ファイル
        private string mSelectMusiDataFolder;       //  選択されている音楽ファイルのフォルダ
        private int mSelectMusicDataIndex;          //  演奏中のファイルのインデックス
        private string mPlayLength;                 //  演奏時間の文字列
        private long mPlayPosition;                 //  演奏位置
        private double mSlPrevPosition = 0;         //  スライダーの演奏位置
        private DispatcherTimer dispatcherTimer;    //  タイマーオブジェクト
        private bool mDispDataSetOn = true;         //  表示データセットフラグ(不要なデータの表示更新を削減)
        private string mCurMusicPath = "";          //  タグ情報のMusicPath
        private int mCurImageNo = 0;                //  タグ情報のImageNo
        private FullView mFullView = null;          //  タグイメージ表示ダイヤログ
        private CoverView mCoverView = null;        //  タグイメージ表示ダイヤログ

        //  音楽ファイル再生プレイヤ
        public enum PLAYERTYPE { INNERPLAYER, AUDIOPLAER, OUTTERPLAYER }
        private PLAYERTYPE mOutterPlayer = PLAYERTYPE.INNERPLAYER;
        private bool mIsMediaPlayer = true;         //  再生にMediaPlayerを使う場合(falseだとNAudioを使用)
        private AudioPlay mPlayer;                  //  AudioPlayクラス(波形、スペクトラム表示プレイヤー)
        private AudioPlay mAudioPlay;
        private SpectrumAnalyzer mSpecrtrumAnalyzer;
        private AudioLib.AudioLib audioLib = new AudioLib.AudioLib(); //  NAudioを再生に使うクラス

        private string mErrorMsg;
        private Random mRandom = new Random();
        private YLib ylib = new YLib();

        public MusicExplorer()
        {
            InitializeComponent();

            WindowFormLoad();

            //  初期データ
            mDataList = new Dictionary<string, MusicFileData>(StringComparer.OrdinalIgnoreCase);
            mAlbumList = new Dictionary<string, AlbumData>(StringComparer.OrdinalIgnoreCase);
            mAppFolder = ylib.getAppFolderPath();                                   //  アプリフォルダ
            mDataFolder = Properties.Settings.Default.MusicExplorerFolder;          //  初期検索ホルダー
            mMusicFileCategory = Properties.Settings.Default.MusicExploreFIleCategory;     //  音楽リスト名の取得
            mSearchPathListPath = Path.Combine(mAppFolder, "SearchPathList.csv");   //  検索パスリストパス
            mSearchWordListPath = Path.Combine(mAppFolder, "SearchWordList.csv");   //  検索ワードリストパス
            mArtistInfoListPath = Path.Combine(mAppFolder, "ArtistInfoList.csv");   //  アーティスト情報リストファイルパス
            mArtistInfoList = new ArtistInfoList(mArtistInfoListPath);              //  アーティスト情報リスト
            CbSearchPath.Items.Add(mFolderSelectMessage);                           //  登録パス
            loadSearchPathList(mSearchPathListPath);                                //  検索パスを取り込む
            CbSearchPath.SelectedIndex = 0;
            loadSearchWordList(mSearchWordListPath);                                //  検索ワードの取り込み
            CbSearchExt.ItemsSource = mFileFormat;                                  //  ファイルフォーマット
            CbSearchExt.SelectedIndex = 0;

            initFileData();

            //  タイマーインスタンスの作成
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);    //  日,時,分,秒,m秒
        }

        /// <summary>
        /// 初期データの読み込みと登録
        /// </summary>
        private void initFileData()
        {
            audioLib.setUseMediaPlayer(mIsMediaPlayer);
            //  ファイルの設定
            this.Title = "Music Explorer [ " + mMusicFileCategory + " ]";           //  タイトル設定リストパス
            string albumListName = "AlbumList" + mMusicFileCategory + ".csv";       //  曲リストファイル名
            string musicFileListName = "MusicFileList" + mMusicFileCategory + ".csv";// アルバムリストファイル名
            mAlbumListPath = Path.Combine(mAppFolder, albumListName);               //  音楽ファイルリストパス
            mMusicFileListPath = Path.Combine(mAppFolder, musicFileListName);       //  音楽ファイル
            MusicFileData musicFileData = new MusicFileData();                      //  音楽ファイルリストデータ
            mMusicDataTitle = musicFileData.getTitle();                             //  CSV音楽データファイルのタイトル
            mOutterPlayer = (PLAYERTYPE)Enum.ToObject(typeof(PLAYERTYPE), Properties.Settings.Default.MusicExplorerOuterPlayer);   //  外部プレイヤ
            TbSearchFileCount.Text = "";                    //  追加登録数表示
            setPlayOrderButton(mPlayOrder);                 //  演奏順ボタンの設定
            VolumePositionInit(0.8);                        //  ボリュームの初期値
            BalancePositionInit(0);                         //  バランスの初期値

            //  初期データの読み込み
            initListData();
            RbSearchAlbum.IsChecked = true;
            RbAlbumInfo.IsChecked = true;
            loadMusicData(mMusicFileListPath);              //  ファイルからデータを取り込む
            UpdateAllListData(true, mAlbumFileUse);         //  すべてのリストデータを更新する
        }

        /// <summary>
        /// データリストをすべて初期化する
        /// </summary>
        private void initListData()
        {
            if (mDataList != null)
                mDataList.Clear();
            if (mAlbumList != null)
                mAlbumList.Clear();
            if (mArtistList != null)
                mArtistList.Clear();
            if (mDispFileList != null)
                mDispFileList.Clear();
            if (mDispAlbumList != null)
                mDispAlbumList.Clear();
            if (mDispArtistList != null)
                mDispArtistList.Clear();
        }

        /// <summary>
        /// クローズ処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PlayStop();
            dispatcherTimer = null;                         //  タイマーをクローズ
            if (audioLib != null)                           //  オーディオライブラリをクローズ
                audioLib.dispose();
            //if (mMediaPlayer != null)                       //  MediaPlayerをクローズ
            //    mMediaPlayer = null;
            //if (mFullView != null)                          //  タグのイメージダイヤログをクローズ
            //    mFullView.Close();
            if (mCoverView != null)                          //  タグのイメージダイヤログをクローズ
                mCoverView.Close();
            saveFileAll();                                  //  すべてのデータファイルを保存
            Properties.Settings.Default.MusicExplorerFolder = mDataFolder;  //  初期検索パスを保存
            Properties.Settings.Default.MusicExplorerOuterPlayer = (int)mOutterPlayer;  //  外部プレイヤー設定保存
            WindowFormSave();                               //  Windowの状態保存
        }

        private void MusicExplorerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // システムメニューに設定メニューを追加
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            IntPtr menu = GetSystemMenu(hwnd, 0);
            AppendMenu(menu, MF_SEPARATOR, 0, null);
            AppendMenu(menu, 0, APP_SETTING_MENU, "アプリケーション設定");
            TbSearchFileCount.Visibility = Visibility.Collapsed;
            PbReadFile.Visibility = Visibility.Collapsed;
            tbSearchFileTitle.Visibility = Visibility.Collapsed;        }

        /// <summary>
        /// システムメニューに追加するためのフック設定
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // フックを追加
            hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource != null) {
                hwndSource.AddHook(new HwndSourceHook(this.hwndSourceHook));
            }
        }

        /// <summary>
        /// システムメニューに追加したメニューの処理
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private IntPtr hwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SYSCOMMAND) {
                if (wParam.ToInt32() == APP_SETTING_MENU) {
                    musicExploreSetting();
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            mWindowWidth = Width;
            mWindowHeight = Height;
            mSearchPathWidth = CbSearchPath.Width;
            mFileListDataHeight = DgFileListData.Height;

            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.MusicExplorerWindowWidth < 100 || Properties.Settings.Default.MusicExplorerWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.MusicExplorerWindowHeight) {
                Properties.Settings.Default.MusicExplorerWindowWidth = mWindowWidth;
                Properties.Settings.Default.MusicExplorerWindowHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.MusicExplorerWindowTop;
                Left = Properties.Settings.Default.MusicExplorerWindowLeft;
                Width = Properties.Settings.Default.MusicExplorerWindowWidth;
                Height = Properties.Settings.Default.MusicExplorerWindowHeight;
                //  GridのalbumListの幅は直接設定できないのでGridLength()に変換して設定
                GridLength ln = new GridLength(Properties.Settings.Default.MusicExploreAlbumListWidth);
                albumList.Width = ln;
                double dy = Height - mWindowHeight;
            }
            mAlbumFileUse = Properties.Settings.Default.MusicExploreAlbumFileUse;
            mDispArtistType = Properties.Settings.Default.MusicExploreDispArtistType;
            mAlbumUnDisp = Properties.Settings.Default.MusicExploreAlbumUnDisp;
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            Properties.Settings.Default.MusicExploreAlbumFileUse = mAlbumFileUse;
            Properties.Settings.Default.MusicExploreDispArtistType = mDispArtistType;
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.MusicExplorerWindowTop = Top;
            Properties.Settings.Default.MusicExplorerWindowLeft = Left;
            Properties.Settings.Default.MusicExplorerWindowWidth = Width;
            Properties.Settings.Default.MusicExplorerWindowHeight = Height;
            Properties.Settings.Default.MusicExploreAlbumListWidth = albumList.ActualWidth;
            Properties.Settings.Default.MusicExploreAlbumUnDisp = mAlbumUnDisp;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Window状態の更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MusicExplorerWindow_LayoutUpdated(object sender, EventArgs e)
        {
            //  Windowの幅、高さ、変位分を取得
            double WindowWidth = System.Windows.SystemParameters.WorkArea.Width;
            double WindowHeight = System.Windows.SystemParameters.WorkArea.Height;
            double dx = Width - mWindowWidth;
            double dy = Height - mWindowHeight;
            CbSearchPath.Width = mSearchPathWidth + dx;
            //  最大化時の処理
            if (this.WindowState == WindowState.Maximized)
                dy = WindowHeight - mWindowHeight;

            //fileList.Width = mFileListWidth + dx;
            DgFileListData.Height = mFileListDataHeight + dy;
        }

        /// <summary>
        /// [設定]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btSetting_Click(object sender, RoutedEventArgs e)
        {
            musicExploreSetting();
        }

        /// <summary>
        /// 設定ダイヤログ
        /// </summary>
        private void musicExploreSetting()
        {
            MusicExplorerSetting musicExplorerSetting = new MusicExplorerSetting();
            musicExplorerSetting.mOuterPlayer = mOutterPlayer;
            musicExplorerSetting.mDispArtistType = mDispArtistType;
            musicExplorerSetting.mAlbumFileUse = mAlbumFileUse;
            musicExplorerSetting.mAlbumUnDisp = mAlbumUnDisp;
            var result = musicExplorerSetting.ShowDialog();
            if (result == true) {
                saveFileAll();                                          //  すべてのデータファイルを保存
                mOutterPlayer = musicExplorerSetting.mOuterPlayer;      //  ダブルクリック時の起動プレイヤ
                mAlbumFileUse = musicExplorerSetting.mAlbumFileUse;     //  アルバムデータをファイル保存
                //  アルバムデータの変更
                if (mMusicFileCategory.CompareTo(musicExplorerSetting.mMusicFileCategory) != 0 ||
                    mDispArtistType != musicExplorerSetting.mDispArtistType) {
                    mMusicFileCategory = musicExplorerSetting.mMusicFileCategory;
                    mDispArtistType = musicExplorerSetting.mDispArtistType;
                    initFileData();
                }
                if (mAlbumUnDisp != musicExplorerSetting.mAlbumUnDisp) {
                    mAlbumUnDisp = musicExplorerSetting.mAlbumUnDisp;   //  非表示設定
                    UpdateAlbumDispData();
                }
            }
        }

        /// <summary>
        /// [ファイル検索パス]コンボボックス ダブルクリックで参照
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbSearchPath_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (0 < CbSearchPath.Text.Length) {
                mDataFolder = CbSearchPath.Text;
                if (Path.GetExtension(mDataFolder).Length <= 0) {
                    mDataFolder = Path.GetDirectoryName(mDataFolder + "\\");
                } else {
                    mDataFolder = Path.GetDirectoryName(mDataFolder);
                }
            }
            mDataFolder = ylib.folderSelect(mDataFolder);
            if (0 < mDataFolder.Length)
                setSearchPath(mDataFolder, CbSearchExt.Text);
        }

        /// <summary>
        /// [ファイル検索拡張子]の変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbSearchExt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = CbSearchExt.SelectedIndex;
            if (0 <= index && 0 < CbSearchPath.Text.Length
                && 0 < CbSearchPath.Items.Count && 0 < CbSearchExt.Items.Count) {
                string ext = Path.GetExtension(CbSearchPath.Text);
                if (0 < ext.Length) {
                    string newExt = "." + CbSearchExt.SelectedItem.ToString().ToLower();
                    string searchPath = CbSearchPath.Text.Replace(ext, newExt);
                    if (CbSearchPath.Items.Contains(searchPath))
                        CbSearchPath.Items.Remove(searchPath);
                    CbSearchPath.Items.Insert(1, searchPath);
                    CbSearchPath.SelectedIndex = 1;
                }
            }
        }

        /// <summary>
        /// [追加]ボタン 音楽ファイルの追加
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (bt.Content.ToString().CompareTo("追加") == 0) {
                //  ファイルを検索して登録
                bt.IsEnabled = false;                   //  ボタンの使用不可設定
                mDataFolder = CbSearchPath.Text;        //  登録フォルダの設定
                string ext = Path.GetExtension(mDataFolder);
                string folder = Path.GetDirectoryName(mDataFolder);
                if (0 < ext.Length) {
                    ext = ext.Substring(1).ToLower();
                } else {
                    ext = CbSearchExt.Text.ToLower();
                    folder = Path.GetDirectoryName(mDataFolder + "\\");
                }
                setSearchPath(folder, ext);             //  登録フォルダをComboBoxに設定
                addFileFind(folder, "*." + ext, true, ChkOverWrite.IsChecked == true, ChkOnlyTag.IsChecked == true);  //  ファイル検索
                bt.IsEnabled = true;                    //  ボタンの使用可設定
                bt.Content = "中断";
            } else if (bt.Content.ToString().CompareTo("中断") == 0) {
                //  登録処理中断のフラグを設定
                mFileAddStop = true;
                bt.Content = "追加";
            }
        }

        /// <summary>
        /// [保存]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            saveFileAll();          //  データをファイルに保存
            MessageBox.Show("データを保存しました", "確認");
        }

        /// <summary>
        /// [演奏順/ランダム]ボタン リスト順かランダム化切り替える
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtPlayOrder_Click(object sender, RoutedEventArgs e)
        {
            if (mPlayOrder == PLAYORDER.NORMALORDER) {
                setPlayOrderButton(PLAYORDER.RANDOM_ALBUM);
            } else if (mPlayOrder == PLAYORDER.RANDOM_ALBUM) {
                setPlayOrderButton(PLAYORDER.RANDOM_SONG);
            } else if (mPlayOrder == PLAYORDER.RANDOM_SONG) {
                setPlayOrderButton(PLAYORDER.NORMALORDER);
            }
        }

        /// <summary>
        /// [アーティストリスト]コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArtistInfoMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("ArtistInfoMenu") == 0) {
                //  アーティスト情報ダイヤログを開く
                artistInfoData();
            } else if (menuItem.Name.CompareTo("OpenArtistInfoMenu") == 0) {
                //  アーティスト情報ファイルを関連付けで開く
                if (System.IO.File.Exists(mArtistInfoListPath)) {
                    System.Diagnostics.Process p =
                            System.Diagnostics.Process.Start(mArtistInfoListPath);
                } else {
                    MessageBox.Show(mArtistInfoListPath + "\nファイルがありません");
                }
            } else if (menuItem.Name.CompareTo("ImportArtistInfoMenu") == 0) {
                //  ファイルのインポート
                string filePath = ylib.fileSelect("", "csv");
                if (0 < filePath.Length) {
                    mArtistInfoList.loadData(filePath);
                }
            } else if (menuItem.Name.CompareTo("ExportArtistInfoMenu") == 0) {
                //  ファイルのエクスポート
                string filePath = ylib.saveFileSelect("", "csv");
                if (0 < filePath.Length) {
                    mArtistInfoList.saveData(filePath);
                }
            }
        }

        /// <summary>
        /// [アルバムリスト]コンテキストメニュー処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void albumListMenuClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            IList selItems = DgAlbumListData.SelectedItems;
            if (menuItem.Name.CompareTo("albumListCopyMenu") == 0) {
                //  選択されているアルバムリストのコピー、
                selectAlbumToClipbord();
            } else if (menuItem.Name.CompareTo("albumExecuteFileMenu") == 0) {
                //  選択されたアルバム(同一フォルダーにあるpdfファイル)を開く
                executeSelectFolderFile();
            } else if (menuItem.Name.CompareTo("albumInfoMenu") == 0) {
                if (1 < selItems.Count) {
                    //  アルバム情報データの一括更新
                    setAlbumInfoData();
                } else {
                    //  アルバム情報の編集・更新
                    editAlbumInfoData();
                }
            } else if (menuItem.Name.CompareTo("albumDataMenu") == 0) {
                //  アルバム情報データの一括更新
                setAlbumInfoData();
            } else if (menuItem.Name.CompareTo("albumUpdateMenu") == 0) {
                //  アルバム情報データ更新
                albumDataUpdate();
            } else if (menuItem.Name.CompareTo("albumTagUpdateMenu") == 0) {
                //  アルバムタグ情報データ更新
                albumDataUpdate(true);
            } else if (menuItem.Name.CompareTo("albumRemoveMenu") == 0) {
                //  アルバムデータの削除
                selectedAlbumDataRemove();
            } else if (menuItem.Name.CompareTo("artistInfoMenu") == 0) {
                //  アーティスト情報
                dispArtistInfoData();
            } else if (menuItem.Name.CompareTo("audioPlayerMenu") == 0) {
                //  AudioPlayクラスで演奏
                playAudioPlayer();
            } else if (menuItem.Name.CompareTo("spectrumAnalyzerMenu") == 0) {
                //  スペクトラム解析で起動
                spectrumAnalyzerPlayer();
            } else if (menuItem.Name.CompareTo("exportMenu") == 0) {
                //  エクスポート
                exportFile();
            } else if (menuItem.Name.CompareTo("openFolderMenu") == 0) {
                //  アルバムのフォルダを開く
                openFolder();
            }
            
        }

        /// <summary>
        /// [ファイルリスト(曲)]コンテキストメニュー 処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileListMenuClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("fileListCopyMenu") == 0) {
                //  選択されているファイルリストをクリップボードにコピー
                selectedFileToClipbord();
            } else if (menuItem.Name.CompareTo("fileListExistMenu") == 0) {
                //  ファイルの存在確認
                existFileCheck();
            } else if (menuItem.Name.CompareTo("fileListDeleteMenu") == 0) {
                //  選択行削除
                selectedFileDataDelete();
            } else if (menuItem.Name.CompareTo("fileListSqueezeMenu") == 0) {
                //  選択行重複削除
                selectedFileDataSqueeze();
            } else if (menuItem.Name.CompareTo("fileListDispDeleteMenu") == 0) {
                //  表示行削除
                dispFileDataDelete();
            } else if (menuItem.Name.CompareTo("fileListAllDeleteMenu") == 0) {
                //  全データ削除
                AllDataClear();
            } else if (menuItem.Name.CompareTo("fileListCommentMenu") == 0) {
                //  曲ごとのコメント追加・編集
                selectFileCommentAdd();
            } else if (menuItem.Name.CompareTo("fileListVolumeMenu") == 0) {
                //  選択行曲に音量設定
                selectedFileVolumeSet();
            } else if (menuItem.Name.CompareTo("fileListDispVolumeMenu") == 0) {
                //  表示行曲に音量設定
                dispFileVolumeSet(); 
            } else if (menuItem.Name.CompareTo("fileListClearVolumeMenu") == 0) {
                //  選択行曲に音量をクリア
                selectedFileVolumeClear();
            }
        }

        /// <summary>
        /// [音楽ファイルリストのダブルクリック処理]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileListData_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //  fileListDataのソート結果の位置はmListに反映されないのでfileListDtaのSelectedItemを使う
            MusicFileData fileData = (MusicFileData)DgFileListData.SelectedItem;
            if (fileData != null)
                filePlayer(fileData.Folder, fileData.FileName);
        }

        /// <summary>
        /// [音楽ファイルリストの選択変更] でタグデータの表示更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileListData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("fileListData_SelectionChanged");
            if (mDispDataSetOn) {
                MusicFileData fileData = (MusicFileData)DgFileListData.SelectedItem;
                if (fileData != null)
                    (mCurMusicPath, mCurImageNo) = setDispTagData(fileData.getPath(), RbAlbumInfo.IsChecked == true, fileData.FileName);
            }
        }

        /// <summary>
        /// [アルバムデータの選択変更] 処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void albumListData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("albumListData_SelectionChanged");
            if (mDispDataSetOn)
                UpdateMusicDispData();
        }

        /// <summary>
        /// [アーティストリストの選択変更]
        /// アルバムリストの更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void artistListData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("artistListData_SelectionChanged");
            if (mDispDataSetOn)
                UpdateAlbumDispData();
        }

        /// <summary>
        /// [選択フィルタの選択変更] の実行(ジャンル、年代、ユーザー設定ジャンル、スタイル)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void filterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("filterList_SelectionChanged");
            if (mDispDataSetOn)
                UpdateAllDispData();
        }

        /// <summary>
        /// [検索ワード選択変更] 音楽データの抽出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void searchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //  未使用
        }

        /// <summary>
        /// [アルバムデータのダブりクリック] で選択したアルバムを演奏する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AlbumListData_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string albumFolder = selectedAlbumFolder();
            if (0 < mDispFileList.Count) {
                filePlayer(albumFolder, mDispFileList[0].FileName);
            }
        }

        /// <summary>
        /// [ファイル読込プログレスバー]の値を見て完了したらリストの表示を更新する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void readFileBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            System.Diagnostics.Debug.WriteLine("readFileBar_ValueChanged");
            if (PbReadFile.Value == PbReadFile.Maximum || mFileAddStop) {
                PbReadFile.Value = 0;
                TbSearchFileCount.Text = "完了";
                TbSearchFileCount.Visibility = Visibility.Collapsed;
                PbReadFile.Visibility = Visibility.Collapsed;
                tbSearchFileTitle.Visibility = Visibility.Collapsed;
                BtAdd.Content = "追加";
                mFileAdding = false;
                UpdateAllListData(false, false);
            }
        }

        /// <summary>
        /// [音量スライドバー]の値を設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SlVolPostion_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            setVolume();
        }

        /// <summary>
        /// [バランススライドバー]の値を設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SlBalPostion_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            setBalance();
        }

        /// <summary>
        /// [検索ワード・キー入力]で抽出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void searchComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("searchComboBox_KeyDown");
            if (e.Key == Key.Return) {
                mSearchWord = CbSearchWord.Text;
                if (RbSearchAlbum.IsChecked == true) {
                    UpdateAllDispData();
                } else if (RbSearchArtist.IsChecked == true) {
                    UpdateAllDispData();
                } else {
                    musicDispDataSet();
                    UpdateMusicDispList();
                }
                setSearchWord(mSearchWord);
                CbSearchWord.Text = "";
                mSearchWord = "";
            }
        }

        /// <summary>
        /// [タグ情報(またはアルバム情報)のリストボックス・ダブルクリック]したとき
        /// 選択した項目がURLであればWebを開き、ファイルであればファイルを開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbTagData_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (0 <= LbTagData.SelectedIndex) {
                string data = LbTagData.Items[LbTagData.SelectedIndex].ToString();
                if (data.IndexOf("http") == 0) {
                    //  URLの時
                    System.Diagnostics.Process p =
                        System.Diagnostics.Process.Start(LbTagData.Items[LbTagData.SelectedIndex].ToString());
                } else {
                    //  ファイルの時
                    if (data[1] != ':')
                        data = Path.Combine(mSelectMusiDataFolder, data);
                    if (File.Exists(data)) {
                        System.Diagnostics.Process p =
                                System.Diagnostics.Process.Start(data);
                    }
                }
            }
        }

        /// <summary>
        /// [アルバム情報表示のラジオボタン]でタグ情報とアルバム情報を切り替える
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RbAlbumInfo_Checked(object sender, RoutedEventArgs e)
        {
            //  最初の行のファイルのタグ情報を表示
            if (mDispFileList != null && 0 < mDispFileList.Count) {
                MusicFileData fileData = (MusicFileData)DgFileListData.SelectedItem;
                if (fileData != null) {
                    (mCurMusicPath, mCurImageNo) = setDispTagData(fileData.getPath(), RbAlbumInfo.IsChecked == true);
                } else {
                    //  曲名が選択されていない時
                    fileData = (MusicFileData)mDispFileList[0];
                    if (fileData != null)
                        (mCurMusicPath, mCurImageNo) = setDispTagData(fileData.getPath(), RbAlbumInfo.IsChecked == true, fileData.FileName);
                }
            }
        }

        /// <summary>
        /// [TagImageのコンテキストメニュー]「コピー」
        /// アルバムのイメージデータをクリップボードにコピー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("ImageCopyMenu") == 0) {
                BitmapImage bitmap = (BitmapImage)TagImage.Source;
                Clipboard.SetImage(bitmap);
            } else if (menuItem.Name.CompareTo("ImageViewMenu") == 0) {
                (mCurMusicPath, mCurImageNo) = setDispTagData(mCurMusicPath, RbAlbumInfo.IsChecked == true, "", mCurImageNo, true);
            } else if (menuItem.Name.CompareTo("ImageNextMenu") == 0) {
                (mCurMusicPath, mCurImageNo) = setDispTagData(mCurMusicPath, RbAlbumInfo.IsChecked == true, "", mCurImageNo + 1);
            } else if (menuItem.Name.CompareTo("ImagePrevMenu") == 0) {
                (mCurMusicPath, mCurImageNo) = setDispTagData(mCurMusicPath, RbAlbumInfo.IsChecked == true, "", mCurImageNo - 1);
            }
        }

        /// <summary>
        /// ファイルを検索しリストに登録する
        /// </summary>
        /// <param name="path">検索パス</param>
        /// <param name="fileName">検索ファイル名(ワイルドカード)</param>
        /// <param name="recursive">再帰検索フラグ</param>
        /// <param name="overwrite">上書きフラグ</param>
        /// <param name="onlyTag">タグ情報のみ上書き</param>
        /// <returns>登録したファイル数</returns>
        private int addFileFind(string path, string fileName, bool recursive, bool overwrite, bool onlyTag)
        {
            if (!Directory.Exists(path))
                return 0;

            //  ファイルの検索
            string[] files;
            if (recursive)
                files = Directory.GetFiles(path, fileName, SearchOption.AllDirectories);
            else
                files = Directory.GetFiles(path, fileName, SearchOption.TopDirectoryOnly);
            if (files.Length < 1)
                return 0;
            return addFileList(files, overwrite, onlyTag);
        }

        /// <summary>
        /// ファイルリストからデータの更新
        /// </summary>
        /// <param name="files">ファイルリスト</param>
        /// <param name="overwrite">上書き</param>
        /// <param name="onlyTag">タグのみ更新</param>
        /// <returns></returns>
        private int addFileList(string[] files, bool overwrite, bool onlyTag)
        {
            int n = 0;
            mFileAdding = true;                 //  検索ファイル追加中のフラグ
            PbReadFile.Maximum = files.Length; //  ファイル検索・追加のプログレスバー
            mFileAddStop = false;
            mErrorMsg = string.Empty;
            TbSearchFileCount.Visibility = Visibility.Visible;
            PbReadFile.Visibility = Visibility.Visible;
            tbSearchFileTitle.Visibility = Visibility.Visible;

            //  ファイルの検索・追加は時間がかかるので別タスクで処理
            Task.Run(() => {
                foreach (string str in files) {
                    if (mFileAddStop)
                        break;
                    if (!addFileList(str, overwrite, onlyTag))
                        break;
                    n++;
                    Application.Current.Dispatcher.Invoke(() => {
                        PbReadFile.Value = n;
                        TbSearchFileCount.Text = n + " / " + files.Length;
                    });
                }
            });

            if (0 < mErrorMsg.Length) {
                MessageBox.Show(mErrorMsg);
            }
            return n;
        }

        /// <summary>
        /// ファイルリストにファイルを追加登録する
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <param name="overWrite">上書きフラグ</param>
        /// <param name="onlyTag">タグ情報のみ上書き</param>
        /// <returns>追加の有無</returns>
        private bool addFileList(string filePath, bool overWrite, bool onlyTag)
        {
            try {
                if (mDataList == null)
                    mDataList = new Dictionary<string, MusicFileData>(StringComparer.OrdinalIgnoreCase);
                FileInfo fi = new System.IO.FileInfo(filePath);
                //  音楽データの取得
                MusicFileData musicFileData = new MusicFileData(fi.Name, fi.DirectoryName, fi.LastWriteTime, fi.Length);
                musicFileData.UpdateFlag = true;
                if (!mDataList.ContainsKey(musicFileData.getPath())) {
                    //  新規データ
                    musicFileData.AddTagData();
                    musicFileData.AddWaveFormat();
                    mDataList.Add(musicFileData.getPath(), musicFileData);
                    return true;
                } else {
                    //  既存データ
                    if (overWrite) {
                        if (onlyTag) {
                            //  ファイル情報以外を元データから追加
                            musicFileData.setTagData(mDataList[musicFileData.getPath()]);
                            musicFileData.setWaveFormat(mDataList[musicFileData.getPath()]);
                        }
                        //  上書き処理
                        if (mDataList.Remove(musicFileData.getPath())) {
                            musicFileData.AddTagData();
                            if (!onlyTag)
                                musicFileData.AddWaveFormat();
                            mDataList.Add(musicFileData.getPath(), musicFileData);
                        }
                    }
                    return true;
                }
            } catch (Exception e) {
                mErrorMsg += "addFileList: " + e.Message + "\n";
                //MessageBoxResult result = MessageBox.Show(e.Message
                //    + "\n継続しますか？", "エラー", MessageBoxButton.OKCancel);
                //if (result == MessageBoxResult.OK)
                //    return true;
                //else
                //    return false;
            }
            return true;
        }

        /// <summary>
        /// 音楽ファイルからタグデータまたは　アルバム情報を取得して表示する
        /// アルバム情報があれば優先的に表示する
        /// </summary>
        /// <param name="path">音楽ファイル名</param>
        /// <param name="albumInfoDisp">アルバム情報優先表示</param>
        /// <param name="musicFileName">曲ファイル名(曲単位のコメント用)</param>
        /// <param name="imageNo">表示するイメージの位置</param>
        /// <param name="imageView">イメージのダイヤログ表示</param>
        /// <returns>(音がファイルのパス,表示したイメージの位置)</returns>
        private (string,int) setDispTagData(string path, bool albumInfoDisp, string musicFileName = "", int imageNo = 0, bool imageView = false)
        {
            FileTagReader fileTagReader = new FileTagReader(path);
            List<string> tagList = fileTagReader.getTagList();
            string title = musicFileName;
            int titleIndex = tagList.FindIndex(p => 0 <= p.IndexOf("Title"));
            if (0 <= titleIndex)
                title = tagList[titleIndex + 1];
            string tuneComment = "";
            LbTagData.Items.Clear();
            if (albumInfoDisp) {
                //  アルバム情報を表示
                mSelectMusiDataFolder = Path.GetDirectoryName(path);
                AlbumInfoData albumInfoData = new AlbumInfoData(Path.GetDirectoryName(path));
                if (albumInfoData.IsDataEnabled()) {
                    LbTagData.Items.Add("アルバム    : " + albumInfoData.getAlbumInfoData("Album"));
                    LbTagData.Items.Add("アーティスト: " + albumInfoData.getAlbumInfoData("Artist"));
                    LbTagData.Items.Add("ジャンル    : " + albumInfoData.getAlbumInfoData("Genre"));
                    LbTagData.Items.Add("スタイル    : " + albumInfoData.getAlbumInfoData("Style"));
                    LbTagData.Items.Add("録音日      : " + albumInfoData.getAlbumInfoData("RecordDate"));
                    LbTagData.Items.Add("元メディア  : " + albumInfoData.getAlbumInfoData("OriginalMedia"));
                    LbTagData.Items.Add("レーベル    : " + albumInfoData.getAlbumInfoData("Label"));
                    LbTagData.Items.Add("レコードNo  : " + albumInfoData.getAlbumInfoData("RecordNo"));
                    LbTagData.Items.Add("入手元      : " + albumInfoData.getAlbumInfoData("Source"));
                    LbTagData.Items.Add("入手日      : " + albumInfoData.getAlbumInfoData("SourceDate"));
                    LbTagData.Items.Add("イメージ数  : " + fileTagReader.getImageDataCount());
                    string[] urlData = albumInfoData.getAlbumInfoDatas("RefURL");
                    if (urlData != null && 1 < urlData.Length && 0 < urlData[1].Length) {
                        LbTagData.Items.Add("参照URL :");
                        for (int i = 1; i < urlData.Length; i++) {
                            if (0 < urlData[i].Length)
                                LbTagData.Items.Add(urlData[i]);
                        }
                    }
                    string[] attachData = albumInfoData.getAlbumInfoDatas("AtachedFile");
                    if (attachData != null && 1 < attachData.Length && 0 < attachData[1].Length) {
                        LbTagData.Items.Add("添付ファイル:");
                        for (int i = 1; i < attachData.Length; i++) {
                            if (0 < attachData[i].Length)
                                LbTagData.Items.Add(attachData[i]);
                        }
                    }
                    string[] personels = ylib.strControlCodeRev(albumInfoData.getAlbumInfoData("Personal")).Split('\n');
                    if (personels != null && 0 < personels.Length) {
                        LbTagData.Items.Add("パーソネル:");
                        foreach (string personal in personels) {
                            LbTagData.Items.Add("  " + personal.Replace("\r", ""));
                        }
                    }
                    string[] comments = ylib.strControlCodeRev(albumInfoData.getAlbumInfoData("Comment")).Split('\n');
                    if (0 < comments.Length && 0 < comments[0].Length) {
                        LbTagData.Items.Add("コメント:");
                        foreach (string comment in comments) {
                            LbTagData.Items.Add("  " + comment.Replace("\r", ""));
                        }
                    }
                    if (0 < musicFileName.Length) {
                        tuneComment = ylib.strControlCodeRev(albumInfoData.getAlbumInfoData("[Tune]" + musicFileName));
                        string[] tuneComments = tuneComment.Split('\n');
                        if (0 < tuneComments.Length && 0 < tuneComments[0].Length) {
                            LbTagData.Items.Add("曲コメント:");
                            foreach (string comment in tuneComments) {
                                LbTagData.Items.Add("  " + comment.Replace("\r", ""));
                            }
                        }
                    }
                } else {
                    //  タグ情報を表示
                    foreach (string tag in tagList) {
                        LbTagData.Items.Add(tag);
                    }
                }
            } else {
                foreach (string tag in tagList) {
                    LbTagData.Items.Add(tag);
                }
            }
            //  画像の表示
            imageNo = Math.Min(imageNo, fileTagReader.getImageDataCount() - 1);
            imageNo = Math.Max(imageNo, 0);
            if (0 < fileTagReader.getImageDataSize(imageNo)) {
                TagImage.Visibility = Visibility.Visible;
                //  ファイルから解放可能なBitmapImageを読み込む
                //  http://neareal.net/index.php?Programming%2F.NetFramework%2FWPF%2FWriteableBitmap%2FLoadReleaseableBitmapImage
                //string ext = fileTagReader.getImageExt(0);
                //string filePath = mImageDataFilePath + "." + ext;
                //File.WriteAllBytes(filePath, fileTagReader.getImageData(0));
                //FileStream stream = File.OpenRead(filePath);

                //  イメージデータをStream化してBitmapImageに使用
                TagImage.Source = ylib.byte2BitmapImage(fileTagReader.getImageData(imageNo));
                //MemoryStream stream = new MemoryStream(fileTagReader.getImageData(0));
                //BitmapImage bitmap = new BitmapImage();
                //try {
                //    bitmap.BeginInit();
                //    bitmap.CacheOption = BitmapCacheOption.OnLoad;  //  作成に使用されたストリームを閉じる
                //    bitmap.StreamSource = stream;
                //    bitmap.EndInit();
                //    stream.Close();
                //    TagImage.Source = bitmap;
                //} catch (Exception e) {
                //    MessageBox.Show(e.Message);
                //}
                if (imageView || (mCoverView != null && mCoverView.IsVisible)) {
                    //  画像をダイヤログ表示
                    if (mCoverView != null)
                        mCoverView.Close();
                    mCoverView = new CoverView();
                    mCoverView.Title = title;
                    mCoverView.mBitmapSource = (BitmapSource)TagImage.Source;
                    mCoverView.mComment = tuneComment;
                    mCoverView.mFullScreen = false;
                    mCoverView.Show();
                }
                //if (imageView || (mFullView != null && mFullView.IsVisible)) {
                //    //  画像をダイヤログ表示
                //    if (mFullView != null)
                //        mFullView.Close();
                //    mFullView = new FullView();
                //    mFullView.mBitmapSource = (BitmapSource)TagImage.Source;
                //    mFullView.mFullScreen = false;
                //    mFullView.mIsModeless = true;
                //    mFullView.Show();
                //}
            } else {
                //  イメージデータがない場合は非表示にする
                TagImage.Visibility = Visibility.Hidden;
            }
            return (path, imageNo);
        }

        /// <summary>
        /// 選択されたアルバムのフォルダ名を返す
        /// </summary>
        /// <returns></returns>
        private string selectedAlbumFolder()
        {
            AlbumData albumData = (AlbumData)DgAlbumListData.SelectedItem;
            return albumData == null ? "" : albumData.Folder;
        }

        /// <summary>
        /// 選択されていアルバムのタイトルを返す
        /// </summary>
        /// <returns></returns>
        private string selectedAlbumTitle()
        {
            AlbumData albumData = (AlbumData)DgAlbumListData.SelectedItem;
            return albumData == null ? "" : albumData.Album;
        }

        /// <summary>
        /// 選択されているアーティスト名のフィルタデータの取得
        /// </summary>
        /// <returns></returns>
        private ArtistData selectedArtist()
        {
            ArtistData artistData = (ArtistData)DgArtistListData.SelectedItem;
            return artistData;
        }

        /// <summary>
        /// 選択されているジャンルのフィルタデータの取得
        /// </summary>
        /// <returns></returns>
        private string selectedGenre()
        {
            return CbGenreList == null ? "" :
                (CbGenreList.Items.Count == 0 ? "" : CbGenreList.Items[CbGenreList.SelectedIndex].ToString());
        }

        /// <summary>
        /// 選択されている年代のフィルタデータの取得
        /// </summary>
        /// <returns></returns>
        private string selectedYear()
        {
            return CbYearList == null ? "" :
                CbYearList.Items.Count == 0 ? "" : CbYearList.Items[CbYearList.SelectedIndex].ToString().Trim();
        }

        /// <summary>
        /// 選択されているユーザー設定ジャンルのフィルタデータの取得
        /// </summary>
        /// <returns></returns>
        private string selectedUserGenre()
        {
            return CbUserGenreList == null ? "" :
                CbUserGenreList.Items.Count == 0 ? "" : CbUserGenreList.Items[CbUserGenreList.SelectedIndex].ToString().Trim();
        }

        /// <summary>
        /// 選択されているユーザースタイルをフィルタデータの取得
        /// </summary>
        /// <returns></returns>
        private string selectedUserStyyle()
        {
            return CbUserStyleList == null ? "" :
                CbUserStyleList.Items.Count == 0 ? "" : CbUserStyleList.Items[CbUserStyleList.SelectedIndex].ToString().Trim();
        }

        /// <summary>
        /// 選択されている元メディアのフィルタデータ取得
        /// </summary>
        /// <returns></returns>
        private string selectedOriginalMedia()
        {
            return CbOriginalMediaList == null ? "" :
                CbOriginalMediaList.Items.Count == 0 ? "" : CbOriginalMediaList.Items[CbOriginalMediaList.SelectedIndex].ToString().Trim();
        }

        /// <summary>
        /// すべてのリストとデータを更新する
        /// </summary>
        /// <param name="allData">全データ更新</param>
        /// <param name="albumFileData">ファイルからアルバムデータを取り込む</param>
        private void UpdateAllListData(bool allData, bool albumFileData)
        {
            if (mFileAdding || !mDispDataSetOn)
                return;
            ylib.stopWatchStartNew();
            System.Diagnostics.Debug.WriteLine($"UpdateAllListData Start [{allData}][{albumFileData}]");
            mDispDataSetOn = false;                 //  表示更新抑制
            if (albumFileData) {
                //  ファイルからアルバムデータを読み込む
                if (!loadAlbumData(mAlbumListPath))
                    albumDataSet(allData);          //  アルバム表示の作成
            } else {
                albumDataSet(allData);              //  アルバム表示の作成
            }

            System.Diagnostics.Debug.WriteLine($"artistDataSet Start {ylib.stopWatchLapTime()}");
            artistDataSet();                        //  アーティストデータの作成
            genreDataSet();                         //  ジャンルデータの設定
            yearDataSet();                          //  年代データ設定
            userGenreDataSet();                     //  ユーザージャンルデータの設定
            userStyleDataSet();                     //  ユーザースタイルデータの設定
            originalMediaDataSet();                 //  元メディアデータの設定
            mDispDataSetOn = true;                  //  表示更新抑制解除
            System.Diagnostics.Debug.WriteLine($"UpdateAllDispData Start {ylib.stopWatchLapTime()}");
            UpdateAllDispData();                    //  表示用データの更新
            System.Diagnostics.Debug.WriteLine($"UpdateAllListData End {ylib.stopWatchTotalTime()}");
        }

        /// <summary>
        /// 表示用データを更新する
        /// </summary>
        private void UpdateAllDispData()
        {
            if (mFileAdding)
                return;
            artistDispDataSet();        //  アーティスト表示データ作成
            albumDispDataSet();         //  アルバム表示データの作成
            musicDispDataSet();         //  表示用データに変換(*)
            UpdateArtistDispList();     //  アーティストデータの表示更新
            UpdateAlbumDispList();      //  アルバムデータ表示の更新
            UpdateMusicDispList();      //  表示データの更新
        }

        /// <summary>
        /// アルバムデータ以下の表示用データを更新する
        /// </summary>
        private void UpdateAlbumDispData()
        {
            albumDispDataSet();         //  アルバム表示データの作成
            musicDispDataSet();         //  表示用データに変換
            UpdateAlbumDispList();      //  アルバムデータ表示の更新
            UpdateMusicDispList();      //  表示データの更新
        }

        /// <summary>
        /// 音楽(曲)データの表示用データを更新する
        /// </summary>
        private void UpdateMusicDispData()
        {
            musicDispDataSet();         //  表示用データに変換
            UpdateMusicDispList();      //  表示データの更新
        }

        /// <summary>
        /// アルバムデータの一つが更新された時に表示を更新
        /// アーティスト情報と音楽データ情報は更新しない
        /// UserArtistが変更した場合、アーティストリストも更新
        /// </summary>
        /// <param name="albumData"></param>
        /// <param name="artistDataUpdate">UserArtistが変更</param>
        private void UpDateAlbumData(AlbumData albumData, bool artistDataUpdate)
        {
            if (mFileAdding)
                return;
            albumDataSet(albumData);
            if (artistDataUpdate) {
                //  アーティストリストの更新
                artistDataSet();
                artistDispDataSet();
                UpdateArtistDispList();
            }
            UpdateAlbumDispList();      //  アルバムデータ表示の更新
        }

        /// <summary>
        /// アルバムデータの更新
        /// </summary>
        /// <param name="albumData">アルバムデータ</param>
        private void albumDataSet(AlbumData albumData)
        {
            if (mDataList == null)
                return;
            if (mAlbumList == null)
                mAlbumList = new Dictionary<string, AlbumData>(StringComparer.OrdinalIgnoreCase);
            string key = albumData.FormatExt + albumData.Folder;
            if (!mAlbumList.ContainsKey(key)) {
                mAlbumList.Add(key, albumData);
            } else {
                //  既に登録されている場合は演奏時間を加算して更新
                mAlbumList[key] = albumData;
            }
        }

        /// <summary>
        /// 音楽ファイルデータからアルバムデータを設定
        /// alldataのフラグがたっていなければ更新されたMusicDataでアルバムデータを更新
        /// </summary>
        /// <param name="alldata">全データの更新</param>
        private void albumDataSet(bool alldata)
        {
            if (mDataList == null)
                return;

            System.Diagnostics.Debug.WriteLine($"albumDataSet Start {ylib.stopWatchLapTime()}");
            if (mAlbumList == null)
                mAlbumList = new Dictionary<string, AlbumData>(StringComparer.OrdinalIgnoreCase);

            if (alldata)
                mAlbumList.Clear();
            HashSet<string> albumFolder = new HashSet<string>();        //  アルバムデータを一部追加リスト用

            System.Diagnostics.Debug.WriteLine($"albumDataSet {mAlbumList.Count} {mDataList.Count} {ylib.stopWatchLapTime()}");
            foreach (MusicFileData musicData in mDataList.Values) {
                if (musicData.UpdateFlag || alldata) {
                    //  アルバムデータの作成・登録
                    AlbumData albumData = new AlbumData(musicData);
                    string key = albumData.FormatExt + albumData.Folder;
                    if (musicData.UpdateFlag && !albumFolder.Contains(key)) {
                        //  アルバムリストを一部追加する時に最初の追加
                        //  アルバムデータの時に一度既存データを削除してから追加(演奏時間を集計するため)
                        if (mAlbumList.ContainsKey(key)) {
                            mAlbumList.Remove(key);
                        }
                        albumFolder.Add(key);
                    }
                    //  アルバムデータの追加
                    if (!mAlbumList.ContainsKey(key)) {
                        mAlbumList.Add(key, albumData);
                    } else {
                        //  既に登録されている場合は演奏時間を加算して更新
                        mAlbumList[key].addCount(musicData);
                    }
                    musicData.UpdateFlag = false;
                }
            }
            System.Diagnostics.Debug.WriteLine($"albumDataSet End {ylib.stopWatchLapTime()}");
        }

        /// <summary>
        /// アーティストデータをアルバムデータをもとに作成
        /// キーワードは大文字と小文字を区別しない
        /// </summary>
        private void artistDataSet()
        {
            if (mAlbumList == null)
                return;
            if (mArtistList == null)
                mArtistList = new Dictionary<string, ArtistData>();
            mArtistList.Clear();
            foreach (AlbumData album in mAlbumList.Values) {
                if (mDispArtistType == 0) {
                    //  ArtistをKeyとして登録
                    if (album.Artist != null) {
                        if (!mArtistList.ContainsKey(album.Artist.ToUpper())) {
                            mArtistList.Add(album.Artist.ToUpper(), new ArtistData(album.Artist, album.AlbumArtist, album.UserArtist));
                        } else {
                            mArtistList[album.Artist.ToUpper()].AlbumCount++;
                        }
                    }
                } else if (mDispArtistType == 1) {
                    //  AlbumArtistをKeyとして登録
                    if (album.AlbumArtist != null) {
                        if (!mArtistList.ContainsKey(album.AlbumArtist.ToUpper())) {
                            mArtistList.Add(album.AlbumArtist.ToUpper(), new ArtistData(album.Artist, album.AlbumArtist, album.UserArtist));
                        } else {
                            mArtistList[album.AlbumArtist.ToUpper()].AlbumCount++;
                        }
                    }
                } else if (mDispArtistType == 2) {
                    //  UserArtistをKeyとして登録
                    if (album.AlbumArtist != null) {
                        if (!mArtistList.ContainsKey(album.UserArtist.ToUpper())) {
                            mArtistList.Add(album.UserArtist.ToUpper(), new ArtistData(album.Artist, album.AlbumArtist, album.UserArtist));
                        } else {
                            mArtistList[album.UserArtist.ToUpper()].AlbumCount++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ジャンルデータを設定する
        /// アルバムデータをもとに作成
        /// </summary>
        private void genreDataSet()
        {
            if (mAlbumList == null)
                return;
            //  ジャンルデータの抽出(Setで重複排除)して一時データ作成
            if (mGenreList == null)
                mGenreList = new List<string>();
            SortedSet<string> genreSet = new SortedSet<string>();
            foreach (AlbumData album in mAlbumList.Values) {
                genreSet.Add(album.Genre);
            }
            //  一時データからListデータに変換
            mGenreList.Clear();
            mGenreList.Add("すべて");
            foreach (string genre in genreSet) {
                mGenreList.Add(genre);
            }
            //  ListデータをComboBoxに登録
            CbGenreList.Items.Clear();
            foreach (string genre in mGenreList) {
                CbGenreList.Items.Add(genre);
            }
            CbGenreList.Text = CbGenreList.Items[0].ToString();
        }

        /// <summary>
        /// 年代データを設定する
        /// アルバムデータをもとに作成
        /// </summary>
        private void yearDataSet()
        {
            if (mAlbumList == null)
                return;
            //  年代データの抽出(Setで重複排除)して一時データ作成
            if (mYearList == null)
                mYearList = new List<string>();
            SortedSet<string> yearSet = new SortedSet<string>();
            foreach (AlbumData album in mAlbumList.Values) {
                yearSet.Add(((album.Year == null || 4 > album.Year.Trim().Length)) ? "" : (album.Year.Trim().Substring(0, 3) + "0"));
            }
            //  一時データからListデータに変換
            mYearList.Clear();
            mYearList.Add("すべて");
            foreach (string year in yearSet) {
                mYearList.Add(year);
            }
            //  ListデータをComboBoxに登録
            CbYearList.Items.Clear();
            foreach (string year in mYearList) {
                CbYearList.Items.Add(year);
            }
            CbYearList.Text = CbYearList.Items[0].ToString();
        }

        /// <summary>
        /// ユーザー設定ジャンルのフィルタデータ作成
        /// </summary>
        private void userGenreDataSet()
        {
            if (mAlbumList == null)
                return;
            //  ジャンルデータの抽出(Setで重複排除)して一時データ作成
            if (mUserGenreList == null)
                mUserGenreList = new List<string>();
            SortedSet<string> userGenreSet = new SortedSet<string>();
            foreach (AlbumData album in mAlbumList.Values) {
                userGenreSet.Add(album.UserGenre);
            }
            //  一時データからListデータに変換
            mUserGenreList.Clear();
            mUserGenreList.Add("すべて");
            foreach (string genre in userGenreSet) {
                mUserGenreList.Add(genre);
            }
            //  ListデータをComboBoxに登録
            CbUserGenreList.Items.Clear();
            foreach (string genre in mUserGenreList) {
                CbUserGenreList.Items.Add(genre);
            }
            CbUserGenreList.Text = CbUserGenreList.Items[0].ToString();
        }

        /// <summary>
        /// ユーザースタイルのフィルタデータの作成と設定
        /// </summary>
        private void userStyleDataSet()
        {
            if (mAlbumList == null)
                return;
            //  スタイルデータの抽出(Setで重複排除)して一時データ作成
            if (mUserStyleList == null)
                mUserStyleList = new List<string>();
            SortedSet<string> userStyleSet = new SortedSet<string>();
            foreach (AlbumData album in mAlbumList.Values) {
                userStyleSet.Add(album.UserStyle);
            }
            //  一時データからListデータに変換
            mUserStyleList.Clear();
            mUserStyleList.Add("すべて");
            foreach (string style in userStyleSet) {
                mUserStyleList.Add(style);
            }
            //  ListデータをComboBoxに登録
            CbUserStyleList.Items.Clear();
            foreach (string style in mUserStyleList) {
                CbUserStyleList.Items.Add(style);
            }
            CbUserStyleList.Text = CbUserStyleList.Items[0].ToString();
        }

        /// <summary>
        /// 元メディアのフィルタデータの作成
        /// </summary>
        private void originalMediaDataSet()
        {
            if (mAlbumList == null)
                return;
            if (mOriginalMediaList == null)
                mOriginalMediaList = new List<string>();
            mOriginalMediaList.Clear();
            mOriginalMediaList.Add("すべて");
            //  スタイルデータの抽出(Setで重複排除)して一時データ作成
            SortedSet<string> originalMediaSet = new SortedSet<string>();
            foreach (AlbumData album in mAlbumList.Values) {
                originalMediaSet.Add(album.OriginalMedia);
            }
            //  一時データからListデータに変換
            foreach (string style in originalMediaSet) {
                mOriginalMediaList.Add(style);
            }
            //  ListデータをComboBoxに登録
            CbOriginalMediaList.Items.Clear();
            foreach (string style in mOriginalMediaList) {
                CbOriginalMediaList.Items.Add(style);
            }
            CbOriginalMediaList.Text = CbOriginalMediaList.Items[0].ToString();
        }

        /// <summary>
        /// 音楽データファイルリストから表示用リストに変換(時間がかかる)
        /// </summary>
        private void musicDispDataSet()
        {
            if (mDataList == null || !mDispDataSetOn)
                return;
            if (mDispFileList == null)
                mDispFileList = new List<MusicFileData>();

            mDispFileList.Clear();
            foreach (MusicFileData musicData in mDataList.Values) {
                //  表示用曲データの作成
                if (musicDataChk(musicData) &&                  //  選択されているアルバムから曲を確認
                    (searchMusicData(musicData, mSearchWord))) {//  検索ワード
                    mDispFileList.Add(musicData);
                }
            }

            //  データのソート(Artist→ Folder→TitleNoの順)
            mDispFileList.Sort((a, b) =>
                //(a.Artist.CompareTo(b.Artist) != 0 ? a.Artist.CompareTo(b.Artist) :   //  曲ごとにアーティストが異なる場合があるので除外
                (a.Folder.CompareTo(b.Folder) != 0 ? a.Folder.CompareTo(b.Folder) :
                a.TitleNo - b.TitleNo));

            //  最初の行のファイルのタグ情報を表示
            if (0 < mDispFileList.Count) {
                MusicFileData fileData = (MusicFileData)mDispFileList[0];
                if (fileData != null)
                    (mCurMusicPath, mCurImageNo) = setDispTagData(fileData.getPath(), RbAlbumInfo.IsChecked == true, fileData.FileName);
            }
        }

        /// <summary>
        /// アルバムの表示用データの設定
        /// フィルタ処理をして表示データを作成
        /// </summary>
        private void albumDispDataSet()
        {
            if (mAlbumList == null || !mDispDataSetOn)
                return;
            //  アルバムデータを表示用データリストにセット
            if (mDispAlbumList == null)
                mDispAlbumList = new List<AlbumData>();

            string genre = selectedGenre();
            string year = selectedYear();
            string userGenre = selectedUserGenre();
            string userStyle = selectedUserStyyle();
            string originalMedia = selectedOriginalMedia();

            int sumTrack = 0;
            long sumTotalTime = 0;
            long sumAlbumSize = 0;

            mDispAlbumList.Clear();
            foreach (AlbumData album in mAlbumList.Values) {
                //  抽出処理(フィルタ)
                if ((album != null && album.Genre != null && album.Year != null) &&
                    (artistSelectDataChk(album)) &&                                                 //  アーティスト
                    (genre.CompareTo("すべて") == 0 || album.Genre.CompareTo(genre) == 0) &&               //  ジャンル
                    (userGenre.CompareTo("すべて") == 0 || album.UserGenre.CompareTo(userGenre) == 0) &&   //  ユーザジャンル
                    (userStyle.CompareTo("すべて") == 0 || album.UserStyle.CompareTo(userStyle) == 0) &&   //  ユーザスタイル
                    (originalMedia.CompareTo("すべて") == 0 || album.OriginalMedia.CompareTo(originalMedia) == 0) &&   //  元メディア
                    (year.CompareTo("すべて") == 0 || ((3 < album.Year.Length ? album.Year.Substring(0, 3) : album.Year).CompareTo((3 < year.Length ? year.Substring(0, 3) : year)) == 0)) &&  //  年代
                    (!mAlbumUnDisp || 0 <= album.UnDisp) &&
                    searchAlbumData(album, mSearchWord)
                    ) {
                    mDispAlbumList.Add(album);
                    sumTrack += album.TrackCount;
                    sumTotalTime += album.TotalTime;
                    sumAlbumSize += album.AlbumSize;
                }
            }

            //  データのソート(Artist→ Albumの順)
            mDispAlbumList.Sort((a, b) =>
                a.Artist.CompareTo(b.Artist) != 0 ? a.Artist.CompareTo(b.Artist) : a.Album.CompareTo(b.Album));
            mDispAlbumList.Insert(0, new AlbumData("すべて", "", "", "", "", "", 0, ""));

            //  アルバムの"すべて"に総曲数と総演奏時間を設定
            foreach (AlbumData album in mDispAlbumList) {
                if (album.Album.CompareTo("すべて") == 0) {
                    album.TrackCount = sumTrack;
                    album.TotalTime = sumTotalTime;
                    album.TotalTimeString = ylib.second2String(sumTotalTime, false);
                    album.AlbumSize = sumAlbumSize;
                    break;
                }
            }
        }

        /// <summary>
        /// 表示用のアーティストデータの作成
        /// </summary>
        private void artistDispDataSet()
        {
            if (mArtistList == null)
                return;
            if (mDispArtistList == null)
                mDispArtistList = new List<ArtistData>();
            mDispArtistList.Clear();
            int albumCount = 0;
            foreach (ArtistData artist in mArtistList.Values) {
                if (searchArtistData(artist, mSearchWord)) {
                    mDispArtistList.Add(artist);
                    albumCount += artist.AlbumCount;
                }
            }
            //  ソート
            if (mDispArtistType == 0) {
                mDispArtistList.Sort((a, b) => a.Artist.CompareTo(b.Artist));
            } else if (mDispArtistType == 1) {
                mDispArtistList.Sort((a, b) => a.AlbumArtist.CompareTo(b.AlbumArtist));
            } else if (mDispArtistType == 2) {
                mDispArtistList.Sort((a, b) => a.UserArtist.CompareTo(b.UserArtist));
            }
            //  先頭に"すべて"を追加
            mDispArtistList.Insert(0, (new ArtistData("すべて", "すべて", "すべて")));
            mDispArtistList[0].AlbumCount = albumCount;
        }

        /// <summary>
        /// 曲データから文字列の検索
        /// 大文字と小文字を区別しない
        /// </summary>
        /// <param name="data">曲データ</param>
        /// <param name="searchWord">検索文字列</param>
        /// <returns></returns>
        private bool searchMusicData(MusicFileData data, string searchWord)
        {
            if (RbSearchMusic.IsChecked != true || searchWord == null || 0 == searchWord.Length)
                return true;
            if (0 <= data.Title.IndexOf(searchWord, StringComparison.OrdinalIgnoreCase) ||
                0 <= data.Artist.IndexOf(searchWord, StringComparison.OrdinalIgnoreCase) ||
                0 <= data.Album.IndexOf(searchWord, StringComparison.OrdinalIgnoreCase) ||
                0 <= data.Id3Tag.IndexOf(searchWord, StringComparison.OrdinalIgnoreCase))
                return true;
            else
                return false;
        }

        /// <summary>
        /// アルバムデータから文字列を検索
        /// 大文字と小文字を区別しない
        /// </summary>
        /// <param name="data">アルバムデータ</param>
        /// <param name="searchWord">検索単語</param>
        /// <returns></returns>
        private bool searchAlbumData(AlbumData data, string searchWord)
        {
            if (RbSearchAlbum.IsChecked != true || searchWord == null || 0 == searchWord.Length)
                return true;
            if (0 <= data.Album.IndexOf(searchWord, StringComparison.OrdinalIgnoreCase) ||
                0 <= data.Artist.IndexOf(searchWord, StringComparison.OrdinalIgnoreCase) ||
                0 <= data.Genre.IndexOf(searchWord, StringComparison.OrdinalIgnoreCase) ||
                0 <= data.Folder.IndexOf(searchWord, StringComparison.OrdinalIgnoreCase) ||
                0 <= data.Label.IndexOf(searchWord, StringComparison.OrdinalIgnoreCase) ||
                0 <= data.OriginalMedia.IndexOf(searchWord, StringComparison.OrdinalIgnoreCase) ||
                0 <= data.Source.IndexOf(searchWord, StringComparison.OrdinalIgnoreCase) ||
                0 <= data.UserGenre.IndexOf(searchWord, StringComparison.OrdinalIgnoreCase) ||
                0 <= data.UserStyle.IndexOf(searchWord, StringComparison.OrdinalIgnoreCase))
                return true;
            else
                return false;
        }

        /// <summary>
        /// アーティストデーから文字列を検索
        /// </summary>
        /// <param name="data">アーティストデータ</param>
        /// <param name="searchWord">検索単語</param>
        /// <returns></returns>
        private bool searchArtistData(ArtistData data, string searchWord)
        {
            if (RbSearchArtist.IsChecked != true || searchWord == null || 0 == searchWord.Length)
                return true;
            if (0 <= data.Artist.IndexOf(searchWord, StringComparison.OrdinalIgnoreCase))   //  大文字小文字を区別ない
                return true;
            else
                return false;
        }

        /// <summary>
        /// 対象の曲がアルバムの表示リストにあるか確認
        /// 対象フォルダが同じかで判断
        /// </summary>
        /// <param name="musicData">ファイル(曲)データ</param>
        /// <returns></returns>
        private bool musicDataChk(MusicFileData musicData)
        {
            IList selArtistItems = DgArtistListData.SelectedItems;
            IList selAlbumItems = DgAlbumListData.SelectedItems;
            if (0 < selAlbumItems.Count && ((AlbumData)selAlbumItems[0]).Album.CompareTo("すべて") != 0) {
                //  選択されたアルバムデータの中に該当するか
                foreach (AlbumData albumData in selAlbumItems) {
                    if (albumData.Folder != null && albumData.FormatExt != null &&
                        albumData.Folder.CompareTo(musicData.Folder) == 0 &&
                        albumData.FormatExt.CompareTo(musicData.getFileType()) == 0)
                        return true;
                }
            } else if (0 < selArtistItems.Count && ((ArtistData)selArtistItems[0]).Artist.CompareTo("すべて") != 0 ||
                !allFilterEnable()) {
                //  表示されている全アルバムデータの中に該当するか
                foreach (AlbumData albumData in mDispAlbumList) {
                    if (albumData.Folder != null && albumData.FormatExt != null &&
                        albumData.Folder.CompareTo(musicData.Folder) == 0 &&
                        albumData.FormatExt.CompareTo(musicData.getFileType()) == 0)
                        return true;
                }
            } else {
                //  アーティストとアルバムの両方が選択アイテムが「すべて」の時無条件でOK
                return true;
            }
            return false;
        }

        /// <summary>
        /// フィルタデータが全て「すべて」になっているかの確認
        /// </summary>
        /// <returns></returns>
        private bool allFilterEnable()
        {
            return selectedGenre().CompareTo("すべて") == 0 &&
                selectedUserGenre().CompareTo("すべて") == 0 &&
                selectedUserStyyle().CompareTo("すべて") == 0 &&
                selectedOriginalMedia().CompareTo("すべて") == 0 &&
                selectedYear().CompareTo("すべて") == 0;
        }

        /// <summary>
        /// アーティストリストに選択された項目に該当するかをチェック
        /// 大文字と小文字は区別しない
        /// </summary>
        /// <param name="album">アーティスト名</param>
        /// <returns></returns>
        private bool artistSelectDataChk(AlbumData album)
        {
            IList selItems = DgArtistListData.SelectedItems;
            if (mDispArtistType == 0) {
                //  Artistデータで表示
                if (0 < selItems.Count && ((ArtistData)selItems[0]).Artist.CompareTo("すべて") != 0) {
                    //  選択されたアルバムデータの中に該当するか
                    foreach (ArtistData artistData in selItems) {
                        if (string.Compare(artistData.Artist, album.Artist, true) == 0)
                            return true;
                    }
                } else {
                    //  表示されている全アルバムデータの中に該当するか
                    foreach (ArtistData artistData in mDispArtistList) {
                        if (string.Compare(artistData.Artist, album.Artist, true) == 0)
                            return true;
                    }
                }
            } else if (mDispArtistType == 1) {
                //  AlbumArtistデータで表示
                if (0 < selItems.Count && ((ArtistData)selItems[0]).AlbumArtist.CompareTo("すべて") != 0) {
                    //  選択されたアルバムデータの中に該当するか
                    foreach (ArtistData artistData in selItems) {
                        if (string.Compare(artistData.AlbumArtist, album.AlbumArtist, true) == 0)
                            return true;
                    }
                } else {
                    //  表示されている全アルバムデータの中に該当するか
                    foreach (ArtistData artistData in mDispArtistList) {
                        if (string.Compare(artistData.AlbumArtist, album.AlbumArtist, true) == 0)
                            return true;
                    }
                }
            } else if (mDispArtistType == 2) {
                //  UserArtistデータで表示
                if (0 < selItems.Count && ((ArtistData)selItems[0]).UserArtist.CompareTo("すべて") != 0) {
                    //  選択されたアルバムデータの中に該当するか
                    foreach (ArtistData artistData in selItems) {
                        if (string.Compare(artistData.UserArtist, album.UserArtist, true) == 0)
                            return true;
                    }
                } else {
                    //  表示されている全アルバムデータの中に該当するか
                    foreach (ArtistData artistData in mDispArtistList) {
                        if (string.Compare(artistData.UserArtist, album.UserArtist, true) == 0)
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 曲データリストの表示内容を更新する
        /// </summary>
        private void UpdateMusicDispList()
        {
            try {
                if (mDispFileList == null)
                    return;
                this.DgFileListData.ItemsSource = new ReadOnlyCollection<MusicFileData>(mDispFileList);
                dispListInfo();
            } catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// アルバムデータリストの表示内容を更新する
        /// </summary>
        private void UpdateAlbumDispList()
        {
            try {
                if (mDispAlbumList == null)
                    return;
                this.DgAlbumListData.ItemsSource = new ReadOnlyCollection<AlbumData>(mDispAlbumList);
            } catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// アーティストデータリストから表示内容を更新する
        /// </summary>
        private void UpdateArtistDispList()
        {
            try {
                if (mArtistList == null)
                    return;
                this.DgArtistListData.ItemsSource = new ReadOnlyCollection<ArtistData>(mDispArtistList);
                DgArtistColumnArtist.Visibility = mDispArtistType == 0 ? Visibility.Visible : Visibility.Hidden;
                DgArtistColumnAlbumArtist.Visibility = mDispArtistType == 1 ? Visibility.Visible : Visibility.Hidden;
                DgArtistColumnUserArtist.Visibility = mDispArtistType == 2 ? Visibility.Visible : Visibility.Hidden;
            } catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// 最下段にデータの数と選択しているデータ数を表示
        /// </summary>
        private void dispListInfo()
        {
            string selectFile = "";
            if (0 < DgFileListData.SelectedItems.Count)
                selectFile = "選択 " + DgFileListData.SelectedItems.Count;

            ListInfo.Text = string.Format("アーティスト{0}  アルバム{1}/{2} 曲{3}/{4}  {5} ",
                mArtistList.Count, mDispAlbumList.Count - 1, mAlbumList.Count, 
                mDispFileList.Count, mDataList.Count, selectFile);
        }

        /// <summary>
        /// ファイルからアルバムデータを読み込む
        /// </summary>
        /// <param name="path">ファイルパス</param>
        /// <returns></returns>
        private bool loadAlbumData(string path)
        {
            if (mAlbumList == null)
                mAlbumList = new Dictionary<string, AlbumData>(StringComparer.OrdinalIgnoreCase);
            AlbumData albumData = new AlbumData();
            List<string[]> list = ylib.loadCsvData(path, albumData.getTitle());
            if (list == null)
                return false;
            foreach (string[] val in list) {
                albumData = new AlbumData(val);
                string key = albumData.FormatExt + albumData.Folder;
                if (!mAlbumList.ContainsKey(key))
                    mAlbumList.Add(key, albumData);
            }
            if (mAlbumList.Count == 0)
                return false;
            else
                return true;
        }

        /// <summary>
        /// 音楽ファイルデータリストの読み込み
        /// </summary>
        /// <param name="path">ファイル名パス</param>
        private void loadMusicData(string path)
        {
            if (mDataList == null)
                mDataList = new Dictionary<string, MusicFileData>(StringComparer.OrdinalIgnoreCase);
            List<string[]> list = ylib.loadCsvData(path, mMusicDataTitle);
            if (list == null)
                return;
            mDataList.Clear();
            foreach (string[] val in list) {
                MusicFileData musicData = new MusicFileData(val);
                if (!mDataList.ContainsKey(musicData.getPath()))
                    mDataList.Add(musicData.getPath(), musicData);
                else
                    System.Diagnostics.Debug.WriteLine($"{musicData.getPath()}");
            }
        }

        /// <summary>
        /// すべてのデータファイルを保存
        /// </summary>
        private void saveFileAll()
        {
            saveMusicData(mMusicFileListPath);              //  音楽ファイルリストデータを保存
            saveAlbumData(mAlbumListPath);                  //  アルバムデータを保存
            saveSearchPathList(mSearchPathListPath);        //  検索パスリストを保存
            saveSearchWordList(mSearchWordListPath);        //  検索ワードリストの保存
            mArtistInfoList.saveData();                     //  アーティスト情報リストを保存
        }

        /// <summary>
        /// アルバムデータをファイルに書き込む
        /// </summary>
        /// <param name="path">ファイルパス</param>
        private void saveAlbumData(string path)
        {
            List<string[]> list = new List<string[]>();
            if (mAlbumList != null) {
                AlbumData albumData = new AlbumData();
                foreach (var v in mAlbumList)
                    list.Add(v.Value.toArray());
                ylib.saveCsvData(path, albumData.getTitle(), list);
            }
        }

        /// <summary>
        /// 音楽ファイルデータリストの保存
        /// </summary>
        /// <param name="path">ファイル名パス</param>
        private void saveMusicData(string path)
        {
            List<string[]> list = new List<string[]>();
            if (mDataList != null) {
                foreach (var v in mDataList)
                    list.Add(v.Value.toArray());
                ylib.saveCsvData(path, mMusicDataTitle, list);
            }
        }

        /// <summary>
        /// 検索パスをComboBoxに設定
        /// 最後に設定したパスを一番上にする
        /// </summary>
        /// <param name="folder"></param>
        private void setSearchPath(string folder, string ext)
        {
            List<string> folderList = new List<string>();
            if (0 < CbSearchPath.Items.Count) {
                foreach (string path in CbSearchPath.Items)
                    if (path.CompareTo(mFolderSelectMessage)!=0)
                        folderList.Add(path);
            }
            string searchPath = Path.Combine(folder, "*." + ext.ToLower());
            CbSearchPath.Items.Clear();
            if (folderList.Contains(searchPath))
                folderList.Remove(searchPath);
            CbSearchPath.Items.Add(searchPath);
            foreach (string path in folderList)
                CbSearchPath.Items.Add(path);

            CbSearchPath.SelectedIndex = CbSearchPath.Items.IndexOf(searchPath);
        }

        /// <summary>
        /// 検索ワードをComboBoxに設定
        /// 最後に設定したワードが一番上になる様にする
        /// </summary>
        /// <param name="word"></param>
        private void setSearchWord(string word)
        {
            List<string> wordList = new List<string>();
            if (0 < CbSearchWord.Items.Count) {
                foreach (string searchWord in CbSearchWord.Items)
                    wordList.Add(searchWord);
            }
            CbSearchWord.Items.Clear();
            if (wordList.Contains(word))
                wordList.Remove(word);
            CbSearchWord.Items.Add(word);
            foreach (string searchWord in wordList)
                CbSearchWord.Items.Add(searchWord);
        }

        /// <summary>
        /// 検索フォルダリストの読み込み
        /// </summary>
        /// <param name="listPath">ファイル名パス</param>
        private void loadSearchPathList(string listPath)
        {
            List<string> list = ylib.loadListData(listPath);
            if (list == null)
                return;
            foreach (var path in list) {
                if (0 < path.Length && !CbSearchPath.Items.Contains(path))
                    if (Directory.Exists(Path.GetDirectoryName(path)))
                        CbSearchPath.Items.Add(path);
            }
        }

        /// <summary>
        /// 検索フォルダリストの保存
        /// 保存数をmSearchWordListMaxまでとする
        /// </summary>
        /// <param name="listPath">ファイル名パス</param>
        private void saveSearchPathList(string listPath)
        {
            List<string> pathList = new List<string>();
            int n = 0;
            foreach (var path in CbSearchPath.Items) {
                pathList.Add(path.ToString());
                if (mSearchPathListMax < n++)
                    break;
            }
            ylib.saveListData(listPath, pathList);
        }

        /// <summary>
        /// 検索ワードリストへの取り込み
        /// </summary>
        /// <param name="listPath"></param>
        private void loadSearchWordList(string listPath)
        {
            List<string> list = ylib.loadListData(listPath);
            if (list == null)
                return;
            foreach (var path in list) {
                if (0 < path.Length && !CbSearchWord.Items.Contains(path))
                    CbSearchWord.Items.Add(path);
            }
        }

        /// <summary>
        /// 検索ワードリストの保存
        /// </summary>
        /// <param name="listPath"></param>
        private void saveSearchWordList(string listPath)
        {
            List<string> wordList = new List<string>();
            int n = 0;
            foreach (var path in CbSearchWord.Items) {
                wordList.Add(path.ToString());
                if (mSearchWordListMax < n++)
                    break;
            }
            ylib.saveListData(listPath, wordList);
        }

        /// <summary>
        /// タグ情報のListBoxのコンテキストメニュー処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tagDataMenuClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("TagCopyMenu") == 0) {
                //  選択行をクリップボードにコピー
                IList selItems = LbTagData.SelectedItems;
                if (0 < selItems.Count) {
                    string buf = "";
                    foreach (string v in selItems)
                        buf += v + "\n";
                    Clipboard.SetText(buf);
                }
            } else if (menuItem.Name.CompareTo("TagAllCopyMenu") == 0) {
                //  タグ情報すべてをクリップボードにコピー
                string buf = "";
                foreach (string v in LbTagData.Items)
                    buf += v + "\n";
                Clipboard.SetText(buf);
            }
        }

        /// <summary>
        /// アーティスト情報のダイヤログ表示
        /// </summary>
        private void artistInfoData()
        {
            ArtistData artistData = (ArtistData)DgArtistListData.SelectedItem;
            if (artistData != null) {
                string artist = mDispArtistType == 0 ? artistData.Artist : mDispArtistType == 1 ? artistData.AlbumArtist : artistData.UserArtist;
                ArtistInfo airtistInfo = new ArtistInfo(artist, mArtistInfoList);
                var result = airtistInfo.ShowDialog();
            }
        }


        /// <summary>
        /// 選択されたファイルのデータをクリップボードにコピーする
        /// </summary>
        private void selectedFileToClipbord()
        {
            string buf = "";
            IList selItems = DgFileListData.SelectedItems;
            if (0 < selItems.Count) {
                foreach (MusicFileData fileData in selItems) {
                    if (buf.Length == 0) {
                        //  タイトル行
                        string[] title = fileData.getTitle();
                        buf += ylib.array2csvString(title) + "\n";
                    }
                    string[] array = fileData.toArray();
                    buf += ylib.array2csvString(array) + "\n";
                }
                Clipboard.SetText(buf);
            }
        }

        /// <summary>
        /// 曲ファイルごとのコメントの追加・編集
        /// </summary>
        private void selectFileCommentAdd()
        {
            IList selItems = DgFileListData.SelectedItems;
            if (0 < selItems.Count) {
                List<MusicFileData> musicFileDataList = new List<MusicFileData>();
                //  編集中に選択アイテムが変わる場合があるのでコピーを作成
                foreach (MusicFileData fileData in selItems)
                    musicFileDataList.Add(new MusicFileData(fileData));
                //  コメントの追加・編集処理
                foreach (MusicFileData fileData in musicFileDataList) {
                    AlbumInfoData albumInfoData = new AlbumInfoData(Path.GetDirectoryName(fileData.getPath()));
                    InputBox inputBox = new InputBox();
                    inputBox.Title = fileData.Title;
                    inputBox.mMultiLine = true;
                    string keyData = "[Tune]" + fileData.FileName;
                    inputBox.mEditText = ylib.strControlCodeRev(albumInfoData.getAlbumInfoData(keyData));
                    if (inputBox.ShowDialog() == true) {
                        albumInfoData.setAlbumInfoData(keyData, ylib.strControlCodeCnv(inputBox.mEditText));
                        albumInfoData.saveData();
                    }
                }
            }
        }

        /// <summary>
        /// アルバムリストで選択されたリストの項目データをクリップボードににコピー
        /// </summary>
        private void selectAlbumToClipbord()
        {
            IList selItems = DgAlbumListData.SelectedItems;
            if (0 < selItems.Count) {
                //  選択されいるアルバムリストをクリップボードにコピー
                string buf = "";
                foreach (AlbumData albumData in selItems) {
                    if (buf.Length == 0) {
                        //  タイトル行
                        string[] title = albumData.getTitle();
                        buf += ylib.array2csvString(title) + "\n";
                    }
                    string[] array = albumData.toArray();
                    buf += ylib.array2csvString(array) + "\n";
                }
                Clipboard.SetText(buf);
            }
        }

        /// <summary>
        /// 選択されたアルバムリストのフォルダーに存在するPDFファイルを開く
        /// </summary>
        private void executeSelectFolderFile()
        {
            IList selItems = DgAlbumListData.SelectedItems;
            if (0 < selItems.Count) {
                //  同一フォルダーにあるpdfファイルを開く
                AlbumData albumData = (AlbumData)selItems[0];
                if (Directory.Exists(albumData.Folder)) {
                    string[] files = Directory.GetFiles(albumData.Folder, "*.pdf", SearchOption.TopDirectoryOnly);
                    foreach (string path in files) {
                        if (System.IO.File.Exists(path)) {
                            System.Diagnostics.Process p =
                                    System.Diagnostics.Process.Start(path);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 選択したアルバムとその下の曲を削除
        /// </summary>
        private void selectedAlbumDataRemove()
        {
            IList selitems = DgAlbumListData.SelectedItems;
            if (0 < selitems.Count) {
                MessageBoxResult result = MessageBox.Show("選択行を削除します", "確認",
                    MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK) {
                    mDispDataSetOn = false;                 //  表示更新抑制
                    List<AlbumData> albumList = new List<AlbumData>();
                    foreach (AlbumData albumData in selitems)
                        albumList.Add(albumData);
                    albumDataListRemove(albumList);         //  音楽データとアルバム削除
                    mDispDataSetOn = true;                  //  表示更新抑制解除
                    UpdateAllListData(false, false);
                }
            }
        }

        /// <summary>
        /// アルバム情報を追加編集する
        /// </summary>
        private void editAlbumInfoData()
        {
            AlbumData albumData = (AlbumData)DgAlbumListData.SelectedItem;
            if (albumData != null) {
                string userArtist = albumData.UserArtist;
                long undisp = albumData.UnDisp;
                AlbumInfo albumInfo = new AlbumInfo(albumData);
                var result = albumInfo.ShowDialog();
                if (result == true) {
                    //  アルバムデータの表示更新
                    albumData.updateAlbumInfoData();
                    UpDateAlbumData(albumData, userArtist.CompareTo(albumData.UserArtist)!=0);
                    if (undisp != albumData.UnDisp)
                        UpdateAlbumDispData();
                }
            }
        }

        /// <summary>
        /// アルバムリストからアーティスト情報を表示する
        /// </summary>
        private void dispArtistInfoData()
        {
            AlbumData albumData = (AlbumData)DgAlbumListData.SelectedItem;
            if (albumData != null) {
                string artist = albumData.Artist;
                if (artist.Length == 0)
                    artist = albumData.AlbumArtist;
                if (artist.Length == 0)
                    artist = albumData.UserArtist;
                ArtistInfo airtistInfo = new ArtistInfo(artist, mArtistInfoList);
                var result = airtistInfo.ShowDialog();
            }
        }

        /// <summary>
        /// オーディオプレイヤで演奏
        /// </summary>
        private void playAudioPlayer()
        {
            AlbumData albumData = (AlbumData)DgAlbumListData.SelectedItem;
            if (albumData != null) {
                string folder = albumData.Folder + "\\*." + albumData.FormatExt;
                PlayStop();
                if (mAudioPlay == null || !mAudioPlay.IsVisible)
                    mAudioPlay = new AudioPlay();
                mAudioPlay.setFolder(folder, "");
                mAudioPlay.Show();
            }
        }

        /// <summary>
        /// スペクトラム解析で演奏
        /// </summary>
        private void spectrumAnalyzerPlayer()
        {
            AlbumData albumData = (AlbumData)DgAlbumListData.SelectedItem;
            if (albumData != null) {
                string folder = albumData.Folder + "\\*." + albumData.FormatExt;
                PlayStop();
                if (mSpecrtrumAnalyzer == null || !mSpecrtrumAnalyzer.IsVisible)
                    mSpecrtrumAnalyzer = new SpectrumAnalyzer();
                mSpecrtrumAnalyzer.setFolder(folder, "");
                mSpecrtrumAnalyzer.Show();
            }
        }

        /// <summary>
        /// 選択したアルバムの音楽ファイルを指定ディレクトリにコピーする
        /// 指定先のフォルダにアーティスト名+アルバム名のフォルダを作成
        /// </summary>
        private void exportFile()
        {
            IList selItems = DgAlbumListData.SelectedItems;
            if (0 < selItems.Count) {
                //  エクスポート先のフォルダを選択
                var destFolder = ylib.folderSelect(".");
                if (0 < destFolder.Length) {
                    //  個々のアルバムデータの更新
                    foreach (AlbumData albumData in selItems) {
                        exportFile(albumData, destFolder);
                    }
                    MessageBox.Show($"{destFolder} にコピーしました。");

                }
            }
        }

        /// <summary>
        /// アルバムのファイルを指定のディレクトリにコピー
        /// </summary>
        /// <param name="album">AlbumData</param>
        /// <param name="destFolder">コピー先フォルダ</param>
        private void exportFile(AlbumData album, string destFolder)
        {
            string destPath = Path.Combine(destFolder, getArtist(album));
            destPath = Path.Combine(destPath, getAlbumTitle(album));
            string[] files = ylib.getFiles(Path.Combine(album.Folder, "*.*"));
            if (0 < files.Length && !Directory.Exists(destPath))
                Directory.CreateDirectory(destPath);
            foreach (string file in files) {
                string ext = Path.GetExtension(file).ToLower();
                if (ext.CompareTo("." + album.FormatExt.ToLower()) != 0 &&
                    ext.CompareTo(".csv") != 0 && ext.CompareTo(".pdf") != 0) continue;
                string destFile = Path.Combine(destPath, Path.GetFileName(file));
                ylib.fileCopy(file, destFile, 2);   //  強制上書き
            }
        }

        /// <summary>
        /// アルバム名の取得
        /// </summary>
        /// <param name="album">AlbumData</param>
        /// <returns>アルバム名</returns>
        private string getAlbumTitle(AlbumData album)
        {
            if (0 < album.Album.Length) {
                if (0 < album.Album.Length)
                    return ylib.convInvalidFileNameChars(album.Album);
            }
            return "不明";
        }

        /// <summary>
        /// アーティスト名の取得
        /// (Artist,AlbumArtist,UserArtistで表示設定されているもの、空白の場合は他を使う)
        /// </summary>
        /// <param name="album">AlbumData</param>
        /// <returns>アーティスト名</returns>
        private string getArtist(AlbumData album)
        {
            //  0:Artist 1:AlbumArtist 2:UserArtist
            string artist = "";
            if (mDispArtistType == 0) {
                if (0 < album.Artist.Length)
                    artist = album.Artist;
                else if (0 < album.AlbumArtist.Length)
                    artist = album.AlbumArtist;
                else if (0 < album.UserArtist.Length)
                    artist = album.UserArtist;
            } else if (mDispArtistType == 1) {
                if (0 < album.AlbumArtist.Length)
                    artist = album.AlbumArtist;
                else if (0 < album.Artist.Length)
                    artist = album.Artist;
                else if (0 < album.UserArtist.Length)
                    artist = album.UserArtist;
            } else if (mDispArtistType == 2) {
                if (0 < album.UserArtist.Length)
                    artist = album.UserArtist;
                else if (0 < album.Artist.Length)
                    artist = album.Artist;
                else if (0 < album.AlbumArtist.Length)
                    artist = album.AlbumArtist;
            }
            if (artist == "")
                return "不明";
            return ylib.convInvalidFileNameChars(artist);
        }

        /// <summary>
        /// 選択されたアルバムリストのフォルダーを開く
        /// </summary>
        private void openFolder()
        {
            AlbumData albumData = (AlbumData)DgAlbumListData.SelectedItem;
            if (albumData != null) {
                if (Directory.Exists(albumData.Folder)) {
                    ylib.openUrl(albumData.Folder);
                }
            }
        }

        /// <summary>
        /// アルバム情報データの一括更新
        /// </summary>
        private void setAlbumInfoData()
        {
            //  選択データの取得
            IList selItems = DgAlbumListData.SelectedItems;
            if (0 < selItems.Count) {
                //  アルバム情報データの設定ダイヤログの表示
                AlbumInfo albumInfo = new AlbumInfo();
                var result = albumInfo.ShowDialog();
                if (result == true) {
                    bool artistListUpdate = false;
                    //  アルバムデータの更新データの取得
                    AlbumInfoData albumInfoUpdateData = albumInfo.getAlbumInfoData();
                    //  個々のアルバムデータの更新
                    foreach (AlbumData albumData in selItems) {
                        //  アルバム情報データファイルの更新
                        AlbumInfoData albumInfoData = new AlbumInfoData(albumData.Folder);
                        if (albumInfoData.mergeData(albumInfoUpdateData))
                            artistListUpdate = true;
                        albumInfoData.saveData();
                        //  アルバムリストデータの表示更新
                        albumData.updateAlbumInfoData();
                    }
                    //  アルバムデータ表示の更新
                    UpdateAlbumDispData();
                    if (artistListUpdate) {
                        //  アーティストリストの更新
                        artistDataSet();
                        artistDispDataSet();
                        UpdateArtistDispList();
                        //UpdateAllListData(true, false);
                    }
                }
            }
        }

        /// <summary>
        /// データの更新
        /// </summary>
        /// <param name="tagOnly">タグ情報のみの更新</param>
        private void albumDataUpdate(bool tagOnly = false)
        {
            IList selItems = DgAlbumListData.SelectedItems;
            List<string> fileList = new List<string>();
            foreach (AlbumData albumData in selItems) {
                string folder = albumData.Folder;
                string ext = albumData.FormatExt;
                if (Directory.Exists(folder)) {
                    string[] files = Directory.GetFiles(folder, "*." + ext, SearchOption.AllDirectories);
                    if (0 < files.Length)
                        fileList.AddRange(files);
                }
            }
            if (0 < fileList.Count) {
                BtAdd.IsEnabled = true;                    //  ボタンの使用可設定
                BtAdd.Content = "中断";
                addFileList(fileList.ToArray(), true, tagOnly);
            }
        }

        /// <summary>
        /// 表示されているファイルリストのファイルの存在を確認する
        /// ファイルがない場合はファイルリストから削除する
        /// </summary>
        private void existFileCheck()
        {
            int n = 0;
            List<String> nonExistFile = new List<string>();
            foreach (MusicFileData musicFile in DgFileListData.Items) {
                if (!File.Exists(musicFile.getPath())) {
                    n++;
                    nonExistFile.Add(musicFile.getPath());
                }
            }
            if (n == 0) {
                MessageBox.Show("ファイルが存在しない項目はありませんでした。");
            } else {
                MessageBoxResult result = MessageBox.Show(DgFileListData.Items.Count.ToString() +
                    "ファイル中 " + n.ToString() + " ファイルが存在しません\n項目から削除しますか?",
                    "確認", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK) {
                    mDispDataSetOn = false;                 //  表示更新抑制
                    List<string> albumList = new List<string>();
                    foreach (string filePath in nonExistFile) {
                        if (!albumList.Contains(mDataList[filePath].Album))
                            albumList.Add(mDataList[filePath].Album);
                        mDataList.Remove(filePath);
                    }
                    albumDataRemove(albumList);             //  音楽データのないアルバム削除
                    mDispDataSetOn = true;                  //  表示更新抑制解除
                    UpdateAllListData(false, false);
                }
            }
        }

        /// <summary>
        /// ファイルリストで選択されている項目を削除する
        /// </summary>
        private void selectedFileDataDelete()
        {
            IList selitems = DgFileListData.SelectedItems;
            if (0 < selitems.Count) {
                MessageBoxResult result = MessageBox.Show("選択行を削除します", "確認",
                    MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK) {
                    mDispDataSetOn = false;                 //  表示更新抑制
                    List<string> albumList = new List<string>();
                    foreach (FileData fileData in selitems) {
                        if (!albumList.Contains(mDataList[fileData.getPath()].Album))
                            albumList.Add(mDataList[fileData.getPath()].Album);
                        mDataList.Remove(fileData.getPath());
                    }
                    albumDataRemove(albumList);             //  音楽データのないアルバム削除
                    mDispDataSetOn = true;                  //  表示更新抑制解除
                    UpdateAllListData(false, false);
                }
            }
        }

        /// <summary>
        /// 選択した範囲で重複項目を削除
        /// </summary>
        private void selectedFileDataSqueeze()
        {
            IList selitems = DgFileListData.SelectedItems;
            if (0 < selitems.Count) {
                MessageBoxResult result = MessageBox.Show("選択行を重複を削除します", "確認",
                    MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK) {
                    mDispDataSetOn = false;                 //  表示更新抑制
                    Dictionary<string, MusicFileData> musicData = new Dictionary<string, MusicFileData>(StringComparer.OrdinalIgnoreCase); ;
                    foreach (FileData fileData in selitems) {
                        if (mDataList.ContainsKey(fileData.getPath())) {
                            if (!musicData.ContainsKey(fileData.getPath()))
                                musicData.Add(fileData.getPath(), mDataList[fileData.getPath()]);
                            mDataList.Remove(fileData.getPath());
                        }
                    }
                    foreach (var keyval in musicData)
                        mDataList.Add(keyval.Key, keyval.Value);
                    mDispDataSetOn = true;                 //  表示更新抑制解除
                    UpdateAllListData(false, false);
                }
            }
        }

        /// <summary>
        /// 表示されているファイルリストをすべて削除
        /// </summary>
        private void dispFileDataDelete()
        {
            MessageBoxResult result = MessageBox.Show("表示データをすべて削除します", "確認",
                MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK) {
                mDispDataSetOn = false;                 //  表示更新抑制
                List<string> albumList = new List<string>();
                foreach (FileData fileData in DgFileListData.Items) {
                    if (!albumList.Contains(mDataList[fileData.getPath()].Album))
                        albumList.Add(mDataList[fileData.getPath()].Album);
                    mDataList.Remove(fileData.getPath());
                }
                albumDataRemove(albumList);             //  音楽データのないアルバム削除
                mDispDataSetOn = true;                  //  表示更新抑制解除
                UpdateAllListData(false, false);
            }
        }

        /// <summary>
        /// すべてのファイルリストのデータを削除する
        /// </summary>
        private void AllDataClear()
        {
            if (mDataList != null) {
                MessageBoxResult result = MessageBox.Show("全データを削除します", "確認",
                    MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK) {
                    mDispDataSetOn = false;                 //  表示更新抑制
                    mDataList.Clear();
                    mDispDataSetOn = true;                  //  表示更新抑制解除
                    UpdateAllListData(true, false);         //  すべてのリストデータを更新する
                }
            }
        }

        /// <summary>
        /// アルバムとその下の曲データを削除
        /// </summary>
        /// <param name="albumList">削除アルバムリスト</param>
        /// <returns>削除数</returns>
        private int albumDataListRemove(List<AlbumData> albumList)
        {
            int count = 0;
            if (albumList.Count == 0)
                return count;
            foreach (AlbumData album in albumList) {
                foreach (string key in mAlbumList.Keys) {
                    if (mAlbumList[key].Folder.CompareTo(album.Folder) == 0) {
                        dataListRemove(mAlbumList[key]);
                        mAlbumList.Remove(key);
                        count++;
                        break;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// アルバムの下の曲データをリストから削除
        /// </summary>
        /// <param name="album">削除アルバムデータ</param>
        /// <returns>削除数</returns>
        private int dataListRemove(AlbumData album)
        {
            int count = 0;
            List<string> musicList = new List<string>();
            foreach (KeyValuePair<string, MusicFileData> item in mDataList) {
                if (album.Folder.CompareTo(item.Value.Folder) == 0 &&
                        album.FormatExt.CompareTo(item.Value.getFileType()) == 0)
                    musicList.Add(item.Key);
            }
            foreach (string key in musicList)
                mDataList.Remove(key);
            return count;
        }


        /// <summary>
        /// 音楽データのないアルバム削除
        /// </summary>
        /// <param name="albumList">アルバムリスト</param>
        /// <returns>削除アルバム数</returns>
        private int albumDataRemove(List<string> albumList)
        {
            int count = 0;
            for (int i = albumList.Count - 1; 0 <= i; i--) {
                if (0 < albumMusicDataCont(albumList[i]))
                    albumList.Remove(albumList[i]);
            }
            if (albumList.Count == 0)
                return count;
            foreach (string album in albumList) {
                foreach (string key in mAlbumList.Keys) {
                    if (mAlbumList[key].Album == album) {
                        mAlbumList.Remove(key);
                        count++;
                        break;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// アルバム内の曲数をカウント
        /// </summary>
        /// <param name="album">アルバム名</param>
        /// <returns>曲数</returns>
        private int albumMusicDataCont(string album)
        {
            int count = 0;
            foreach (MusicFileData data in mDataList.Values) {
                if (data.Album == album)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// ファイルリストで選択されている項目に現在の音量を設定
        /// </summary>
        private void selectedFileVolumeSet()
        {
            IList selitems = DgFileListData.SelectedItems;
            if (0 < selitems.Count) {
                foreach (FileData fileData in selitems) {
                    mDataList[fileData.getPath()].Volume = SlVolPostion.Value;
                }
            }
        }

        /// <summary>
        /// ファイルリストで選択されている項目に現在の音量をクリア
        /// </summary>
        private void selectedFileVolumeClear()
        {
            IList selitems = DgFileListData.SelectedItems;
            if (0 < selitems.Count) {
                foreach (FileData fileData in selitems) {
                    mDataList[fileData.getPath()].Volume = 0;
                }
            }
        }

        /// <summary>
        /// 表示されているファイルリストをすべてに現在の音量を設定
        /// </summary>
        private void dispFileVolumeSet()
        {
            foreach (FileData fileData in DgFileListData.Items) {
                mDataList[fileData.getPath()].Volume = SlVolPostion.Value;
            }
        }

        /// <summary>
        /// 演奏順のボタンの設定を行う
        /// </summary>
        /// <param name="playOrder"></param>
        private void setPlayOrderButton(PLAYORDER playOrder)
        {
            if (playOrder == PLAYORDER.RANDOM_ALBUM) {
                BtPlayOrder.Content = "アルバムランダム";
                BtPlayOrder.ToolTip = "演奏順をランダムなアルバム単位からランダムな曲単位に変更します";
            } else if (playOrder == PLAYORDER.RANDOM_SONG) {
                BtPlayOrder.Content = "曲ランダム";
                BtPlayOrder.ToolTip = "演奏順をランダムな曲単位のリスト順に変更します";
            } else if (playOrder == PLAYORDER.NORMALORDER) {
                BtPlayOrder.Content = "リスト順";
                BtPlayOrder.ToolTip = "演奏順をリスト順からランダムにアルバム単位に変更します";
            }
            mPlayOrder = playOrder;
        }

        /// <summary>
        /// 選択されているフォルダまたは外部アプリまたはAudioPlay.classで
        /// 曲を演奏する
        /// </summary>
        /// <param name="folder">演奏対象フォルダ</param>
        /// <param name="fileName">演奏ファイル</param>
        private void filePlayer(string folder, string fileName)
        {
            if (mOutterPlayer == PLAYERTYPE.OUTTERPLAYER) {
                //  ファイルに関連付けられたプレイヤ演奏
                PlayStop();
                string path = Path.Combine(folder, fileName);
                if (System.IO.File.Exists(path)) {
                    System.Diagnostics.Process p =
                            System.Diagnostics.Process.Start(path);
                } else {
                    MessageBox.Show(path + "\nファイルがありません");
                }
            } else if (mOutterPlayer == PLAYERTYPE.AUDIOPLAER) {
                //  AudioPlayクラスで演奏
                PlayStop();
                if (mPlayer == null || !mPlayer.IsVisible)
                    mPlayer = new AudioPlay();
                mPlayer.Show();
                mPlayer.setFolder(folder, fileName);
            } else {
                PlayFileList();
                if (0 < DgFileListData.SelectedItems.Count)
                    mSelectMusicDataIndex = DgFileListData.SelectedIndex;
                else
                    mSelectMusicDataIndex = 0;
                PlayFile(mSelectMusicData[mSelectMusicDataIndex]);
            }
        }


        /// <summary>
        /// [音楽再生関連]ボタンの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (bt.Name.CompareTo("BtPlay") == 0) {             //  再生ボタン
                Play();
            } else if (bt.Name.CompareTo("BtNextPlay") == 0) {  //  次を再生
                NextPlay();
            } else if (bt.Name.CompareTo("BtPrevPlay") == 0) {  //  前を再生
                PrevPlay();
            } else if (bt.Name.CompareTo("BtPause") == 0) {     //  一時停止ボタン
                PlayPause();
            } else if (bt.Name.CompareTo("BtStop") == 0) {      //  停止ボタン
                PlayStop();
            } else if (bt.Name.CompareTo("BtExit") == 0) {      //  終了ボタン
                Close();
            }
        }

        /// <summary>
        /// 再生ボタンの処理
        /// 新規に再生する場合と中断時からの継続再生の処理
        /// </summary>
        private void Play()
        {
            if (audioLib.IsPause()) {
                //  中断した演奏がある場合、再開する
                if (audioLib.Play(-1))
                    dispatcherTimer.Start();        //  タイマー割込み開始
            } else {
                //  ファイルリストが選択された場合、選択されたファイルをリスト化
                //  再生順を決める
                if (mPlayOrder == PLAYORDER.RANDOM_ALBUM) {
                    RandomAlbumPlay();
                    PlayFileList();
                    mSelectMusicDataIndex = 0;
                } else if (mPlayOrder == PLAYORDER.RANDOM_SONG) {
                    PlayFileList();
                    mSelectMusicDataIndex = mRandom.Next(mSelectMusicData.Length - 1);
                } else {
                    PlayFileList();
                    if (0 < DgFileListData.SelectedItems.Count)
                        mSelectMusicDataIndex = DgFileListData.SelectedIndex;
                    else
                        mSelectMusicDataIndex = 0;
                }
                PlayFile(mSelectMusicData[mSelectMusicDataIndex]);
            }
        }

        /// <summary>
        /// 再生処理
        /// </summary>
        /// <param name="musicFile">曲ファイルデータ</param>
        private void PlayFile(MusicFileData musicFile)
        {
            if (musicFile == null || !File.Exists(musicFile.getPath()))
                return;

            //  曲情報の表示
            ArtistTitle.Content = 0 < musicFile.AlbumArtist.Length ? musicFile.AlbumArtist :
                0 < musicFile.Artist.Length ? musicFile.Artist : "??";
            AlbumTitle.Content = musicFile.Album;
            FileTitle.Content = musicFile.getTitleNo() + ". " + musicFile.Title;
            //  タグ情報と画像の表示
            (mCurMusicPath, mCurImageNo) = setDispTagData(musicFile.getPath(), RbAlbumInfo.IsChecked == true, musicFile.FileName);
            //  曲ファイルを開く
            if (audioLib.Open(musicFile.getPath())) {
                mCurMusicData = musicFile;
                //  演奏時間のスライダの設定・初期化
                SlPlayPostion.Maximum = audioLib.mTotalTime.TotalSeconds;
                SlPlayPostion.LargeChange = audioLib.mTotalTime.TotalSeconds / 20;
                SlPlayPostion.Value = 0;
                mSlPrevPosition = 0;
                mPlayLength = ylib.second2String(audioLib.mTotalTime.TotalSeconds, true);
                dispPosionTime();                               //  曲の演奏時間表示
                VolumePositionInit(musicFile.Volume);           //  ボリューム初期化
                //BalancePositionInit(0);                         //  バランスの初期化
                audioLib.Play(0);                               //  演奏開始
                dispatcherTimer.Start();                        //  タイマー割込み開始
            }

            //  再生曲のファイルリストを選択状態にし見える位置にスクロールさせる
            DgFileListData.SelectedIndex = DgFileListData.Items.IndexOf(musicFile);
            DgFileListData.ScrollIntoView(musicFile);
        }

        /// <summary>
        /// 演奏を一時停止する
        /// </summary>
        private void PlayPause()
        {
            if (audioLib.IsPlaying()) {
                mPlayPosition = audioLib.Pause();   //  演奏中断
                dispatcherTimer.Stop();             //  タイマー割込み停止
            }
        }

        /// <summary>
        /// 演奏の停止
        /// </summary>
        private void PlayStop()
        {
            audioLib.Stop();
            dispatcherTimer.Stop();         //  タイマー割込み停止
            SlPlayPostion.Value = 0;        //  演奏位置初期化
            mSlPrevPosition = 0;
            dispPosionTime();
            ArtistTitle.Content = "";
            AlbumTitle.Content = "";
            FileTitle.Content = "";

            mSelectMusicData = null;
            mSelectMusicDataIndex = 0;
        }

        /// <summary>
        /// 次の曲を再生
        /// </summary>
        private void NextPlay()
        {
            if (mSelectMusicData != null && 0 < mSelectMusicData.Length &&
                mSelectMusicDataIndex < mSelectMusicData.Length - 1) {
                mSelectMusicDataIndex++;
                PlayFile(mSelectMusicData[mSelectMusicDataIndex]);
            }
        }

        /// <summary>
        /// 前の曲を再生
        /// </summary>
        private void PrevPlay()
        {
            if (mSelectMusicData != null && 0 < mSelectMusicData.Length &&
                0 < mSelectMusicDataIndex) {
                mSelectMusicDataIndex--;
                PlayFile(mSelectMusicData[mSelectMusicDataIndex]);
            }
        }

        /// <summary>
        /// ランダムに曲を再生する
        /// </summary>
        private void RandomPlay()
        {
            if (mSelectMusicData != null && 0 < mSelectMusicData.Length) {
                //  曲リストからランダムに選択
                mSelectMusicDataIndex = mRandom.Next(mSelectMusicData.Length - 1);
                //  選択曲がリスト外になった時は最初の曲にする
                if (mSelectMusicData.Length <= mSelectMusicDataIndex || mSelectMusicDataIndex < 0)
                    mSelectMusicDataIndex = 0;

                PlayFile(mSelectMusicData[mSelectMusicDataIndex]);
            }
        }

        /// <summary>
        /// アルバムリストからランダムにアルバムを選ぶ
        /// </summary>
        private void RandomAlbumPlay()
        {
            if (DgAlbumListData.Items.Count < 2)
                return;
            //  次のアルバムをアルバムリストからランダムに選択
            DgAlbumListData.SelectedIndex = mRandom.Next(DgAlbumListData.Items.Count - 1);
            AlbumData albumData = (AlbumData)DgAlbumListData.Items[DgAlbumListData.SelectedIndex];
            if (albumData.Album.CompareTo("すべて") == 0) {
                if (DgAlbumListData.SelectedIndex + 1 < DgAlbumListData.Items.Count - 1) {
                    DgAlbumListData.SelectedIndex++;
                } else {
                    if (0 < DgAlbumListData.SelectedIndex - 1)
                        DgAlbumListData.SelectedIndex--;
                    else
                        return;
                }
            }
            //  選択アルバムを表示領域に入れる
            albumData = (AlbumData)DgAlbumListData.Items[DgAlbumListData.SelectedIndex];
            DgAlbumListData.ScrollIntoView(albumData);
        }

        /// <summary>
        /// アルバムリストで次のアルバムに移る
        /// </summary>
        /// <returns></returns>
        private bool NextAlbumPlay()
        {
            if (DgAlbumListData.Items.Count < 2 ||
                DgAlbumListData.Items.Count <= DgAlbumListData.SelectedIndex + 1)
                return false;
            //  次のアルバム
            DgAlbumListData.SelectedIndex++;
            //  選択アルバムを表示領域に入れる
            AlbumData albumData = (AlbumData)DgAlbumListData.Items[DgAlbumListData.SelectedIndex];
            albumData = (AlbumData)DgAlbumListData.Items[DgAlbumListData.SelectedIndex];
            DgAlbumListData.ScrollIntoView(albumData);
            return true;
        }

        /// <summary>
        /// 現在表示されている状態からファイルリストを作成
        /// </summary>
        private void PlayFileList()
        {
            if (DgFileListData.SelectedItems.Count < 2) {
                //  ファイルリストで選択ファイルがない場合、表示されている全ファイルをリスト化
                mSelectMusicData = new MusicFileData[DgFileListData.Items.Count];
                int i = 0;
                foreach (MusicFileData fileData in DgFileListData.Items) {
                    mSelectMusicData[i++] = fileData;
                }
            } else {
                //  選択されているファイルリストを作成
                IList selItems = DgFileListData.SelectedItems;
                mSelectMusicData = new MusicFileData[selItems.Count];
                int i = 0;
                mSelectMusicDataIndex = 0;
                foreach (MusicFileData fileData in selItems) {
                    mSelectMusicData[i++] = fileData;
                }
            }
        }

        /// <summary>
        /// アルバムの曲リストの終わりを確認
        /// </summary>
        /// <returns></returns>
        private bool IsNewAlbumPlay()
        {
            if (mSelectMusicDataIndex < mSelectMusicData.Length - 1)
                return false;
            else
                return true;
        }

        /// <summary>
        /// タイマー処理
        /// 演奏時間、演奏位置の更新と音楽再生終了確認
        /// 演奏が終われば次の曲を再生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //  演奏位置の表示
            dispPosionTime();
            //  演奏位置の調整
            updateTimePositionSlider();
            //  ボリューム設定
            setVolume();
            //  バランスの設定
            setBalance();
            //  演奏が終わっているか
            if (audioLib.IsStopped()) {
                if (mPlayOrder == PLAYORDER.NORMALORDER) {
                    //  リスト順に演奏
                    if (IsNewAlbumPlay()) {
                        if (NextAlbumPlay()) {
                            PlayFileList();
                            mSelectMusicDataIndex = 0;
                            PlayFile(mSelectMusicData[mSelectMusicDataIndex]);
                        }
                    } else {
                        NextPlay();
                    }
                } else if (mPlayOrder == PLAYORDER.RANDOM_ALBUM) {
                    //  アルバムランダムで演奏
                    if (IsNewAlbumPlay()) {
                        RandomAlbumPlay();
                        PlayFileList();
                        mSelectMusicDataIndex = 0;
                        PlayFile(mSelectMusicData[mSelectMusicDataIndex]);
                    } else {
                        NextPlay();
                    }
                } else if (mPlayOrder == PLAYORDER.RANDOM_SONG) {
                    //  曲ランダムで演奏
                    RandomPlay();
                } else {
                    NextPlay();
                }
            }

            //throw new NotImplementedException();
        }

        /// <summary>
        /// 演奏位置表示の更新
        /// </summary>
        private void updateTimePositionSlider()
        {
            if (SlPlayPostion.Value != mSlPrevPosition) {
                //  スライダーの位置が前回と異なるときスライダーの位置に演奏位置を合わせる
                audioLib.setCurPositionSecond(SlPlayPostion.Value);
            } else {
                //  スライダーの位置を演奏位置に合わせる
                SlPlayPostion.Value = audioLib.getCurPositionSecond();
            }
            mSlPrevPosition = SlPlayPostion.Value;
        }

        /// <summary>
        /// 曲の長さと経過時間を表示
        /// </summary>
        private void dispPosionTime()
        {
            RecordPosition.Text = ylib.second2String(audioLib.getCurPositionSecond(), true) + "/" + mPlayLength;
        }


        /// <summary>
        /// ボリュームの初期設定
        /// </summary>
        /// <param name="vol">初期値</param>
        private void VolumePositionInit(double vol)
        {
            SlVolPostion.Minimum = 0;
            SlVolPostion.Maximum = 1;
            if (0 < vol)
                SlVolPostion.Value = vol;
            SlVolPostion.LargeChange = 0.05;
            SlVolPostion.SmallChange = 0.01;
            TbVolume.Text = SlVolPostion.Value.ToString("0.00");
        }

        /// <summary>
        /// ボリュームの設定
        /// </summary>
        private void setVolume()
        {
            audioLib.setVolume((float)SlVolPostion.Value);
            TbVolume.Text = SlVolPostion.Value.ToString("0.00");
            //mCurMusicData.Volume = SlVolPostion.Value;
        }

        /// <summary>
        /// バランスの初期設定
        /// </summary>
        /// <param name="bal">初期値</param>
        private void BalancePositionInit(double bal)
        {
            if (mIsMediaPlayer) {
                SlBalPostion.Visibility = Visibility.Visible;
                TbBalance.Visibility = Visibility.Visible;
            } else {
                SlBalPostion.Visibility = Visibility.Hidden;
                TbBalance.Visibility = Visibility.Hidden;
            }
            if (bal < -1 && 1 < bal)
                bal = 0;
            SlBalPostion.Minimum = -1;
            SlBalPostion.Maximum = 1;
            SlBalPostion.Value = bal;
            SlBalPostion.LargeChange = 0.05;
            SlBalPostion.SmallChange = 0.01;
            TbBalance.Text = SlBalPostion.Value.ToString("0.00");
        }

        /// <summary>
        /// バランスの設定
        /// </summary>
        private void setBalance()
        {
            audioLib.setBalance((float)SlBalPostion.Value);
            TbBalance.Text = SlBalPostion.Value.ToString("0.00");
        }

        /// <summary>
        /// [アーティストリストのカスタムソーティング]
        /// 「すべて」を除いてソート
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgArtistListData_Sorting(object sender, DataGridSortingEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"DgArtistListData_Sorting [{mDispDataSetOn}]");
            e.Handled = true;   //  既存のソート停止
            if (!mDispDataSetOn)
                return;

            //  ソートの方向
            var sortDir = e.Column.SortDirection;
            if (ListSortDirection.Ascending != sortDir)
                sortDir = ListSortDirection.Ascending;  //  昇順
            else
                sortDir = ListSortDirection.Descending; //  降順

            if (ListSortDirection.Ascending == sortDir) {
                if ("Artist" == e.Column.SortMemberPath)
                    mDispArtistList.Sort((a, b) => (a.Artist.CompareTo("すべて") == 0) ? -1 :
                    (b.Artist.CompareTo("すべて") == 0) ? 1 : a.Artist.CompareTo(b.Artist));
                if ("AlbumArtist" == e.Column.SortMemberPath)
                    mDispArtistList.Sort((a, b) => (a.AlbumArtist.CompareTo("すべて") == 0) ? -1 :
                    (b.AlbumArtist.CompareTo("すべて") == 0) ? 1 : a.AlbumArtist.CompareTo(b.AlbumArtist));
                if ("UserArtist" == e.Column.SortMemberPath)
                    mDispArtistList.Sort((a, b) => (a.UserArtist.CompareTo("すべて") == 0) ? -1 :
                    (b.UserArtist.CompareTo("すべて") == 0) ? 1 : a.UserArtist.CompareTo(b.UserArtist));
                if ("AlbumCount" == e.Column.SortMemberPath)
                    mDispArtistList.Sort((a, b) => (a.Artist.CompareTo("すべて") == 0) ? -1 :
                    (b.Artist.CompareTo("すべて") == 0) ? 1 : (a.AlbumCount - b.AlbumCount));
            } else {
                if ("Artist" == e.Column.SortMemberPath)
                    mDispArtistList.Sort((a, b) => (a.Artist.CompareTo("すべて") == 0) ? -1 :
                    (b.Artist.CompareTo("すべて") == 0) ? 1 : b.Artist.CompareTo(a.Artist));
                if ("AlbumArtist" == e.Column.SortMemberPath)
                    mDispArtistList.Sort((a, b) => (a.AlbumArtist.CompareTo("すべて") == 0) ? -1 :
                    (b.AlbumArtist.CompareTo("すべて") == 0) ? 1 : b.AlbumArtist.CompareTo(a.AlbumArtist));
                if ("UserArtist" == e.Column.SortMemberPath)
                    mDispArtistList.Sort((a, b) => (a.UserArtist.CompareTo("すべて") == 0) ? -1 :
                    (b.UserArtist.CompareTo("すべて") == 0) ? 1 : b.UserArtist.CompareTo(a.UserArtist));
                if ("AlbumCount" == e.Column.SortMemberPath)
                    mDispArtistList.Sort((a, b) => (a.Artist.CompareTo("すべて") == 0) ? -1 :
                    (b.Artist.CompareTo("すべて") == 0) ? 1 : (b.AlbumCount - a.AlbumCount));
            }
            DgArtistListData.ItemsSource = new ReadOnlyCollection<ArtistData>(mDispArtistList);

            foreach (var column in DgArtistListData.Columns) {
                if (column.SortMemberPath == e.Column.SortMemberPath) {
                    column.SortDirection = sortDir;
                }
            }
        }

        /// <summary>
        /// [アルバムリストのカスタムソーティング]
        /// 「すべて」を除いてソート
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgAlbumListData_Sorting(object sender, DataGridSortingEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"DgAlbumListData_Sorting [{mDispDataSetOn}]");
            e.Handled = true;   //  既存のソート停止
            if (!mDispDataSetOn)
                return;

            //  ソートの方向
            var sortDir = e.Column.SortDirection;
            if (ListSortDirection.Ascending != sortDir)
                sortDir = ListSortDirection.Ascending;  //  昇順
            else
                sortDir = ListSortDirection.Descending; //  降順

            if (ListSortDirection.Ascending == sortDir) {
                if ("Album" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.Album.CompareTo(b.Album));
                if ("Year" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.Year.CompareTo(b.Year));
                if ("Artist" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.Artist.CompareTo(b.Artist));
                if ("AlbumArtist" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.AlbumArtist.CompareTo(b.AlbumArtist));
                if ("UserArtist" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.UserArtist.CompareTo(b.UserArtist));
                if ("Genre" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.Genre.CompareTo(b.Genre));
                if ("TrackCount" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.TrackCount - b.TrackCount);
                if ("TotalTimeString" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.TotalTimeString.CompareTo(b.TotalTimeString));
                if ("FormatExt" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.FormatExt.CompareTo(b.FormatExt));
                if ("UserGenre" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.UserGenre.CompareTo(b.UserGenre));
                if ("UserStyle" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.UserStyle.CompareTo(b.UserStyle));
                if ("OriginalMedia" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.OriginalMedia.CompareTo(b.OriginalMedia));
                if ("Label" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.Label.CompareTo(b.Label));
                if ("SourceDate" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.SourceDate.CompareTo(b.SourceDate));
                if ("Source" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.Source.CompareTo(b.Source));
                if ("Folder" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.Folder.CompareTo(b.Folder));
                if ("AlbumSize" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.AlbumSize.CompareTo(b.AlbumSize));
                if ("UnDisp" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.UnDisp.CompareTo(b.UnDisp));
            } else {
                if ("Album" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.Album.CompareTo(a.Album));
                if ("Year" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.Year.CompareTo(a.Year));
                if ("Artist" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.Artist.CompareTo(a.Artist));
                if ("AlbumArtist" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.AlbumArtist.CompareTo(a.AlbumArtist));
                if ("UserArtist" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.UserArtist.CompareTo(a.UserArtist));
                if ("Genre" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.Genre.CompareTo(a.Genre));
                if ("TrackCount" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.TrackCount - a.TrackCount);
                if ("TotalTimeString" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.TotalTimeString.CompareTo(a.TotalTimeString));
                if ("FormatExt" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.FormatExt.CompareTo(a.FormatExt));
                if ("UserGenre" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.UserGenre.CompareTo(a.UserGenre));
                if ("UserStyle" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.UserStyle.CompareTo(a.UserStyle));
                if ("OriginalMedia" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : a.OriginalMedia.CompareTo(b.OriginalMedia));
                if ("Label" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.Label.CompareTo(a.Label));
                if ("SourceDate" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.SourceDate.CompareTo(a.SourceDate));
                if ("Source" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.Source.CompareTo(a.Source));
                if ("Folder" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.Folder.CompareTo(a.Folder));
                if ("AlbumSize" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.AlbumSize.CompareTo(a.AlbumSize));
                if ("UnDisp" == e.Column.SortMemberPath)
                    mDispAlbumList.Sort((a, b) => (a.Album.CompareTo("すべて") == 0) ? -1 :
                    (b.Album.CompareTo("すべて") == 0) ? 1 : b.UnDisp.CompareTo(a.UnDisp));
            }
            DgAlbumListData.ItemsSource = new ReadOnlyCollection<AlbumData>(mDispAlbumList);

            foreach (var column in DgAlbumListData.Columns) {
                if (column.SortMemberPath == e.Column.SortMemberPath) {
                    column.SortDirection = sortDir;
                }
            }
        }
    }
}
