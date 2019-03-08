using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;
using UnityInjector.Attributes;
using PluginExt;
using System.Collections;
using System.Data;
using System.Text;
using static COM3D2.Scriplay.Plugin.ScriplayPlugin;
using UnityEngine.UI;



namespace COM3D2.Scriplay.Plugin
{

    [PluginFilter("COM3D2x64"), PluginFilter("COM3D2x86"), PluginFilter("COM3D2VRx64"),
    PluginFilter("COM3D2OHx64"), PluginFilter("COM3D2OHx86"), PluginFilter("COM3D2OHVRx64"),
    PluginName("Scriplay"), PluginVersion("0.1.0.0")]


    public class ScriplayPlugin : ExPluginBase
    {
        //　設定クラス（Iniファイルで読み書きしたい変数はここに格納する）
        public class ScriplayConfig
        {
            internal bool debugMode = true;

            internal readonly float faceAnimeFadeTime = 1f;    //フェイスアニメフェード時間　1sec
            internal readonly string csvPath = @"Sybaris\UnityInjector\Config\Scriplay\csv\";
            internal readonly string scriptsPath = @"Sybaris\UnityInjector\Config\Scriplay\scripts\";
            internal string onceVoicePrefix = "oncevoice_";
            internal string loopVoicePrefix = "loopvoice_";
            internal string motionListPrefix = "motion_";
            internal string faceListPrefix = "face_";
            internal string PluginName = "Scriplay";
            internal string debugPrintColor = "red";
            internal bool enModMotionLoad = false;  //true;

            internal float sio_baseTime = 1f;           //潮　ベース時間
            internal float nyo_baseTime = 3f;           //尿　ベース時間


            internal int studioModeSceneLevel = 26;     //スタジオモードのシーンレベル

        }

        public static ScriplayConfig cfg = null;
        public static List<string> zeccyou_fn_list = new List<string>();  //絶頂モーションファイル名のリスト、絶頂モーション検索用
        public static List<string> motionNameAllList = new List<string>();        //ゲームデータ内の全モーションデータ


        /// <summary>
        /// 全モーション一覧
        /// </summary>
        public static HashSet<string> motionCategorySet = new HashSet<string>();

        //メイド情報
        private Transform[] maidHead = new Transform[20];

        /// <summary>
        /// メイドリスト
        /// </summary>
        public static List<IMaid> maidList = new List<IMaid>();
        public static List<IMan> manList = new List<IMan>();
        private bool gameCfg_isChuBLipEnabled = false;
        private bool gameCfg_isVREnabled = false;
        private bool gameCfg_isPluginEnabledScene = false;

        /// <summary>
        /// モーションのベース名　算出のための定義リスト
        /// sufix　を除去していくことでベース名を求める
        /// </summary>
        private static Dictionary<string, string> motionBaseRegexDefDic = new Dictionary<string, string>()
            {
                {@"_[123]_.*",          "_" },
                {@"_[123]\w\d\d.*",     "_" },
                {@"\.anm",              "" },
                {@"_asiname_.*",          "_"},
                //{@"_aibu_.*",          "_"},
                {@"_cli[\d]?_.*",       "_"},
                {@"_daki_.*",            "_"},
                {@"_fera_.*",             "_"},
                {@"_gr_.*",            "_"},
                {@"_housi_.*",            "_"},
                {@"_hibu_.*",            "_"},
                {@"_hibuhiraki_.*",            "_"},
                {@"_ir_.*",               "_"},
                {@"_kakae_.*",             "_"},
                {@"_kuti_.*",             "_"},
                {@"_kiss_.*",             "_"},
                {@"_momi_.*",             "_" },
                {@"_onani_.*",            "_"},
                {@"_oku_.*",            "_"},
                {@"_peace_.*",            "_"},
                {@"_ran4p_.*",            "_"},
                {@"_ryoutenaburi_.*",            "_"},
                {@"_siri_.*",           "_" },
                {@"_sissin_.*",          "_"},
                {@"_shasei_.*",           "_" },
                {@"_shaseigo_.*",         "_" },
                {@"_sixnine_.*",          "_"},
                {@"_surituke_.*",         "_"},
                {@"_siriname_.*",         "_"},
                {@"_taiki_.*",            "_" },
                {@"_tikubi_.*",          "_"},
                {@"_tekoki_.*",          "_"},
                {@"_tikubiname_.*",       "_"},
                {@"_tati_.*",       "_"},
                {@"_ubi\d?_.*",       "_"},
                {@"_vibe_.*",        "_" },
                {@"_zeccyougo_.*",        "_" },
                {@"_zeccyou_.*",          "_" },
                {@"_zikkyou_.*",       "_"},
            };
        //正規表現をコンパイルするのにコストがかかるため、あらかじめコンパイルしたものを使用する。https://docs.unity3d.com/ja/current/Manual/BestPracticeUnderstandingPerformanceInUnity5.html
        private static Dictionary<Regex, string> motionBaseRegexDic = new Dictionary<Regex, string>();



        /// <summary>
        /// メイドリスト・男リスト　初期化、メイドステータスも初期化
        /// </summary>
        private void initMaidList()
        {
            Util.info("メイド一覧読み込み開始");
            maidList.Clear();
            manList.Clear();
            CharacterMgr cm = GameMain.Instance.CharacterMgr;
            for (int i = 0; i < cm.GetMaidCount(); i++)
            {
                Maid m = cm.GetMaid(i);
                if (!isMaidAvailable(m)) continue;

                maidList.Add(new IMaid(i, m));
                Util.info(string.Format("メイド「{0}」を検出しました", m.status.fullNameJpStyle));
            }
            //男は最大6人、cm.GetManCount()は機能してない？ぽいので決め打ちでループ回す。
            for (int i = 0; i < 6; i++)
            {
                Maid m = cm.GetMan(i);  //無効な男Noならnullが返ってくる、nullチェックする
                if (!isMaidAvailable(m)) continue;

                manList.Add(new IMan(m));
                Util.info(string.Format("ご主人様「{0}」を検出しました", m.status.fullNameJpStyle));
            }
            GameMain.Instance.SoundMgr.StopSe();
        }
        private bool isMaidAvailable(Maid m)
        {
            return m != null && m.Visible && m.AudioMan != null;
        }


        public void Awake()
        {
            GameObject.DontDestroyOnLoad(this);

            string gameDataPath = UnityEngine.Application.dataPath;

            loadSetting();

            // ChuBLip判別
            gameCfg_isChuBLipEnabled = gameDataPath.Contains("COM3D2OHx64") || gameDataPath.Contains("COM3D2OHx86") || gameDataPath.Contains("COM3D2OHVRx64");
            // VR判別
            gameCfg_isVREnabled = gameDataPath.Contains("COM3D2OHVRx64") || gameDataPath.Contains("COM3D2VRx64") || Environment.CommandLine.ToLower().Contains("/vr");

            //MotionBase変換用正規表現　事前コンパイル
            foreach (KeyValuePair<string, string> kvp in motionBaseRegexDefDic)
            {
                Regex regex1 = new Regex(kvp.Key);
                motionBaseRegexDic.Add(regex1, kvp.Value);
            }

            //UI表示　初期化
            initGuiStyle();
        }

        /// <summary>
        /// ファイル読み込み　コンフィグ、モーション、ボイス
        /// </summary>
        private void loadSetting()
        {
            cfg = ReadConfig<ScriplayConfig>("ScriplayConfig");
            load_ConfigCsv();
            load_motionGameData(cfg.enModMotionLoad);
        }

        private void load_ConfigCsv()
        {
            Util.info("CSVファイル読み込み");
            OnceVoiceTable.init();
            LoopVoiceTable.init();
            MotionTable.init();
            FaceTable.init();
            motionCategorySet.Clear();
            List<string> filelist = Util.getFileFullpathList(cfg.csvPath, "csv");
            string filenameList = "\r\n";
            foreach (string fullpath in filelist)
            {
                string basename = Path.GetFileNameWithoutExtension(fullpath);
                filenameList += basename + "\r\\n";
                Util.info(string.Format("CSV:{0}", basename));
                if (basename.Contains(cfg.motionListPrefix))
                {
                    MotionTable.parse(Util.ReadCsvFile(fullpath, false), basename);
                }
                else if (basename.Contains(cfg.onceVoicePrefix))
                {
                    OnceVoiceTable.parse(Util.ReadCsvFile(fullpath, false), basename);
                }
                else if (basename.Contains(cfg.loopVoicePrefix))
                {
                    LoopVoiceTable.parse(Util.ReadCsvFile(fullpath, false), basename);
                }
                else if (basename.Contains(cfg.faceListPrefix))
                {
                    FaceTable.parse(Util.ReadCsvFile(fullpath, false), basename);
                }
            }
            if (filenameList == "\r\n") filenameList = "（CSVファイルが見つかりませんでした）";
            Util.info(filenameList);

            foreach (string s in MotionTable.getCategoryList())
            {
                motionCategorySet.Add(s);
            }
        }

        /// <summary>
        /// Unityが把握しているモーションデータを取得して一覧作成
        /// </summary>
        /// <param name="allLoad">バニラのモーションデータのみ読み込み（Modのモーションは含まず）</param>
        private void load_motionGameData(bool allLoad = true)
        {
            // COM3D2のモーションファイル全列挙
            Util.info("モーションファイル読み込み開始");
            motionNameAllList.Clear();

            if (!allLoad)
            {
                ///*
                // FileSystemArchiveのGetFileは全てのファイルを検索対象とするため時間がかかる
                // 全体を対象とするよりは、「motion」「motion2」配下だけを対象としたほうが早いため、下記のように処理
                //参考：https://github.com/Neerhom/COM3D2.ModLoader/blob/master/COM3D2.ModMenuAccel.patcher/COM3D2.ModMenuAccel.Hook/COM3D2.ModMenuAccel.Hook/FastStart.cs
                //*/
                string[] motionDirList = { "motion", "motion2", "motion_3d21reserve_2", "motion_cos021_2", "motion_denkigai2017w_2" };
                ArrayList Files = new ArrayList();
                foreach (string s in motionDirList)
                {
                    Files.AddRange(GameUty.FileSystem.GetList(s, AFileSystemBase.ListType.AllFile));
                }
                foreach (string file in Files)
                {
                    if (Path.GetExtension(file) == ".anm") motionNameAllList.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            else
            {
                foreach (string file in GameUty.FileSystem.GetFileListAtExtension(".anm"))      //数秒かかる
                {
                    motionNameAllList.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            //ファイル名でソート
            motionNameAllList.Sort();
            string added = "\r\n";
            foreach (string s in motionNameAllList)
            {
                added += s + "\r\n";
            }
            //Util.debug(added);
            Util.info("モーションファイル読み込み終了");

            //絶頂モーションファイル名リスト　作成
            foreach (string filename in motionNameAllList)
            {
                if (filename.Contains("zeccyou_f_once")) zeccyou_fn_list.Add(filename);
            }
            zeccyou_fn_list.Sort();
        }

        public void Start()
        {
        }

        public void OnDestroy()
        {
        }



        void OnLevelWasLoaded(int level)
        {
            // スタジオモードならプラグイン有効
            if (level == cfg.studioModeSceneLevel)
            //if (isYotogiScene(level))
            {
                gameCfg_isPluginEnabledScene = true;
            }
            else
            {
                gameCfg_isPluginEnabledScene = false;
                return;
            }

            //各変数の初期化 TODO

            initMaidList();


        }

        private bool isYotogiScene(int sceneLevel)
        {
            return GameMain.Instance.CharacterMgr.GetMaidCount() != 0;
            //int yotogiManagerCount = FindObjectsOfType<YotogiManager>().Length;
            //return yotogiManagerCount > 0;
        }


        private static readonly float UIwidth = 300;
        private static readonly float UIheight = 400;
        private static readonly float UIposX_rightMargin = 10;
        private static readonly float UIposY_bottomMargin = 120;
        Rect node_main = new Rect(UnityEngine.Screen.width - UIposX_rightMargin - UIwidth, UnityEngine.Screen.height - UIposY_bottomMargin - UIheight, UIwidth, UIheight);
        Rect node_scripts = new Rect(UnityEngine.Screen.width - UIposX_rightMargin - UIwidth, UnityEngine.Screen.height - (UIheight + UIposY_bottomMargin) - UIheight, UIwidth, UIheight);

        private static readonly float UIwidth_showArea = 800;
        private static readonly float UIheight_showArea = 150;
        private static readonly float UIposX_rightMargin_showArea = 10;
        private static readonly float UIposY_bottomMargin_showArea = 10;
        Rect node_showArea = new Rect(UnityEngine.Screen.width - UIposX_rightMargin_showArea - UIwidth_showArea,
            UnityEngine.Screen.height - UIposY_bottomMargin_showArea - UIheight_showArea, UIwidth_showArea, UIheight_showArea);

        private Vector2 scrollPosition = new Vector2();
        private Queue<string> debug_strQueue = new Queue<string>();
        private Dictionary<string, string> debug_ovtQueryMap = new Dictionary<string, string>()
        {
            {"Personal","" },
            {"Category","" },

        };
        private string debug_toast = "";
        private Queue<string> debug_toastQueue = new Queue<string>();
        private string debug_playVoice = "";
        private Queue<string> debug_playVoiceQueue = new Queue<string>();
        private string debug_playMotion = "";
        private Queue<string> debug_playMotionQueue = new Queue<string>();
        private string debug_face = "";
        private Queue<string> debug_faceQueue = new Queue<string>();
        private string debug_script = "";
        private Queue<string> debug_scriptQueue = new Queue<string>();

        void OnGUI()
        {
            if (!gameCfg_isPluginEnabledScene) return;

            GUIStyle guiTitleStyle = new GUIStyle("box");
            guiTitleStyle.fontSize = 11;
            guiTitleStyle.alignment = TextAnchor.UpperLeft;

            if (scriplayContext.scriptFinished)
            {
                node_main = GUI.Window(21, node_main, WindowCallback_mainUI, cfg.PluginName + " Main UI", guiTitleStyle);
            }
            //if (scriplayContext.selection_selectionList.Count != 0)
            if (!scriplayContext.scriptFinished)
            {
                node_showArea = GUI.Window(22, node_showArea, WindowCallback_showArea, "", guiTitleStyle);
            }
            if (en_showScripts && scriplayContext.scriptFinished) node_scripts = GUI.Window(23, node_scripts, WindowCallback_scriptsView, cfg.PluginName + "スクリプト一覧", guiTitleStyle);
        }

        private ScriplayContext scriplayContext = ScriplayContext.None;
        private bool en_showScripts = false;
        private Vector2 scriptsList_scrollPosition = new Vector2();
        private List<string> scripts_fullpathList = new List<string>();
        private static MonoBehaviour instance;
        private Vector2 showArea_scrollPosition = new Vector2();

        ScriplayPlugin()
        {
            instance = this;
        }

        void WindowCallback_scriptsView(int id)
        {
            GUILayout.Space(20);
            scriptsList_scrollPosition = GUILayout.BeginScrollView(scriptsList_scrollPosition);

            foreach (string fullpath in scripts_fullpathList)
            {
                string basename = Path.GetFileNameWithoutExtension(fullpath);
                if (GUILayout.Button(basename, gsButton))
                {
                    scriplayContext = ScriplayContext.readScriptFile(fullpath);
                }
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        void WindowCallback_showArea(int id)
        {
            GUIStyle gsButton_stopScript = new GUIStyle("button");
            gsButton_stopScript.fontSize = 10;
            gsButton_stopScript.alignment = TextAnchor.MiddleCenter;
            gsButton_stopScript.fixedWidth = 100;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Stop Script", gsButton_stopScript))
            {
                scriplayContext.scriptFinished = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            showArea_scrollPosition = GUILayout.BeginScrollView(showArea_scrollPosition);
            if (!scriplayContext.showText.Equals(""))
            {
                GUILayout.Label(scriplayContext.showText, gsLabel);
            }
            foreach (ScriplayContext.Selection s in scriplayContext.selection_selectionList)
            {
                if (GUILayout.Button(s.viewStr, gsButton))
                {
                    scriplayContext.selection_selectedItem = s;
                }
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        GUIStyle gsLabelTitle = new GUIStyle("label");
        GUIStyle gsLabel = new GUIStyle("label");
        GUIStyle gsLabelSmall = new GUIStyle("label");
        GUIStyle gsButton = new GUIStyle("button");
        GUIStyle gsButtonSmall = new GUIStyle("button");

        private void initGuiStyle()
        {
            gsLabelTitle.fontSize = 14;
            gsLabelTitle.alignment = TextAnchor.MiddleCenter;

            gsLabel.fontSize = 12;
            gsLabel.alignment = TextAnchor.MiddleLeft;

            gsLabelSmall.fontSize = 10;
            gsLabelSmall.alignment = TextAnchor.MiddleLeft;

            gsButton.fontSize = 12;
            gsButton.alignment = TextAnchor.MiddleCenter;

            gsButtonSmall.fontSize = 10;
            gsButtonSmall.alignment = TextAnchor.MiddleCenter;
        }
        void WindowCallback_mainUI(int id)
        {
            //メインアイコン------------------------------------------------

            GUILayout.Space(20);    //UIタイトルとかぶらないように
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload Maid", gsButtonSmall))
            {
                initMaidList();
            }
            if (GUILayout.Button("Reload CSV", gsButtonSmall))
            {
                load_ConfigCsv();
            }
            if (GUILayout.Button("Show Scripts", gsButtonSmall))
            {
                en_showScripts = !en_showScripts;
                if (en_showScripts)
                {
                    scripts_fullpathList = Util.getFileFullpathList(cfg.scriptsPath, suffix: "md");
                    Util.info(string.Format("以下のスクリプトが見つかりました"));
                    foreach (string s in scripts_fullpathList)
                    {
                        Util.info(s);
                    }
                }
            }
            GUILayout.EndHorizontal();
            //TODO Configファイル新規作成できてない
            /*
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Reload Config.ini", gsButtonSmall))
                        {
                            cfg = ReadConfig<ScriplayConfig>("ScriplayConfig");
                        }
                        if (GUILayout.Button("Save Config.ini", gsButtonSmall))
                        {
                            SaveConfig<ScriplayConfig>(cfg,"ScriplayConfig");
                        }
                        GUILayout.EndHorizontal();
            */
            GUILayout.Space(10);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            //メインコンテンツ------------------------------------------------


            GUILayout.Label("スクリプト", gsLabel);
            GUILayout.Label(string.Format("スクリプト名：{0}", scriplayContext.scriptName), gsLabelSmall);
            GUILayout.Label(string.Format("状態：{0}", scriplayContext.scriptFinished ? "スクリプト終了" : "スクリプト実行中"), gsLabelSmall);
            GUILayout.Label(string.Format("現在実行行：{0}", scriplayContext.currentExecuteLine), gsLabelSmall);
            GUILayout.Space(10);




            GUILayout.Label("ボイス再生", gsLabelSmall);
            debug_playVoice = GUILayout.TextField(debug_playVoice);
            if (Event.current.keyCode == KeyCode.Return && debug_playVoice != "")
            {
                maidList[0].maid.AudioMan.LoadPlay(debug_playVoice, 0f, false, false);
                debug_playVoiceQueue.Enqueue(debug_playVoice);
                if (debug_playVoiceQueue.Count > 3) debug_playVoiceQueue.Dequeue();
                debug_playVoice = "";
            }
            foreach (string s in debug_playVoiceQueue)
            {
                if (GUILayout.Button(s, gsButtonSmall))
                {
                    maidList[0].maid.AudioMan.LoadPlay(s, 0f, false, false);
                }
            }

            GUILayout.Label("モーション再生", gsLabelSmall);
            debug_playMotion = GUILayout.TextField(debug_playMotion);
            if (Event.current.keyCode == KeyCode.Return && debug_playMotion != "")
            {
                Util.animate(maidList[0].maid, debug_playMotion, true);
                debug_playMotionQueue.Enqueue(debug_playMotion);
                if (debug_playMotionQueue.Count > 3) debug_playMotionQueue.Dequeue();
                debug_playMotion = "";
            }
            foreach (string s in debug_playMotionQueue)
            {
                if (GUILayout.Button(s, gsButtonSmall))
                {
                    Util.animate(maidList[0].maid, s, true);
                }
            }

            GUILayout.Label("表情再生", gsLabelSmall);
            debug_face = GUILayout.TextField(debug_face);
            if (Event.current.keyCode == KeyCode.Return && debug_face != "")
            {
                maidList[0].change_faceAnime(debug_face);
                debug_faceQueue.Enqueue(debug_face);
                if (debug_faceQueue.Count > 3) debug_faceQueue.Dequeue();
                debug_face = "";
            }
            foreach (string s in debug_faceQueue)
            {
                if (GUILayout.Button(s, gsButtonSmall))
                {
                    maidList[0].change_faceAnime(s);
                }
            }

            GUILayout.Label("トースト再生", gsLabelSmall);
            debug_toast = GUILayout.TextField(debug_toast);
            if (Event.current.keyCode == KeyCode.Return && debug_toast != "")
            {
                toast(debug_toast);
                debug_toastQueue.Enqueue(debug_toast);
                if (debug_toastQueue.Count > 3) debug_toastQueue.Dequeue();
                debug_toast = "";
            }
            foreach (string s in debug_toastQueue)
            {
                if (GUILayout.Button(s, gsButtonSmall))
                {
                    toast(s);
                }
            }

            GUILayout.Label("スクリプト実行", gsLabelSmall);
            if (!scriplayContext.scriptFinished)
            {
                GUILayout.Label("　（スクリプト実行中）", gsLabelSmall);
            }
            else
            {
                debug_script = GUILayout.TextField(debug_script);
                if (Event.current.keyCode == KeyCode.Return && debug_script != "")
                {
                    this.scriplayContext = ScriplayContext.readScriptFile("スクリプト実行テスト", debug_script.Split(new string[] { "\r\n" }, StringSplitOptions.None));
                    debug_scriptQueue.Enqueue(debug_script);
                    if (debug_scriptQueue.Count > 3) debug_scriptQueue.Dequeue();
                    debug_script = "";
                }
                foreach (string s in debug_scriptQueue)
                {
                    if (GUILayout.Button(s, gsButtonSmall))
                    {
                        this.scriplayContext = ScriplayContext.readScriptFile("スクリプト実行テスト", s.Split(new string[] { "\r\n" }, StringSplitOptions.None));
                    }
                }
            }


            GUILayout.Label("各Table確認", gsLabelSmall);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Personal", gsLabelSmall);
            GUILayout.Label((maidList.Count != 0) ? maidList[0].sPersonal : "メイドがロードされていません", gsLabelSmall);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Category", gsLabelSmall);
            debug_ovtQueryMap["Category"] = GUILayout.TextField(debug_ovtQueryMap["Category"]);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OnceVoice", gsButtonSmall))
            {
                debug_ovtQueryMap["Personal"] = maidList[0].sPersonal;
                StringBuilder str = new StringBuilder();
                foreach (string s in OnceVoiceTable.queryTable(debug_ovtQueryMap["Personal"], debug_ovtQueryMap["Category"]))
                {
                    str.Append(s + ",");
                }
                Util.info(string.Format("OnceVoiceTable　クエリ結果 {0},{1} \r\n {2}", debug_ovtQueryMap["Personal"], debug_ovtQueryMap["Category"], str.ToString()));
            }
            if (GUILayout.Button("LoopVoice", gsButtonSmall))
            {
                debug_ovtQueryMap["Personal"] = maidList[0].sPersonal;
                StringBuilder str = new StringBuilder();
                foreach (string s in LoopVoiceTable.queryTable(debug_ovtQueryMap["Personal"], debug_ovtQueryMap["Category"]))
                {
                    str.Append(s + ",");
                }
                Util.info(string.Format("LoopVoiceTable　クエリ結果 {0},{1} \r\n {2}", debug_ovtQueryMap["Personal"], debug_ovtQueryMap["Category"], str.ToString()));
            }
            if (GUILayout.Button("Motion", gsButtonSmall))
            {
                debug_ovtQueryMap["Personal"] = maidList[0].sPersonal;
                StringBuilder str = new StringBuilder();
                foreach (MotionInfo mi in MotionTable.queryTable_motionNameBase(debug_ovtQueryMap["Category"]))
                {
                    str.Append(mi.motionName + ",");
                }
                Util.info(string.Format("MotionTable　クエリ結果 {0}  \r\n {1}", debug_ovtQueryMap["Category"], str.ToString()));
            }
            if (GUILayout.Button("Face", gsButtonSmall))
            {
                debug_ovtQueryMap["Personal"] = maidList[0].sPersonal;
                StringBuilder str = new StringBuilder();
                foreach (string mi in FaceTable.queryTable(debug_ovtQueryMap["Category"]))
                {
                    str.Append(mi + ",");
                }
                Util.info(string.Format("FaceTable　クエリ結果 {0}  \r\n {1}", debug_ovtQueryMap["Category"], str.ToString()));
            }
            GUILayout.EndHorizontal();

            if (maidList.Count != 0)
            {
                IMaid maid = maidList[0];
                GUILayout.Label("メインメイド状態", gsLabelSmall);
                GUILayout.BeginHorizontal();
                var maidInfoTable = new Dictionary<string, string>()
                    {
                        {"性格",               maid.sPersonal },
                        {"再生中ボイス",       maid.getPlayingVoice() },
                        {"再生中モーション",    maid.getCurrentMotionName() },
                    };

                if (GUILayout.Button("潮", gsButtonSmall))
                {
                    maid.change_sio();
                }
                if (GUILayout.Button("尿", gsButtonSmall))
                {
                    maid.change_nyo();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

            }

            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        /// <summary>
        /// Unity MonoBehaviour
        /// 毎フレーム呼ばれる処理
        /// </summary>
        public void Update()
        {
            if (!gameCfg_isPluginEnabledScene) return;
            //int gameMaidCount = GameMain.Instance.CharacterMgr.GetMaidCount();    //内部的には要素数１８固定の配列のため判定に使えない
            //if (GameMain.Instance.CharacterMgr.GetMaid(0)!=null  && maidList.Count != gameMaidCount) initMaidList();
            if (GameMain.Instance.CharacterMgr.GetMaid(maidList.Count) != null    //メイド数1増えた場合
                || (maidList.Count > 0 && GameMain.Instance.CharacterMgr.GetMaid(maidList.Count - 1) == null))     //メイド数1減った場合
                initMaidList();

            //ショートカットキー

            //スクリプトの実行
            if (!scriplayContext.scriptFinished)
            {
                scriplayContext.Update();
            }

            //各メイド処理
            foreach (IMaid maid in maidList)
            {
                Util.sw_start();

                //再生処理　一括変更
                maid.update_playing();
                Util.sw_showTime("update_playing");

                maid.update_eyeToCam();
                maid.update_headToCam();

                Util.sw_showTime("update_eyeHeadCamera");
                Util.sw_stop();

            }




        }


        /// <summary>
        /// Updateの後に呼ばれる処理
        /// </summary>
        public void LateUpdate()
        {

        }

        /// <summary>
        /// SE変更処理
        /// </summary>
        public static void change_SE(string seFileName)
        {
            GameMain.Instance.SoundMgr.StopSe();
            if (seFileName != "") GameMain.Instance.SoundMgr.PlaySe(seFileName, true);
        }


        public void change_SE_vibeLow()
        {
            change_SE("se020.ogg");
        }
        public void change_SE_vibeHigh()
        {
            change_SE("se019.ogg");
        }
        public void change_SE_stop()
        {
            change_SE("");
        }
        public void change_SE_insertLow()
        {
            change_SE("se029.ogg");
        }
        public void change_SE_insertHigh()
        {
            change_SE("se028.ogg");
        }
        public void change_SE_slapLow()
        {
            change_SE("se012.ogg");
        }
        public void change_SE_slapHigh()
        {
            change_SE("se013.ogg");
        }



        public class IMaid
        {
            public readonly Maid maid;
            public string sPersonal;      //性格名 ex.Muku


            public bool loopVoiceBackuped = false; //OnceVoice再生で遮られたLoopVoiceを復元する必要あるか？

            public string currentLoopVoice = "";
            private string currentFaceAnime = "";

            public IMaid(int maidNo, Maid maid)
            {
                this.maid = maid;
                this.sPersonal = maid.status.personal.uniqueName;

                this.maidNo = maidNo;
                this.maid.EyeToCamera((Maid.EyeMoveType)5, 0.8f); //顔と目の追従を有効にする fadeTime=0.8sec
                                                                  //        public enum EyeMoveType
                                                                  //{
                                                                  //    無し,
                                                                  //    無視する,
                                                                  //    顔を向ける,
                                                                  //    顔だけ動かす,
                                                                  //    顔をそらす,
                                                                  //    目と顔を向ける,      ←　これ
                                                                  //    目だけ向ける,
                                                                  //    目だけそらす
                                                                  //}


            }

            public bool isPlayingVoice()
            {
                return maid.AudioMan.audiosource.isPlaying;
            }
            public bool isPlayingMotion()
            {
                //Unity Animation.IsPlaying
                //https://docs.unity3d.com/ScriptReference/Animation.IsPlaying.html
                return maid.body0.m_Animation.isPlaying;
            }

            /// <summary>
            /// 再生中のボイスを返す
            /// </summary>
            /// <param name="maid"></param>
            /// <returns>再生ナシなら"", 再生中なら*.ogg</returns>
            public string getPlayingVoice()
            {
                if (!maid.AudioMan.isPlay()) return "";
                return maid.AudioMan.FileName;
            }

            /// <summary>
            /// メイドの位置を相対座標で指定した分だけ移動
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="z"></param>
            public Vector3 change_positionRelative(float x = 0, float y = 0, float z = 0)
            {
                Vector3 v = maid.transform.position;
                maid.transform.position = new Vector3(v.x + x, v.y + y, v.z + z);
                return maid.transform.position;
            }
            /// <summary>
            /// メイドの位置を絶対座標で指定
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="z"></param>
            public Vector3 change_positionAbsolute(float x = 0, float y = 0, float z = 0, bool keepX = false, bool keepY = false, bool keepZ = false)
            {
                Vector3 v = maid.transform.eulerAngles;
                maid.transform.position = new Vector3(keepX ? v.x : x, keepY ? v.y : y, keepZ ? v.z : z);
                return maid.transform.position;
            }
            /// <summary>
            /// メイドの現在位置を絶対座標で返す
            /// </summary>
            /// <returns></returns>
            public Vector3 getPosition()
            {
                return maid.transform.position;
            }

            /// <summary>
            /// メイドの向きを絶対座標系の向き（度）で指定
            /// </summary>
            /// <param name="x_deg"></param>
            /// <param name="y_deg"></param>
            /// <param name="z_deg"></param>
            /// <returns></returns>
            public Vector3 change_angleAbsolute(float x_deg = 0, float y_deg = 0, float z_deg = 0, bool keepX = false, bool keepY = false, bool keepZ = false)
            {
                Vector3 v = maid.transform.eulerAngles;
                maid.transform.eulerAngles = new Vector3(keepX ? v.x : x_deg, keepY ? v.y : y_deg, keepZ ? v.z : z_deg);
                return maid.transform.eulerAngles;
            }
            /// <summary>
            /// 指定角度だけメイドの向きを変える
            /// </summary>
            /// <param name="x_deg"></param>
            /// <param name="y_deg"></param>
            /// <param name="z_deg"></param>
            /// <returns></returns>
            public Vector3 change_angleRelative(float x_deg = 0, float y_deg = 0, float z_deg = 0)
            {
                Vector3 v = maid.transform.eulerAngles;
                maid.transform.eulerAngles = new Vector3(v.x + x_deg, v.y + y_deg, v.z + z_deg);
                return maid.transform.eulerAngles;
            }
            /// <summary>
            /// メイドの向きを絶対座標系の向きで返す
            /// </summary>
            /// <returns></returns>
            public Vector3 getAngle()
            {
                return maid.transform.eulerAngles;
            }



            /// <summary>
            /// 現在のメイド状態などに合わせて表情変更
            /// </summary>
            public void change_faceAnime(string faceAnime, float fadeTime = -1)
            {
                if (currentFaceAnime == faceAnime) return;
                if (fadeTime == -1) fadeTime = cfg.faceAnimeFadeTime * 2;
                currentFaceAnime = faceAnime;

                //TODO　Unityの把握している表情一覧に照らし合わせて、存在しない表情なら実行しない
                maid.FaceAnime(currentFaceAnime, fadeTime, 0);
            }
            public void change_faceAnime(List<string> faceAnimeList, float fadeTime = -1)
            {
                string face = Util.pickOneOrEmptyString(faceAnimeList);
                if (face.Equals(""))
                {
                    Util.info("表情リストが空でした");
                    return;
                }
                change_faceAnime(face, fadeTime);
            }


            Regex reg_hoho = new Regex(@"(頬.)");
            Regex reg_namida = new Regex(@"(涙.)");
            /// <summary>
            /// 頬・涙・よだれ　を設定
            /// </summary>
            /// <param name="hoho">0~3で指定、-1なら変更しない</param>
            /// <param name="namida">0~3で指定、-1なら変更しない</param>
            /// <param name="enableYodare"></param>
            /// <returns></returns>
            public string change_FaceBlend(int hoho = -1, int namida = -1, bool enableYodare = false)
            {
                //頬・涙を-1,0,1,2,3のいずれかへ制限
                hoho = (int)Mathf.Clamp(hoho, -1, 3);
                namida = (int)Mathf.Clamp(namida, -1, 3);

                string currentFaceBlend = maid.FaceName3;
                if (currentFaceBlend.Equals("オリジナル")) currentFaceBlend = "頬０涙０";    //初期状態ではオリジナルとなっているため、整合取る

                string cheekStr = reg_hoho.Match(currentFaceBlend).Groups[1].Value;         //現在の頬値で初期化
                string tearsStr = reg_namida.Match(currentFaceBlend).Groups[1].Value;
                string yodareStr = "";

                if (hoho == 0) cheekStr = "頬０";
                if (hoho == 1) cheekStr = "頬１";
                if (hoho == 2) cheekStr = "頬２";
                if (hoho == 3) cheekStr = "頬３";

                if (namida == 0) tearsStr = "涙０";
                if (namida == 1) tearsStr = "涙１";
                if (namida == 2) tearsStr = "涙２";
                if (namida == 3) tearsStr = "涙３";

                if (enableYodare) yodareStr = "よだれ";
                string blendSetStr = cheekStr + tearsStr + yodareStr;
                maid.FaceBlend(blendSetStr);
                return blendSetStr;
            }

            /// <summary>
            /// 瞳のY位置操作
            /// </summary>
            /// <param name="eyePosY">両目の瞳Y位置　０～５０？　０で初期値</param>
            private void updateMaidEyePosY(float eyePosY)
            {
                eyePosY = Mathf.Clamp(eyePosY, 0f, 50f);    //最大値は不明　50？

                Vector3 vl = maid.body0.trsEyeL.localPosition;
                Vector3 vr = maid.body0.trsEyeR.localPosition;
                const float fEyePosToSliderMul = 5000f;
                maid.body0.trsEyeL.localPosition = new Vector3(vl.x, Math.Max(eyePosY / fEyePosToSliderMul, 0f), vl.z);
                maid.body0.trsEyeR.localPosition = new Vector3(vr.x, Math.Min(eyePosY / fEyePosToSliderMul, 0f), vr.z);     //TODO -eyePosYかも。軸の正負が逆。

            }

            /// <summary>
            /// 各種再生処理の一括変更
            /// MaidState、感度、バイブ強度、
            /// </summary>
            public void update_playing()
            {
                if (loopVoiceBackuped)
                {
                    //　ループ音声を再生中、もしくは一回再生音声が再生済みなら介入してよい
                    if (maid.AudioMan.audiosource.loop || (!maid.AudioMan.audiosource.loop && !maid.AudioMan.audiosource.isPlaying))
                    {
                        change_LoopVoice(currentLoopVoice);
                        //change_LoopVoice();
                        loopVoiceBackuped = false;
                    }

                }

                //何もモーション再生していなかったら、同一カテゴリの待機モーション探して再生を試みる。OnceMotionの後などを想定
                if (!isPlayingMotion())
                {
                    List<MotionAttribute> attList = new List<MotionAttribute>();
                    attList.Add(MotionAttribute.Taiki);
                    List<string> motionList = searchMotionList(getMotionNameBase(getCurrentMotionName()), attList);
                    if (motionList.Count != 0)
                    {
                        string motion = motionList[0];
                        change_Motion(motion, isLoop: true);
                    }
                }
            }
            private float eyeToCam_turnSec = 0;
            private float headToCam_turnSec = 0;
            private EyeHeadToCamState eyeToCam_state = EyeHeadToCamState.Auto;
            private EyeHeadToCamState headToCam_state = EyeHeadToCamState.Auto;

            /// <summary>
            /// 目線・顔をカメラへ向けるかの状態管理
            /// </summary>
            public class EyeHeadToCamState
            {
                private static int nextOrdinal = 0;
                public static readonly List<EyeHeadToCamState> items = new List<EyeHeadToCamState>();
                //フィールド一覧
                public readonly int ordinal;            //このEnumのインスタンス順序
                public readonly string viewStr;
                //コンストラクタ
                private EyeHeadToCamState(string viewStr)
                {
                    this.viewStr = viewStr;
                    this.ordinal = nextOrdinal;
                    nextOrdinal++;
                    items.Add(this);
                }
                // 参照用インスタンス
                public static readonly EyeHeadToCamState No = new EyeHeadToCamState("no");
                public static readonly EyeHeadToCamState Auto = new EyeHeadToCamState("auto");
                public static readonly EyeHeadToCamState Yes = new EyeHeadToCamState("yes");
            }

            public void change_eyeToCam(EyeHeadToCamState state)
            {
                this.eyeToCam_state = state;
            }
            public void change_headToCam(EyeHeadToCamState state, float fadeSec = -1f)
            {
                this.headToCam_state = state;
                if (fadeSec != -1)
                {
                    maid.body0.HeadToCamFadeSpeed = fadeSec;
                }
            }

            /// <summary>
            ///目線更新
            /// ６～１０秒ごとに50％の確率で目線変更
            /// </summary>
            public void update_eyeToCam()
            {
                if (eyeToCam_state == EyeHeadToCamState.No)
                {
                    maid.body0.boEyeToCam = false;
                    return;
                }
                else if (eyeToCam_state == EyeHeadToCamState.Yes)
                {
                    maid.body0.boEyeToCam = true;
                    return;
                }
                else if (eyeToCam_state == EyeHeadToCamState.Auto)
                {
                    eyeToCam_turnSec -= Time.deltaTime;
                    if (eyeToCam_turnSec > 0) return;

                    if (UnityEngine.Random.Range(0, 100) < 50) maid.body0.boEyeToCam = !maid.body0.boEyeToCam;

                    eyeToCam_turnSec = UnityEngine.Random.Range(6, 10);  //6~10秒ごとに変える
                }
            }


            /// <summary>
            /// 顔の向き更新
            /// 6~10秒ごとにカメラを向くorそっぽ向く
            /// </summary>
            public void update_headToCam()
            {
                if (headToCam_state == EyeHeadToCamState.No)
                {
                    maid.body0.boHeadToCam = false;
                    return;
                }
                else if (headToCam_state == EyeHeadToCamState.Yes)
                {
                    maid.body0.boHeadToCam = true;
                    return;
                }
                else if (headToCam_state == EyeHeadToCamState.Auto)
                {
                    headToCam_turnSec -= Time.deltaTime;
                    if (headToCam_turnSec > 0) return;

                    if (maid.body0.boHeadToCam)
                    {
                        if (UnityEngine.Random.Range(0, 100) < 70) maid.body0.boHeadToCam = !maid.body0.boHeadToCam;
                    }
                    else
                    {
                        if (UnityEngine.Random.Range(0, 100) < 30) maid.body0.boHeadToCam = !maid.body0.boHeadToCam;
                    }

                    headToCam_turnSec = UnityEngine.Random.Range(6, 10);  //6~10秒ごとに変える
                }
            }




            public class MotionAttribute
            {
                //フィールド一覧
                public readonly string attributeStr;
                //コンストラクタ
                private MotionAttribute(string viewStr)
                {
                    this.attributeStr = viewStr;
                }
                //メソッド
                public string getViewStr()
                {
                    return this.attributeStr;
                }

                // 参照用インスタンス
                public static readonly MotionAttribute Level1 = new MotionAttribute("_1");
                public static readonly MotionAttribute Level2 = new MotionAttribute("_2");
                public static readonly MotionAttribute Level3 = new MotionAttribute("_3");
                public static readonly MotionAttribute Momi1 = new MotionAttribute("_momi_1");
                public static readonly MotionAttribute Momi2 = new MotionAttribute("_momi_2");
                public static readonly MotionAttribute Momi3 = new MotionAttribute("_momi_3");

                public static readonly MotionAttribute Oku = new MotionAttribute("_oku");
                public static readonly MotionAttribute Taiki = new MotionAttribute("_taiki");
                public static readonly MotionAttribute Zeccyougo = new MotionAttribute("_zeccyougo");
                public static readonly MotionAttribute Hatu3 = new MotionAttribute("_hatu_3");

                public static readonly MotionAttribute Insert = new MotionAttribute("_in_f_once");
                public static readonly MotionAttribute Zeccyou = new MotionAttribute("_zeccyou_f_once");
            }

            /// <summary>
            /// 通常のモーション名からモーションのベースとなる名前を推定する
            /// 例：dildo_onani_1a01_f.anm　-> dildo_onani_
            /// </summary>
            /// <param name="motionName"></param>
            /// <returns></returns>
            public string getMotionNameBase(string motionName)
            {
                if (motionName == null) return motionName;
                string ret = motionName;     //_1_...　などを除去
                foreach (KeyValuePair<Regex, string> kvp in motionBaseRegexDic)
                {
                    Regex regex = kvp.Key;
                    ret = regex.Replace(ret, kvp.Value);                    //_momi_　を除去
                }

                Util.debug(string.Format("getMotionNameBase {0} -> {1}", motionName, ret));
                return ret;
            }
            public List<string> searchMotionList(string motionNameBase, List<MotionAttribute> attributeList = null)
            {
                List<string> attributeStrList = new List<string>();
                foreach (MotionAttribute ma in attributeList)
                {
                    attributeStrList.Add(ma.attributeStr);
                }
                return searchMotionList(motionNameBase, attributeStrList);
            }

            /// <summary>
            /// 有効なモーション名を探す
            /// motionNameBaseから始まるモーション名のリストを返す
            /// attributeListを指定した場合は、いずれかのattributeを含むモーション名のリストを返す
            /// </summary>
            /// <param name="motionNameBase"></param>
            /// <param name="attributeList"></param>
            /// <returns></returns>
            public List<string> searchMotionList(string motionName, List<string> attributeList = null)
            {
                string motionNameBase = getMotionNameBase(motionName);
                if (attributeList == null) attributeList = new List<string>();
                List<string> possibleMotionList = new List<string>();

                if (motionNameBase == null) return possibleMotionList;

                //motionNameAllList.FindAll(s => s.Contains(motionNameBase)).FindAll(s => s.Contains(addition));
                foreach (string mn in motionNameAllList)
                {
                    if (!mn.StartsWith(motionNameBase)) continue;

                    foreach (string addition in attributeList)
                    {
                        if (mn.Contains(addition))
                        {
                            possibleMotionList.Add(mn);
                            break;
                        }
                    }
                }
                if (possibleMotionList.Count == 0)
                {
                    Util.info(string.Format("有効なモーションがありませんでした : {0} \r\n motionBaseName : {1}", this.maid.name, motionNameBase));
                }
                return possibleMotionList;
            }
            public string getCurrentMotionName()
            {
                return maid.body0.LastAnimeFN;      //現在のモーションを調べる
            }

            /// <summary>
            /// 指定したモーションを実行
            /// </summary>
            /// <param name="motionNameOrNameBase"></param>
            /// <param name="isLoop"></param>
            /// <param name="addQue"></param>
            /// <param name="motionSpeed"></param>
            /// <param name="fadeTime"></param>
            /// <returns></returns>
            public string change_Motion(string motionNameOrNameBase, bool isLoop = true, bool addQue = false, float motionSpeed = -1, float fadeTime = -1)
            {
                if (motionSpeed == -1) motionSpeed = Util.var20p(1f);
                if (fadeTime == -1) fadeTime = Util.var20p(0.8f);

                if (!motionNameOrNameBase.EndsWith(".anm")) motionNameOrNameBase += ".anm";
                if (motionNameOrNameBase == getCurrentMotionName()) return motionNameOrNameBase;            //現在再生しているモーションを再生しようとすると動作が不連続になるため

                Util.animate(maid, motionNameOrNameBase, isLoop, fadeTime, motionSpeed, addQue);
                return motionNameOrNameBase;
            }
            /// <summary>
            /// モーションリストの中から1つ選んで実行
            /// ただし、現在実行中のモーションは選択しない
            /// </summary>
            /// <param name="motionList"></param>
            /// <param name="isLoop"></param>
            /// <returns></returns>
            public string change_Motion(List<string> motionList, bool isLoop = true, bool enSelectForVibeState = true)
            {
                int currentIndex = motionList.IndexOf(getCurrentMotionName());   //見つからないとき-1
                int randomIndex = Util.randomInt(0, motionList.Count - 1, currentIndex);
                return change_Motion(motionList[randomIndex], isLoop, enSelectForVibeState);
            }
            public string change_Motion(List<MotionInfo> motionList, bool isLoop = true, bool enSelectForVibeState = true)
            {
                List<string> list = new List<string>();
                foreach (MotionInfo mi in motionList)
                {
                    list.Add(mi.motionName);
                }
                return change_Motion(list, isLoop, enSelectForVibeState);
            }




            public void change_nyo()
            {
                string nyoSound = "SE011.ogg";
                GameMain.Instance.SoundMgr.PlaySe(nyoSound, false);
                maid.AddPrefab("Particle/pNyou_cm3D2", "pNyou_cm3D2", "_IK_vagina", new Vector3(0f, -0.047f, 0.011f), new Vector3(20.0f, -180.0f, 180.0f));
            }

            public void change_sio()
            {
                maid.AddPrefab("Particle/pSio2_cm3D2", "pSio2_cm3D2", "_IK_vagina", new Vector3(0f, 0f, -0.01f), new Vector3(0f, 180.0f, 0f));
            }
            public void change_onceVoice(string voicename)
            {
                List<string> VoiceList = new List<string>();
                VoiceList.Add(voicename);
                change_onceVoice(VoiceList);
            }
            public void change_onceVoice(List<string> VoiceList)
            {
                _playVoice(VoiceList.ToArray(), false);
            }


            /// <summary>
            /// 繰り返し再生するボイスの変更
            /// </summary>
            /// <param name="VoiceList"></param>
            public void change_LoopVoice(List<string> VoiceList)
            {

                currentLoopVoice = _playVoice(VoiceList.ToArray(), isLoop: true);
            }

            public void change_LoopVoice(string voicename)
            {
                List<string> list = new List<string>();
                list.Add(voicename);
                change_LoopVoice(list);
            }
            public void change_stopVoice()
            {
                maid.AudioMan.Stop();
            }


            /// <summary>
            /// ボイスリストからボイス１つを選んで再生
            /// </summary>
            /// <param name="voiceList"></param>
            /// <param name="maid">ボイスを再生するメイド</param>
            /// <param name="isLoop">ボイスをループするか</param>
            /// <param name="exclusionVoiceIndex">再生しないボイスNo.</param>
            /// <param name="forcedVoiceIndex">再生ボイスNo.を直接指定</param>
            /// <returns>再生したボイスファイル名</returns>
            private string _playVoice(string[] voiceList, bool isLoop = true, int exclusionVoiceIndex = -1, int forcedVoiceIndex = -1)
            {
                if (voiceList.Length == 0)
                {
                    Util.info("VoiceListが空です");
                    throw new ArgumentException("VoiceListが空です");
                }

                int voiceIndex;
                if (forcedVoiceIndex == -1)   //明示的にボイスNo指定してないとき
                {
                    //特定数を除外して、再生するボイスNo.をランダムに生成
                    voiceIndex = Util.randomInt(0, voiceList.Length - 1, exclusionVoiceIndex);
                }
                else
                {
                    voiceIndex = forcedVoiceIndex;
                }
                voiceIndex = (int)Mathf.Clamp(voiceIndex, 0, voiceList.Length - 1);        //voiceIndexは配列の指数なので 0以上・サイズ-1以下    
                string playVoice = voiceList[voiceIndex];
                maid.AudioMan.LoadPlay(playVoice, 0f, false, isLoop);
                string loopStr = isLoop ? "ループあり" : "ループなし";
                Util.info(string.Format("ボイスを再生：{0}, {1}", playVoice, loopStr));
                return playVoice;
            }



            private bool currentEnableReverseFront = false;

            static List<TMorph> m_NeedFixTMorphs = new List<TMorph>();

            public readonly int maidNo;

            //シェイプキー操作
            //戻り値はsTagの存在有無
            static public bool VertexMorph_FromProcItem(TBody body, string sTag, float f)
            {
                bool bRes = false;

                if (!body || sTag == null || sTag == "")
                    return false;

                for (int i = 0; i < body.goSlot.Count; i++)
                {
                    TMorph morph = body.goSlot[i].morph;
                    if (morph != null)
                    {
                        if (morph.Contains(sTag))
                        {
                            /*if (i == 1)
                            {
                                bFace = true;
                            }*/
                            bRes = true;
                            int h = (int)morph.hash[sTag];
                            //morph.BlendValues[h] = f;
                            morph.SetBlendValues(h, f);
                            //後でまとめて更新する
                            //body.goSlot[i].morph.FixBlendValues();

                            //更新リストに追加
                            if (!m_NeedFixTMorphs.Contains(morph))
                                m_NeedFixTMorphs.Add(morph);
                        }
                    }
                }
                return bRes;
            }

            //シェイプキー操作Fix(基本はUpdate等の最後に一度呼ぶだけで良いはず）
            static public void VertexMorph_FixBlendValues()
            {
                foreach (TMorph tm in m_NeedFixTMorphs)
                {
                    tm.FixBlendValues();
                }

                m_NeedFixTMorphs.Clear();
            }
        }

        public class IMan
        {
            readonly Maid maid;

            public IMan(Maid maid)
            {
                this.maid = maid;
            }

        }



        /// <summary>
        /// 性格リスト
        /// </summary>
        public sealed class PersonnalList
        {
            static readonly string[] uniqueName = new string[] { "Pure", "Pride", "Cool", "Yandere", "Anesan", "Genki", "Sadist", "Muku", "Majime", "Rindere", "dummy_noSelected" };
            static readonly string[] viewName = new string[] { "純真", "ツンデレ", "クーデレ", "ヤンデレ", "姉ちゃん", "僕っ娘", "ドＳ", "無垢", "真面目", "凛デレ", "指定無" };
            public static string getUniqueName(int index)
            {
                if (index > uniqueName.Length - 1) throw new ArgumentException(string.Format("性格が見つかりませんでした。　PersonalList : 指数：{0}", index));
                return uniqueName[index];
            }
            public static string getViewName(int index)
            {
                if (index > viewName.Length - 1) throw new ArgumentException(string.Format("性格が見つかりませんでした。　PersonalList : 指数：{0}", index));
                return viewName[index];
            }
            public static int uniqueNameListLength()
            {
                return uniqueName.Length;
            }
            public static int viewNameListLength()
            {
                return viewName.Length;
            }

            internal static int uniqueNameIndexOf(string v)
            {
                return Array.IndexOf(uniqueName, v);
            }
        }

        public static VoiceTable OnceVoiceTable = new VoiceTable("OnceVoice");
        public static VoiceTable LoopVoiceTable = new VoiceTable("LoopVoice");


        /// <summary>
        /// １回orループ発声するボイスのテーブル
        /// 「oncevoice_」or「loopvoice_」から始まる複数のcsvからボイス一覧を読込み、
        /// 条件に合うボイスをフィルタリングして取得
        /// </summary>
        public class VoiceTable
        {
            public class ColItem
            {
                private static int nextOrdinal = 0;
                public static readonly List<ColItem> items = new List<ColItem>();
                //フィールド一覧
                public readonly string colName;
                public readonly string typeStr;
                public readonly int ordinal;            //このEnumのインスタンス順序
                public readonly int colNo;              //csvファイルの何列目に相当するか 0：２列目（1列目はCSV読み込み時に読み飛ばすため）
                public static int maxColNo = 0;
                //コンストラクタ
                private ColItem(int colNo, string colName, string typeStr)
                {
                    this.colName = colName;
                    this.typeStr = typeStr;
                    this.ordinal = nextOrdinal;
                    nextOrdinal++;
                    this.colNo = colNo;
                    items.Add(this);
                    if (maxColNo < colNo) maxColNo = colNo;
                }
                // 参照用インスタンス
                //public static readonly ColItem RecordName = new ColItem(0, "レコード名", "System.String");       //1列目はレコード名、読み込まない
                public static readonly ColItem Personal = new ColItem(1, "性格", "System.String");
                public static readonly ColItem Category = new ColItem(2, "カテゴリ", "System.String");
                public static readonly ColItem FileName = new ColItem(3, "ボイスファイル名", "System.String");
            }

            /// <summary>
            /// 複数のボイステーブルを保持するデータ構造（エクセルブック相当）
            /// 性格・カテゴリ名をテーブル名として、複数のテーブルを持つ
            /// </summary>
            private DataSet voiceDataSet = new DataSet();

            /// <summary>
            /// 全カテゴリ名一覧
            /// </summary>
            private HashSet<string> categorySet = new HashSet<string>();
            public readonly string voiceType;

            public VoiceTable(string voiceType)
            {
                this.voiceType = voiceType;
            }

            public void init()
            {
                voiceDataSet = new DataSet();
            }

            /// <summary>
            /// DataSetに新規シートを追加
            /// </summary>
            /// <param name="sheetName"></param>
            /// <returns></returns>
            private DataTable addNewDataTable(string sheetName)
            {
                DataTable ret = new DataTable(sheetName);
                voiceDataSet.Tables.Add(ret);
                // カラム名の追加
                foreach (ColItem c in ColItem.items)
                {
                    ret.Columns.Add(c.colName, Type.GetType(c.typeStr));
                }
                return ret;
            }

            /// <summary>
            /// CSVから読み込んでテーブルへ追加
            /// </summary>
            /// <param name="csvContent"></param>
            /// <param name="filename"></param>
            public void parse(string[][] csvContent, string filename = "")
            {
                foreach (string[] row in csvContent)
                {
                    if (row.Length - 1 < ColItem.maxColNo) continue;

                    string category = row[ColItem.Category.colNo];
                    string sPersonal = row[ColItem.Personal.colNo];
                    string sheetName = getUniqueSheetName(sPersonal, category);
                    if (!voiceDataSet.Tables.Contains(sheetName))
                    {
                        categorySet.Add(sheetName);
                        addNewDataTable(sheetName);
                    }
                    DataRow dr = voiceDataSet.Tables[sheetName].NewRow();
                    foreach (ColItem ovc in ColItem.items)
                    {
                        if (ovc.typeStr == "System.Boolean")
                        {
                            dr[ovc.colName] = (int.Parse(row[ovc.colNo]) != 0); //0ならfalseとして解釈
                        }
                        else
                        {
                            dr[ovc.colName] = row[ovc.colNo];
                        }
                    }
                    //カテゴリ名ごとにDataTableを追加
                    voiceDataSet.Tables[sheetName].Rows.Add(dr);
                }
            }

            /// <summary>
            /// 性格・カテゴリ文字列からシート名を生成
            /// （シート名の命名規則）
            /// </summary>
            /// <param name="sPersonal"></param>
            /// <param name="category"></param>
            /// <returns></returns>
            public string getUniqueSheetName(string sPersonal, string category)
            {
                return sPersonal + "_" + category;
            }

            /// <summary>
            /// 条件に一致するファイル名のリストを返す
            /// </summary>
            /// <param name="category"></param>
            /// <returns></returns>
            public List<string> queryTable(string sPersonal, string category)
            {
                List<string> ret = new List<string>();
                string sheetName = getUniqueSheetName(sPersonal, category);
                if (!voiceDataSet.Tables.Contains(sheetName))
                {
                    Util.info(string.Format("{0}テーブルから「{1}」という名前の性格・カテゴリは見つかりませんでした", voiceType, sheetName));
                    return ret;
                }
                //DataSetから指定カテゴリのテーブルを取得
                foreach (DataRow dr in voiceDataSet.Tables[sheetName].Rows)
                {
                    ret.Add(dr[ColItem.FileName.ordinal].ToString());
                }
                if (ret.Count == 0)
                {
                    Util.info(string.Format("{0}テーブルから「{1}」という名前の性格・カテゴリは見つかりませんでした", voiceType, sheetName));
                }
                return ret;
            }
        }

        public class MotionInfo
        {
            public string category = "";
            public bool FrontReverse;   //前後反転するか？  0:false
            public float AzimuthAngle;     //X方向回転角度
            public float DeltaY;           //Y方向オフセット
            public string MaidState;
            public bool EnMotionChange;
            public string motionName = "";
        }
        /// <summary>
        /// モーションのテーブル
        /// csvからモーション一覧を読込み、
        /// 条件に合うモーションをフィルタリングして取得
        /// </summary>
        public class MotionTable
        {
            public class ColItem
            {
                private static int nextOrdinal = 0;
                public static List<ColItem> items = new List<ColItem>();
                //フィールド一覧
                public readonly string colName;
                public readonly string typeStr;
                public readonly int ordinal;            //このEnumのインスタンス順序
                public readonly int colNo;              //csvファイルの何列目に相当するか 0：２列目（1列目はCSV読み込み時に読み飛ばすため）
                public static int maxColNo = 0;
                //コンストラクタ
                private ColItem(int colNo, string colName, string typeStr)
                {
                    this.colName = colName;
                    this.typeStr = typeStr;
                    this.ordinal = nextOrdinal;
                    nextOrdinal++;
                    this.colNo = colNo;
                    items.Add(this);
                    if (maxColNo < colNo) maxColNo = colNo;
                }
                // 参照用インスタンス
                //public static readonly ColItem RecordName = new ColItem(0, "レコード名", "System.String");       //1列目はレコード名、読み込まない
                public static readonly ColItem Category = new ColItem(1, "カテゴリ", "System.String");
                public static readonly ColItem FrontReverse = new ColItem(2, "正面反転？", "System.Boolean");
                public static readonly ColItem AzimuthAngle = new ColItem(3, "横回転角度", "System.Double");
                public static readonly ColItem DeltaY = new ColItem(4, "Y軸位置", "System.Double");
                public static readonly ColItem MaidState = new ColItem(5, "メイド状態", "System.String");
                public static readonly ColItem EnMotionChange = new ColItem(6, "モーション変更許可", "System.Boolean");
                public static readonly ColItem MotionName = new ColItem(7, "モーションファイル名", "System.String");
            }

            private static DataTable motionTable = new DataTable("Motion");

            public static void init()
            {
                motionTable = new DataTable("Motion");
                // カラム名の追加
                foreach (ColItem c in ColItem.items)
                {
                    motionTable.Columns.Add(c.colName, Type.GetType(c.typeStr));
                }
            }


            /// <summary>
            /// CSVから読み込んでテーブルへ追加
            /// </summary>
            /// <param name="csvContent"></param>
            /// <param name="filename"></param>
            public static void parse(string[][] csvContent, string filename = "")
            {
                //string categoryname = filename.Replace(cfg.loopVoicePrefix, "").Replace(".csv", "").Replace(".CSV", "");
                foreach (string[] row in csvContent)
                {
                    if (row.Length - 1 < ColItem.maxColNo) continue;

                    DataRow dr = motionTable.NewRow();
                    foreach (ColItem ovc in ColItem.items)
                    {
                        if (ovc.typeStr == "System.Boolean")
                        {
                            dr[ovc.colName] = (int.Parse(row[ovc.colNo]) != 0); //0ならfalseとして解釈
                        }
                        else
                        {
                            dr[ovc.colName] = row[ovc.colNo];
                        }
                    }
                    motionTable.Rows.Add(dr);
                }
            }


            /// <summary>
            /// 条件に一致するモーション情報のリストを返す
            /// モーション名はベース部分のみの項目もある。
            /// </summary>
            /// <param name="personal"></param>
            /// <param name="category"></param>
            /// <param name="maidState"></param>
            /// <param name="feelMin"></param>
            /// <param name="feelMax"></param>
            /// <returns></returns>
            public static List<MotionInfo> queryTable_motionNameBase(string category, string maidState = "-") // int feelMin = 0, int feelMax = 3)
            {
                // Selectメソッドを使ってデータを抽出
                List<MotionInfo> ret = query(category, maidState);
                if (ret.Count == 0)
                {
                    //Util.info(string.Format("MaidState「{0}」のMotionが見つからなかったのでデフォルトMaidStateのモーションを探します", maidState));
                    ret = query(category, "-", "MotionTable　再検索　MaidState「デフォルト」");
                }
                //Util.debug(string.Format("Motionクエリ結果\r\n カテゴリ:{0},MaidState:{1}\r\n{2}",
                //    category, maidState, Util.list2Str(ret)));
                return ret;
            }
            private static List<MotionInfo> query(string category, string maidState = "-", string comment = "MotionTable 検索")
            {
                string query = createCondition(category, maidState);
                DataRow[] dRows = motionTable.Select(query);
                List<MotionInfo> ret = new List<MotionInfo>();
                foreach (DataRow dr in dRows)
                {
                    try
                    {
                        MotionInfo mi = new MotionInfo();
                        mi.category = dr[ColItem.Category.ordinal].ToString();
                        mi.motionName = dr[ColItem.MotionName.ordinal].ToString();
                        mi.DeltaY = float.Parse(dr[ColItem.DeltaY.ordinal].ToString());
                        mi.AzimuthAngle = float.Parse(dr[ColItem.AzimuthAngle.ordinal].ToString());
                        mi.EnMotionChange = bool.Parse(dr[ColItem.EnMotionChange.ordinal].ToString());
                        mi.FrontReverse = bool.Parse(dr[ColItem.FrontReverse.ordinal].ToString());
                        ret.Add(mi);
                    }
                    catch (Exception e)
                    {
                        Util.debug(string.Format("MotionTableから読み出し失敗:{0} \r\n エラー内容 : {1}", dr.ToString(), e.StackTrace));
                    }
                }
                Util.debug(string.Format("{0}\r\n  {1}\r\n  検索結果\r\n  {2}", comment, query, Util.list2Str(ret)));
                return ret;
            }
            private static string createCondition(string category, string maidState = "-") // int feelMin = 0, int feelMax = 3)
            {
                StringBuilder condition = new StringBuilder();
                condition.Append(string.Format(" {0} = '{1}'", ColItem.Category.colName, category));
                if (maidState != "-")
                {
                    if (condition.Length != 0) condition.Append(" AND ");
                    condition.Append(string.Format(" {0} = '{1}'", ColItem.MaidState.colName, maidState));
                }
                return condition.ToString();
            }
            public static DataTable getTable()
            {
                return motionTable;
            }
            public static IEnumerable<string> getCategoryList()
            {
                HashSet<string> ret = new HashSet<string>();
                foreach (DataRow dr in motionTable.Rows)
                {
                    ret.Add((string)dr[ColItem.Category.colName]);
                }
                return ret;
            }
        }

        /// <summary>
        /// 表情のテーブル
        /// 「face_」から始まる複数のcsvから表情一覧を読込み。
        /// 条件に合う表情をフィルタリングして取得
        /// </summary>
        public class FaceTable
        {
            public class ColItem
            {
                private static int nextOrdinal = 0;
                public static List<ColItem> items = new List<ColItem>();
                //フィールド一覧
                public readonly string colName;
                public readonly string typeStr;
                public readonly int ordinal;            //このEnumのインスタンス順序
                public readonly int colNo;              //csvファイルの何列目に相当するか 0：２列目（1列目はCSV読み込み時に読み飛ばすため）
                public static int maxColNo = 0;
                //コンストラクタ
                private ColItem(int colNo, string colName, string typeStr)
                {
                    this.colName = colName;
                    this.typeStr = typeStr;
                    this.ordinal = nextOrdinal;
                    nextOrdinal++;
                    this.colNo = colNo;
                    items.Add(this);
                    if (maxColNo < colNo) maxColNo = colNo;
                }
                // 参照用インスタンス
                //public static readonly ColItem RecordName = new ColItem(0, "レコード名", "System.String");       //1列目はレコード名、読み込まない
                public static readonly ColItem Category = new ColItem(1, "カテゴリ", "System.String");
                public static readonly ColItem FaceName = new ColItem(2, "表情ファイル名", "System.String");
                public static readonly ColItem Hoho = new ColItem(3, "頬", "System.Int32");
                public static readonly ColItem Namida = new ColItem(4, "涙", "System.Int32");
                public static readonly ColItem Yodare = new ColItem(5, "よだれ", "System.Int32");
            }

            /// <summary>
            /// 複数の表情テーブルを保持するデータ構造（エクセルブック相当）
            /// カテゴリ名をテーブル名として、複数のテーブルを持つ
            /// </summary>
            private static DataSet faceDataSet = new DataSet();

            /// <summary>
            /// 全カテゴリ名一覧
            /// </summary>
            private static HashSet<string> categorySet = new HashSet<string>();

            public static void init()
            {
                faceDataSet = new DataSet();
            }

            /// <summary>
            /// faceDataSetに新規シートを追加
            /// </summary>
            /// <param name="sheetName"></param>
            /// <returns></returns>
            private static DataTable addNewDataTable(string sheetName)
            {
                DataTable ret = new DataTable(sheetName);
                faceDataSet.Tables.Add(ret);
                // カラム名の追加
                foreach (ColItem c in ColItem.items)
                {
                    ret.Columns.Add(c.colName, Type.GetType(c.typeStr));
                }
                return ret;
            }

            /// <summary>
            /// CSVから読み込んでテーブルへ追加
            /// </summary>
            /// <param name="csvContent"></param>
            /// <param name="filename"></param>
            public static void parse(string[][] csvContent, string filename = "")
            {
                foreach (string[] row in csvContent)
                {
                    if (row.Length - 1 < ColItem.maxColNo) continue;
                    string category = row[ColItem.Category.colNo];  //dr[ColItem.Category.colName].ToString();
                    if (!faceDataSet.Tables.Contains(category))
                    {
                        categorySet.Add(category);
                        addNewDataTable(category);
                    }
                    DataRow dr = faceDataSet.Tables[category].NewRow();
                    foreach (ColItem ovc in ColItem.items)
                    {
                        if (ovc.typeStr == "System.Boolean")
                        {
                            dr[ovc.colName] = (int.Parse(row[ovc.colNo]) != 0); //0ならfalseとして解釈
                        }
                        else
                        {
                            dr[ovc.colName] = row[ovc.colNo];
                        }
                    }
                    //カテゴリ名ごとにDataTableを追加
                    faceDataSet.Tables[category].Rows.Add(dr);
                }
            }

            /// <summary>
            /// 条件に一致するファイル名のリストを返す
            /// </summary>
            /// <param name="category"></param>
            /// <returns></returns>
            public static List<string> queryTable(string category)
            {
                List<string> ret = new List<string>();
                if (!faceDataSet.Tables.Contains(category))
                {
                    Util.info(string.Format("表情テーブルから「{0}」という名前のカテゴリは見つかりませんでした", category));
                    return ret;
                }
                //DataSetから指定カテゴリのテーブルを取得
                foreach (DataRow dr in faceDataSet.Tables[category].Rows)
                {
                    ret.Add(dr[ColItem.FaceName.ordinal].ToString());
                }
                if (ret.Count == 0)
                {
                    Util.info(string.Format("表情テーブルから「{0}」という名前のカテゴリは見つかりませんでした", category));
                }
                return ret;
            }
        }

        /// <summary>
        /// Androidのトースト風メッセージ
        /// </summary>
        /// <param name="message"></param>
        public static void toast(string message)
        {
            ToastUtil.Toast(ScriplayPlugin.instance, message);
        }

        /// <summary>
        /// トースト生成用クラス
        /// 参考：https://qiita.com/maebaru/items/23e85a8f2f1ce69482b7
        /// </summary>
        public class ToastUtil : MonoBehaviour
        {
            public static Color imgColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            public static Color textColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            public static Vector2 startPos = new Vector2(0, -500); // 開始場所
            public static Vector2 endPos = new Vector2(0, -300); // 終了場所
            public static int fontSize = 20;
            public static int moveFrame = 30; // 浮き上がりの時間(フレーム)
            public static int waitFrame = (int)3 * 60; // 浮き上がり後の時間(フレーム)
            public static int pad = 100; // padding
            public static Sprite imgSprite;
            public static Font textFont;

            public static void Toast<T>(MonoBehaviour mb, T m)
            {
                string msg = m.ToString();
                GameObject g = new GameObject("ToastCanbas");
                Canvas canvas = g.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; //最前
                g.AddComponent<CanvasScaler>();
                g.AddComponent<GraphicRaycaster>();

                GameObject g2 = new GameObject("Image");
                g2.transform.parent = g.transform;
                Image im = g2.AddComponent<Image>();
                if (imgSprite) im.sprite = imgSprite;
                im.color = imgColor;
                g2.GetComponent<RectTransform>().anchoredPosition = startPos;

                GameObject g3 = new GameObject("Text");
                g3.transform.parent = g2.transform;
                Text t = g3.AddComponent<Text>();
                g3.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

                t.alignment = TextAnchor.MiddleCenter;
                if (textFont)
                    t.font = textFont;
                else
                    t.font = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
                t.fontSize = fontSize;
                t.text = msg;
                t.enabled = true;
                t.color = textColor;

                g3.GetComponent<RectTransform>().sizeDelta = new Vector2(t.preferredWidth, t.preferredHeight);
                g3.GetComponent<RectTransform>().sizeDelta = new Vector2(t.preferredWidth, t.preferredHeight);//2回必要
                g2.GetComponent<RectTransform>().sizeDelta = new Vector2(t.preferredWidth + pad, t.preferredHeight + pad);

                mb.StartCoroutine(
                  DoToast(
                    g2.GetComponent<RectTransform>(), (endPos - startPos) * (1f / moveFrame), g
                  )
                );
            }
            static IEnumerator DoToast(RectTransform rec, Vector2 dif, GameObject g)
            {
                for (var i = 1; i <= moveFrame; i++) { rec.anchoredPosition += dif; yield return null; }
                for (var i = 1; i <= waitFrame; i++) yield return null;
                Destroy(g);
            }
        }
    }

    public static class Util
    {


        /// <summary>
        /// ファイル名のリストアップ、デフォルトはCSVファイルのみ
        /// </summary>
        /// <param name="searchPath"></param>
        /// <param name="suffix">例　"csv"</param>
        /// <returns></returns>
        public static List<string> getFileFullpathList(string searchPath, string suffix)
        {
            suffix = "*." + suffix;
            //フォルダ確認
            if (!Directory.Exists(searchPath))
            {
                //ない場合はフォルダ作成
                DirectoryInfo di = Directory.CreateDirectory(searchPath);
            }
            return new List<string>(Directory.GetFiles(searchPath, suffix));
        }


        /// <summary>
        /// コンソールにテキスト出力
        /// </summary>
        /// <param name="message"></param>
        public static void info(string message)
        {
            Console.WriteLine("I " + PluginMessage(message));
        }

        /// <summary>
        /// コンソールにテキスト出力
        /// </summary>
        /// <param name="message"></param>
        public static void debug(string message)
        {
            //Console.WriteLine("<color=" + cfg.debugPrintColor + ">" + PluginMessage(message) + "</color>");
            //UnityEngine.Debug.Log("<color=" + ScriplayPlugin.cfg.debugPrintColor + ">" + PluginMessage(message) + "</color>");
            if (ScriplayPlugin.cfg.debugMode) UnityEngine.Debug.Log("D " + PluginMessage(message));
        }

        private static string PluginMessage(string originalMessage)
        {
            return string.Format("{0} > {1}", ScriplayPlugin.cfg.PluginName, originalMessage);
        }

        /// <summary>
        /// 指定範囲内から特定数を除外してランダムな整数を返す
        /// Usage:
        ///  Util.randomInt(0,list.Length-1,currentIndex);
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="excludeList"></param>
        /// <returns></returns>
        public static int randomInt(int min, int max, List<int> excludeList = null)
        {
            if (min >= max) return min;
            if (excludeList == null) excludeList = new List<int>();
            Util.debug(string.Format("randomInt min:{0}, max:{1}, exclude{2}", min, max, Util.list2Str(excludeList)));
            List<int> indexList = Enumerable.Range(min, max).ToList(); //.Where(item => item != exclusionVoiceIndex).ToArray(); //ラムダ式は使えないぽい
            foreach (int i in excludeList)
            {
                if (indexList.Count == 1) break;
                indexList.Remove(i);
            }
            return indexList[new System.Random().Next(indexList.ToArray().Length)];

        }

        /// <summary>
        /// Linq使えないため？、joinでエラー出る。代わりにコレクションを文字列化するメソッド
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static string list2Str<T>(IEnumerable<T> collection)   // where T: object
        {
            if (collection == null || collection.Count() == 0) return "(要素数　０）";
            StringBuilder str = new StringBuilder();
            foreach (T t in collection)
            {
                str.Append(t.ToString() + ", ");
            }
            return str.ToString();
        }

        public static int randomInt(int min, int max, int excludeValue)
        {
            return randomInt(min, max, new List<int> { excludeValue });
        }

        /// <summary>
        /// ステータス表示用テキストを返す
        /// </summary>
        private static string[] SucoreText = new string[] { "☆ ☆ ☆", "★ ☆ ☆", "★ ★ ☆", "★ ★ ★" };
        //private static string[] SucoreText = new string[] { "_", "Ⅰ", "Ⅱ", "Ⅲ" };
        public static string getScoreText(int level)
        {
            level = (int)Mathf.Clamp(level, 0, 3);
            return SucoreText[level];
        }

        private static bool enSW = false;
        private static System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private static readonly int stopwatch_executeFrames = 60;
        private static int sw_frames = 0;
        public static void sw_start(string s = "")
        {
            if (!enSW) return;
            if (!(sw_frames++ > stopwatch_executeFrames)) return;
            sw.Start();
        }

        public static void sw_stop(string s = "")
        {
            if (!enSW) return;
            if (!(sw_frames > stopwatch_executeFrames)) return;
            sw_frames = 0;
            sw.Stop();
            sw.Reset();
        }

        public static void sw_showTime(string s = "")
        {
            if (!enSW) return;
            if (!(sw_frames > stopwatch_executeFrames)) return;
            sw.Stop();
            Util.debug(string.Format("{0} 経過時間：{1} ms", s, sw.ElapsedMilliseconds));
            sw.Reset();
            sw.Start();
        }

        /// <summary>
        /// メイドのモーションを実行
        /// </summary>
        /// <param name="maid"></param>
        /// <param name="motionName"></param>
        /// <param name="isLoop"></param>
        /// <param name="fadeTime"></param>
        /// <param name="speed"></param>
        public static void animate(Maid maid, string motionName, bool isLoop, float fadeTime = 0.5f, float speed = 1f, bool addQue = false)
        {
            try
            {
                if (!addQue)
                {
                    maid.CrossFadeAbsolute(motionName, false, isLoop, false, fadeTime, 1f);
                    maid.body0.m_Animation[motionName].speed = speed;
                }
                else
                {
                    maid.CrossFade(motionName, false, isLoop, true, fadeTime, 1f);
                }
            }
            catch (Exception e)
            {
                Util.info("モーション再生失敗" + e.Message + "\r\n" + e.StackTrace);
            }

        }

        /// <summary>
        /// ±20%の範囲の値を返す
        /// Variation20Percent
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static float var20p(float v)
        {
            return UnityEngine.Random.Range(v * 0.8f, v * 1.2f);
        }
        public static float var50p(float v)
        {
            return UnityEngine.Random.Range(v * 0.5f, v * 1.5f);
        }

        internal static float var50p(object sio_baseTime)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// リストからランダムに１つピックアップ
        /// </summary>
        /// <param name="list"></param>
        /// <param name="excludeIndex"></param>
        /// <returns></returns>
        public static string pickOneOrEmptyString(List<string> list, int excludeIndex = -1)
        //public static dynamic pickOneOrNull<T>(List<string> list, int excludeIndex = -1)    //プラグイン読み込み時にエラー
        {
            if (list.Count == 0) return "";
            return list[randomInt(0, list.Count - 1, excludeIndex)];
        }

        /// <summary>
        /// filepathからファイル内容を読み出し
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static string[] readAllText(string filepath)
        {
            string[] csvTextAry;
            Util.debug(string.Format("文字列読み込み開始 {0}", filepath));
            string csvContent = System.IO.File.ReadAllText(filepath);   //UTF-8のみ読み込み可能
            csvTextAry = csvContent.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            if (csvTextAry.Length < 2)
            {
                //改行コードが\nだった場合の保険
                csvTextAry = csvContent.Split(new string[] { "\n" }, StringSplitOptions.None);
            }
            Util.debug(string.Format("文字列読み込み終了 {0}", filepath));
            return csvTextAry;
        }

        /// <summary>
        /// CSVファイル読み込み
        /// 1行目・１列目は読み飛ばし、カンマ区切り
        /// 2次元string配列で返す
        /// </summary>
        /// <param name="file">対象ファイルへのフルパス</param>
        /// <returns></returns>
        public static string[][] ReadCsvFile(string file, bool enSkipFirstCol = true)
        {
            string[] csvTextAry = readAllText(file);
            List<string[]> csvData = new List<string[]>();

            bool isReadedLabel = false;
            foreach (string m in csvTextAry)
            {
                List<string> lineData = new List<string>();
                //string m = sr.ReadLine();
                int i = 0;

                if (!isReadedLabel)
                {   //１行目はラベルなので読み飛ばす
                    isReadedLabel = true;
                    continue;
                }

                string[] values;
                if (enSkipFirstCol)
                {
                    // 読み込んだ一行に対して、1列目を飛ばしてカンマ毎に分けて配列に格納する
                    values = m.Split(',').Skip<string>(1).ToArray();
                }
                else
                {
                    values = m.Split(',').ToArray();
                }
                // 出力する
                foreach (string value in values)
                {
                    if (value != "")
                    {
                        lineData.Add(value);
                    }
                    else if (i <= 3 && value == "")
                    {
                        lineData.Add("0");
                    }
                    ++i;
                }
                csvData.Add(lineData.ToArray());
            }
            Util.debug(string.Format("文字配列へ分割終了 {0}", file));

            return csvData.ToArray();
        }


    }


    /// <summary>
    /// Interpreterパターン
    /// メイドさんの制御とインタラクティブなゲーム進行制御
    /// </summary>
    public class ScriplayContext
    {
        private ScriplayContext(string scriptName, bool finished = false)
        {
            this.scriptFinished = finished;
            this.scriptName = scriptName;
        }

        /// <summary>
        /// 初期化用Nullオブジェクト
        /// </summary>
        public static ScriplayContext None = new ScriplayContext(" - ", finished: true);


        /// <summary>
        /// ラベル名：行番号、ジャンプ時に使用
        /// </summary>
        IDictionary<string, int> labelMap = new Dictionary<string, int>();

        /// <summary>
        /// スクリプト全文
        /// </summary>
        string[] scriptArray = new string[0];

        /// <summary>
        /// 実行中の行の番号
        /// </summary>
        public int currentExecuteLine = -1;

        private float waitSecond = 0f;

        /// <summary>
        /// スクリプト終了したか
        /// </summary>
        public bool scriptFinished
        {
            get { return this.scriptFinished_flag; }
            set
            {
                this.scriptFinished_flag = value;
                if (value) tearDown();
            }
        }
        public bool scriptFinished_flag = false;


        /// <summary>
        /// 選択肢待ち時間
        /// </summary>
        private float selection_waitSecond = 0f;
        /// <summary>
        /// 選択肢　選択された項目
        /// </summary>
        public Selection selection_selectedItem = Selection.None;
        /// <summary>
        /// 選択肢　選択できる項目
        /// </summary>
        public List<Selection> selection_selectionList = new List<Selection>();

        /// <summary>
        /// talk　発言終わるまで待ち
        /// key:maidNo, value:発言待ちか否か
        /// </summary>
        public Dictionary<int, bool> talk_waitUntilFinishSpeekingDict = new Dictionary<int, bool>();

        /// <summary>
        /// スクリプトのファイル名
        /// </summary>
        public readonly string scriptName = "";

        /// <summary>
        /// 表示領域に表示するテキスト
        /// </summary>
        public string showText = "";
        private float showText_waitTime = -1f;

        static Regex reg_comment = new Regex(@"^//.+", RegexOptions.IgnoreCase);     // //...　コメントアウト（を明示。解釈できない行はコメント同様実行されない）
        static Regex reg_label = new Regex(@"^#+\s*(.+)", RegexOptions.IgnoreCase);      //#...　ジャンプ先ラベル

        static Regex reg_scriptInfo = new Regex(@"^@info\s+(.+)", RegexOptions.IgnoreCase);   //@info version=1 ...  スクリプトへの注釈
        static Regex reg_require = new Regex(@"^@require\s+(.+)", RegexOptions.IgnoreCase);   //@require maidNum=2           //スクリプトの実行条件　（未実装：manNum=1）
        static Regex reg_auto = new Regex(@"^@auto\s+(.+)", RegexOptions.IgnoreCase);   //@auto auto1=じっくり auto2=ふつう             //autoモードの定義 auto1~9まで
        static Regex reg_posRelative = new Regex(@"^@posRelative\s+(.+)", RegexOptions.IgnoreCase);   //@posRelative x=1 y=1 z=1       //メイド位置を相対位置で指定
        static Regex reg_posAbsolute = new Regex(@"^@posAbsolute\s+(.+)", RegexOptions.IgnoreCase);   //@posAbsolute x=1 y=1 z=1       //メイド位置を絶対位置で指定
        static Regex reg_rotRelative = new Regex(@"^@rotRelative\s+(.+)", RegexOptions.IgnoreCase);   //@rotRelative x=1 y=1 z=1       //メイド向きを相対角度（度）で指定
        static Regex reg_rotAbsolute = new Regex(@"^@rotAbsolute\s+(.+)", RegexOptions.IgnoreCase);   //@rotAbsolute x=1 y=1 z=1       //メイド向きを相対角度（度）で指定
        static Regex reg_sound = new Regex(@"^@sound\s+(.+)", RegexOptions.IgnoreCase);    //@sound name=xxx   / @sound category=xxx     //SE再生 name=stopで再生停止

        static Regex reg_motion = new Regex(@"^@motion\s+(.+)", RegexOptions.IgnoreCase);    //@motion name=xxx   / @motion category=xxx     //モーション指定
        static Regex reg_face = new Regex(@"^@face\s+(.+)", RegexOptions.IgnoreCase);        //@face maid=0 name=エロ我慢 頬=0 涙=0 よだれ=1 /@face category=xxx       //表情指定    hoho=0 namida=0 yodare=1 も可, hoho/namida:0~3,yodare:0~1
        static Regex reg_wait = new Regex(@"^@wait\s+(.+)", RegexOptions.IgnoreCase);        //@wait 3sec                         //n秒待ち
        static Regex reg_goto = new Regex(@"^@goto\s+(.+)", RegexOptions.IgnoreCase);        //@goto （ラベル名）                  //ラベルへジャンプ
        static Regex reg_show = new Regex(@"^@show\s+(.+)", RegexOptions.IgnoreCase);        //@show text=（表示文字列） wait=3s                 //テキストを表示
        static Regex reg_talk = new Regex(@"^@talk\s+(.+)", RegexOptions.IgnoreCase);        //@talk name=xxx finish=1 /@talk category=絶頂１                      //oncevoice発話, nameなしor name=stopで停止
        static Regex reg_talkRepeat = new Regex(@"^@talkrepeat\s+(.+)", RegexOptions.IgnoreCase);        //@talkRepeat name=xxx     //loopvoice設定, nameなしor name=stopで停止
        static Regex reg_selection = new Regex(@"^@selection\s*([^\s]+)?", RegexOptions.IgnoreCase);   //@selection wait=3sec...    //選択肢開始
        static Regex reg_selectionItem = new Regex(@"[-]\s+([^\s]+)\s+(.+)", RegexOptions.IgnoreCase);   //- 選択肢名 goto=ジャンプ先ラベル (auto1Name)=90   //選択肢項目
        static Regex reg_eyeToCam = new Regex(@"^@eyeToCam\s+(.+)", RegexOptions.IgnoreCase);    //@eyeToCam mode=no/auto/yes             //目線をカメラへ向けるか
        static Regex reg_headToCam = new Regex(@"^@headToCam\s+(.+)", RegexOptions.IgnoreCase);    //@headToCam mode=no/auto/yes fade=1sec            //目線をカメラへ向けるか

        /// <summary>
        /// スクリプト終了時の処理
        /// </summary>
        private void tearDown()
        {
            selection_selectionList.Clear();
            foreach (IMaid m in maidList)
            {
                m.change_stopVoice();
            }
            change_SE("");
        }

        public static ScriplayContext readScriptFile(string scriptName, string[] scriptArray)
        {
            ScriplayContext ret = new ScriplayContext(scriptName);
            List<string> list = new List<string>(scriptArray);
            list.Insert(0, "");                 //スクリプトの行番号とcurrentExecuteLineを合わせるためにダミー行挿入
            scriptArray = list.ToArray();

            ret.scriptArray = scriptArray;
            foreach (string s in ret.scriptArray)
            {
                if (reg_scriptInfo.IsMatch(s))
                {

                    Util.info("スクリプトバージョンを検出しました");
                    break;
                }
            }

            //構文解析
            for (int i = 0; i < ret.scriptArray.Length; i++)
            {
                string line = ret.scriptArray[i];
                if (reg_label.IsMatch(line))
                {
                    //ラベルがあればlabelMapに追加
                    Match matched = reg_label.Match(line);
                    string labelStr = matched.Groups[1].Value;    //1 origin
                    ret.labelMap.Add(labelStr, i);
                }

            }

            return ret;
        }

        public static ScriplayContext readScriptFile(string filePath)
        {
            Util.info(string.Format("スクリプトファイル読み込み： {0}", filePath));
            FileInfo fi1 = new FileInfo(filePath);
            string[] contentArray = Util.readAllText(filePath);
            return readScriptFile(fi1.Name, contentArray);
        }



        /// <summary>
        /// 毎フレームスクリプトを実行
        /// 空白行などは読み飛ばして、１フレームにつき1コマンド実行
        /// </summary>
        public void Update()
        {
            //最低実行条件：メイド１人以上表示中
            if (maidList.Count == 0) return;

            if (waitSecond > 0f)
            {
                //@wait　で待ちの場合
                waitSecond -= Time.deltaTime;
                return;
            }
            if (showText_waitTime > 0f)
            {
                //@show で待ちの場合
                showText_waitTime -= Time.deltaTime;
                if (showText_waitTime < 0f)
                {
                    showText = "";  //表示を解除
                }
                return;
            }
            if (selection_waitSecond > 0f)
            {
                //選択肢待ちの場合
                selection_waitSecond -= Time.deltaTime;
                if (selection_waitSecond < 0f)
                {
                    //時間切れ
                    selection_selectionList = new List<Selection>();
                    return; //次フレームからスクリプト処理開始
                }

                if (selection_selectedItem != Selection.None)
                {
                    exec_goto(selection_selectedItem.gotoLabel);
                    selection_waitSecond = -1;
                    selection_selectedItem = Selection.None;
                    selection_selectionList.Clear();
                    return; //次フレームからスクリプト処理開始
                }
                else
                {
                    //選択されるまで待つ
                    return;
                }
            }
            List<int> talk_wait_removeKeyList = new List<int>();
            foreach (KeyValuePair<int, bool> kvp in talk_waitUntilFinishSpeekingDict)
            {
                //@talkの発言待ち
                int maidNo = kvp.Key;
                bool isWaiting = kvp.Value;
                if (isWaiting)
                {
                    if (maidList[maidNo].isPlayingVoice())
                    {
                        return; //発言終わるまで待ち
                    }
                    else
                    {
                        talk_wait_removeKeyList.Add(maidNo);        //発言終わったら待ちを解除
                    }
                }
            }
            foreach (int i in talk_wait_removeKeyList)
            {
                talk_waitUntilFinishSpeekingDict.Remove(i);
            }

            //スクリプト1行ずつ実行
            while (!scriptFinished)
            {
                currentExecuteLine++;
                if (currentExecuteLine >= scriptArray.Length)
                {
                    //スクリプト終了
                    this.scriptFinished = true;
                    Util.info(string.Format("すべてのスクリプトを実行しました. 行数：{0},{1}", currentExecuteLine.ToString(), scriptName));
                    return;
                }
                string line = scriptArray[currentExecuteLine];

                //対象行の解釈
                if (reg_comment.IsMatch(line))
                {
                    continue;
                }
                else if (reg_label.IsMatch(line))
                {
                    continue;
                }
                else if (reg_scriptInfo.IsMatch(line))
                {
                    continue;
                }
                else if (reg_require.IsMatch(line))
                {
                    exec_require(parseParameter(reg_require, line));
                    return;
                }
                else if (reg_auto.IsMatch(line))
                {
                    var paramDict = parseParameter(reg_auto, line);
                    for (int i = 1; i < 10; i++)
                    {
                        string key = "auto" + i.ToString();
                        if (paramDict.ContainsKey(key))
                        {
                            autoModeList.Add(paramDict[key]);
                        }
                    }
                }
                else if (reg_posAbsolute.IsMatch(line))
                {
                    exec_posAbsolute(parseParameter(reg_posAbsolute, line));
                    return;
                }
                else if (reg_posRelative.IsMatch(line))
                {
                    exec_posRelative(parseParameter(reg_posRelative, line));
                    return;
                }
                else if (reg_rotAbsolute.IsMatch(line))
                {
                    exec_rotAbsolute(parseParameter(reg_rotAbsolute, line));
                    return;
                }
                else if (reg_rotRelative.IsMatch(line))
                {
                    exec_rotRelative(parseParameter(reg_rotRelative, line));
                    return;
                }
                else if (reg_show.IsMatch(line))
                {
                    exec_show(parseParameter(reg_show, line));
                    return;
                }
                else if (reg_sound.IsMatch(line))
                {
                    exec_sound(parseParameter(reg_sound, line));
                    return;
                }
                else if (reg_motion.IsMatch(line))
                {
                    exec_motion(parseParameter(reg_motion, line));
                    return;
                }
                else if (reg_face.IsMatch(line))
                {
                    exec_face(parseParameter(reg_face, line));
                    return;
                }
                else if (reg_wait.IsMatch(line))
                {
                    Match matched = reg_wait.Match(line);
                    string waitStr = matched.Groups[1].Value;
                    selection_waitSecond = parseFloat(waitStr, suffix: new string[] { "sec", "s" });
                    return;
                }
                else if (reg_goto.IsMatch(line))
                {
                    //goto　-------------------------------------
                    Match matched = reg_goto.Match(line);
                    string gotoLabel = matched.Groups[1].Value;
                    exec_goto(gotoLabel);
                }
                else if (reg_talk.IsMatch(line))
                {
                    //talk　-------------------------------------
                    exec_talk(parseParameter(reg_talk, line), lineNo: currentExecuteLine);
                    return;
                }
                else if (reg_talkRepeat.IsMatch(line))
                {
                    //talkrepeat　-------------------------------------
                    exec_talk(parseParameter(reg_talkRepeat, line), loop: true, lineNo: currentExecuteLine);
                    return;
                }
                else if (reg_eyeToCam.IsMatch(line))
                {
                    exec_eyeToCam(parseParameter(reg_eyeToCam, line));
                    return;
                }
                else if (reg_headToCam.IsMatch(line))
                {
                    exec_headToCam(parseParameter(reg_headToCam, line));
                    return;
                }
                else if (reg_selection.IsMatch(line))
                {
                    //選択肢-------------------------------------
                    var paramDict = parseParameter(reg_selection, line);
                    if (paramDict.ContainsKey(key_wait))
                    {
                        selection_waitSecond = parseFloat(paramDict[key_wait], suffix: new string[] { "sec", "s" });
                    }
                    else
                    {
                        //待ち時間 指定ないときは表示したままにする
                        selection_waitSecond = 60 * 60 * 24 * 365f;
                    }

                    //各選択肢を読む
                    while (true)
                    {
                        //次の行が選択肢でなければ終了
                        int nextLine = currentExecuteLine + 1;
                        if (nextLine >= scriptArray.Length || !reg_selectionItem.IsMatch(scriptArray[nextLine])) break;

                        //次の行へ進んで選択肢追加処理
                        currentExecuteLine++;
                        line = scriptArray[currentExecuteLine];
                        Match matched = reg_selectionItem.Match(line);
                        string itemStr = matched.Groups[1].Value;
                        string paramStr = matched.Groups[2].Value;
                        paramDict = parseParameter(paramStr);

                        addSelection(itemStr, paramDict);
                    }
                    //次フレームから選択肢待ち
                    return;

                }
                else if (line == "")
                {
                    continue;
                }
                else
                {
                    //解釈できない行は読み飛ばし
                    Util.info(string.Format("解釈できませんでした：{0}:{1}", currentExecuteLine.ToString(), line));
                }

            }
        }

        private void exec_eyeToCam(Dictionary<string, string> paramDict)
        {

            Util.debug(string.Format("line{0} : eyeToCam ", currentExecuteLine.ToString()));
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {

                IMaid.EyeHeadToCamState state = IMaid.EyeHeadToCamState.Auto;

                if (paramDict.ContainsKey(key_mode))
                {
                    string mode = paramDict[key_mode].ToLower();
                    if (mode == IMaid.EyeHeadToCamState.Auto.viewStr)
                    {
                        state = IMaid.EyeHeadToCamState.Auto;

                    }
                    else if (mode == IMaid.EyeHeadToCamState.Yes.viewStr)
                    {
                        state = IMaid.EyeHeadToCamState.Yes;

                    }
                    else if (mode == IMaid.EyeHeadToCamState.No.viewStr)
                    {
                        state = IMaid.EyeHeadToCamState.No;

                    }
                    else
                    {
                        Util.info(string.Format("line{0} : モード指定が不適切です:{1}", currentExecuteLine.ToString(), mode));
                    }
                }
                else
                {
                    Util.info(string.Format("line{0} : モードが指定されていません", currentExecuteLine.ToString()));
                }
                maid.change_eyeToCam(state);
            }
        }

        private void exec_headToCam(Dictionary<string, string> paramDict)
        {
            Util.debug(string.Format("line{0} : headToCam ", currentExecuteLine.ToString()));
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {

                IMaid.EyeHeadToCamState state = IMaid.EyeHeadToCamState.Auto;
                float fadeSec = -1f;
                if (paramDict.ContainsKey(key_mode))
                {
                    string mode = paramDict[key_mode].ToLower();
                    if (mode == IMaid.EyeHeadToCamState.Auto.viewStr)
                    {
                        state = IMaid.EyeHeadToCamState.Auto;
                    }
                    else if (mode == IMaid.EyeHeadToCamState.Yes.viewStr)
                    {
                        state = IMaid.EyeHeadToCamState.Yes;
                    }
                    else if (mode == IMaid.EyeHeadToCamState.No.viewStr)
                    {
                        state = IMaid.EyeHeadToCamState.No;
                    }
                    else
                    {
                        Util.info(string.Format("line{0} : モード指定が不適切です:{1}", currentExecuteLine.ToString(), mode));
                    }

                }
                else
                {
                    Util.info(string.Format("line{0} : モードが指定されていません", currentExecuteLine.ToString()));
                }

                if (paramDict.ContainsKey(key_fade))
                {
                    fadeSec = parseFloat(paramDict[key_fade], new string[] { "sec", "s" });
                    maid.change_headToCam(state, fadeSec: fadeSec);
                    return;
                }
                else
                {
                    maid.change_headToCam(state);
                    return;
                }
            }
        }



        /// <summary>
        /// コマンドのパラメータ文字列を解釈して辞書を返す
        /// ex.
        /// @command key1=value1...
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="lineStr"></param>
        /// <returns></returns>
        private Dictionary<string, string> parseParameter(Regex reg, string lineStr)
        {
            string paramStr = "";
            if (!reg.IsMatch(lineStr)) return new Dictionary<string, string>();

            paramStr = reg.Match(lineStr).Groups[1].Value;
            return parseParameter(paramStr);
        }
        /// <summary>
        /// コマンドのパラメータ文字列を解釈して辞書を返す
        /// パラメータ形式：
        /// ex. key1=value1 key2=value2
        /// </summary>
        /// <param name="paramStr"></param>
        /// <returns></returns>
        private Dictionary<string, string> parseParameter(string paramStr)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            paramStr = parseParameter_regex.Replace(paramStr, " "); //複数かもしれない空白文字を１つへ
            paramStr = parseParameter_regex_header.Replace(paramStr, ""); //先頭空白除去
            paramStr = parseParameter_regex_footer.Replace(paramStr, ""); //後方空白除去
            string[] ss = paramStr.Split(' ');
            foreach (string s in ss)
            {
                string[] kv = s.Split('=');
                if (kv.Length != 2)
                {
                    Util.info(string.Format("line{0} : パラメータを読み込めませんでした。「key=value」形式になっていますか？ : {1}", currentExecuteLine.ToString(), s));
                    continue;
                }
                ret.Add(kv[0], kv[1]);
            }
            return ret;
        }

        static Regex parseParameter_regex = new Regex(@"\s+");
        static Regex parseParameter_regex_header = new Regex(@"^\s+");
        static Regex parseParameter_regex_footer = new Regex(@"\s+$");

        private List<string> autoModeList = new List<string>();


        /// <summary>
        /// 数値文字列を解釈。除去する接尾辞（単位 sec,sなど）を除去順に列挙のこと
        /// だめならログ出力
        /// </summary>
        /// <param name="floatStr"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        private float parseFloat(string floatStr, string[] suffix = null)
        {
            float ret = -1;
            if (suffix != null)
            {
                floatStr = floatStr.ToLower();
                foreach (string s in suffix)
                {
                    floatStr = floatStr.Replace(s, "");
                }
            }
            try
            {
                ret = float.Parse(floatStr);
            }
            catch (Exception e)
            {
                Util.info(string.Format("line{0} : 数値を読み込めませんでした : {1}", currentExecuteLine.ToString(), floatStr));
                Util.debug(e.StackTrace);
            }
            return ret;
        }

        private void exec_sound(Dictionary<string, string> paramDict)
        {
            Util.debug(string.Format("line{0} : sound ", currentExecuteLine.ToString()));
            if (paramDict.ContainsKey(key_name))
            {
                string name = paramDict[key_name];
                if (name == "stop") name = "";
                change_SE(name);
            }
            else
            {
                //nameパラメータなしなら再生停止
                change_SE("");
            }
        }

        private void exec_require(Dictionary<string, string> paramDict)
        {
            if (paramDict.ContainsKey(key_maidNum))
            {
                int maidNum = (int)parseFloat(paramDict[key_maidNum]);
                if (maidList.Count < maidNum)
                {
                    string mes = string.Format("メイドさんが{0}人以上必要です", maidNum);
                    toast(mes);
                    Util.info(mes);
                    scriptFinished = true;
                    return;
                }
            }
            //if (paramDict.ContainsKey(key_manNum))
            //{
            //    int manNum = (int)parseFloat(paramDict[key_manNum]);
            //    if (manList.Count < manNum)
            //    {
            //        string mes = string.Format("ご主人様が{0}人以上必要です", manNum);
            //        toast(mes);
            //        Util.info(mes);
            //        scriptFinished = true;
            //        return;
            //    }
            //}
        }

        private void exec_motion(Dictionary<string, string> paramDict)
        {
            Util.debug(string.Format("line{0} : motion ", currentExecuteLine.ToString()));
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {

                if (paramDict.ContainsKey(key_name))
                {
                    maid.change_Motion(paramDict[key_name], isLoop: true);
                }
                else if (paramDict.ContainsKey(key_category))
                {
                    List<MotionInfo> motionList = MotionTable.queryTable_motionNameBase(paramDict[key_category]);
                    maid.change_Motion(motionList, isLoop: true);
                }
                else
                {
                    Util.info(string.Format("line{0} : モーションが指定されていません", currentExecuteLine.ToString()));
                }
            }
        }

        private void exec_face(Dictionary<string, string> paramDict)
        {
            Util.debug(string.Format("line{0} : face ", currentExecuteLine.ToString()));
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {

                float fadeTime = -1f;
                if (paramDict.ContainsKey(key_fade)) fadeTime = parseFloat(paramDict[key_fade], new string[] { "sec", "s" });
                if (paramDict.ContainsKey(key_name))
                {
                    string name = paramDict[key_name];
                    if (fadeTime == -1f)
                    {
                        maid.change_faceAnime(name);
                    }
                    else
                    {
                        maid.change_faceAnime(name, fadeTime);
                    }
                }
                else if (paramDict.ContainsKey(key_category))
                {
                    List<string> faceList = FaceTable.queryTable(paramDict[key_category]);
                    if (fadeTime == -1f)
                    {
                        maid.change_faceAnime(faceList);
                    }
                    else
                    {
                        maid.change_faceAnime(faceList, fadeTime);
                    }
                }

                int hoho = -1;
                int namida = -1;
                bool yodare = false;
                if (paramDict.ContainsKey(key_namida) || paramDict.ContainsKey(key_涙))
                {
                    if (paramDict.ContainsKey(key_namida)) namida = (int)parseFloat(paramDict[key_namida]);
                    if (paramDict.ContainsKey(key_涙)) namida = (int)parseFloat(paramDict[key_涙]);
                    if (namida < 0 || namida > 3)
                    {
                        Util.info(string.Format("line{0} : 涙の値は0~3である必要があります。強制的に0にします。", currentExecuteLine.ToString()));
                        namida = 0;
                    }
                }
                if (paramDict.ContainsKey(key_hoho) || paramDict.ContainsKey(key_頬))
                {
                    if (paramDict.ContainsKey(key_hoho)) hoho = (int)parseFloat(paramDict[key_hoho]);
                    if (paramDict.ContainsKey(key_頬)) hoho = (int)parseFloat(paramDict[key_頬]);
                    if (hoho < 0 || hoho > 3)
                    {
                        Util.info(string.Format("line{0} : 頬の値は0~3である必要があります。強制的に0にします。", currentExecuteLine.ToString()));
                        hoho = 0;
                    }
                }
                if (paramDict.ContainsKey(key_yodare) || paramDict.ContainsKey(key_よだれ))
                {
                    int yodareInt = -1;
                    if (paramDict.ContainsKey(key_yodare)) yodareInt = (int)parseFloat(paramDict[key_yodare]);
                    if (paramDict.ContainsKey(key_頬)) yodareInt = (int)parseFloat(paramDict[key_頬]);
                    if (yodareInt == 1) yodare = true;
                    maid.change_FaceBlend(enableYodare: yodare);
                }
                maid.change_FaceBlend(hoho: hoho, namida: namida, enableYodare: yodare);
            }
        }


        private void exec_posRelative(Dictionary<string, string> paramDict)
        {
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {

                float x = 0;
                float y = 0;
                float z = 0;
                if (paramDict.ContainsKey(key_x))
                {
                    x = parseFloat(paramDict[key_x]);
                }
                if (paramDict.ContainsKey(key_y))
                {
                    y = parseFloat(paramDict[key_y]);
                }
                if (paramDict.ContainsKey(key_z))
                {
                    z = parseFloat(paramDict[key_z]);
                }
                maid.change_positionRelative(x, y, z);
            }
        }

        private void exec_rotRelative(Dictionary<string, string> paramDict)
        {
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {
                float x = 0;
                float y = 0;
                float z = 0;
                if (paramDict.ContainsKey(key_x))
                {
                    x = parseFloat(paramDict[key_x]);
                }
                if (paramDict.ContainsKey(key_y))
                {
                    y = parseFloat(paramDict[key_y]);
                }
                if (paramDict.ContainsKey(key_z))
                {
                    z = parseFloat(paramDict[key_z]);
                }
                maid.change_angleRelative(x, y, z);
            }
        }


        private void exec_posAbsolute(Dictionary<string, string> paramDict)
        {
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {
                bool keepX = true;
                bool keepY = true;
                bool keepZ = true;
                float x = 0;
                float y = 0;
                float z = 0;
                if (paramDict.ContainsKey(key_x))
                {
                    keepX = false;
                    x = parseFloat(paramDict[key_x]);
                }
                if (paramDict.ContainsKey(key_y))
                {
                    keepY = false;
                    y = parseFloat(paramDict[key_y]);
                }
                if (paramDict.ContainsKey(key_z))
                {
                    keepZ = false;
                    z = parseFloat(paramDict[key_z]);
                }
                maid.change_positionAbsolute(x, y, z, keepX, keepY, keepZ);
            }
        }

        private void exec_rotAbsolute(Dictionary<string, string> paramDict)
        {
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {
                bool keepX = true;
                bool keepY = true;
                bool keepZ = true;
                float x = 0;
                float y = 0;
                float z = 0;
                if (paramDict.ContainsKey(key_x))
                {
                    keepX = false;
                    x = parseFloat(paramDict[key_x]);
                }
                if (paramDict.ContainsKey(key_y))
                {
                    keepY = false;
                    y = parseFloat(paramDict[key_y]);
                }
                if (paramDict.ContainsKey(key_z))
                {
                    keepZ = false;
                    z = parseFloat(paramDict[key_z]);
                }
                maid.change_angleAbsolute(x, y, z, keepX, keepY, keepZ);
            }
        }

        const string key_maid = "maid";
        const string key_name = "name";
        const string key_category = "category";
        const string key_mode = "mode";
        const string key_fade = "fade";
        const string key_wait = "wait";
        const string key_finish = "finish";
        const string key_goto = "goto";
        const string key_maidNum = "maidNum";
        const string key_manNum = "manNum";
        const string key_text = "text";
        const string key_hoho = "hoho";
        const string key_namida = "namida";
        const string key_yodare = "yodare";
        const string key_頬 = "頬";
        const string key_涙 = "涙";
        const string key_よだれ = "よだれ";
        const string key_x = "x";
        const string key_y = "y";
        const string key_z = "z";

        private void exec_show(Dictionary<string, string> paramDict, int lineNo = -1)
        {
            Util.debug(string.Format("line{0} : show ", currentExecuteLine.ToString()));
            if (paramDict.ContainsKey(key_text))
            {
                this.showText = paramDict[key_text];
            }
            else
            {
                Util.info(string.Format("line{0} : 表示するテキストが見つかりません", currentExecuteLine.ToString()));
                return;
            }
            if (paramDict.ContainsKey(key_wait))
            {
                this.showText_waitTime = parseFloat(paramDict[key_wait], new string[] { "sec", "s" });
            }
            else
            {
                //文字数から自動計算　10文字1秒、最小で1秒
                this.showText_waitTime = ((float)this.showText.Length) / 10f;
                this.showText_waitTime = Math.Max(showText_waitTime, 1f);
            }

        }

        /// <summary>
        /// talk/talkrepeat　コマンドの実行
        /// </summary>
        /// <param name="paramStr"></param>
        /// <param name="loop"></param>
        /// <param name="lineNo"></param>
        private void exec_talk(Dictionary<string, string> paramDict, bool loop = false, int lineNo = -1)
        {
            Util.debug(string.Format("line{0} : talk ", currentExecuteLine.ToString()));
            List<IMaid> maidList = selectMaid(paramDict);

            foreach (IMaid maid in maidList)
            {
                List<string> voiceList = new List<string>();
                if (paramDict.ContainsKey(key_finish))
                {
                    if (paramDict[key_finish] == "1") talk_waitUntilFinishSpeekingDict[maid.maidNo] = true;
                }

                if (paramDict.ContainsKey(key_name))
                {
                    voiceList.Add(paramDict[key_name]);
                }
                else if (paramDict.ContainsKey(key_category))
                {
                    string voiceCategory = paramDict[key_category];
                    if (loop)
                    {
                        voiceList = LoopVoiceTable.queryTable(maid.sPersonal, voiceCategory);
                    }
                    else
                    {
                        voiceList = OnceVoiceTable.queryTable(maid.sPersonal, voiceCategory);
                    }
                    if (voiceList.Count == 0)
                    {
                        Util.info(string.Format("line{0} : カテゴリのボイスが見つかりません。カテゴリ：{1}", currentExecuteLine.ToString(), voiceCategory));
                        return;
                    }
                }
                if (voiceList.Count == 0 | (voiceList.Count == 1 && voiceList[0].ToLower().Equals("stop")))
                {
                    //nameパラメータなしor name=stop　の場合は音声停止
                    maid.change_stopVoice();
                }
                if (loop)
                {
                    maid.change_LoopVoice(voiceList);
                }
                else
                {
                    maid.change_onceVoice(voiceList);
                }
            }
        }

        /// <summary>
        /// 指定されたmaidNoのIMaidを返す
        /// 指定なき場合はmaidNo.0のIMaidを返す
        /// </summary>
        /// <param name="paramDict"></param>
        /// <returns></returns>
        private List<IMaid> selectMaid(Dictionary<string, string> paramDict)
        {
            List<IMaid> ret = new List<IMaid>();
            if (paramDict.ContainsKey(key_maid))
            {
                int maidNum = int.Parse(paramDict[key_maid]);
                if (maidNum < ScriplayPlugin.maidList.Count)
                {
                    ret.Add(ScriplayPlugin.maidList[maidNum]);
                }
                else
                {
                    Util.info(string.Format("メイドは{0}人しか有効にしていません。maidNo.{1}は無効です", ScriplayPlugin.maidList.Count, maidNum));
                }
            }
            else
            {
                ret = new List<IMaid>(ScriplayPlugin.maidList);
            }
            return ret;
        }


        /// <summary>
        /// gotoコマンドの実行
        /// 指定したラベルに対応した行へジャンプ
        /// </summary>
        /// <param name="gotoLabel"></param>
        private void exec_goto(string gotoLabel)
        {
            if (!labelMap.ContainsKey(gotoLabel))
            {
                Util.info(string.Format("line{0} : ジャンプ先ラベルが見つかりません。ジャンプ先：{1}", currentExecuteLine.ToString(), gotoLabel));
                scriptFinished = true;
            }
            currentExecuteLine = labelMap[gotoLabel];
            Util.debug(string.Format("line{0} : 「{1}」へジャンプしました", currentExecuteLine.ToString(), gotoLabel));
        }

        /// <summary>
        /// 選択肢項目を追加
        /// </summary>
        /// <param name="itemViewStr"></param>
        public void addSelection(string itemViewStr, Dictionary<string, string> dict)
        {
            string gotoLabel = "";
            if (dict.ContainsKey(key_goto)) gotoLabel = dict[key_goto];

            //上記以外のキーはautomode名として解釈
            Dictionary<string, int> autoProbDict = new Dictionary<string, int>();
            foreach (string key in dict.Keys)
            {
                if (key.Equals(key_goto)) continue;

                string auto_name = key;
                int auto_prob;
                try
                {
                    auto_prob = int.Parse(dict[key].Replace("%", ""));
                    autoProbDict.Add(auto_name, auto_prob);
                }
                catch (Exception e)
                {
                    Util.info(string.Format("選択肢　自動選択確率の読み込みに失敗 : {0}, {1}, {2} \r\n {3}", itemViewStr, key, dict[key], e.StackTrace));
                }
            }

            selection_selectionList.Add(new Selection(itemViewStr, gotoLabel, autoProbDict));
            Util.debug(string.Format("選択肢「{0}」を追加", itemViewStr));
        }

        public class Selection
        {
            //Nullオブジェクト
            public static readonly Selection None = new Selection("選択肢なし", "", new Dictionary<string, int>());

            //フィールド一覧
            public readonly string viewStr;
            public readonly string gotoLabel;
            public readonly Dictionary<string, int> autoProbDict;

            //コンストラクタ
            public Selection(string viewStr, string gotoLabel, Dictionary<string, int> autoProbDict)
            {
                this.viewStr = viewStr;
                this.gotoLabel = gotoLabel;
                this.autoProbDict = autoProbDict;
            }
        }
    }

}


