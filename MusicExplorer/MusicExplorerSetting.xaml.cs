using System.Collections.Generic;
using System.IO;
using System.Windows;
using WpfLib;

namespace AudioApp
{
    /// <summary>
    /// MusicExplorerSetting.xaml の相互作用ロジック
    /// 
    /// ダブルクリック時の起動プレイヤーの設定
    /// </summary>
    public partial class MusicExplorerSetting : Window
    {
        public MusicExplorer.PLAYERTYPE mOuterPlayer = MusicExplorer.PLAYERTYPE.INNERPLAYER;
        public bool mAlbumFileUse = false;          //  アルバムデータをファイル保存
        public int mDispArtistType = 0;             //  アーティスト
        public string mMusicFileCategory;           //  音楽ファイルリスト名
        public bool mAlbumUnDisp = false;           //  重複アルバムの非表示

        private string mAppFolder;
        private string mMusicFileCategoryPath = "";
        private YLib ylib = new YLib();

        public MusicExplorerSetting()
        {
            InitializeComponent();

            mAppFolder = ylib.getAppFolderPath();
            mMusicFileCategoryPath = Path.Combine(mAppFolder, "MusicFileCategory.csv");
            loadMusiCategoryList(mMusicFileCategoryPath);
            Properties.Settings.Default.Reload();
            if (CbMusicListFile.Items.Contains(Properties.Settings.Default.MusicExploreFIleCategory)) {
                CbMusicListFile.SelectedIndex = CbMusicListFile.Items.IndexOf(Properties.Settings.Default.MusicExploreFIleCategory);
            } else {
                CbMusicListFile.Items.Add(Properties.Settings.Default.MusicExploreFIleCategory);
                CbMusicListFile.Text = Properties.Settings.Default.MusicExploreFIleCategory;
            }
            mMusicFileCategory = Properties.Settings.Default.MusicExploreFIleCategory;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RbOuterPlayer.IsChecked = mOuterPlayer == MusicExplorer.PLAYERTYPE.OUTTERPLAYER ? true : false;
            RbAudioPlayer.IsChecked = mOuterPlayer == MusicExplorer.PLAYERTYPE.AUDIOPLAER ? true : false;
            RbNoPlayer.IsChecked = mOuterPlayer == MusicExplorer.PLAYERTYPE.INNERPLAYER ? true : false;
            CbAlbumFileUse.IsChecked = mAlbumFileUse;
            RbDispArtist.IsChecked = mDispArtistType == 0 ? true : false;
            RbDispAlbumArtist.IsChecked = mDispArtistType == 1 ? true : false;
            RbDispUserArtist.IsChecked = mDispArtistType == 2 ? true : false;
            CbAlbumUnDisp.IsChecked = mAlbumUnDisp;
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            saveMusicCategoryList(mMusicFileCategoryPath);
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// [OK]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtOK_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.MusicExploreFIleCategory = CbMusicListFile.Text;
            mMusicFileCategory = CbMusicListFile.Text;
            mOuterPlayer = RbOuterPlayer.IsChecked == true ? MusicExplorer.PLAYERTYPE.OUTTERPLAYER :
                RbAudioPlayer.IsChecked == true ? MusicExplorer.PLAYERTYPE.AUDIOPLAER : MusicExplorer.PLAYERTYPE.INNERPLAYER;
            mAlbumFileUse = CbAlbumFileUse.IsChecked == true ? true : false;
            mDispArtistType = RbDispArtist.IsChecked == true ? 0 :
                RbDispAlbumArtist.IsChecked == true ? 1 : RbDispUserArtist.IsChecked == true ? 2 : 0;
            mAlbumUnDisp = CbAlbumUnDisp.IsChecked == true ? true : false;

            DialogResult = true;
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
        /// 検索フォルダリストの読み込み
        /// </summary>
        /// <param name="listPath">ファイル名パス</param>
        private void loadMusiCategoryList(string listPath)
        {
            List<string> list = ylib.loadListData(listPath);
            if (list == null)
                return;
            foreach (var path in list)
                CbMusicListFile.Items.Add(path);
        }

        /// <summary>
        /// 検索フォルダリストの保存
        /// 保存数を30までとする
        /// </summary>
        /// <param name="listPath">ファイル名パス</param>
        private void saveMusicCategoryList(string listPath)
        {
            List<string> pathList = new List<string>();
            int n = 0;
            foreach (var path in CbMusicListFile.Items) {
                pathList.Add(path.ToString());
                if (30 < n++)
                    break;
            }
            ylib.saveListData(listPath, pathList);
        }
    }
}
