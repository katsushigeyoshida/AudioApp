using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using WpfLib;

namespace AudioApp
{
    /// <summary>
    /// ExportFileDialog.xaml の相互作用ロジック
    /// 音楽ファイルのエクスポート
    /// (音楽ファイルを他のディレクトリにコピーする)
    /// </summary>
    public partial class ExportFileDialog : Window
    {
        public List<AlbumData> mAlbumDatas;                     //  コピー対象のアルバムデータリスト
        
        public int mDispArtistType = 0;                         //  アーティスト名フォルダの種別(0:Artist 1:AlbumArtist 2:UserArtist)
        private List<string> mFolderList = new List<string>();  //  コピー先フォルダーリスト
        private string mFolderListPath = "ExportFolderList.csv";//  フォルダリストファイル名
        private bool mCopyStop = false;                         //  コピー中断フラグ

        private YLib ylib = new YLib();

        public ExportFileDialog()
        {
            InitializeComponent();

            //  フォルダリストの読込と設定
            List<string> folderList = ylib.loadListData(mFolderListPath);
            if (folderList != null) {
                foreach (var folder in folderList) {
                    if (20 < mFolderList.Count) break;
                    mFolderList.Add(folder);
                }
                cbDestFolder.ItemsSource = mFolderList;
                if (0 < mFolderList.Count)
                    cbDestFolder.SelectedIndex = 0;
            }
            lbFileCount.Content = "";
        }

        /// <summary>
        /// エクスポート先のフォルダを選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbDestFolder_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string folder = ".";
            if (0 < cbDestFolder.Text.Length)
                folder = cbDestFolder.Text;
            folder = ylib.folderSelect(folder);
            if (0 < folder.Length) {
                if (mFolderList.Contains(folder))
                    mFolderList.Remove(folder);
                mFolderList.Insert(0, folder);
                cbDestFolder.ItemsSource = mFolderList;
                cbDestFolder.Text = folder;
            }
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //  フォルダーリストの保存
            ylib.saveListData(mFolderListPath, mFolderList);
        }

        /// <summary>
        /// 転送先フォルダの選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbDestFolder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string folder = ".";
            if (0 <= cbDestFolder.SelectedIndex) {
                folder = cbDestFolder.Items[cbDestFolder.SelectedIndex].ToString();
                if (0 < folder.Length) {
                    if (mFolderList.Contains(folder))
                        mFolderList.Remove(folder);
                    mFolderList.Insert(0, folder);
                    cbDestFolder.ItemsSource = mFolderList;
                }
            }
        }

        /// <summary>
        /// 進捗バーの変更処理処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbTranslateCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (pbTranslateCount.Value == pbTranslateCount.Maximum || mCopyStop) {
                //  進捗バーの終了処理
                btStart.Content = "終了";
                mCopyStop = false;
                Close();
            }
        }

        /// <summary>
        /// コピー開始ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btStart_Click(object sender, RoutedEventArgs e)
        {
            if (btStart.Content.ToString() == "開始") {
                btStart.Content = "中断";
                //  音楽ファイルのエクスポート
                exportFiles();
            } else if (btStart.Content.ToString() == "中断") {
                mCopyStop = true;
                btStart.Content = "終了";
            } else if (btStart.Content.ToString() == "終了") {
                Close();
            }
        }

        /// <summary>
        /// 音楽ファイルのエクスポート(他のディレクトリにコピー)
        /// </summary>
        private void exportFiles()
        {
            List<string[]>fileList = new List<string[]>();
            string destFolder = cbDestFolder.Text;
            foreach(var album in mAlbumDatas)
                fileList.AddRange(getFiles(album, destFolder));
            int count = 0;
            int update =　cbOverWrite.IsChecked == true ? 2 : 0;     //  0: 更新  2: 強制上書き
            pbTranslateCount.Minimum = 0;
            pbTranslateCount.Maximum = fileList.Count;
            //  タスク処理でファイルコピー
            Task.Run(async () => {
                foreach (var file in fileList) {
                    if (mCopyStop) break;                           //  中断
                    string folder = Path.GetDirectoryName(file[1]);
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    ylib.fileCopy(file[0], file[1], update);
                    count++;
                    //  進捗状況表示
                    Application.Current.Dispatcher.Invoke(() => {
                        lbFileCount.Content = count.ToString() + " / " + fileList.Count.ToString();
                        pbTranslateCount.Value++;
                    });
                }
            });
        }

        /// <summary>
        /// アルバムデータから転送元ファイルパスと転送先パスのペアデータのリストを作成
        /// </summary>
        /// <param name="album">アルバムデータ</param>
        /// <param name="destFolder">転送先フォルダ</param>
        /// <returns>ファイルリスト(転送元パス,転送先パス)</returns>
        private List<string[]>getFiles(AlbumData album, string destFolder)
        {
            List<string[]> files = new List<string[]>();
            string[] srcFiles = ylib.getFiles(Path.Combine(album.Folder, "*.*"));
            string destPath = Path.Combine(destFolder, getArtist(album));
            destPath = Path.Combine(destPath, getAlbumTitle(album));
            foreach (string srcFile in srcFiles) {
                //  転送先ファイルパスの作成
                string ext = Path.GetExtension(srcFile).ToLower();
                if (ext.CompareTo("." + album.FormatExt.ToLower()) != 0 &&
                    ext.CompareTo(".csv") != 0 && ext.CompareTo(".pdf") != 0) continue;
                string destFile = Path.Combine(destPath, Path.GetFileName(srcFile));
                files.Add(new string[] { srcFile, destFile });
            }
            return files;
        }

        /// <summary>
        /// アルバムタイトル名の抽出
        /// </summary>
        /// <param name="album">アルバムデータ</param>
        /// <returns>タイトル名</returns>
        private string getAlbumTitle(AlbumData album)
        {
            if (0 < album.Album.Length) {
                if (0 < album.Album.Length)
                    return ylib.convInvalidFileNameChars(album.Album);
            }
            return "不明";
        }

        /// <summary>
        /// アーティスト名の抽出
        /// </summary>
        /// <param name="album">アルバムデータ</param>
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
    }
}
