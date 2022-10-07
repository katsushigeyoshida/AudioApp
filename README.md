# AudioApp
## 音楽ファイルの再生、録音、スペクトラム表示

### 機能

音楽ファイル(MP3,FLAC)やデバイスからの入出力など6つの機能を選択できる。
音楽ファイルの再生や解析、デバイスからの取り込みには ライブラリとしてNAudioとNAudioFlacを使用している。
また音楽ファイルの再生にはMediaPlayerも利用している。

起動画面  
<img src="Image/MainWindow.png" width="40%">

1.マイクテスト  
PC1のマイク入力から取り込んだデータの波形表示やファイル保存を行う。  
<img src="Image/MicRecord.png" width="50%"> 

2.オーディオプレイヤー  
音楽ファイルの再生と波形表示を行う。  
<img src="Image/AudioPlay.png" width="50%">  

3.スピーカ出力キャプチャ  
PCのスピーカ出力をキャプチャーし、波形表示やファイル保存を行う。  
<img src="Image/LoopbackCapture.png" width="50%">  

4.音楽ファイル管理  
音楽ファイルを演奏者、アルバム、曲でリスト表示して管理する。音楽ファイルのタグデータやイメージデータを表示する。  
<img src="Image/MusicExplorer.png" width="50%">  

5.スペクトラム表示  
音楽ファイルの波形、スペクトラム、周波数特性表示と再生を行う。  
<img src="Image/SpectrumAnalyzer.png" width="50%">  

6.ミュージシャン検索  
Wikipedia のミュージシャン一覧リストを読み込んでリスト表示し、個々のミュージシャンの基本情報を取得して表示する。  
<img src="Image/FindPlayer.png" width="50%">  

### ■実行環境
AudioApp.zipをダウンロードして適当なフォルダに展開し、フォルダ内の AudioApp.exe をダブルクリックして実行します。  

### ■開発環境  
開発ソフト : Microsoft Visual Studio 2022  
開発言語　 : C# 7.3 Windows アプリケーション  
フレームワーク　 :  .NET framework 4.7.2  
NuGetライブラリ　: NAudio(1.10.0),NAudio.Flac(1.0.5702.29018),NAudio.Wma(1.0.1),MathNet.Numerics(4.12.0),OxyPlot.Core(2.0.0),OxyPlot.Wpf(2.0.0)  
自作ライブラリ  : WpfLib,AudioLib