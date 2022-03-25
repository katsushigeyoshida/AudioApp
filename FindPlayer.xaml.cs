using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfLib;

namespace AudioApp
{

    /// <summary>
    /// FindPlayer.xaml の相互作用ロジック
    /// </summary>
    public partial class FindPlayer : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        List<string[]> mUrlList = new List<string[]>() {
            new string[]{ "ジャズ音楽家の一覧", "https://ja.wikipedia.org/wiki/ジャズ音楽家の一覧" },
            new string[]{"List of jazz musicians", "https://en.wikipedia.org/wiki/List_of_jazz_musicians"},
            new string[]{"List of jazz banjoists", "https://en.wikipedia.org/wiki/List_of_jazz_musicians" },
            new string[]{"List of jazz bassists", "https://en.wikipedia.org/wiki/List_of_jazz_bassists" },
            new string[]{"list of jazz drummers", "https://en.wikipedia.org/wiki/List_of_jazz_drummers" },
            new string[]{"list of jazz guitarists", "https://en.wikipedia.org/wiki/List_of_jazz_guitarists" },
            new string[]{"List of jazz organists", "https://en.wikipedia.org/wiki/List_of_jazz_organists" },
            new string[]{"list of jazz pianists", "https://en.wikipedia.org/wiki/List_of_jazz_pianists" },
            new string[]{"list of jazz saxophonists", "https://en.wikipedia.org/wiki/List_of_jazz_saxophonists" },
            new string[]{"list of jazz trombonistss", "https://en.wikipedia.org/wiki/List_of_jazz_trombonists" },
            new string[]{"List of jazz trumpeters", "https://en.wikipedia.org/wiki/List_of_jazz_trumpeters" },
            new string[]{"List of jazz vibraphonists", "https://en.wikipedia.org/wiki/List_of_jazz_vibraphonists" },
            new string[]{"List of jazz violinists", "https://en.wikipedia.org/wiki/List_of_jazz_violinists" },
            new string[]{"List of jazz vocalists", "https://en.wikipedia.org/wiki/List_of_jazz_vocalists" },
        };
        private string[] mUrlListFormat = new string[] { "タイトル", "URLアドレス" };
        private enum PROGRESSMODE { NON, GETDETAIL, SEARCHFILE };
        private PROGRESSMODE mProgressMode = PROGRESSMODE.NON;

        private MusicianDataList mMusicianDataList = new MusicianDataList();
        private bool mGetInfoDataAbort = true;      //  詳細データ取得
        private string mAppFolder;                  //  アプリケーションフォルダ
        private string mDataFolder;
        private string mUrlListPath = "";

        public string[] mMusicianData;
        public bool mCloseBottonVisible = false;

        YLib ylib = new YLib();

        public FindPlayer()
        {
            InitializeComponent();

            WindowFormLoad();                       //  Windowの位置とサイズを復元
            mAppFolder = ylib.getAppFolderPath();   //  アプリフォルダ
            mDataFolder = Path.Combine(mAppFolder, "MusicianData");
            mUrlListPath = Path.Combine(mAppFolder, "MusicianUrlList.csv");

            loadUrlList(mUrlListPath);
            setUrlList();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (mCloseBottonVisible) {
                BtOK.Visibility = Visibility.Visible;
                BtCancel.Visibility = Visibility.Visible;
            } else {
                BtOK.Visibility = Visibility.Hidden;
                BtCancel.Visibility = Visibility.Hidden;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            saveUrlList(mUrlListPath);
            //curPlayerListSave(mCurTitle);    //  演奏者データをファイルに保存
            WindowFormSave();       //  ウィンドの位置と大きさを保存
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
            //sampleGraphInit();
            //drawSampleGraph(mStartPosition, mEndPosition);
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.FindPlayerWidth < 100 ||
                Properties.Settings.Default.FindPlayerHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.FindPlayerHeight) {
                Properties.Settings.Default.FindPlayerWidth = mWindowWidth;
                Properties.Settings.Default.FindPlayerHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.FindPlayerTop;
                Left = Properties.Settings.Default.FindPlayerLeft;
                Width = Properties.Settings.Default.FindPlayerWidth;
                Height = Properties.Settings.Default.FindPlayerHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.FindPlayerTop = Top;
            Properties.Settings.Default.FindPlayerLeft = Left;
            Properties.Settings.Default.FindPlayerWidth = Width;
            Properties.Settings.Default.FindPlayerHeight = Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// [検索対象タイトル]で演奏者リストを選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbTitle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= CbTitle.SelectedIndex && mGetInfoDataAbort) {
                //  URLまたはファイルから演奏者リストを取得
                LbUrlAddress.Content = mUrlList[CbTitle.SelectedIndex][1];
                getMusicPlayerList(mUrlList[CbTitle.SelectedIndex][0],mUrlList[CbTitle.SelectedIndex][1], false);
            }
        }

        /// <summary>
        /// [検索URL]ダフルクリックで演奏者リストの Webページを開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbUrlAddress_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LbUrlAddress.Content.ToString().Length != 0) {
                System.Diagnostics.Process.Start(LbUrlAddress.Content.ToString());
            }
        }

        /// <summary>
        /// [演奏者一覧ＵＲＬアドレス]コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbUrlContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("LbUrlCopyMenu") == 0) {
                //  URLのコピー
                Clipboard.SetText(LbUrlAddress.Content.ToString());
            } else if (menuItem.Name.CompareTo("LbUrlOpenMenu") == 0) {
                //  URLを開く
                System.Diagnostics.Process.Start(LbUrlAddress.Content.ToString());
            } else if (menuItem.Name.CompareTo("LbUrlAddMenu") == 0) {
                //  URLの追加
                addUrlList();
            } else if (menuItem.Name.CompareTo("LbUrlEditMenu") == 0) {
                //  URLの追加
                editUrlList();
            } else if (menuItem.Name.CompareTo("LbUrlRemoveMenu") == 0) {
                //  URLの削除
                delUrlList();
            }
        }

        /// <summary>
        /// [詳細取得]ボタン 演奏者データを各Webページから取得
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtGetData_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (bt.Content.ToString().CompareTo("詳細取得") == 0) {
                CbTitle.IsEnabled = false;
                mGetInfoDataAbort = false;
                getInfoData();
                bt.Content = "中断";
            } else if (bt.Content.ToString().CompareTo("中断") == 0) {
                //  登録処理中断のフラグを設定
                CbTitle.IsEnabled = true;
                mGetInfoDataAbort = true;
                bt.Content = "詳細取得";
                setDispMusicianData();
            }
        }

        /// <summary>
        /// [詳細表示]ボタン データカラムの表示非表示切替
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtInfoData_Click(object sender, RoutedEventArgs e)
        {
            if (DhBirthName.Visibility == Visibility.Hidden) {
                DhBirthName.Visibility = Visibility.Visible;
                DhAlsoKnownAs.Visibility = Visibility.Visible;
                DhBirthPlace.Visibility = Visibility.Visible;
                DhBorn.Visibility = Visibility.Visible;
                DhDied.Visibility = Visibility.Visible;
                DhGenres.Visibility = Visibility.Visible;
                DhOccupations.Visibility = Visibility.Visible;
                DhInstruments.Visibility = Visibility.Visible;
                DhYearsActive.Visibility = Visibility.Visible;
                DhLables.Visibility = Visibility.Visible;
                DhAssociatedActs.Visibility = Visibility.Visible;
                DhMember.Visibility = Visibility.Visible;
            } else {
                DhBirthName.Visibility = Visibility.Hidden;
                DhAlsoKnownAs.Visibility = Visibility.Hidden;
                DhBirthPlace.Visibility = Visibility.Hidden;
                DhBorn.Visibility = Visibility.Hidden;
                DhDied.Visibility = Visibility.Hidden;
                DhGenres.Visibility = Visibility.Hidden;
                DhOccupations.Visibility = Visibility.Hidden;
                DhInstruments.Visibility = Visibility.Hidden;
                DhYearsActive.Visibility = Visibility.Hidden;
                DhLables.Visibility = Visibility.Hidden;
                DhAssociatedActs.Visibility = Visibility.Hidden;
                DhMember.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// [一覧更新]ボタン Webぺーじのデータを再取得する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtUpdateData_Click(object sender, RoutedEventArgs e)
        {
            if (0 <= CbTitle.SelectedIndex && mGetInfoDataAbort) {
                string listTitle = mUrlList[CbTitle.SelectedIndex][0];
                string listUrl = mUrlList[CbTitle.SelectedIndex][1];
                mMusicianDataList.getPlayerList(listTitle, listUrl, false);
                playerListSave(listTitle);
                List<MusicianData> musicianList = mMusicianDataList.mMusicianList;
                if (musicianList != null) {
                    DgPlayer.ItemsSource = new ReadOnlyCollection<MusicianData>(musicianList);
                    setDispMusicianData();
                }
            }
        }

        /// <summary>
        /// [次検索] タイトルから検索文字を検索
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtNextSearch_Click(object sender, RoutedEventArgs e)
        {
            int n = mMusicianDataList.nextSearchPlayer(TbSearch.Text.ToString(), DgPlayer.SelectedIndex);
            if (0 <= n) {
                DgPlayer.SelectedIndex = n;
                MusicianData item = (MusicianData)DgPlayer.Items[n];
                DgPlayer.ScrollIntoView(item);
            }
        }

        /// <summary>
        /// [前検索] タイトルから検索文字を検索
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtPrevSearch_Click(object sender, RoutedEventArgs e)
        {
            int n = mMusicianDataList.prevSearchPlayer(TbSearch.Text.ToString(), DgPlayer.SelectedIndex);
            if (0 <= n) {
                DgPlayer.SelectedIndex = n;
                MusicianData item = (MusicianData)DgPlayer.Items[n];
                DgPlayer.ScrollIntoView(item);
            }
        }

        /// <summary>
        /// [検索]ボタン]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtAllSearch_Click(object sender, RoutedEventArgs e)
        {
            if (0 < TbSearch.Text.Length) {
                mMusicianDataList.getSearchAllPlayer(TbSearch.Text, mDataFolder);
                setDispMusicianData();
                CbTitle.SelectedIndex = -1;
                //getSearchAllPlayer(TbSearch.Text, mDataFolder);
            }
        }

        /// <summary>
        /// [OK]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtOK_Click(object sender, RoutedEventArgs e)
        {
            MusicianData musicianData = (MusicianData)DgPlayer.SelectedItem;
            if (musicianData != null) {
                mMusicianData = musicianData.getStringData();
                DialogResult = true;
                Close();
            }
        }

        /// <summary>
        /// [Cancel]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// [ヘルプ] ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtHelp_Click(object sender, RoutedEventArgs e)
        {
            HelpView help = new HelpView();
            help.mHelpText = HelpText.mFindPlayerHelp;
            help.Show();
        }

        /// <summary>
        /// [演奏者リスト]をダブルクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgPlayer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MusicianData musicianData = (MusicianData)DgPlayer.SelectedItem;
            if (musicianData != null) {
                System.Diagnostics.Process.Start(musicianData.mUrl);
            }
        }

        /// <summary>
        /// [演奏者リスト]のコンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgPlayerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("DgCopyMenu") == 0) {
                //  選択データのコピー
                if (0 < DgPlayer.SelectedItems.Count) {
                    string buffer = ylib.array2csvString(MusicianData.mDataFormat);
                    foreach (MusicianData data in DgPlayer.SelectedItems) {
                        buffer += "\n";
                        buffer += ylib.array2csvString(data.getStringData());
                    }
                    Clipboard.SetText(buffer);
                }
            } else if (menuItem.Name.CompareTo("DgUrlCopyMenu") == 0) {
                //  URLのみコピー
                MusicianData musicianData = (MusicianData)DgPlayer.SelectedItem;
                if (musicianData != null)
                    Clipboard.SetText(musicianData.mUrl);
            } else if (menuItem.Name.CompareTo("DgOpenMenu") == 0) {
                //  選択アイテムのURLを開く
                MusicianData musicianData = (MusicianData)DgPlayer.SelectedItem;
                if (musicianData != null)
                    System.Diagnostics.Process.Start(musicianData.mUrl);
            } else if (menuItem.Name.CompareTo("DgDispMenu") == 0) {
                //  詳細表示
                MusicianData musicianData = (MusicianData)DgPlayer.SelectedItem;
                if (musicianData != null) {
                    string[] data = musicianData.getStringData();
                    string buffer = "";
                    for (int i = 0; i < MusicianData.mDataFormat.Length; i++) {
                        buffer += MusicianData.mDataFormat[i] + " : ";
                        buffer += data[i] + "\n";
                    }
                    messageBox(buffer, musicianData.mTitle);
                    //MessageBox.Show(buffer, "詳細表示");
                }
            }
        }

        /// <summary>
        /// [演奏者リスト]の選択行の変更で検索開始行を設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        /// <summary>
        /// [進捗バー]で終了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PbGetInfoData_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PbGetInfoData.Value == PbGetInfoData.Maximum || mGetInfoDataAbort) {
                if (mProgressMode == PROGRESSMODE.GETDETAIL) {
                    PbGetInfoData.Value = 0;
                    LbGetDataProgress.Content = "完了";
                    BtGetData.Content = "詳細取得";
                    CbTitle.IsEnabled = true;
                    mGetInfoDataAbort = true;
                    playerListSave(CbTitle.Text);
                    setDispMusicianData();
                } else if (mProgressMode == PROGRESSMODE.SEARCHFILE) {
                    setDispMusicianData();
                    if (0 < mMusicianDataList.mMusicianList.Count)
                        DgPlayer.SelectedIndex = 0;
                    CbTitle.SelectedIndex = -1;
                }
                mProgressMode = PROGRESSMODE.NON;
            }
        }

        /// <summary>
        /// Webからの演奏者データの取得進捗の表示(非同期処理)
        /// </summary>
        private void getInfoData()
        {
            mProgressMode = PROGRESSMODE.GETDETAIL;
            PbGetInfoData.Maximum = mMusicianDataList.mMusicianList.Count - 1;
            PbGetInfoData.Minimum = 0;
            PbGetInfoData.Value = 0;
            Task.Run(() => {
                for (int i = 0; i < mMusicianDataList.mMusicianList.Count; i++) {
                    if (mGetInfoDataAbort)
                        break;
                    mMusicianDataList.mMusicianList[i].updateInfoData();
                    Application.Current.Dispatcher.Invoke(() => {
                        PbGetInfoData.Value = i;
                        LbGetDataProgress.Content = "進捗 " + (i + 1) + " / " + mMusicianDataList.mMusicianList.Count;
                    });
                }
            });
        }

        public void getSearchAllPlayer(string searchText, string dataFoleder)
        {
            string[] fileList = ylib.getFiles(dataFoleder + "\\*.csv");
            if (fileList != null) {
                mProgressMode = PROGRESSMODE.SEARCHFILE;
                PbGetInfoData.Maximum = fileList.Length - 1;
                PbGetInfoData.Minimum = 0;
                PbGetInfoData.Value = 0;
                int i = 0;
                mMusicianDataList.mMusicianList.Clear();
                Task.Run(() => {
                    foreach (string path in fileList) {
                        mMusicianDataList.mMusicianList.AddRange(mMusicianDataList.getSerchPlayerFile(searchText, path));
                        Application.Current.Dispatcher.Invoke(() => {
                            LbGetDataProgress.Content = "検出 " + mMusicianDataList.mMusicianList.Count;
                            PbGetInfoData.Value = i++;
                        });
                    }
                });
            }
        }

        /// <summary>
        /// 演奏者一覧URLリストの追加
        /// </summary>
        private void addUrlList()
        {
            InputBox2 dlg = new InputBox2();
            dlg.Title = "演奏者リストWebページ設定";
            dlg.mTitle1 = "リストタイトル";
            dlg.mTitle2 = "ＵＲＬアドレス";
            var result = dlg.ShowDialog();
            if (result == true) {
                string[] data = new string[2];
                data[0] = dlg.mEditText1;
                data[1] = Uri.UnescapeDataString(dlg.mEditText2);
                if (data[0].Length <= 0)
                    data[0] = data[1].Substring(data[1].LastIndexOf('/') + 1);
                data[0] = data[0].Replace(':', '_');
                data[0] = data[0].Replace('#', '_');
                data[0] = data[0].Replace('\n', '_');
                data[0] = data[0].Replace('?', '_');
                data[0] = data[0].Replace('*', '_');
                mUrlList.Add(data);
                setUrlList();
            }
        }

        /// <summary>
        /// 演奏者一覧URLリストの編集
        /// </summary>
        private void editUrlList()
        {
            if (0 <= CbTitle.SelectedIndex) {
                InputBox2 dlg = new InputBox2();
                dlg.Title = "演奏者リストWebページ設定";
                dlg.mTitle1 = "リストタイトル";
                dlg.mTitle2 = "ＵＲＬアドレス";
                dlg.mEditText1 = mUrlList[CbTitle.SelectedIndex][0];
                dlg.mEditText2 = mUrlList[CbTitle.SelectedIndex][1];
                dlg.mEditText2Enabled = false;
                string spath = Path.Combine(mDataFolder, mUrlList[CbTitle.SelectedIndex][0] + ".csv");
                var result = dlg.ShowDialog();
                if (result == true) {
                    string[] data = new string[2];
                    data[0] = dlg.mEditText1;
                    data[1] = Uri.UnescapeDataString(dlg.mEditText2);
                    if (data[0].Length <= 0)
                        data[0] = data[1].Substring(data[1].LastIndexOf('/') + 1);
                    data[0] = data[0].Replace(':', '_');
                    data[0] = data[0].Replace('#', '_');
                    data[0] = data[0].Replace('\n', '_');
                    data[0] = data[0].Replace('?', '_');
                    data[0] = data[0].Replace('*', '_');
                    string dpath = Path.Combine(mDataFolder, data[0] + ".csv");
                    if (File.Exists(spath))
                        File.Move(spath, dpath);
                    mUrlList.RemoveAt(CbTitle.SelectedIndex);
                    mUrlList.Add(data);
                    setUrlList();
                    CbTitle.SelectedIndex = CbTitle.Items.IndexOf(data[0]);
                }
            }
        }

        /// <summary>
        /// 演奏者一覧URLをリストから削除
        /// </summary>
        private void delUrlList()
        {
            if (0 <= CbTitle.SelectedIndex) {
                var result = MessageBox.Show("[" + mUrlList[CbTitle.SelectedIndex][0] + "] を削除します", "削除確認", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK) {
                    mUrlList.RemoveAt(CbTitle.SelectedIndex);
                    setUrlList();
                }
            }
        }

        /// <summary>
        /// 演奏者一覧Webページのリストを設定
        /// </summary>
        private void setUrlList()
        {
            CbTitle.Items.Clear();
            foreach (string[] data in mUrlList)
                CbTitle.Items.Add(data[0]);
            if (0 <= CbTitle.SelectedIndex)
                LbUrlAddress.Content = mUrlList[CbTitle.SelectedIndex][1];
            else
                LbUrlAddress.Content = "";
        }

        /// <summary>
        /// メッセージ表示
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="title"></param>
        private void messageBox(string buf, string title)
        {
            InputBox dlg = new InputBox();
            dlg.Title = title;
            dlg.mWindowSizeOutSet = true;
            dlg.mWindowWidth = 500.0;
            dlg.mWindowHeight = 400.0;
            dlg.mMultiLine = true;
            dlg.mReadOnly = true;
            dlg.mEditText = buf;
            dlg.ShowDialog();
        }

        /// <summary>
        /// URLの一覧リストを読み込む
        /// </summary>
        /// <param name="filePath"></param>
        private void loadUrlList(string filePath)
        {
            List<string[]> urlList = ylib.loadCsvData(filePath, mUrlListFormat);
            if (urlList != null) {
                foreach (string[] data in urlList) {
                    string[] odata = new string[data.Length];
                    for (int i = 0; i < data.Length; i++)
                        odata[i] = data[i].Replace("\\n", "\n");
                    bool dup = false;
                    foreach (var udata in mUrlList) {
                        if (udata[0].CompareTo(odata[0]) == 0) {
                            dup = true;
                            break;
                        }
                    }
                    if (!dup)
                        mUrlList.Add(odata);
                }
                setUrlList();
            }
        }

        /// <summary>
        /// URLの一覧リストを保存する
        /// </summary>
        /// <param name="filePath"></param>
        private void saveUrlList(string filePath)
        {
            List<string[]> urlList = new List<string[]>();
            foreach (string[] data in mUrlList) {
                string[] odata = new string[data.Length];
                for (int i = 0; i < data.Length; i++)
                    odata[i] = data[i].Replace("\n", "\\n");
                urlList.Add(odata);
            }
            ylib.saveCsvData(filePath, mUrlListFormat, urlList);
        }

        /// <summary>
        /// 指定URLの演奏かリストの取得
        /// </summary>
        /// <param name="title">一覧のタイトル</param>
        /// <param name="url">一覧ページのURL</param>
        /// <param name="infoData">詳細データ取得有無</param>
        private void getMusicPlayerList(string title, string url, bool infoData)
        {
            string filePath = Path.Combine(mDataFolder, title + ".csv");
            if (File.Exists(filePath)) {
                //  ファイルからデータを取得
                mMusicianDataList.loadData(filePath, title);
            } else {
                //  Webページからデータを取得
                mMusicianDataList.getPlayerList(title, url, infoData);
                playerListSave(title);
            }
            setDispMusicianData();
        }

        /// <summary>
        /// MusicianDataを表示データとして再設定
        /// </summary>
        private void setDispMusicianData()
        {
            List<MusicianData> musicianList = mMusicianDataList.mMusicianList;
            if (musicianList != null) {
                DgPlayer.ItemsSource = new ReadOnlyCollection<MusicianData>(musicianList);
                LbGetDataProgress.Content = "データ数 " + mMusicianDataList.mMusicianList.Count;
            }
        }

        /// <summary>
        /// 演奏者リストをファイルに保存
        /// </summary>
        private void playerListSave(string title)
        {
            if (0 < title.Length) {
                string filePath = Path.Combine(mDataFolder, title + ".csv");
                mMusicianDataList.saveData(filePath);
            }
        }
    }
}