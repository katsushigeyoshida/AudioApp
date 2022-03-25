using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfLib;
using Path = System.IO.Path;

namespace AudioApp
{
    /// <summary>
    /// AlbumInfo.xaml の相互作用ロジック
    /// </summary>
    public partial class AlbumInfo : Window
    {
        private string mFolder;                                     //  アルバムファイル保存フォルダ
        private AlbumInfoData mAlbumInfoData;                       //  アルバム情報データ
        private YLib ylib = new YLib();

        public AlbumInfo()
        {
            InitializeComponent();

            mAlbumInfoData = new AlbumInfoData();   //  アルバム情報データの取得

            //  メディアタイプの設定
            foreach (string title in mAlbumInfoData.getGenreData())
                CbGenre.Items.Add(title);
            foreach (string title in mAlbumInfoData.getStyleData())
                CbStyle.Items.Add(title);
            foreach (string title in mAlbumInfoData.getMediaType())
                CbOriginalMedia.Items.Add(title);

            LbFolder.Content = "***";
            LbAlbum.Content = "***";
            LbArtist.Content = "***";
        }

        /// <summary>
        /// コンストラクタ
        /// アルバム情報データを更新する場合
        /// </summary>
        /// <param name="albumData"></param>
        public AlbumInfo(AlbumData albumData)
        {
            InitializeComponent();

            mFolder = albumData.Folder;
            mAlbumInfoData = new AlbumInfoData(albumData.Folder);   //  アルバム情報データの取得

            //  メディアタイプの設定
            foreach (string title in mAlbumInfoData.getGenreData())
                CbGenre.Items.Add(title);
            foreach (string title in mAlbumInfoData.getStyleData())
                CbStyle.Items.Add(title);
            foreach (string title in mAlbumInfoData.getMediaType())
                CbOriginalMedia.Items.Add(title);

            LbFolder.Content = albumData.Folder;
            LbAlbum.Content = albumData.Album;
            LbArtist.Content = albumData.Artist;

            CbUsrArtist.Items.Add(albumData.Artist);
            if (!CbUsrArtist.Items.Contains(albumData.AlbumArtist))
                CbUsrArtist.Items.Add(albumData.AlbumArtist);
            if (!CbUsrArtist.Items.Contains(albumData.UserArtist))
                CbUsrArtist.Items.Add(albumData.UserArtist);

            //  取得したデータを画面に設定
            loadDataSet();
            if (TbSourceDate.Text.Length == 0) {
                DateTime dt = getFirstFileTime();
                TbSourceDate.Text = dt.ToString("yyyy/MM/dd");
            }
        }

        /// <summary>
        /// [OK]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            saveData();
            Close();
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
        /// [フォルダ、アルバム、アーティスト]コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LabelMenuClick(object sender, RoutedEventArgs e)
        {
            string searchWord = "";
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("LbFolderCopyMenu") == 0) {
                Clipboard.SetText(LbFolder.Content.ToString());
            } else if (menuItem.Name.CompareTo("LbAlbumCopyMenu") == 0) {
                Clipboard.SetText(LbAlbum.Content.ToString());
            } else if (menuItem.Name.CompareTo("LbArtistCopyMenu") == 0) {
                Clipboard.SetText(LbArtist.Content.ToString());
            } else if (menuItem.Name.CompareTo("LbAlbumGoogleMenu") == 0) {
                searchWord = LbArtist.Content.ToString() + " " + LbAlbum.Content.ToString();
                ylib.WebSerach("googleJpn", searchWord);
            } else if (menuItem.Name.CompareTo("LbAlbumGoogleComMenu") == 0) {
                searchWord = LbArtist.Content.ToString() + " " + LbAlbum.Content.ToString();
                ylib.WebSerach("google", searchWord);
            } else if (menuItem.Name.CompareTo("LbAlbumBingMenu") == 0) {
                searchWord = LbArtist.Content.ToString() + " " + LbAlbum.Content.ToString();
                ylib.WebSerach("Bing", searchWord);
            } else if (menuItem.Name.CompareTo("LbAlbumDiscogsMenu") == 0) {
                searchWord = LbArtist.Content.ToString() + " " + LbAlbum.Content.ToString();
                ylib.WebSerach("Discogs", searchWord);
            } else if (menuItem.Name.CompareTo("LbAlbumWikiMenu") == 0) {
                searchWord = LbArtist.Content.ToString() + " " + LbAlbum.Content.ToString();
                ylib.WebSerach("Wikipedia", searchWord);
            } else if (menuItem.Name.CompareTo("LbArtistGoogleMenu") == 0) {
                searchWord = LbArtist.Content.ToString();
                ylib.WebSerach("googleJpn", searchWord);
            } else if (menuItem.Name.CompareTo("LbArtistGoogleComMenu") == 0) {
                searchWord = LbArtist.Content.ToString();
                ylib.WebSerach("google", searchWord);
            } else if (menuItem.Name.CompareTo("LbArtistBingMenu") == 0) {
                searchWord = LbArtist.Content.ToString();
                ylib.WebSerach("Bing", searchWord);
            } else if (menuItem.Name.CompareTo("LbArtistDiscogsMenu") == 0) {
                searchWord = LbArtist.Content.ToString();
                ylib.WebSerach("Discogs", searchWord);
            } else if (menuItem.Name.CompareTo("LbArtistWikiMenu") == 0) {
                searchWord = LbArtist.Content.ToString();
                ylib.WebSerach("Wikipedia", searchWord);
            }
        }

        /// <summary>
        /// [URL]コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void URLMenuClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("URLPasteMenu") == 0) {
                if (LbURL.Items.Count < 10)
                    LbURL.Items.Add(Clipboard.GetText());
            } else if (menuItem.Name.CompareTo("URLCopyMenu") == 0) {
                if (0 <= LbURL.SelectedIndex) {
                    Clipboard.SetText(LbURL.Items[LbURL.SelectedIndex].ToString());
                }
            } else if (menuItem.Name.CompareTo("URLRemoveMenu") == 0) {
                if (0 <= LbURL.SelectedIndex) {
                    LbURL.Items.Remove(LbURL.Items[LbURL.SelectedIndex]);
                }
            }
        }

        /// <summary>
        /// [添付ファイル]コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AtachFileMenuClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("AtachFilePasteMenu") == 0) {
                if (LbAttachFile.Items.Count < 10)
                    LbAttachFile.Items.Add(Clipboard.GetText());
            } else if (menuItem.Name.CompareTo("AtachFileCopyMenu") == 0) {
                if (0 <= LbAttachFile.SelectedIndex) {
                    Clipboard.SetText(LbAttachFile.Items[LbAttachFile.SelectedIndex].ToString());
                }
            } else if (menuItem.Name.CompareTo("AtachFileRemoveMenu") == 0) {
                if (0 <= LbAttachFile.SelectedIndex) {
                    LbAttachFile.Items.Remove(LbAttachFile.Items[LbAttachFile.SelectedIndex]);
                }
            } else if (menuItem.Name.CompareTo("AtachFileAddMenu") == 0) {
                if (!Directory.Exists(mFolder))
                    mFolder = "";
                string path = ylib.fileSelect(mFolder, "pdf");
                if (0 < path.Length) {
                    path = path.Replace(mFolder, "");
                    path = path.TrimStart('\\');
                    LbAttachFile.Items.Add(path);
                }
            }
        }

        /// <summary>
        /// [URL]ダブルクリックで開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbURL_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (0 <= LbURL.SelectedIndex) {
                System.Diagnostics.Process p =
                        System.Diagnostics.Process.Start(LbURL.Items[LbURL.SelectedIndex].ToString());
            }
        }

        /// <summary>
        /// [添付ファイル]ダブルクリックで開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbAttachFile_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (0 <= LbAttachFile.SelectedIndex) {
                string path = LbAttachFile.Items[LbAttachFile.SelectedIndex].ToString();
                if (path[1] != ':')
                    path = Path.Combine(mFolder, path);
                if (File.Exists(path)) {
                    System.Diagnostics.Process p =
                            System.Diagnostics.Process.Start(path);
                } else {
                    MessageBox.Show(path + "\nファイルがありません");
                }
            }
        }

        /// <summary>
        /// アルバム情報データをロードする
        /// </summary>
        private void loadDataSet()
        {
            CbUsrArtist.Text = mAlbumInfoData.getAlbumInfoData("UserArtist");
            TbPersonal.Text = ylib.strControlCodeRev(mAlbumInfoData.getAlbumInfoData("Personal"));
            CbGenre.Text = mAlbumInfoData.getAlbumInfoData("Genre");
            CbStyle.Text = mAlbumInfoData.getAlbumInfoData("Style");
            TbRecordDate.Text = mAlbumInfoData.getAlbumInfoData("RecordDate");
            CbOriginalMedia.Text = mAlbumInfoData.getAlbumInfoData("OriginalMedia");
            TbRecordLabel.Text = mAlbumInfoData.getAlbumInfoData("Label");
            TbRecordNo.Text = mAlbumInfoData.getAlbumInfoData("RecordNo");
            TbSource.Text = mAlbumInfoData.getAlbumInfoData("Source");
            TbSourceDate.Text = mAlbumInfoData.getAlbumInfoData("SourceDate");
            string[] urlData = mAlbumInfoData.getAlbumInfoDatas("RefURL");
            LbURL.Items.Clear();
            if (urlData != null) {
                for (int i = 1; i < urlData.Length; i++) {
                    if (0 < urlData[i].Length)
                        LbURL.Items.Add(urlData[i]);
                }
            }
            string[] attachData = mAlbumInfoData.getAlbumInfoDatas("AtachedFile");
            LbAttachFile.Items.Clear();
            if (attachData != null) {
                for (int i = 1; i < attachData.Length; i++) {
                    if (0 < attachData[i].Length)
                        LbAttachFile.Items.Add(attachData[i]);
                }
            }
            TbComment.Text = ylib.strControlCodeRev(mAlbumInfoData.getAlbumInfoData("Comment"));
        }

        /// <summary>
        /// アルバム情報データを保存する
        /// </summary>
        private void saveData()
        {
            mAlbumInfoData.setAlbumInfoData("UserArtist", CbUsrArtist.Text);
            mAlbumInfoData.setAlbumInfoData("Album", LbAlbum.Content.ToString());
            mAlbumInfoData.setAlbumInfoData("Artist", LbArtist.Content.ToString());
            mAlbumInfoData.setAlbumInfoData("Personal", ylib.strControlCodeCnv(TbPersonal.Text));
            mAlbumInfoData.setAlbumInfoData("Genre", CbGenre.Text);
            mAlbumInfoData.setAlbumInfoData("Style", CbStyle.Text);
            mAlbumInfoData.setAlbumInfoData("RecordDate", TbRecordDate.Text);
            mAlbumInfoData.setAlbumInfoData("OriginalMedia", CbOriginalMedia.Text);
            mAlbumInfoData.setAlbumInfoData("Label", TbRecordLabel.Text);
            mAlbumInfoData.setAlbumInfoData("RecordNo", TbRecordNo.Text);
            mAlbumInfoData.setAlbumInfoData("Source", TbSource.Text);
            mAlbumInfoData.setAlbumInfoData("SourceDate", TbSourceDate.Text);
            string[] datas = new string[mAlbumInfoData.getDataSize()];
            int j = 0;
            for (int k = 0; k < LbURL.Items.Count; k++) {
                if (0 < LbURL.Items[k].ToString().Length)
                    datas[j++] = Uri.UnescapeDataString(LbURL.Items[k].ToString());
            }
            mAlbumInfoData.setAlbumInfoData("RefURL", datas);
            datas = new string[mAlbumInfoData.getDataSize()];
            j = 0;
            for (int k = 0; k < LbAttachFile.Items.Count; k++) {
                if (0 < LbAttachFile.Items[k].ToString().Length)
                    datas[j++] = LbAttachFile.Items[k].ToString();
            }
            mAlbumInfoData.setAlbumInfoData("AtachedFile", datas);
            mAlbumInfoData.setAlbumInfoData("Comment", ylib.strControlCodeCnv(TbComment.Text));

            if (LbFolder.Content.ToString().CompareTo("***") != 0)
                mAlbumInfoData.saveData();
        }

        /// <summary>
        /// アルバム情報データの取得
        /// </summary>
        /// <returns></returns>
        public AlbumInfoData getAlbumInfoData()
        {
            return mAlbumInfoData;
        }

        /// <summary>
        /// フォルダにあるMP3ファイルの日付を取得
        /// MP3ファイルがない時は現在の日付を取得
        /// </summary>
        /// <returns>日時</returns>
        private DateTime getFirstFileTime()
        {
            if (!Directory.Exists(mFolder))
                return DateTime.Now;
            string[] files = Directory.GetFiles(mFolder, "*.mp3", SearchOption.TopDirectoryOnly);
            if (0 < files.Length) {
                FileInfo fi = new System.IO.FileInfo(files[0]);
                return fi.CreationTime;
            } else {
                return DateTime.Now;
            }
        }
    }
}
