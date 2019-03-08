using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using UnityEngine;
using System.Collections;
using System.Data;
using System.Text;
using PluginExt;
using UnityEngine.UI;
using UnityInjector.Attributes;

namespace COM3D2.Scriplay.Plugin
{

    // Token: 0x02000002 RID: 2
    [PluginFilter("COM3D2x64")]
    [PluginFilter("COM3D2x86")]
    [PluginFilter("COM3D2VRx64")]
    [PluginFilter("COM3D2OHx64")]
    [PluginFilter("COM3D2OHx86")]
    [PluginFilter("COM3D2OHVRx64")]
    [PluginName("Scriplay edit by lilly")]
    [PluginVersion("0.1.1.3")]
    public class ScriplayPlugin : ExPluginBase
    {
        // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
        private void initMaidList()
        {
            Util.info("메이드 목록을 불러오는 시작");
            ScriplayPlugin.maidList.Clear();
            ScriplayPlugin.manList.Clear();
            CharacterMgr characterMgr = GameMain.Instance.CharacterMgr;
            for (int i = 0; i < characterMgr.GetMaidCount(); i++)
            {
                Maid maid = characterMgr.GetMaid(i);
                bool flag = !this.isMaidAvailable(maid);
                if (!flag)
                {
                    ScriplayPlugin.maidList.Add(new ScriplayPlugin.IMaid(i, maid));
                    Util.info(string.Format("メイド「{0}」を検出しました", maid.status.fullNameJpStyle));
                }
            }
            for (int j = 0; j < 6; j++)
            {
                Maid man = characterMgr.GetMan(j);
                bool flag2 = !this.isMaidAvailable(man);
                if (!flag2)
                {
                    ScriplayPlugin.manList.Add(new ScriplayPlugin.IMan(man));
                    Util.info(string.Format("ご主人様「{0}」を検出しました", man.status.fullNameJpStyle));
                }
            }
            GameMain.Instance.SoundMgr.StopSe();
        }

        // Token: 0x06000002 RID: 2 RVA: 0x0000215C File Offset: 0x0000035C
        private bool isMaidAvailable(Maid m)
        {
            return m != null && m.Visible && m.AudioMan != null;
        }

        // Token: 0x06000003 RID: 3 RVA: 0x00002190 File Offset: 0x00000390
        public void Awake()
        {
            UnityEngine.Object.DontDestroyOnLoad(this);
            string dataPath = Application.dataPath;
            this.loadSetting();
            this.gameCfg_isChuBLipEnabled = (dataPath.Contains("COM3D2OHx64") || dataPath.Contains("COM3D2OHx86") || dataPath.Contains("COM3D2OHVRx64"));
            this.gameCfg_isVREnabled = (dataPath.Contains("COM3D2OHVRx64") || dataPath.Contains("COM3D2VRx64") || Environment.CommandLine.ToLower().Contains("/vr"));
            foreach (KeyValuePair<string, string> keyValuePair in ScriplayPlugin.motionBaseRegexDefDic)
            {
                Regex key = new Regex(keyValuePair.Key);
                ScriplayPlugin.motionBaseRegexDic.Add(key, keyValuePair.Value);
            }
            this.initGuiStyle();
        }

        // Token: 0x06000004 RID: 4 RVA: 0x00002280 File Offset: 0x00000480
        private void loadSetting()
        {
            ScriplayPlugin.cfg = base.ReadConfig<ScriplayPlugin.ScriplayConfig>("ScriplayConfig");
            this.load_ConfigCsv();
            this.load_motionGameData(ScriplayPlugin.cfg.enModMotionLoad);
        }

        // Token: 0x06000005 RID: 5 RVA: 0x000022AC File Offset: 0x000004AC
        private void load_ConfigCsv()
        {
            Util.info("CSV 파일 가져 오기");
            ScriplayPlugin.OnceVoiceTable.init();
            ScriplayPlugin.LoopVoiceTable.init();
            ScriplayPlugin.MotionTable.init();
            ScriplayPlugin.FaceTable.init();
            ScriplayPlugin.motionCategorySet.Clear();
            List<string> fileFullpathList = Util.getFileFullpathList(ScriplayPlugin.cfg.csvPath, "csv");
            string text = "\r\n";
            foreach (string text2 in fileFullpathList)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text2);
                text = text + fileNameWithoutExtension + "\r\\n";
                Util.info(string.Format("CSV:{0}", fileNameWithoutExtension));
                bool flag = fileNameWithoutExtension.Contains(ScriplayPlugin.cfg.motionListPrefix);
                if (flag)
                {
                    ScriplayPlugin.MotionTable.parse(Util.ReadCsvFile(text2, false), fileNameWithoutExtension);
                }
                else
                {
                    bool flag2 = fileNameWithoutExtension.Contains(ScriplayPlugin.cfg.onceVoicePrefix);
                    if (flag2)
                    {
                        ScriplayPlugin.OnceVoiceTable.parse(Util.ReadCsvFile(text2, false), fileNameWithoutExtension);
                    }
                    else
                    {
                        bool flag3 = fileNameWithoutExtension.Contains(ScriplayPlugin.cfg.loopVoicePrefix);
                        if (flag3)
                        {
                            ScriplayPlugin.LoopVoiceTable.parse(Util.ReadCsvFile(text2, false), fileNameWithoutExtension);
                        }
                        else
                        {
                            bool flag4 = fileNameWithoutExtension.Contains(ScriplayPlugin.cfg.faceListPrefix);
                            if (flag4)
                            {
                                ScriplayPlugin.FaceTable.parse(Util.ReadCsvFile(text2, false), fileNameWithoutExtension);
                            }
                        }
                    }
                }
            }
            bool flag5 = text == "\r\n";
            if (flag5)
            {
                text = "（CSV 파일을 찾을 수 없습니다）";
            }
            Util.info(text);
            foreach (string item in ScriplayPlugin.MotionTable.getCategoryList())
            {
                ScriplayPlugin.motionCategorySet.Add(item);
            }
        }

        // Token: 0x06000006 RID: 6 RVA: 0x00002498 File Offset: 0x00000698
        private void load_motionGameData(bool allLoad = true)
        {
            Util.info("모션 파일 읽기 시작");
            ScriplayPlugin.motionNameAllList.Clear();
            bool flag = !allLoad;
            if (flag)
            {
                string[] array = new string[]
                {
                    "motion",
                    "motion2",
                    "motion_3d21reserve_2",
                    "motion_cos021_2",
                    "motion_denkigai2017w_2"
                };
                ArrayList arrayList = new ArrayList();
                foreach (string f_str_path in array)
                {
                    arrayList.AddRange(GameUty.FileSystem.GetList(f_str_path, AFileSystemBase.ListType.AllFile));
                }
                foreach (object obj in arrayList)
                {
                    string path = (string)obj;
                    bool flag2 = Path.GetExtension(path) == ".anm";
                    if (flag2)
                    {
                        ScriplayPlugin.motionNameAllList.Add(Path.GetFileNameWithoutExtension(path));
                    }
                }
            }
            else
            {
                foreach (string path2 in GameUty.FileSystem.GetFileListAtExtension(".anm"))
                {
                    ScriplayPlugin.motionNameAllList.Add(Path.GetFileNameWithoutExtension(path2));
                }
            }
            ScriplayPlugin.motionNameAllList.Sort();
            string str = "\r\n";
            foreach (string str2 in ScriplayPlugin.motionNameAllList)
            {
                str = str + str2 + "\r\n";
            }
            Util.info("모션 파일 읽기 종료");
            foreach (string text in ScriplayPlugin.motionNameAllList)
            {
                bool flag3 = text.Contains("zeccyou_f_once");
                if (flag3)
                {
                    ScriplayPlugin.zeccyou_fn_list.Add(text);
                }
            }
            ScriplayPlugin.zeccyou_fn_list.Sort();
        }

        // Token: 0x06000007 RID: 7 RVA: 0x000026C0 File Offset: 0x000008C0
        public void Start()
        {
        }

        // Token: 0x06000008 RID: 8 RVA: 0x000026C0 File Offset: 0x000008C0
        public void OnDestroy()
        {
        }



        // Token: 0x06000009 RID: 9 RVA: 0x000026C4 File Offset: 0x000008C4
        private void OnLevelWasLoaded(int level)
        {
            this.initMaidList();
            this.gameCfg_isPluginEnabledScene = true;
            //bool flag = level == ScriplayPlugin.cfg.studioModeSceneLevel;
            //if (flag)
            //{
            //    this.gameCfg_isPluginEnabledScene = true;
            //    this.initMaidList();
            //}
            //else
            //{
            //    this.gameCfg_isPluginEnabledScene = false;
            //}
        }

        // Token: 0x0600000A RID: 10 RVA: 0x00002700 File Offset: 0x00000900
        private bool isYotogiScene(int sceneLevel)
        {
            return GameMain.Instance.CharacterMgr.GetMaidCount() != 0;
        }

        // Token: 0x0600000B RID: 11 RVA: 0x00002724 File Offset: 0x00000924
        private void OnGUI()
        {
            bool flag = !this.gameCfg_isPluginEnabledScene;
            if (!flag)
            {
                GUIStyle guistyle = new GUIStyle("box");
                guistyle.fontSize = 11;
                guistyle.alignment = TextAnchor.UpperLeft;
                bool scriptFinished = this.scriplayContext.scriptFinished;
                if (scriptFinished)
                {
                    this.node_main = GUI.Window(21, this.node_main, new GUI.WindowFunction(this.WindowCallback_mainUI), ScriplayPlugin.cfg.PluginName + " Main UI", guistyle);
                }
                bool flag2 = !this.scriplayContext.scriptFinished;
                if (flag2)
                {
                    this.node_showArea = GUI.Window(22, this.node_showArea, new GUI.WindowFunction(this.WindowCallback_showArea), "", guistyle);
                }
                bool flag3 = this.en_showScripts && this.scriplayContext.scriptFinished;
                if (flag3)
                {
                    this.node_scripts = GUI.Window(23, this.node_scripts, new GUI.WindowFunction(this.WindowCallback_scriptsView), ScriplayPlugin.cfg.PluginName + "スクリプト一覧", guistyle);
                }
            }
        }

        // Token: 0x0600000C RID: 12 RVA: 0x00002834 File Offset: 0x00000A34
        private ScriplayPlugin()
        {
            ScriplayPlugin.instance = this;
        }

        // Token: 0x0600000D RID: 13 RVA: 0x00002A74 File Offset: 0x00000C74
        private void WindowCallback_scriptsView(int id)
        {
            GUILayout.Space(20f);
            this.scriptsList_scrollPosition = GUILayout.BeginScrollView(this.scriptsList_scrollPosition, new GUILayoutOption[0]);
            foreach (string text in this.scripts_fullpathList)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
                bool flag = GUILayout.Button(fileNameWithoutExtension, this.gsButton, new GUILayoutOption[0]);
                if (flag)
                {
                    this.scriplayContext = ScriplayContext.readScriptFile(text);
                }
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        // Token: 0x0600000E RID: 14 RVA: 0x00002B20 File Offset: 0x00000D20
        private void WindowCallback_showArea(int id)
        {
            GUIStyle guistyle = new GUIStyle("button");
            guistyle.fontSize = 10;
            guistyle.alignment = TextAnchor.MiddleCenter;
            guistyle.fixedWidth = 100f;
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            bool flag = GUILayout.Button("Stop Script", guistyle, new GUILayoutOption[0]);
            if (flag)
            {
                this.scriplayContext.scriptFinished = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
            this.showArea_scrollPosition = GUILayout.BeginScrollView(this.showArea_scrollPosition, new GUILayoutOption[0]);
            bool flag2 = !this.scriplayContext.showText.Equals("");
            if (flag2)
            {
                GUILayout.Label(this.scriplayContext.showText, this.gsLabel, new GUILayoutOption[0]);
            }
            foreach (ScriplayContext.Selection selection in this.scriplayContext.selection_selectionList)
            {
                bool flag3 = GUILayout.Button(selection.viewStr, this.gsButton, new GUILayoutOption[0]);
                if (flag3)
                {
                    this.scriplayContext.selection_selectedItem = selection;
                }
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        // Token: 0x0600000F RID: 15 RVA: 0x00002C70 File Offset: 0x00000E70
        private void initGuiStyle()
        {
            this.gsLabelTitle.fontSize = 14;
            this.gsLabelTitle.alignment = TextAnchor.MiddleCenter;
            this.gsLabel.fontSize = 12;
            this.gsLabel.alignment = TextAnchor.MiddleLeft;
            this.gsLabelSmall.fontSize = 10;
            this.gsLabelSmall.alignment = TextAnchor.MiddleLeft;
            this.gsButton.fontSize = 12;
            this.gsButton.alignment = TextAnchor.MiddleCenter;
            this.gsButtonSmall.fontSize = 10;
            this.gsButtonSmall.alignment = TextAnchor.MiddleCenter;
        }

        // Token: 0x06000010 RID: 16 RVA: 0x00002D08 File Offset: 0x00000F08
        private void WindowCallback_mainUI(int id)
        {
            GUILayout.Space(20f);
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            bool flag = GUILayout.Button("Reload Maid", this.gsButtonSmall, new GUILayoutOption[0]);
            if (flag)
            {
                this.initMaidList();
            }
            bool flag2 = GUILayout.Button("Reload CSV", this.gsButtonSmall, new GUILayoutOption[0]);
            if (flag2)
            {
                this.load_ConfigCsv();
            }
            bool flag3 = GUILayout.Button("Show Scripts", this.gsButtonSmall, new GUILayoutOption[0]);
            if (flag3)
            {
                this.en_showScripts = !this.en_showScripts;
                bool flag4 = this.en_showScripts;
                if (flag4)
                {
                    this.scripts_fullpathList = Util.getFileFullpathList(ScriplayPlugin.cfg.scriptsPath, "md");
                    Util.info(string.Format("다음 스크립트 발견", new object[0]));
                    foreach (string message in this.scripts_fullpathList)
                    {
                        Util.info(message);
                    }
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
            this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition, new GUILayoutOption[0]);
            GUILayout.Label("스크립트", this.gsLabel, new GUILayoutOption[0]);
            GUILayout.Label(string.Format("스크립트 이름：{0}", this.scriplayContext.scriptName), this.gsLabelSmall, new GUILayoutOption[0]);
            GUILayout.Label(string.Format("상태：{0}", this.scriplayContext.scriptFinished ? "스크립트 종료" : "스크립트 실행중"), this.gsLabelSmall, new GUILayoutOption[0]);
            GUILayout.Label(string.Format("현재 실행 행：{0}", this.scriplayContext.currentExecuteLine), this.gsLabelSmall, new GUILayoutOption[0]);
            GUILayout.Space(10f);
            GUILayout.Label("음성 재생", this.gsLabelSmall, new GUILayoutOption[0]);
            this.debug_playVoice = GUILayout.TextField(this.debug_playVoice, new GUILayoutOption[0]);
            bool flag5 = Event.current.keyCode == KeyCode.Return && this.debug_playVoice != "";
            if (flag5)
            {
                ScriplayPlugin.maidList[0].maid.AudioMan.LoadPlay(this.debug_playVoice, 0f, false, false);
                this.debug_playVoiceQueue.Enqueue(this.debug_playVoice);
                bool flag6 = this.debug_playVoiceQueue.Count > 3;
                if (flag6)
                {
                    this.debug_playVoiceQueue.Dequeue();
                }
                this.debug_playVoice = "";
            }
            foreach (string text in this.debug_playVoiceQueue)
            {
                bool flag7 = GUILayout.Button(text, this.gsButtonSmall, new GUILayoutOption[0]);
                if (flag7)
                {
                    ScriplayPlugin.maidList[0].maid.AudioMan.LoadPlay(text, 0f, false, false);
                }
            }
            GUILayout.Label("모션 재생", this.gsLabelSmall, new GUILayoutOption[0]);
            this.debug_playMotion = GUILayout.TextField(this.debug_playMotion, new GUILayoutOption[0]);
            bool flag8 = Event.current.keyCode == KeyCode.Return && this.debug_playMotion != "";
            if (flag8)
            {
                Util.animate(ScriplayPlugin.maidList[0].maid, this.debug_playMotion, true, 0.5f, 1f, false);
                this.debug_playMotionQueue.Enqueue(this.debug_playMotion);
                bool flag9 = this.debug_playMotionQueue.Count > 3;
                if (flag9)
                {
                    this.debug_playMotionQueue.Dequeue();
                }
                this.debug_playMotion = "";
            }
            foreach (string text2 in this.debug_playMotionQueue)
            {
                bool flag10 = GUILayout.Button(text2, this.gsButtonSmall, new GUILayoutOption[0]);
                if (flag10)
                {
                    Util.animate(ScriplayPlugin.maidList[0].maid, text2, true, 0.5f, 1f, false);
                }
            }
            GUILayout.Label("표정 재생", this.gsLabelSmall, new GUILayoutOption[0]);
            this.debug_face = GUILayout.TextField(this.debug_face, new GUILayoutOption[0]);
            bool flag11 = Event.current.keyCode == KeyCode.Return && this.debug_face != "";
            if (flag11)
            {
                ScriplayPlugin.maidList[0].change_faceAnime(this.debug_face, -1f);
                this.debug_faceQueue.Enqueue(this.debug_face);
                bool flag12 = this.debug_faceQueue.Count > 3;
                if (flag12)
                {
                    this.debug_faceQueue.Dequeue();
                }
                this.debug_face = "";
            }
            foreach (string text3 in this.debug_faceQueue)
            {
                bool flag13 = GUILayout.Button(text3, this.gsButtonSmall, new GUILayoutOption[0]);
                if (flag13)
                {
                    ScriplayPlugin.maidList[0].change_faceAnime(text3, -1f);
                }
            }
            GUILayout.Label("토스트 재생", this.gsLabelSmall, new GUILayoutOption[0]);
            this.debug_toast = GUILayout.TextField(this.debug_toast, new GUILayoutOption[0]);
            bool flag14 = Event.current.keyCode == KeyCode.Return && this.debug_toast != "";
            if (flag14)
            {
                ScriplayPlugin.toast(this.debug_toast);
                this.debug_toastQueue.Enqueue(this.debug_toast);
                bool flag15 = this.debug_toastQueue.Count > 3;
                if (flag15)
                {
                    this.debug_toastQueue.Dequeue();
                }
                this.debug_toast = "";
            }
            foreach (string text4 in this.debug_toastQueue)
            {
                bool flag16 = GUILayout.Button(text4, this.gsButtonSmall, new GUILayoutOption[0]);
                if (flag16)
                {
                    ScriplayPlugin.toast(text4);
                }
            }
            GUILayout.Label("스크립트 실행", this.gsLabelSmall, new GUILayoutOption[0]);
            bool flag17 = !this.scriplayContext.scriptFinished;
            if (flag17)
            {
                GUILayout.Label("\u3000（스크립트 실행중）", this.gsLabelSmall, new GUILayoutOption[0]);
            }
            else
            {
                this.debug_script = GUILayout.TextField(this.debug_script, new GUILayoutOption[0]);
                bool flag18 = Event.current.keyCode == KeyCode.Return && this.debug_script != "";
                if (flag18)
                {
                    this.scriplayContext = ScriplayContext.readScriptFile("스크립트 실행 테스트", this.debug_script.Split(new string[]
                    {
                        "\r\n"
                    }, StringSplitOptions.None));
                    this.debug_scriptQueue.Enqueue(this.debug_script);
                    bool flag19 = this.debug_scriptQueue.Count > 3;
                    if (flag19)
                    {
                        this.debug_scriptQueue.Dequeue();
                    }
                    this.debug_script = "";
                }
                foreach (string text5 in this.debug_scriptQueue)
                {
                    bool flag20 = GUILayout.Button(text5, this.gsButtonSmall, new GUILayoutOption[0]);
                    if (flag20)
                    {
                        this.scriplayContext = ScriplayContext.readScriptFile("스크립트 실행 테스트", text5.Split(new string[]
                        {
                            "\r\n"
                        }, StringSplitOptions.None));
                    }
                }
            }
            GUILayout.Label("각 Table 확인", this.gsLabelSmall, new GUILayoutOption[0]);
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label("Personal", this.gsLabelSmall, new GUILayoutOption[0]);
            GUILayout.Label((ScriplayPlugin.maidList.Count != 0) ? ScriplayPlugin.maidList[0].sPersonal : "메이드가로드되지 않습니다", this.gsLabelSmall, new GUILayoutOption[0]);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label("Category", this.gsLabelSmall, new GUILayoutOption[0]);
            this.debug_ovtQueryMap["Category"] = GUILayout.TextField(this.debug_ovtQueryMap["Category"], new GUILayoutOption[0]);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            bool flag21 = GUILayout.Button("OnceVoice", this.gsButtonSmall, new GUILayoutOption[0]);
            if (flag21)
            {
                this.debug_ovtQueryMap["Personal"] = ScriplayPlugin.maidList[0].sPersonal;
                StringBuilder stringBuilder = new StringBuilder();
                foreach (string str in ScriplayPlugin.OnceVoiceTable.queryTable(this.debug_ovtQueryMap["Personal"], this.debug_ovtQueryMap["Category"]))
                {
                    stringBuilder.Append(str + ",");
                }
                Util.info(string.Format("OnceVoiceTable\u3000クエリ結果 {0},{1} \r\n {2}", this.debug_ovtQueryMap["Personal"], this.debug_ovtQueryMap["Category"], stringBuilder.ToString()));
            }
            bool flag22 = GUILayout.Button("LoopVoice", this.gsButtonSmall, new GUILayoutOption[0]);
            if (flag22)
            {
                this.debug_ovtQueryMap["Personal"] = ScriplayPlugin.maidList[0].sPersonal;
                StringBuilder stringBuilder2 = new StringBuilder();
                foreach (string str2 in ScriplayPlugin.LoopVoiceTable.queryTable(this.debug_ovtQueryMap["Personal"], this.debug_ovtQueryMap["Category"]))
                {
                    stringBuilder2.Append(str2 + ",");
                }
                Util.info(string.Format("LoopVoiceTable\u3000クエリ結果 {0},{1} \r\n {2}", this.debug_ovtQueryMap["Personal"], this.debug_ovtQueryMap["Category"], stringBuilder2.ToString()));
            }
            bool flag23 = GUILayout.Button("Motion", this.gsButtonSmall, new GUILayoutOption[0]);
            if (flag23)
            {
                this.debug_ovtQueryMap["Personal"] = ScriplayPlugin.maidList[0].sPersonal;
                StringBuilder stringBuilder3 = new StringBuilder();
                foreach (ScriplayPlugin.MotionInfo motionInfo in ScriplayPlugin.MotionTable.queryTable_motionNameBase(this.debug_ovtQueryMap["Category"], "-"))
                {
                    stringBuilder3.Append(motionInfo.motionName + ",");
                }
                Util.info(string.Format("MotionTable\u3000쿼리 결과 {0}  \r\n {1}", this.debug_ovtQueryMap["Category"], stringBuilder3.ToString()));
            }
            bool flag24 = GUILayout.Button("Face", this.gsButtonSmall, new GUILayoutOption[0]);
            if (flag24)
            {
                this.debug_ovtQueryMap["Personal"] = ScriplayPlugin.maidList[0].sPersonal;
                StringBuilder stringBuilder4 = new StringBuilder();
                foreach (string str3 in ScriplayPlugin.FaceTable.queryTable(this.debug_ovtQueryMap["Category"]))
                {
                    stringBuilder4.Append(str3 + ",");
                }
                Util.info(string.Format("FaceTable\u3000쿼리 결과 {0}  \r\n {1}", this.debug_ovtQueryMap["Category"], stringBuilder4.ToString()));
            }
            GUILayout.EndHorizontal();
            bool flag25 = ScriplayPlugin.maidList.Count != 0;
            if (flag25)
            {
                ScriplayPlugin.IMaid maid = ScriplayPlugin.maidList[0];
                GUILayout.Label("メインメイド状態", this.gsLabelSmall, new GUILayoutOption[0]);
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add("性格", maid.sPersonal);
                dictionary.Add("再生中ボイス", maid.getPlayingVoice());
                dictionary.Add("재생 중 모션", maid.getCurrentMotionName());
                bool flag26 = GUILayout.Button("潮", this.gsButtonSmall, new GUILayoutOption[0]);
                if (flag26)
                {
                    maid.change_sio();
                }
                bool flag27 = GUILayout.Button("尿", this.gsButtonSmall, new GUILayoutOption[0]);
                if (flag27)
                {
                    maid.change_nyo();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(10f);
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        // Token: 0x06000011 RID: 17 RVA: 0x00003A84 File Offset: 0x00001C84
        public void Update()
        {
            bool flag = !this.gameCfg_isPluginEnabledScene;
            if (!flag)
            {
                bool flag2 = GameMain.Instance.CharacterMgr.GetMaid(ScriplayPlugin.maidList.Count) != null || (ScriplayPlugin.maidList.Count > 0 && GameMain.Instance.CharacterMgr.GetMaid(ScriplayPlugin.maidList.Count - 1) == null);
                if (flag2)
                {
                    this.initMaidList();
                }
                bool flag3 = !this.scriplayContext.scriptFinished;
                if (flag3)
                {
                    this.scriplayContext.Update();
                }
                foreach (ScriplayPlugin.IMaid maid in ScriplayPlugin.maidList)
                {
                    Util.sw_start("");
                    maid.update_playing();
                    Util.sw_showTime("update_playing");
                    maid.update_eyeToCam();
                    maid.update_headToCam();
                    Util.sw_showTime("update_eyeHeadCamera");
                    Util.sw_stop("");
                }
            }
        }

        // Token: 0x06000012 RID: 18 RVA: 0x000026C0 File Offset: 0x000008C0
        public void LateUpdate()
        {
        }

        // Token: 0x06000013 RID: 19 RVA: 0x00003BAC File Offset: 0x00001DAC
        public static void change_SE(string seFileName)
        {
            GameMain.Instance.SoundMgr.StopSe();
            bool flag = seFileName != "";
            if (flag)
            {
                GameMain.Instance.SoundMgr.PlaySe(seFileName, true);
            }
        }

        // Token: 0x06000014 RID: 20 RVA: 0x00003BEB File Offset: 0x00001DEB
        public void change_SE_vibeLow()
        {
            ScriplayPlugin.change_SE("se020.ogg");
        }

        // Token: 0x06000015 RID: 21 RVA: 0x00003BF9 File Offset: 0x00001DF9
        public void change_SE_vibeHigh()
        {
            ScriplayPlugin.change_SE("se019.ogg");
        }

        // Token: 0x06000016 RID: 22 RVA: 0x00003C07 File Offset: 0x00001E07
        public void change_SE_stop()
        {
            ScriplayPlugin.change_SE("");
        }

        // Token: 0x06000017 RID: 23 RVA: 0x00003C15 File Offset: 0x00001E15
        public void change_SE_insertLow()
        {
            ScriplayPlugin.change_SE("se029.ogg");
        }

        // Token: 0x06000018 RID: 24 RVA: 0x00003C23 File Offset: 0x00001E23
        public void change_SE_insertHigh()
        {
            ScriplayPlugin.change_SE("se028.ogg");
        }

        // Token: 0x06000019 RID: 25 RVA: 0x00003C31 File Offset: 0x00001E31
        public void change_SE_slapLow()
        {
            ScriplayPlugin.change_SE("se012.ogg");
        }

        // Token: 0x0600001A RID: 26 RVA: 0x00003C3F File Offset: 0x00001E3F
        public void change_SE_slapHigh()
        {
            ScriplayPlugin.change_SE("se013.ogg");
        }

        // Token: 0x0600001B RID: 27 RVA: 0x00003C4D File Offset: 0x00001E4D
        public static void toast(string message)
        {
            ScriplayPlugin.ToastUtil.Toast<string>(ScriplayPlugin.instance, message);
        }

        // Token: 0x04000001 RID: 1
        public static ScriplayPlugin.ScriplayConfig cfg = null;

        // Token: 0x04000002 RID: 2
        public static List<string> zeccyou_fn_list = new List<string>();

        // Token: 0x04000003 RID: 3
        public static List<string> motionNameAllList = new List<string>();

        // Token: 0x04000004 RID: 4
        public static HashSet<string> motionCategorySet = new HashSet<string>();

        // Token: 0x04000005 RID: 5
        private Transform[] maidHead = new Transform[20];

        // Token: 0x04000006 RID: 6
        public static List<ScriplayPlugin.IMaid> maidList = new List<ScriplayPlugin.IMaid>();

        // Token: 0x04000007 RID: 7
        public static List<ScriplayPlugin.IMan> manList = new List<ScriplayPlugin.IMan>();

        // Token: 0x04000008 RID: 8
        private bool gameCfg_isChuBLipEnabled = false;

        // Token: 0x04000009 RID: 9
        private bool gameCfg_isVREnabled = false;

        // Token: 0x0400000A RID: 10
        private bool gameCfg_isPluginEnabledScene = false;

        // Token: 0x0400000B RID: 11
        private static Dictionary<string, string> motionBaseRegexDefDic = new Dictionary<string, string>
        {
            {
                "_[123]_.*",
                "_"
            },
            {
                "_[123]\\w\\d\\d.*",
                "_"
            },
            {
                "\\.anm",
                ""
            },
            {
                "_asiname_.*",
                "_"
            },
            {
                "_cli[\\d]?_.*",
                "_"
            },
            {
                "_daki_.*",
                "_"
            },
            {
                "_fera_.*",
                "_"
            },
            {
                "_gr_.*",
                "_"
            },
            {
                "_housi_.*",
                "_"
            },
            {
                "_hibu_.*",
                "_"
            },
            {
                "_hibuhiraki_.*",
                "_"
            },
            {
                "_ir_.*",
                "_"
            },
            {
                "_kakae_.*",
                "_"
            },
            {
                "_kuti_.*",
                "_"
            },
            {
                "_kiss_.*",
                "_"
            },
            {
                "_momi_.*",
                "_"
            },
            {
                "_onani_.*",
                "_"
            },
            {
                "_oku_.*",
                "_"
            },
            {
                "_peace_.*",
                "_"
            },
            {
                "_ran4p_.*",
                "_"
            },
            {
                "_ryoutenaburi_.*",
                "_"
            },
            {
                "_siri_.*",
                "_"
            },
            {
                "_sissin_.*",
                "_"
            },
            {
                "_shasei_.*",
                "_"
            },
            {
                "_shaseigo_.*",
                "_"
            },
            {
                "_sixnine_.*",
                "_"
            },
            {
                "_surituke_.*",
                "_"
            },
            {
                "_siriname_.*",
                "_"
            },
            {
                "_taiki_.*",
                "_"
            },
            {
                "_tikubi_.*",
                "_"
            },
            {
                "_tekoki_.*",
                "_"
            },
            {
                "_tikubiname_.*",
                "_"
            },
            {
                "_tati_.*",
                "_"
            },
            {
                "_ubi\\d?_.*",
                "_"
            },
            {
                "_vibe_.*",
                "_"
            },
            {
                "_zeccyougo_.*",
                "_"
            },
            {
                "_zeccyou_.*",
                "_"
            },
            {
                "_zikkyou_.*",
                "_"
            }
        };

        // Token: 0x0400000C RID: 12
        private static Dictionary<Regex, string> motionBaseRegexDic = new Dictionary<Regex, string>();

        // Token: 0x0400000D RID: 13
        private static readonly float UIwidth = 300f;

        // Token: 0x0400000E RID: 14
        private static readonly float UIheight = 400f;

        // Token: 0x0400000F RID: 15
        private static readonly float UIposX_rightMargin = 10f;

        // Token: 0x04000010 RID: 16
        private static readonly float UIposY_bottomMargin = 120f;

        // Token: 0x04000011 RID: 17
        private Rect node_main = new Rect((float)Screen.width - ScriplayPlugin.UIposX_rightMargin - ScriplayPlugin.UIwidth, (float)Screen.height - ScriplayPlugin.UIposY_bottomMargin - ScriplayPlugin.UIheight, ScriplayPlugin.UIwidth, ScriplayPlugin.UIheight);

        // Token: 0x04000012 RID: 18
        private Rect node_scripts = new Rect((float)Screen.width - ScriplayPlugin.UIposX_rightMargin - ScriplayPlugin.UIwidth, (float)Screen.height - (ScriplayPlugin.UIheight + ScriplayPlugin.UIposY_bottomMargin) - ScriplayPlugin.UIheight, ScriplayPlugin.UIwidth, ScriplayPlugin.UIheight);

        // Token: 0x04000013 RID: 19
        private static readonly float UIwidth_showArea = 400f;

        // Token: 0x04000014 RID: 20
        private static readonly float UIheight_showArea = 300f;

        // Token: 0x04000015 RID: 21
        private static readonly float UIposX_rightMargin_showArea = 10f;

        // Token: 0x04000016 RID: 22
        private static readonly float UIposY_bottomMargin_showArea = 10f;

        // Token: 0x04000017 RID: 23
        private Rect node_showArea = new Rect((float)Screen.width - ScriplayPlugin.UIposX_rightMargin_showArea - ScriplayPlugin.UIwidth_showArea, (float)Screen.height - ScriplayPlugin.UIposY_bottomMargin_showArea - ScriplayPlugin.UIheight_showArea, ScriplayPlugin.UIwidth_showArea, ScriplayPlugin.UIheight_showArea);

        // Token: 0x04000018 RID: 24
        private Vector2 scrollPosition = default(Vector2);

        // Token: 0x04000019 RID: 25
        private Queue<string> debug_strQueue = new Queue<string>();

        // Token: 0x0400001A RID: 26
        private Dictionary<string, string> debug_ovtQueryMap = new Dictionary<string, string>
        {
            {
                "Personal",
                ""
            },
            {
                "Category",
                ""
            }
        };

        // Token: 0x0400001B RID: 27
        private string debug_toast = "";

        // Token: 0x0400001C RID: 28
        private Queue<string> debug_toastQueue = new Queue<string>();

        // Token: 0x0400001D RID: 29
        private string debug_playVoice = "";

        // Token: 0x0400001E RID: 30
        private Queue<string> debug_playVoiceQueue = new Queue<string>();

        // Token: 0x0400001F RID: 31
        private string debug_playMotion = "";

        // Token: 0x04000020 RID: 32
        private Queue<string> debug_playMotionQueue = new Queue<string>();

        // Token: 0x04000021 RID: 33
        private string debug_face = "";

        // Token: 0x04000022 RID: 34
        private Queue<string> debug_faceQueue = new Queue<string>();

        // Token: 0x04000023 RID: 35
        private string debug_script = "";

        // Token: 0x04000024 RID: 36
        private Queue<string> debug_scriptQueue = new Queue<string>();

        // Token: 0x04000025 RID: 37
        private ScriplayContext scriplayContext = ScriplayContext.None;

        // Token: 0x04000026 RID: 38
        private bool en_showScripts = false;

        // Token: 0x04000027 RID: 39
        private Vector2 scriptsList_scrollPosition = default(Vector2);

        // Token: 0x04000028 RID: 40
        private List<string> scripts_fullpathList = new List<string>();

        // Token: 0x04000029 RID: 41
        private static MonoBehaviour instance;

        // Token: 0x0400002A RID: 42
        private Vector2 showArea_scrollPosition = default(Vector2);

        // Token: 0x0400002B RID: 43
        private GUIStyle gsLabelTitle = new GUIStyle("label");

        // Token: 0x0400002C RID: 44
        private GUIStyle gsLabel = new GUIStyle("label");

        // Token: 0x0400002D RID: 45
        private GUIStyle gsLabelSmall = new GUIStyle("label");

        // Token: 0x0400002E RID: 46
        private GUIStyle gsButton = new GUIStyle("button");

        // Token: 0x0400002F RID: 47
        private GUIStyle gsButtonSmall = new GUIStyle("button");

        // Token: 0x04000030 RID: 48
        public static ScriplayPlugin.VoiceTable OnceVoiceTable = new ScriplayPlugin.VoiceTable("OnceVoice");

        // Token: 0x04000031 RID: 49
        public static ScriplayPlugin.VoiceTable LoopVoiceTable = new ScriplayPlugin.VoiceTable("LoopVoice");

        // Token: 0x02000005 RID: 5
        public class ScriplayConfig
        {
            // Token: 0x04000071 RID: 113
            internal bool debugMode = true;

            // Token: 0x04000072 RID: 114
            internal readonly float faceAnimeFadeTime = 1f;

            // Token: 0x04000073 RID: 115
            internal readonly string csvPath = "Sybaris\\UnityInjector\\Config\\Scriplay\\csv\\";

            // Token: 0x04000074 RID: 116
            internal readonly string scriptsPath = "Sybaris\\UnityInjector\\Config\\Scriplay\\scripts\\";

            // Token: 0x04000075 RID: 117
            internal string onceVoicePrefix = "oncevoice_";

            // Token: 0x04000076 RID: 118
            internal string loopVoicePrefix = "loopvoice_";

            // Token: 0x04000077 RID: 119
            internal string motionListPrefix = "motion_";

            // Token: 0x04000078 RID: 120
            internal string faceListPrefix = "face_";

            // Token: 0x04000079 RID: 121
            internal string PluginName = "Scriplay";

            // Token: 0x0400007A RID: 122
            internal string debugPrintColor = "red";

            // Token: 0x0400007B RID: 123
            internal bool enModMotionLoad = false;

            // Token: 0x0400007C RID: 124
            internal float sio_baseTime = 1f;

            // Token: 0x0400007D RID: 125
            internal float nyo_baseTime = 3f;

            // Token: 0x0400007E RID: 126
            internal int studioModeSceneLevel = 26;
        }

        // Token: 0x02000006 RID: 6
        public class IMaid
        {
            // Token: 0x0600004B RID: 75 RVA: 0x00006574 File Offset: 0x00004774
            public IMaid(int maidNo, Maid maid)
            {
                this.maid = maid;
                this.sPersonal = maid.status.personal.uniqueName;
                this.maidNo = maidNo;
                this.maid.EyeToCamera(Maid.EyeMoveType.目と顔を向ける, 0.8f);
            }

            // Token: 0x0600004C RID: 76 RVA: 0x00006630 File Offset: 0x00004830
            public bool isPlayingVoice()
            {
                return this.maid.AudioMan.audiosource.isPlaying;
            }

            // Token: 0x0600004D RID: 77 RVA: 0x00006658 File Offset: 0x00004858
            public bool isPlayingMotion()
            {
                return this.maid.body0.m_Animation.isPlaying;
            }

            // Token: 0x0600004E RID: 78 RVA: 0x00006680 File Offset: 0x00004880
            public string getPlayingVoice()
            {
                bool flag = !this.maid.AudioMan.isPlay();
                string result;
                if (flag)
                {
                    result = "";
                }
                else
                {
                    result = this.maid.AudioMan.FileName;
                }
                return result;
            }

            // Token: 0x0600004F RID: 79 RVA: 0x000066C4 File Offset: 0x000048C4
            public Vector3 change_positionRelative(float x = 0f, float y = 0f, float z = 0f)
            {
                Vector3 position = this.maid.transform.position;
                this.maid.transform.position = new Vector3(position.x + x, position.y + y, position.z + z);
                return this.maid.transform.position;
            }

            // Token: 0x06000050 RID: 80 RVA: 0x00006728 File Offset: 0x00004928
            public Vector3 change_positionAbsolute(float x = 0f, float y = 0f, float z = 0f, bool keepX = false, bool keepY = false, bool keepZ = false)
            {
                Vector3 eulerAngles = this.maid.transform.eulerAngles;
                this.maid.transform.position = new Vector3(keepX ? eulerAngles.x : x, keepY ? eulerAngles.y : y, keepZ ? eulerAngles.z : z);
                return this.maid.transform.position;
            }

            // Token: 0x06000051 RID: 81 RVA: 0x00006798 File Offset: 0x00004998
            public Vector3 getPosition()
            {
                return this.maid.transform.position;
            }

            // Token: 0x06000052 RID: 82 RVA: 0x000067BC File Offset: 0x000049BC
            public Vector3 change_angleAbsolute(float x_deg = 0f, float y_deg = 0f, float z_deg = 0f, bool keepX = false, bool keepY = false, bool keepZ = false)
            {
                Vector3 eulerAngles = this.maid.transform.eulerAngles;
                this.maid.transform.eulerAngles = new Vector3(keepX ? eulerAngles.x : x_deg, keepY ? eulerAngles.y : y_deg, keepZ ? eulerAngles.z : z_deg);
                return this.maid.transform.eulerAngles;
            }

            // Token: 0x06000053 RID: 83 RVA: 0x0000682C File Offset: 0x00004A2C
            public Vector3 change_angleRelative(float x_deg = 0f, float y_deg = 0f, float z_deg = 0f)
            {
                Vector3 eulerAngles = this.maid.transform.eulerAngles;
                this.maid.transform.eulerAngles = new Vector3(eulerAngles.x + x_deg, eulerAngles.y + y_deg, eulerAngles.z + z_deg);
                return this.maid.transform.eulerAngles;
            }

            // Token: 0x06000054 RID: 84 RVA: 0x00006890 File Offset: 0x00004A90
            public Vector3 getAngle()
            {
                return this.maid.transform.eulerAngles;
            }

            // Token: 0x06000055 RID: 85 RVA: 0x000068B4 File Offset: 0x00004AB4
            public void change_faceAnime(string faceAnime, float fadeTime = -1f)
            {
                bool flag = this.currentFaceAnime == faceAnime;
                if (!flag)
                {
                    bool flag2 = fadeTime == -1f;
                    if (flag2)
                    {
                        fadeTime = ScriplayPlugin.cfg.faceAnimeFadeTime * 2f;
                    }
                    this.currentFaceAnime = faceAnime;
                    this.maid.FaceAnime(this.currentFaceAnime, fadeTime, 0);
                }
            }

            // Token: 0x06000056 RID: 86 RVA: 0x00006910 File Offset: 0x00004B10
            public void change_faceAnime(List<string> faceAnimeList, float fadeTime = -1f)
            {
                string text = Util.pickOneOrEmptyString(faceAnimeList, -1);
                bool flag = text.Equals("");
                if (flag)
                {
                    Util.info("표정 목록이 비었습니다");
                }
                else
                {
                    this.change_faceAnime(text, fadeTime);
                }
            }

            // Token: 0x06000057 RID: 87 RVA: 0x0000694C File Offset: 0x00004B4C
            public string change_FaceBlend(int hoho = -1, int namida = -1, bool enableYodare = false)
            {
                hoho = Mathf.Clamp(hoho, -1, 3);
                namida = Mathf.Clamp(namida, -1, 3);
                string text = this.maid.FaceName3;
                bool flag = text.Equals("オリジナル");
                if (flag)
                {
                    text = "頬０涙０";
                }
                string str = this.reg_hoho.Match(text).Groups[1].Value;
                string str2 = this.reg_namida.Match(text).Groups[1].Value;
                string str3 = "";
                bool flag2 = hoho == 0;
                if (flag2)
                {
                    str = "頬０";
                }
                bool flag3 = hoho == 1;
                if (flag3)
                {
                    str = "頬１";
                }
                bool flag4 = hoho == 2;
                if (flag4)
                {
                    str = "頬２";
                }
                bool flag5 = hoho == 3;
                if (flag5)
                {
                    str = "頬３";
                }
                bool flag6 = namida == 0;
                if (flag6)
                {
                    str2 = "涙０";
                }
                bool flag7 = namida == 1;
                if (flag7)
                {
                    str2 = "涙１";
                }
                bool flag8 = namida == 2;
                if (flag8)
                {
                    str2 = "涙２";
                }
                bool flag9 = namida == 3;
                if (flag9)
                {
                    str2 = "涙３";
                }
                if (enableYodare)
                {
                    str3 = "よだれ";
                }
                string text2 = str + str2 + str3;
                this.maid.FaceBlend(text2);
                return text2;
            }

            // Token: 0x06000058 RID: 88 RVA: 0x00006A80 File Offset: 0x00004C80
            private void updateMaidEyePosY(float eyePosY)
            {
                eyePosY = Mathf.Clamp(eyePosY, 0f, 50f);
                Vector3 localPosition = this.maid.body0.trsEyeL.localPosition;
                Vector3 localPosition2 = this.maid.body0.trsEyeR.localPosition;
                this.maid.body0.trsEyeL.localPosition = new Vector3(localPosition.x, Math.Max(eyePosY / 5000f, 0f), localPosition.z);
                this.maid.body0.trsEyeR.localPosition = new Vector3(localPosition2.x, Math.Min(eyePosY / 5000f, 0f), localPosition2.z);
            }

            // Token: 0x06000059 RID: 89 RVA: 0x00006B3C File Offset: 0x00004D3C
            public void update_playing()
            {
                bool flag = this.loopVoiceBackuped;
                if (flag)
                {
                    bool flag2 = this.maid.AudioMan.audiosource.loop || (!this.maid.AudioMan.audiosource.loop && !this.maid.AudioMan.audiosource.isPlaying);
                    if (flag2)
                    {
                        this.change_LoopVoice(this.currentLoopVoice);
                        this.loopVoiceBackuped = false;
                    }
                }
                bool flag3 = !this.isPlayingMotion();
                if (flag3)
                {
                    List<ScriplayPlugin.IMaid.MotionAttribute> list = new List<ScriplayPlugin.IMaid.MotionAttribute>();
                    list.Add(ScriplayPlugin.IMaid.MotionAttribute.Taiki);
                    List<string> list2 = this.searchMotionList(this.getMotionNameBase(this.getCurrentMotionName()), list);
                    bool flag4 = list2.Count != 0;
                    if (flag4)
                    {
                        string motionNameOrNameBase = list2[0];
                        this.change_Motion(motionNameOrNameBase, true, false, -1f, -1f);
                    }
                }
            }

            // Token: 0x0600005A RID: 90 RVA: 0x00006C23 File Offset: 0x00004E23
            public void change_eyeToCam(ScriplayPlugin.IMaid.EyeHeadToCamState state)
            {
                this.eyeToCam_state = state;
            }

            // Token: 0x0600005B RID: 91 RVA: 0x00006C30 File Offset: 0x00004E30
            public void change_headToCam(ScriplayPlugin.IMaid.EyeHeadToCamState state, float fadeSec = -1f)
            {
                this.headToCam_state = state;
                bool flag = fadeSec != -1f;
                if (flag)
                {
                    this.maid.body0.HeadToCamFadeSpeed = fadeSec;
                }
            }

            // Token: 0x0600005C RID: 92 RVA: 0x00006C68 File Offset: 0x00004E68
            public void update_eyeToCam()
            {
                bool flag = this.eyeToCam_state == ScriplayPlugin.IMaid.EyeHeadToCamState.No;
                if (flag)
                {
                    this.maid.body0.boEyeToCam = false;
                }
                else
                {
                    bool flag2 = this.eyeToCam_state == ScriplayPlugin.IMaid.EyeHeadToCamState.Yes;
                    if (flag2)
                    {
                        this.maid.body0.boEyeToCam = true;
                    }
                    //else
                    //{
                    //    bool flag3 = this.eyeToCam_state == ScriplayPlugin.IMaid.EyeHeadToCamState.Auto;
                    //    if (flag3)
                    //    {
                    //        this.eyeToCam_turnSec -= Time.deltaTime;
                    //        bool flag4 = this.eyeToCam_turnSec > 0f;
                    //        if (!flag4)
                    //        {
                    //            bool flag5 = UnityEngine.Random.Range(0, 100) < 50;
                    //            if (flag5)
                    //            {
                    //                this.maid.body0.boEyeToCam = !this.maid.body0.boEyeToCam;
                    //            }
                    //            this.eyeToCam_turnSec = (float)UnityEngine.Random.Range(6, 10);
                    //        }
                    //    }
                    //}
                }
            }

            // Token: 0x0600005D RID: 93 RVA: 0x00006D40 File Offset: 0x00004F40
            public void update_headToCam()
            {
                bool flag = this.headToCam_state == ScriplayPlugin.IMaid.EyeHeadToCamState.No;
                if (flag)
                {
                    this.maid.body0.boHeadToCam = false;
                }
                else
                {
                    bool flag2 = this.headToCam_state == ScriplayPlugin.IMaid.EyeHeadToCamState.Yes;
                    if (flag2)
                    {
                        this.maid.body0.boHeadToCam = true;
                    }
                    //else
                    //{
                    //    bool flag3 = this.headToCam_state == ScriplayPlugin.IMaid.EyeHeadToCamState.Auto;
                    //    if (flag3)
                    //    {
                    //        this.headToCam_turnSec -= Time.deltaTime;
                    //        bool flag4 = this.headToCam_turnSec > 0f;
                    //        if (!flag4)
                    //        {
                    //            bool boHeadToCam = this.maid.body0.boHeadToCam;
                    //            if (boHeadToCam)
                    //            {
                    //                bool flag5 = UnityEngine.Random.Range(0, 100) < 70;
                    //                if (flag5)
                    //                {
                    //                    this.maid.body0.boHeadToCam = !this.maid.body0.boHeadToCam;
                    //                }
                    //            }
                    //            else
                    //            {
                    //                bool flag6 = UnityEngine.Random.Range(0, 100) < 30;
                    //                if (flag6)
                    //                {
                    //                    this.maid.body0.boHeadToCam = !this.maid.body0.boHeadToCam;
                    //                }
                    //            }
                    //            this.headToCam_turnSec = (float)UnityEngine.Random.Range(6, 10);
                    //        }
                    //    }
                    //}
                }
            }

            // Token: 0x0600005E RID: 94 RVA: 0x00006E74 File Offset: 0x00005074
            public string getMotionNameBase(string motionName)
            {
                bool flag = motionName == null;
                string result;
                if (flag)
                {
                    result = motionName;
                }
                else
                {
                    string text = motionName;
                    foreach (KeyValuePair<Regex, string> keyValuePair in ScriplayPlugin.motionBaseRegexDic)
                    {
                        Regex key = keyValuePair.Key;
                        text = key.Replace(text, keyValuePair.Value);
                    }
                    Util.debug(string.Format("getMotionNameBase {0} -> {1}", motionName, text));
                    result = text;
                }
                return result;
            }

            // Token: 0x0600005F RID: 95 RVA: 0x00006F04 File Offset: 0x00005104
            public List<string> searchMotionList(string motionNameBase, List<ScriplayPlugin.IMaid.MotionAttribute> attributeList = null)
            {
                List<string> list = new List<string>();
                foreach (ScriplayPlugin.IMaid.MotionAttribute motionAttribute in attributeList)
                {
                    list.Add(motionAttribute.attributeStr);
                }
                return this.searchMotionList(motionNameBase, list);
            }

            // Token: 0x06000060 RID: 96 RVA: 0x00006F70 File Offset: 0x00005170
            public List<string> searchMotionList(string motionName, List<string> attributeList = null)
            {
                string motionNameBase = this.getMotionNameBase(motionName);
                bool flag = attributeList == null;
                if (flag)
                {
                    attributeList = new List<string>();
                }
                List<string> list = new List<string>();
                bool flag2 = motionNameBase == null;
                List<string> result;
                if (flag2)
                {
                    result = list;
                }
                else
                {
                    foreach (string text in ScriplayPlugin.motionNameAllList)
                    {
                        bool flag3 = !text.StartsWith(motionNameBase);
                        if (!flag3)
                        {
                            foreach (string value in attributeList)
                            {
                                bool flag4 = text.Contains(value);
                                if (flag4)
                                {
                                    list.Add(text);
                                    break;
                                }
                            }
                        }
                    }
                    bool flag5 = list.Count == 0;
                    if (flag5)
                    {
                        Util.info(string.Format("有効なモーションがありませんでした : {0} \r\n motionBaseName : {1}", this.maid.name, motionNameBase));
                    }
                    result = list;
                }
                return result;
            }

            // Token: 0x06000061 RID: 97 RVA: 0x00007090 File Offset: 0x00005290
            public string getCurrentMotionName()
            {
                return this.maid.body0.LastAnimeFN;
            }

            // Token: 0x06000062 RID: 98 RVA: 0x000070B4 File Offset: 0x000052B4
            public string change_Motion(string motionNameOrNameBase, bool isLoop = true, bool addQue = false, float motionSpeed = -1f, float fadeTime = -1f)
            {
                bool flag = motionSpeed == -1f;
                if (flag)
                {
                    motionSpeed = Util.var20p(1f);
                }
                bool flag2 = fadeTime == -1f;
                if (flag2)
                {
                    fadeTime = Util.var20p(0.8f);
                }
                bool flag3 = !motionNameOrNameBase.EndsWith(".anm");
                if (flag3)
                {
                    motionNameOrNameBase += ".anm";
                }
                bool flag4 = motionNameOrNameBase == this.getCurrentMotionName();
                string result;
                if (flag4)
                {
                    result = motionNameOrNameBase;
                }
                else
                {
                    Util.animate(this.maid, motionNameOrNameBase, isLoop, fadeTime, motionSpeed, addQue);
                    result = motionNameOrNameBase;
                }
                return result;
            }

            // Token: 0x06000063 RID: 99 RVA: 0x00007144 File Offset: 0x00005344
            public string change_Motion(List<string> motionList, bool isLoop = true, bool enSelectForVibeState = true)
            {
                int excludeValue = motionList.IndexOf(this.getCurrentMotionName());
                int index = Util.randomInt(0, motionList.Count - 1, excludeValue);
                return this.change_Motion(motionList[index], isLoop, enSelectForVibeState, -1f, -1f);
            }

            // Token: 0x06000064 RID: 100 RVA: 0x0000718C File Offset: 0x0000538C
            public string change_Motion(List<ScriplayPlugin.MotionInfo> motionList, bool isLoop = true, bool enSelectForVibeState = true)
            {
                List<string> list = new List<string>();
                foreach (ScriplayPlugin.MotionInfo motionInfo in motionList)
                {
                    list.Add(motionInfo.motionName);
                }
                return this.change_Motion(list, isLoop, enSelectForVibeState);
            }

            // Token: 0x06000065 RID: 101 RVA: 0x000071F8 File Offset: 0x000053F8
            public void change_nyo()
            {
                string f_strFileName = "SE011.ogg";
                GameMain.Instance.SoundMgr.PlaySe(f_strFileName, false);
                this.maid.AddPrefab("Particle/pNyou_cm3D2", "pNyou_cm3D2", "_IK_vagina", new Vector3(0f, -0.047f, 0.011f), new Vector3(20f, -180f, 180f));
            }

            // Token: 0x06000066 RID: 102 RVA: 0x00007264 File Offset: 0x00005464
            public void change_sio()
            {
                this.maid.AddPrefab("Particle/pSio2_cm3D2", "pSio2_cm3D2", "_IK_vagina", new Vector3(0f, 0f, -0.01f), new Vector3(0f, 180f, 0f));
            }

            // Token: 0x06000067 RID: 103 RVA: 0x000072B8 File Offset: 0x000054B8
            public void change_onceVoice(string voicename)
            {
                this.change_onceVoice(new List<string>
                {
                    voicename
                });
            }

            // Token: 0x06000068 RID: 104 RVA: 0x000072DC File Offset: 0x000054DC
            public void change_onceVoice(List<string> VoiceList)
            {
                this._playVoice(VoiceList.ToArray(), false, -1, -1);
            }

            // Token: 0x06000069 RID: 105 RVA: 0x000072EF File Offset: 0x000054EF
            public void change_LoopVoice(List<string> VoiceList)
            {
                this.currentLoopVoice = this._playVoice(VoiceList.ToArray(), true, -1, -1);
            }

            // Token: 0x0600006A RID: 106 RVA: 0x00007308 File Offset: 0x00005508
            public void change_LoopVoice(string voicename)
            {
                this.change_LoopVoice(new List<string>
                {
                    voicename
                });
            }

            // Token: 0x0600006B RID: 107 RVA: 0x0000732C File Offset: 0x0000552C
            public void change_stopVoice()
            {
                this.maid.AudioMan.Stop();
            }

            // Token: 0x0600006C RID: 108 RVA: 0x00007340 File Offset: 0x00005540
            private string _playVoice(string[] voiceList, bool isLoop = true, int exclusionVoiceIndex = -1, int forcedVoiceIndex = -1)
            {
                bool flag = voiceList.Length == 0;
                if (flag)
                {
                    Util.info("VoiceListが空です");
                    throw new ArgumentException("VoiceListが空です");
                }
                bool flag2 = forcedVoiceIndex == -1;
                int num;
                if (flag2)
                {
                    num = Util.randomInt(0, voiceList.Length - 1, exclusionVoiceIndex);
                }
                else
                {
                    num = forcedVoiceIndex;
                }
                num = Mathf.Clamp(num, 0, voiceList.Length - 1);
                string text = voiceList[num];
                this.maid.AudioMan.LoadPlay(text, 0f, false, isLoop);
                string arg = isLoop ? "ループあり" : "ループなし";
                Util.info(string.Format("ボイスを再生：{0}, {1}", text, arg));
                return text;
            }

            // Token: 0x0600006D RID: 109 RVA: 0x000073E4 File Offset: 0x000055E4
            public static bool VertexMorph_FromProcItem(TBody body, string sTag, float f)
            {
                bool flag = false;
                bool flag2 = !body || sTag == null || sTag == "";
                bool result;
                if (flag2)
                {
                    result = false;
                }
                else
                {
                    for (int i = 0; i < body.goSlot.Count; i++)
                    {
                        TMorph morph = body.goSlot[i].morph;
                        bool flag3 = morph != null;
                        if (flag3)
                        {
                            bool flag4 = morph.Contains(sTag);
                            if (flag4)
                            {
                                flag = true;
                                int f_nIdx = (int)morph.hash[sTag];
                                morph.SetBlendValues(f_nIdx, f);
                                bool flag5 = !ScriplayPlugin.IMaid.m_NeedFixTMorphs.Contains(morph);
                                if (flag5)
                                {
                                    ScriplayPlugin.IMaid.m_NeedFixTMorphs.Add(morph);
                                }
                            }
                        }
                    }
                    result = flag;
                }
                return result;
            }

            // Token: 0x0600006E RID: 110 RVA: 0x000074B4 File Offset: 0x000056B4
            public static void VertexMorph_FixBlendValues()
            {
                foreach (TMorph tmorph in ScriplayPlugin.IMaid.m_NeedFixTMorphs)
                {
                    tmorph.FixBlendValues();
                }
                ScriplayPlugin.IMaid.m_NeedFixTMorphs.Clear();
            }

            // Token: 0x0400007F RID: 127
            public readonly Maid maid;

            // Token: 0x04000080 RID: 128
            public string sPersonal;

            // Token: 0x04000081 RID: 129
            public bool loopVoiceBackuped = false;

            // Token: 0x04000082 RID: 130
            public string currentLoopVoice = "";

            // Token: 0x04000083 RID: 131
            private string currentFaceAnime = "";

            // Token: 0x04000084 RID: 132
            private Regex reg_hoho = new Regex("(頬.)");

            // Token: 0x04000085 RID: 133
            private Regex reg_namida = new Regex("(涙.)");

            // Token: 0x04000086 RID: 134
            private float eyeToCam_turnSec = 0f;

            // Token: 0x04000087 RID: 135
            private float headToCam_turnSec = 0f;

            // Token: 0x04000088 RID: 136
            private ScriplayPlugin.IMaid.EyeHeadToCamState eyeToCam_state = ScriplayPlugin.IMaid.EyeHeadToCamState.Auto;

            // Token: 0x04000089 RID: 137
            private ScriplayPlugin.IMaid.EyeHeadToCamState headToCam_state = ScriplayPlugin.IMaid.EyeHeadToCamState.Auto;

            // Token: 0x0400008A RID: 138
            private bool currentEnableReverseFront = false;

            // Token: 0x0400008B RID: 139
            private static List<TMorph> m_NeedFixTMorphs = new List<TMorph>();

            // Token: 0x0400008C RID: 140
            public readonly int maidNo;

            // Token: 0x0200000F RID: 15
            public class EyeHeadToCamState
            {
                // Token: 0x06000094 RID: 148 RVA: 0x000084A2 File Offset: 0x000066A2
                private EyeHeadToCamState(string viewStr)
                {
                    this.viewStr = viewStr;
                    this.ordinal = ScriplayPlugin.IMaid.EyeHeadToCamState.nextOrdinal;
                    ScriplayPlugin.IMaid.EyeHeadToCamState.nextOrdinal++;
                    ScriplayPlugin.IMaid.EyeHeadToCamState.items.Add(this);
                }

                // Token: 0x040000AB RID: 171
                private static int nextOrdinal = 0;

                // Token: 0x040000AC RID: 172
                public static readonly List<ScriplayPlugin.IMaid.EyeHeadToCamState> items = new List<ScriplayPlugin.IMaid.EyeHeadToCamState>();

                // Token: 0x040000AD RID: 173
                public readonly int ordinal;

                // Token: 0x040000AE RID: 174
                public readonly string viewStr;

                // Token: 0x040000AF RID: 175
                public static readonly ScriplayPlugin.IMaid.EyeHeadToCamState No = new ScriplayPlugin.IMaid.EyeHeadToCamState("no");

                // Token: 0x040000B0 RID: 176
                public static readonly ScriplayPlugin.IMaid.EyeHeadToCamState Auto = new ScriplayPlugin.IMaid.EyeHeadToCamState("auto");

                // Token: 0x040000B1 RID: 177
                public static readonly ScriplayPlugin.IMaid.EyeHeadToCamState Yes = new ScriplayPlugin.IMaid.EyeHeadToCamState("yes");
            }

            // Token: 0x02000010 RID: 16
            public class MotionAttribute
            {
                // Token: 0x06000096 RID: 150 RVA: 0x00008515 File Offset: 0x00006715
                private MotionAttribute(string viewStr)
                {
                    this.attributeStr = viewStr;
                }

                // Token: 0x06000097 RID: 151 RVA: 0x00008528 File Offset: 0x00006728
                public string getViewStr()
                {
                    return this.attributeStr;
                }

                // Token: 0x040000B2 RID: 178
                public readonly string attributeStr;

                // Token: 0x040000B3 RID: 179
                public static readonly ScriplayPlugin.IMaid.MotionAttribute Level1 = new ScriplayPlugin.IMaid.MotionAttribute("_1");

                // Token: 0x040000B4 RID: 180
                public static readonly ScriplayPlugin.IMaid.MotionAttribute Level2 = new ScriplayPlugin.IMaid.MotionAttribute("_2");

                // Token: 0x040000B5 RID: 181
                public static readonly ScriplayPlugin.IMaid.MotionAttribute Level3 = new ScriplayPlugin.IMaid.MotionAttribute("_3");

                // Token: 0x040000B6 RID: 182
                public static readonly ScriplayPlugin.IMaid.MotionAttribute Momi1 = new ScriplayPlugin.IMaid.MotionAttribute("_momi_1");

                // Token: 0x040000B7 RID: 183
                public static readonly ScriplayPlugin.IMaid.MotionAttribute Momi2 = new ScriplayPlugin.IMaid.MotionAttribute("_momi_2");

                // Token: 0x040000B8 RID: 184
                public static readonly ScriplayPlugin.IMaid.MotionAttribute Momi3 = new ScriplayPlugin.IMaid.MotionAttribute("_momi_3");

                // Token: 0x040000B9 RID: 185
                public static readonly ScriplayPlugin.IMaid.MotionAttribute Oku = new ScriplayPlugin.IMaid.MotionAttribute("_oku");

                // Token: 0x040000BA RID: 186
                public static readonly ScriplayPlugin.IMaid.MotionAttribute Taiki = new ScriplayPlugin.IMaid.MotionAttribute("_taiki");

                // Token: 0x040000BB RID: 187
                public static readonly ScriplayPlugin.IMaid.MotionAttribute Zeccyougo = new ScriplayPlugin.IMaid.MotionAttribute("_zeccyougo");

                // Token: 0x040000BC RID: 188
                public static readonly ScriplayPlugin.IMaid.MotionAttribute Hatu3 = new ScriplayPlugin.IMaid.MotionAttribute("_hatu_3");

                // Token: 0x040000BD RID: 189
                public static readonly ScriplayPlugin.IMaid.MotionAttribute Insert = new ScriplayPlugin.IMaid.MotionAttribute("_in_f_once");

                // Token: 0x040000BE RID: 190
                public static readonly ScriplayPlugin.IMaid.MotionAttribute Zeccyou = new ScriplayPlugin.IMaid.MotionAttribute("_zeccyou_f_once");
            }
        }

        // Token: 0x02000007 RID: 7
        public class IMan
        {
            // Token: 0x06000070 RID: 112 RVA: 0x00007524 File Offset: 0x00005724
            public IMan(Maid maid)
            {
                this.maid = maid;
            }

            // Token: 0x0400008D RID: 141
            private readonly Maid maid;
        }

        // Token: 0x02000008 RID: 8
        public sealed class PersonnalList
        {
            // Token: 0x06000071 RID: 113 RVA: 0x00007538 File Offset: 0x00005738
            public static string getUniqueName(int index)
            {
                bool flag = index > ScriplayPlugin.PersonnalList.uniqueName.Length - 1;
                if (flag)
                {
                    throw new ArgumentException(string.Format("性格が見つかりませんでした。\u3000PersonalList : 指数：{0}", index));
                }
                return ScriplayPlugin.PersonnalList.uniqueName[index];
            }

            // Token: 0x06000072 RID: 114 RVA: 0x00007578 File Offset: 0x00005778
            public static string getViewName(int index)
            {
                bool flag = index > ScriplayPlugin.PersonnalList.viewName.Length - 1;
                if (flag)
                {
                    throw new ArgumentException(string.Format("性格が見つかりませんでした。\u3000PersonalList : 指数：{0}", index));
                }
                return ScriplayPlugin.PersonnalList.viewName[index];
            }

            // Token: 0x06000073 RID: 115 RVA: 0x000075B8 File Offset: 0x000057B8
            public static int uniqueNameListLength()
            {
                return ScriplayPlugin.PersonnalList.uniqueName.Length;
            }

            // Token: 0x06000074 RID: 116 RVA: 0x000075D4 File Offset: 0x000057D4
            public static int viewNameListLength()
            {
                return ScriplayPlugin.PersonnalList.viewName.Length;
            }

            // Token: 0x06000075 RID: 117 RVA: 0x000075F0 File Offset: 0x000057F0
            internal static int uniqueNameIndexOf(string v)
            {
                return Array.IndexOf<string>(ScriplayPlugin.PersonnalList.uniqueName, v);
            }

            // Token: 0x0400008E RID: 142
            private static readonly string[] uniqueName = new string[]
            {
                "Pure",
                "Pride",
                "Cool",
                "Yandere",
                "Anesan",
                "Genki",
                "Sadist",
                "Muku",
                "Majime",
                "Rindere",
                "dummy_noSelected"
            };

            // Token: 0x0400008F RID: 143
            private static readonly string[] viewName = new string[]
            {
                "純真",
                "ツンデレ",
                "クーデレ",
                "ヤンデレ",
                "姉ちゃん",
                "僕っ娘",
                "ドＳ",
                "無垢",
                "真面目",
                "凛デレ",
                "指定無"
            };
        }

        // Token: 0x02000009 RID: 9
        public class VoiceTable
        {
            // Token: 0x06000078 RID: 120 RVA: 0x000076F1 File Offset: 0x000058F1
            public VoiceTable(string voiceType)
            {
                this.voiceType = voiceType;
            }

            // Token: 0x06000079 RID: 121 RVA: 0x00007718 File Offset: 0x00005918
            public void init()
            {
                this.voiceDataSet = new DataSet();
            }

            // Token: 0x0600007A RID: 122 RVA: 0x00007728 File Offset: 0x00005928
            private DataTable addNewDataTable(string sheetName)
            {
                DataTable dataTable = new DataTable(sheetName);
                this.voiceDataSet.Tables.Add(dataTable);
                foreach (ScriplayPlugin.VoiceTable.ColItem colItem in ScriplayPlugin.VoiceTable.ColItem.items)
                {
                    dataTable.Columns.Add(colItem.colName, Type.GetType(colItem.typeStr));
                }
                return dataTable;
            }

            // Token: 0x0600007B RID: 123 RVA: 0x000077B4 File Offset: 0x000059B4
            public void parse(string[][] csvContent, string filename = "")
            {
                foreach (string[] array in csvContent)
                {
                    bool flag = array.Length - 1 < ScriplayPlugin.VoiceTable.ColItem.maxColNo;
                    if (!flag)
                    {
                        string category = array[ScriplayPlugin.VoiceTable.ColItem.Category.colNo];
                        string sPersonal = array[ScriplayPlugin.VoiceTable.ColItem.Personal.colNo];
                        string uniqueSheetName = this.getUniqueSheetName(sPersonal, category);
                        bool flag2 = !this.voiceDataSet.Tables.Contains(uniqueSheetName);
                        if (flag2)
                        {
                            this.categorySet.Add(uniqueSheetName);
                            this.addNewDataTable(uniqueSheetName);
                        }
                        DataRow dataRow = this.voiceDataSet.Tables[uniqueSheetName].NewRow();
                        foreach (ScriplayPlugin.VoiceTable.ColItem colItem in ScriplayPlugin.VoiceTable.ColItem.items)
                        {
                            bool flag3 = colItem.typeStr == "System.Boolean";
                            if (flag3)
                            {
                                dataRow[colItem.colName] = (int.Parse(array[colItem.colNo]) != 0);
                            }
                            else
                            {
                                dataRow[colItem.colName] = array[colItem.colNo];
                            }
                        }
                        this.voiceDataSet.Tables[uniqueSheetName].Rows.Add(dataRow);
                    }
                }
            }

            // Token: 0x0600007C RID: 124 RVA: 0x00007928 File Offset: 0x00005B28
            public string getUniqueSheetName(string sPersonal, string category)
            {
                return sPersonal + "_" + category;
            }

            // Token: 0x0600007D RID: 125 RVA: 0x00007948 File Offset: 0x00005B48
            public List<string> queryTable(string sPersonal, string category)
            {
                List<string> list = new List<string>();
                string uniqueSheetName = this.getUniqueSheetName(sPersonal, category);
                bool flag = !this.voiceDataSet.Tables.Contains(uniqueSheetName);
                List<string> result;
                if (flag)
                {
                    Util.info(string.Format("{0}テーブルから「{1}」という名前の性格・カテゴリは見つかりませんでした", this.voiceType, uniqueSheetName));
                    result = list;
                }
                else
                {
                    foreach (object obj in this.voiceDataSet.Tables[uniqueSheetName].Rows)
                    {
                        DataRow dataRow = (DataRow)obj;
                        list.Add(dataRow[ScriplayPlugin.VoiceTable.ColItem.FileName.ordinal].ToString());
                    }
                    bool flag2 = list.Count == 0;
                    if (flag2)
                    {
                        Util.info(string.Format("{0}テーブルから「{1}」という名前の性格・カテゴリは見つかりませんでした", this.voiceType, uniqueSheetName));
                    }
                    result = list;
                }
                return result;
            }

            // Token: 0x04000090 RID: 144
            private DataSet voiceDataSet = new DataSet();

            // Token: 0x04000091 RID: 145
            private HashSet<string> categorySet = new HashSet<string>();

            // Token: 0x04000092 RID: 146
            public readonly string voiceType;

            // Token: 0x02000011 RID: 17
            public class ColItem
            {
                // Token: 0x06000099 RID: 153 RVA: 0x00008604 File Offset: 0x00006804
                private ColItem(int colNo, string colName, string typeStr)
                {
                    this.colName = colName;
                    this.typeStr = typeStr;
                    this.ordinal = ScriplayPlugin.VoiceTable.ColItem.nextOrdinal;
                    ScriplayPlugin.VoiceTable.ColItem.nextOrdinal++;
                    this.colNo = colNo;
                    ScriplayPlugin.VoiceTable.ColItem.items.Add(this);
                    bool flag = ScriplayPlugin.VoiceTable.ColItem.maxColNo < colNo;
                    if (flag)
                    {
                        ScriplayPlugin.VoiceTable.ColItem.maxColNo = colNo;
                    }
                }

                // Token: 0x040000BF RID: 191
                private static int nextOrdinal = 0;

                // Token: 0x040000C0 RID: 192
                public static readonly List<ScriplayPlugin.VoiceTable.ColItem> items = new List<ScriplayPlugin.VoiceTable.ColItem>();

                // Token: 0x040000C1 RID: 193
                public readonly string colName;

                // Token: 0x040000C2 RID: 194
                public readonly string typeStr;

                // Token: 0x040000C3 RID: 195
                public readonly int ordinal;

                // Token: 0x040000C4 RID: 196
                public readonly int colNo;

                // Token: 0x040000C5 RID: 197
                public static int maxColNo = 0;

                // Token: 0x040000C6 RID: 198
                public static readonly ScriplayPlugin.VoiceTable.ColItem Personal = new ScriplayPlugin.VoiceTable.ColItem(1, "性格", "System.String");

                // Token: 0x040000C7 RID: 199
                public static readonly ScriplayPlugin.VoiceTable.ColItem Category = new ScriplayPlugin.VoiceTable.ColItem(2, "カテゴリ", "System.String");

                // Token: 0x040000C8 RID: 200
                public static readonly ScriplayPlugin.VoiceTable.ColItem FileName = new ScriplayPlugin.VoiceTable.ColItem(3, "ボイスファイル名", "System.String");
            }
        }

        // Token: 0x0200000A RID: 10
        public class MotionInfo
        {
            // Token: 0x04000093 RID: 147
            public string category = "";

            // Token: 0x04000094 RID: 148
            public bool FrontReverse;

            // Token: 0x04000095 RID: 149
            public float AzimuthAngle;

            // Token: 0x04000096 RID: 150
            public float DeltaY;

            // Token: 0x04000097 RID: 151
            public string MaidState;

            // Token: 0x04000098 RID: 152
            public bool EnMotionChange;

            // Token: 0x04000099 RID: 153
            public string motionName = "";
        }

        // Token: 0x0200000B RID: 11
        public class MotionTable
        {
            // Token: 0x0600007F RID: 127 RVA: 0x00007A68 File Offset: 0x00005C68
            public static void init()
            {
                ScriplayPlugin.MotionTable.motionTable = new DataTable("Motion");
                foreach (ScriplayPlugin.MotionTable.ColItem colItem in ScriplayPlugin.MotionTable.ColItem.items)
                {
                    ScriplayPlugin.MotionTable.motionTable.Columns.Add(colItem.colName, Type.GetType(colItem.typeStr));
                }
            }

            // Token: 0x06000080 RID: 128 RVA: 0x00007AE8 File Offset: 0x00005CE8
            public static void parse(string[][] csvContent, string filename = "")
            {
                foreach (string[] array in csvContent)
                {
                    bool flag = array.Length - 1 < ScriplayPlugin.MotionTable.ColItem.maxColNo;
                    if (!flag)
                    {
                        DataRow dataRow = ScriplayPlugin.MotionTable.motionTable.NewRow();
                        foreach (ScriplayPlugin.MotionTable.ColItem colItem in ScriplayPlugin.MotionTable.ColItem.items)
                        {
                            bool flag2 = colItem.typeStr == "System.Boolean";
                            if (flag2)
                            {
                                dataRow[colItem.colName] = (int.Parse(array[colItem.colNo]) != 0);
                            }
                            else
                            {
                                dataRow[colItem.colName] = array[colItem.colNo];
                            }
                        }
                        ScriplayPlugin.MotionTable.motionTable.Rows.Add(dataRow);
                    }
                }
            }

            // Token: 0x06000081 RID: 129 RVA: 0x00007BE4 File Offset: 0x00005DE4
            public static List<ScriplayPlugin.MotionInfo> queryTable_motionNameBase(string category, string maidState = "-")
            {
                List<ScriplayPlugin.MotionInfo> list = ScriplayPlugin.MotionTable.query(category, maidState, "MotionTable 検索");
                bool flag = list.Count == 0;
                if (flag)
                {
                    list = ScriplayPlugin.MotionTable.query(category, "-", "MotionTable\u3000再検索\u3000MaidState「デフォルト」");
                }
                return list;
            }

            // Token: 0x06000082 RID: 130 RVA: 0x00007C24 File Offset: 0x00005E24
            private static List<ScriplayPlugin.MotionInfo> query(string category, string maidState = "-", string comment = "MotionTable 検索")
            {
                string text = ScriplayPlugin.MotionTable.createCondition(category, maidState);
                DataRow[] array = ScriplayPlugin.MotionTable.motionTable.Select(text);
                List<ScriplayPlugin.MotionInfo> list = new List<ScriplayPlugin.MotionInfo>();
                foreach (DataRow dataRow in array)
                {
                    try
                    {
                        list.Add(new ScriplayPlugin.MotionInfo
                        {
                            category = dataRow[ScriplayPlugin.MotionTable.ColItem.Category.ordinal].ToString(),
                            motionName = dataRow[ScriplayPlugin.MotionTable.ColItem.MotionName.ordinal].ToString(),
                            DeltaY = float.Parse(dataRow[ScriplayPlugin.MotionTable.ColItem.DeltaY.ordinal].ToString()),
                            AzimuthAngle = float.Parse(dataRow[ScriplayPlugin.MotionTable.ColItem.AzimuthAngle.ordinal].ToString()),
                            EnMotionChange = bool.Parse(dataRow[ScriplayPlugin.MotionTable.ColItem.EnMotionChange.ordinal].ToString()),
                            FrontReverse = bool.Parse(dataRow[ScriplayPlugin.MotionTable.ColItem.FrontReverse.ordinal].ToString())
                        });
                    }
                    catch (Exception ex)
                    {
                        Util.debug(string.Format("MotionTableから読み出し失敗:{0} \r\n エラー内容 : {1}", dataRow.ToString(), ex.StackTrace));
                    }
                }
                Util.debug(string.Format("{0}\r\n  {1}\r\n  検索結果\r\n  {2}", comment, text, Util.list2Str<ScriplayPlugin.MotionInfo>(list)));
                return list;
            }

            // Token: 0x06000083 RID: 131 RVA: 0x00007D98 File Offset: 0x00005F98
            private static string createCondition(string category, string maidState = "-")
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(string.Format(" {0} = '{1}'", ScriplayPlugin.MotionTable.ColItem.Category.colName, category));
                bool flag = maidState != "-";
                if (flag)
                {
                    bool flag2 = stringBuilder.Length != 0;
                    if (flag2)
                    {
                        stringBuilder.Append(" AND ");
                    }
                    stringBuilder.Append(string.Format(" {0} = '{1}'", ScriplayPlugin.MotionTable.ColItem.MaidState.colName, maidState));
                }
                return stringBuilder.ToString();
            }

            // Token: 0x06000084 RID: 132 RVA: 0x00007E18 File Offset: 0x00006018
            public static DataTable getTable()
            {
                return ScriplayPlugin.MotionTable.motionTable;
            }

            // Token: 0x06000085 RID: 133 RVA: 0x00007E30 File Offset: 0x00006030
            public static IEnumerable<string> getCategoryList()
            {
                HashSet<string> hashSet = new HashSet<string>();
                foreach (object obj in ScriplayPlugin.MotionTable.motionTable.Rows)
                {
                    DataRow dataRow = (DataRow)obj;
                    hashSet.Add((string)dataRow[ScriplayPlugin.MotionTable.ColItem.Category.colName]);
                }
                return hashSet;
            }

            // Token: 0x0400009A RID: 154
            private static DataTable motionTable = new DataTable("Motion");

            // Token: 0x02000012 RID: 18
            public class ColItem
            {
                // Token: 0x0600009B RID: 155 RVA: 0x000086C8 File Offset: 0x000068C8
                private ColItem(int colNo, string colName, string typeStr)
                {
                    this.colName = colName;
                    this.typeStr = typeStr;
                    this.ordinal = ScriplayPlugin.MotionTable.ColItem.nextOrdinal;
                    ScriplayPlugin.MotionTable.ColItem.nextOrdinal++;
                    this.colNo = colNo;
                    ScriplayPlugin.MotionTable.ColItem.items.Add(this);
                    bool flag = ScriplayPlugin.MotionTable.ColItem.maxColNo < colNo;
                    if (flag)
                    {
                        ScriplayPlugin.MotionTable.ColItem.maxColNo = colNo;
                    }
                }

                // Token: 0x040000C9 RID: 201
                private static int nextOrdinal = 0;

                // Token: 0x040000CA RID: 202
                public static List<ScriplayPlugin.MotionTable.ColItem> items = new List<ScriplayPlugin.MotionTable.ColItem>();

                // Token: 0x040000CB RID: 203
                public readonly string colName;

                // Token: 0x040000CC RID: 204
                public readonly string typeStr;

                // Token: 0x040000CD RID: 205
                public readonly int ordinal;

                // Token: 0x040000CE RID: 206
                public readonly int colNo;

                // Token: 0x040000CF RID: 207
                public static int maxColNo = 0;

                // Token: 0x040000D0 RID: 208
                public static readonly ScriplayPlugin.MotionTable.ColItem Category = new ScriplayPlugin.MotionTable.ColItem(1, "カテゴリ", "System.String");

                // Token: 0x040000D1 RID: 209
                public static readonly ScriplayPlugin.MotionTable.ColItem FrontReverse = new ScriplayPlugin.MotionTable.ColItem(2, "正面反転？", "System.Boolean");

                // Token: 0x040000D2 RID: 210
                public static readonly ScriplayPlugin.MotionTable.ColItem AzimuthAngle = new ScriplayPlugin.MotionTable.ColItem(3, "横回転角度", "System.Double");

                // Token: 0x040000D3 RID: 211
                public static readonly ScriplayPlugin.MotionTable.ColItem DeltaY = new ScriplayPlugin.MotionTable.ColItem(4, "Y軸位置", "System.Double");

                // Token: 0x040000D4 RID: 212
                public static readonly ScriplayPlugin.MotionTable.ColItem MaidState = new ScriplayPlugin.MotionTable.ColItem(5, "メイド状態", "System.String");

                // Token: 0x040000D5 RID: 213
                public static readonly ScriplayPlugin.MotionTable.ColItem EnMotionChange = new ScriplayPlugin.MotionTable.ColItem(6, "モーション変更許可", "System.Boolean");

                // Token: 0x040000D6 RID: 214
                public static readonly ScriplayPlugin.MotionTable.ColItem MotionName = new ScriplayPlugin.MotionTable.ColItem(7, "モーションファイル名", "System.String");
            }
        }

        // Token: 0x0200000C RID: 12
        public class FaceTable
        {
            // Token: 0x06000088 RID: 136 RVA: 0x00007EC5 File Offset: 0x000060C5
            public static void init()
            {
                ScriplayPlugin.FaceTable.faceDataSet = new DataSet();
            }

            // Token: 0x06000089 RID: 137 RVA: 0x00007ED4 File Offset: 0x000060D4
            private static DataTable addNewDataTable(string sheetName)
            {
                DataTable dataTable = new DataTable(sheetName);
                ScriplayPlugin.FaceTable.faceDataSet.Tables.Add(dataTable);
                foreach (ScriplayPlugin.FaceTable.ColItem colItem in ScriplayPlugin.FaceTable.ColItem.items)
                {
                    dataTable.Columns.Add(colItem.colName, Type.GetType(colItem.typeStr));
                }
                return dataTable;
            }

            // Token: 0x0600008A RID: 138 RVA: 0x00007F60 File Offset: 0x00006160
            public static void parse(string[][] csvContent, string filename = "")
            {
                foreach (string[] array in csvContent)
                {
                    bool flag = array.Length - 1 < ScriplayPlugin.FaceTable.ColItem.maxColNo;
                    if (!flag)
                    {
                        string text = array[ScriplayPlugin.FaceTable.ColItem.Category.colNo];
                        bool flag2 = !ScriplayPlugin.FaceTable.faceDataSet.Tables.Contains(text);
                        if (flag2)
                        {
                            ScriplayPlugin.FaceTable.categorySet.Add(text);
                            ScriplayPlugin.FaceTable.addNewDataTable(text);
                        }
                        DataRow dataRow = ScriplayPlugin.FaceTable.faceDataSet.Tables[text].NewRow();
                        foreach (ScriplayPlugin.FaceTable.ColItem colItem in ScriplayPlugin.FaceTable.ColItem.items)
                        {
                            bool flag3 = colItem.typeStr == "System.Boolean";
                            if (flag3)
                            {
                                dataRow[colItem.colName] = (int.Parse(array[colItem.colNo]) != 0);
                            }
                            else
                            {
                                dataRow[colItem.colName] = array[colItem.colNo];
                            }
                        }
                        ScriplayPlugin.FaceTable.faceDataSet.Tables[text].Rows.Add(dataRow);
                    }
                }
            }

            // Token: 0x0600008B RID: 139 RVA: 0x000080B0 File Offset: 0x000062B0
            public static List<string> queryTable(string category)
            {
                List<string> list = new List<string>();
                bool flag = !ScriplayPlugin.FaceTable.faceDataSet.Tables.Contains(category);
                List<string> result;
                if (flag)
                {
                    Util.info(string.Format("表情テーブルから「{0}」という名前のカテゴリは見つかりませんでした", category));
                    result = list;
                }
                else
                {
                    foreach (object obj in ScriplayPlugin.FaceTable.faceDataSet.Tables[category].Rows)
                    {
                        DataRow dataRow = (DataRow)obj;
                        list.Add(dataRow[ScriplayPlugin.FaceTable.ColItem.FaceName.ordinal].ToString());
                    }
                    bool flag2 = list.Count == 0;
                    if (flag2)
                    {
                        Util.info(string.Format("表情テーブルから「{0}」という名前のカテゴリは見つかりませんでした", category));
                    }
                    result = list;
                }
                return result;
            }

            // Token: 0x0400009B RID: 155
            private static DataSet faceDataSet = new DataSet();

            // Token: 0x0400009C RID: 156
            private static HashSet<string> categorySet = new HashSet<string>();

            // Token: 0x02000013 RID: 19
            public class ColItem
            {
                // Token: 0x0600009D RID: 157 RVA: 0x000087E0 File Offset: 0x000069E0
                private ColItem(int colNo, string colName, string typeStr)
                {
                    this.colName = colName;
                    this.typeStr = typeStr;
                    this.ordinal = ScriplayPlugin.FaceTable.ColItem.nextOrdinal;
                    ScriplayPlugin.FaceTable.ColItem.nextOrdinal++;
                    this.colNo = colNo;
                    ScriplayPlugin.FaceTable.ColItem.items.Add(this);
                    bool flag = ScriplayPlugin.FaceTable.ColItem.maxColNo < colNo;
                    if (flag)
                    {
                        ScriplayPlugin.FaceTable.ColItem.maxColNo = colNo;
                    }
                }

                // Token: 0x040000D7 RID: 215
                private static int nextOrdinal = 0;

                // Token: 0x040000D8 RID: 216
                public static List<ScriplayPlugin.FaceTable.ColItem> items = new List<ScriplayPlugin.FaceTable.ColItem>();

                // Token: 0x040000D9 RID: 217
                public readonly string colName;

                // Token: 0x040000DA RID: 218
                public readonly string typeStr;

                // Token: 0x040000DB RID: 219
                public readonly int ordinal;

                // Token: 0x040000DC RID: 220
                public readonly int colNo;

                // Token: 0x040000DD RID: 221
                public static int maxColNo = 0;

                // Token: 0x040000DE RID: 222
                public static readonly ScriplayPlugin.FaceTable.ColItem Category = new ScriplayPlugin.FaceTable.ColItem(1, "カテゴリ", "System.String");

                // Token: 0x040000DF RID: 223
                public static readonly ScriplayPlugin.FaceTable.ColItem FaceName = new ScriplayPlugin.FaceTable.ColItem(2, "表情ファイル名", "System.String");

                // Token: 0x040000E0 RID: 224
                public static readonly ScriplayPlugin.FaceTable.ColItem Hoho = new ScriplayPlugin.FaceTable.ColItem(3, "頬", "System.Int32");

                // Token: 0x040000E1 RID: 225
                public static readonly ScriplayPlugin.FaceTable.ColItem Namida = new ScriplayPlugin.FaceTable.ColItem(4, "涙", "System.Int32");

                // Token: 0x040000E2 RID: 226
                public static readonly ScriplayPlugin.FaceTable.ColItem Yodare = new ScriplayPlugin.FaceTable.ColItem(5, "よだれ", "System.Int32");
            }
        }

        // Token: 0x0200000D RID: 13
        public class ToastUtil : MonoBehaviour
        {
            // Token: 0x0600008E RID: 142 RVA: 0x000081AC File Offset: 0x000063AC
            public static void Toast<T>(MonoBehaviour mb, T m)
            {
                string text = m.ToString();
                GameObject gameObject = new GameObject("ToastCanbas");
                Canvas canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
                GameObject gameObject2 = new GameObject("Image");
                gameObject2.transform.parent = gameObject.transform;
                Image image = gameObject2.AddComponent<Image>();
                bool flag = ScriplayPlugin.ToastUtil.imgSprite;
                if (flag)
                {
                    image.sprite = ScriplayPlugin.ToastUtil.imgSprite;
                }
                image.color = ScriplayPlugin.ToastUtil.imgColor;
                gameObject2.GetComponent<RectTransform>().anchoredPosition = ScriplayPlugin.ToastUtil.startPos;
                GameObject gameObject3 = new GameObject("Text");
                gameObject3.transform.parent = gameObject2.transform;
                Text text2 = gameObject3.AddComponent<Text>();
                gameObject3.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);
                text2.alignment = TextAnchor.MiddleCenter;
                bool flag2 = ScriplayPlugin.ToastUtil.textFont;
                if (flag2)
                {
                    text2.font = ScriplayPlugin.ToastUtil.textFont;
                }
                else
                {
                    text2.font = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
                }
                text2.fontSize = ScriplayPlugin.ToastUtil.fontSize;
                text2.text = text;
                text2.enabled = true;
                text2.color = ScriplayPlugin.ToastUtil.textColor;
                gameObject3.GetComponent<RectTransform>().sizeDelta = new Vector2(text2.preferredWidth, text2.preferredHeight);
                gameObject3.GetComponent<RectTransform>().sizeDelta = new Vector2(text2.preferredWidth, text2.preferredHeight);
                gameObject2.GetComponent<RectTransform>().sizeDelta = new Vector2(text2.preferredWidth + (float)ScriplayPlugin.ToastUtil.pad, text2.preferredHeight + (float)ScriplayPlugin.ToastUtil.pad);
                mb.StartCoroutine(ScriplayPlugin.ToastUtil.DoToast(gameObject2.GetComponent<RectTransform>(), (ScriplayPlugin.ToastUtil.endPos - ScriplayPlugin.ToastUtil.startPos) * (1f / (float)ScriplayPlugin.ToastUtil.moveFrame), gameObject));
            }

            // Token: 0x0600008F RID: 143 RVA: 0x000083B2 File Offset: 0x000065B2
            private static IEnumerator DoToast(RectTransform rec, Vector2 dif, GameObject g)
            {
                int num;
                for (int i = 1; i <= ScriplayPlugin.ToastUtil.moveFrame; i = num + 1)
                {
                    rec.anchoredPosition += dif;
                    yield return null;
                    num = i;
                }
                for (int j = 1; j <= ScriplayPlugin.ToastUtil.waitFrame; j = num + 1)
                {
                    yield return null;
                    num = j;
                }
                UnityEngine.Object.Destroy(g);
                yield break;
            }

            // Token: 0x0400009D RID: 157
            public static Color imgColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);

            // Token: 0x0400009E RID: 158
            public static Color textColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Token: 0x0400009F RID: 159
            public static Vector2 startPos = new Vector2(0f, -500f);

            // Token: 0x040000A0 RID: 160
            public static Vector2 endPos = new Vector2(0f, -300f);

            // Token: 0x040000A1 RID: 161
            public static int fontSize = 20;

            // Token: 0x040000A2 RID: 162
            public static int moveFrame = 30;

            // Token: 0x040000A3 RID: 163
            public static int waitFrame = 180;

            // Token: 0x040000A4 RID: 164
            public static int pad = 100;

            // Token: 0x040000A5 RID: 165
            public static Sprite imgSprite;

            // Token: 0x040000A6 RID: 166
            public static Font textFont;
        }
    }

    // Token: 0x02000004 RID: 4
    public class ScriplayContext
    {
        // Token: 0x06000030 RID: 48 RVA: 0x0000464C File Offset: 0x0000284C
        private ScriplayContext(string scriptName, bool finished = false)
        {
            this.scriptFinished = finished;
            this.scriptName = scriptName;
        }

        // Token: 0x17000001 RID: 1
        // (get) Token: 0x06000031 RID: 49 RVA: 0x000046F8 File Offset: 0x000028F8
        // (set) Token: 0x06000032 RID: 50 RVA: 0x00004710 File Offset: 0x00002910
        public bool scriptFinished
        {
            get
            {
                return this.scriptFinished_flag;
            }
            set
            {
                this.scriptFinished_flag = value;
                if (value)
                {
                    this.tearDown();
                }
            }
        }

        // Token: 0x06000033 RID: 51 RVA: 0x00004734 File Offset: 0x00002934
        private void tearDown()
        {
            this.selection_selectionList.Clear();
            foreach (ScriplayPlugin.IMaid maid in ScriplayPlugin.maidList)
            {
                maid.change_stopVoice();
            }
            ScriplayPlugin.change_SE("");
        }

        // Token: 0x06000034 RID: 52 RVA: 0x000047A4 File Offset: 0x000029A4
        public static ScriplayContext readScriptFile(string scriptName, string[] scriptArray)
        {
            ScriplayContext scriplayContext = new ScriplayContext(scriptName, false);
            List<string> list = new List<string>(scriptArray);
            list.Insert(0, "");
            scriptArray = list.ToArray();
            scriplayContext.scriptArray = scriptArray;
            foreach (string input in scriplayContext.scriptArray)
            {
                bool flag = ScriplayContext.reg_scriptInfo.IsMatch(input);
                if (flag)
                {
                    Util.info("スクリプトバージョンを検出しました");
                    break;
                }
            }
            for (int j = 0; j < scriplayContext.scriptArray.Length; j++)
            {
                string input2 = scriplayContext.scriptArray[j];
                bool flag2 = ScriplayContext.reg_label.IsMatch(input2);
                if (flag2)
                {
                    Match match = ScriplayContext.reg_label.Match(input2);
                    string value = match.Groups[1].Value;
                    scriplayContext.labelMap.Add(value, j);
                }
            }
            return scriplayContext;
        }

        // Token: 0x06000035 RID: 53 RVA: 0x00004894 File Offset: 0x00002A94
        public static ScriplayContext readScriptFile(string filePath)
        {
            Util.info(string.Format("スクリプトファイル読み込み： {0}", filePath));
            FileInfo fileInfo = new FileInfo(filePath);
            string[] array = Util.readAllText(filePath);
            return ScriplayContext.readScriptFile(fileInfo.Name, array);
        }

        // Token: 0x06000036 RID: 54 RVA: 0x000048D4 File Offset: 0x00002AD4
        public void Update()
        {
            bool flag = ScriplayPlugin.maidList.Count == 0;
            if (!flag)
            {
                bool flag2 = this.waitSecond > 0f;
                if (flag2)
                {
                    this.waitSecond -= Time.deltaTime;
                }
                else
                {
                    bool flag3 = this.showText_waitTime > 0f;
                    if (flag3)
                    {
                        this.showText_waitTime -= Time.deltaTime;
                        bool flag4 = this.showText_waitTime < 0f;
                        if (flag4)
                        {
                            this.showText = "";
                        }
                    }
                    else
                    {
                        bool flag5 = this.selection_waitSecond > 0f;
                        if (flag5)
                        {
                            this.selection_waitSecond -= Time.deltaTime;
                            bool flag6 = this.selection_waitSecond < 0f;
                            if (flag6)
                            {
                                this.selection_selectionList = new List<ScriplayContext.Selection>();
                            }
                            else
                            {
                                bool flag7 = this.selection_selectedItem != ScriplayContext.Selection.None;
                                if (flag7)
                                {
                                    this.exec_goto(this.selection_selectedItem.gotoLabel);
                                    this.selection_waitSecond = -1f;
                                    this.selection_selectedItem = ScriplayContext.Selection.None;
                                    this.selection_selectionList.Clear();
                                }
                            }
                        }
                        else
                        {
                            List<int> list = new List<int>();
                            foreach (KeyValuePair<int, bool> keyValuePair in this.talk_waitUntilFinishSpeekingDict)
                            {
                                int key = keyValuePair.Key;
                                bool value = keyValuePair.Value;
                                bool flag8 = value;
                                if (flag8)
                                {
                                    bool flag9 = ScriplayPlugin.maidList[key].isPlayingVoice();
                                    if (flag9)
                                    {
                                        return;
                                    }
                                    list.Add(key);
                                }
                            }
                            foreach (int key2 in list)
                            {
                                this.talk_waitUntilFinishSpeekingDict.Remove(key2);
                            }
                            while (!this.scriptFinished)
                            {
                                this.currentExecuteLine++;
                                bool flag10 = this.currentExecuteLine >= this.scriptArray.Length;
                                if (flag10)
                                {
                                    this.scriptFinished = true;
                                    Util.info(string.Format("すべてのスクリプトを実行しました. 行数：{0},{1}", this.currentExecuteLine.ToString(), this.scriptName));
                                    break;
                                }
                                string text = this.scriptArray[this.currentExecuteLine];
                                bool flag11 = ScriplayContext.reg_comment.IsMatch(text);
                                if (!flag11)
                                {
                                    bool flag12 = ScriplayContext.reg_label.IsMatch(text);
                                    if (!flag12)
                                    {
                                        bool flag13 = ScriplayContext.reg_scriptInfo.IsMatch(text);
                                        if (!flag13)
                                        {
                                            bool flag14 = ScriplayContext.reg_require.IsMatch(text);
                                            if (flag14)
                                            {
                                                this.exec_require(this.parseParameter(ScriplayContext.reg_require, text));
                                                break;
                                            }
                                            bool flag15 = ScriplayContext.reg_auto.IsMatch(text);
                                            if (flag15)
                                            {
                                                Dictionary<string, string> dictionary = this.parseParameter(ScriplayContext.reg_auto, text);
                                                for (int i = 1; i < 10; i++)
                                                {
                                                    string key3 = "auto" + i.ToString();
                                                    bool flag16 = dictionary.ContainsKey(key3);
                                                    if (flag16)
                                                    {
                                                        this.autoModeList.Add(dictionary[key3]);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                bool flag17 = ScriplayContext.reg_posAbsolute.IsMatch(text);
                                                if (flag17)
                                                {
                                                    this.exec_posAbsolute(this.parseParameter(ScriplayContext.reg_posAbsolute, text));
                                                    break;
                                                }
                                                bool flag18 = ScriplayContext.reg_posRelative.IsMatch(text);
                                                if (flag18)
                                                {
                                                    this.exec_posRelative(this.parseParameter(ScriplayContext.reg_posRelative, text));
                                                    break;
                                                }
                                                bool flag19 = ScriplayContext.reg_rotAbsolute.IsMatch(text);
                                                if (flag19)
                                                {
                                                    this.exec_rotAbsolute(this.parseParameter(ScriplayContext.reg_rotAbsolute, text));
                                                    break;
                                                }
                                                bool flag20 = ScriplayContext.reg_rotRelative.IsMatch(text);
                                                if (flag20)
                                                {
                                                    this.exec_rotRelative(this.parseParameter(ScriplayContext.reg_rotRelative, text));
                                                    break;
                                                }
                                                bool flag21 = ScriplayContext.reg_show.IsMatch(text);
                                                if (flag21)
                                                {
                                                    this.exec_show(this.parseParameter(ScriplayContext.reg_show, text), -1);
                                                    break;
                                                }
                                                bool flag22 = ScriplayContext.reg_sound.IsMatch(text);
                                                if (flag22)
                                                {
                                                    this.exec_sound(this.parseParameter(ScriplayContext.reg_sound, text));
                                                    break;
                                                }
                                                bool flag23 = ScriplayContext.reg_motion.IsMatch(text);
                                                if (flag23)
                                                {
                                                    this.exec_motion(this.parseParameter(ScriplayContext.reg_motion, text));
                                                    break;
                                                }
                                                bool flag24 = ScriplayContext.reg_face.IsMatch(text);
                                                if (flag24)
                                                {
                                                    this.exec_face(this.parseParameter(ScriplayContext.reg_face, text));
                                                    break;
                                                }
                                                bool flag25 = ScriplayContext.reg_wait.IsMatch(text);
                                                if (flag25)
                                                {
                                                    Match match = ScriplayContext.reg_wait.Match(text);
                                                    string value2 = match.Groups[1].Value;
                                                    this.selection_waitSecond = this.parseFloat(value2, new string[]
                                                    {
                                                        "sec",
                                                        "s"
                                                    });
                                                    break;
                                                }
                                                bool flag26 = ScriplayContext.reg_goto.IsMatch(text);
                                                if (flag26)
                                                {
                                                    Match match2 = ScriplayContext.reg_goto.Match(text);
                                                    string value3 = match2.Groups[1].Value;
                                                    this.exec_goto(value3);
                                                }
                                                else
                                                {
                                                    bool flag27 = ScriplayContext.reg_talk.IsMatch(text);
                                                    if (flag27)
                                                    {
                                                        this.exec_talk(this.parseParameter(ScriplayContext.reg_talk, text), false, this.currentExecuteLine);
                                                        break;
                                                    }
                                                    bool flag28 = ScriplayContext.reg_talkRepeat.IsMatch(text);
                                                    if (flag28)
                                                    {
                                                        this.exec_talk(this.parseParameter(ScriplayContext.reg_talkRepeat, text), true, this.currentExecuteLine);
                                                        break;
                                                    }
                                                    bool flag29 = ScriplayContext.reg_eyeToCam.IsMatch(text);
                                                    if (flag29)
                                                    {
                                                        this.exec_eyeToCam(this.parseParameter(ScriplayContext.reg_eyeToCam, text));
                                                        break;
                                                    }
                                                    bool flag30 = ScriplayContext.reg_headToCam.IsMatch(text);
                                                    if (flag30)
                                                    {
                                                        this.exec_headToCam(this.parseParameter(ScriplayContext.reg_headToCam, text));
                                                        break;
                                                    }
                                                    bool flag31 = ScriplayContext.reg_selection.IsMatch(text);
                                                    if (flag31)
                                                    {
                                                        Dictionary<string, string> dictionary2 = this.parseParameter(ScriplayContext.reg_selection, text);
                                                        bool flag32 = dictionary2.ContainsKey("wait");
                                                        if (flag32)
                                                        {
                                                            this.selection_waitSecond = this.parseFloat(dictionary2["wait"], new string[]
                                                            {
                                                                "sec",
                                                                "s"
                                                            });
                                                        }
                                                        else
                                                        {
                                                            this.selection_waitSecond = 3.1536E+07f;
                                                        }
                                                        for (; ; )
                                                        {
                                                            int num = this.currentExecuteLine + 1;
                                                            bool flag33 = num >= this.scriptArray.Length || !ScriplayContext.reg_selectionItem.IsMatch(this.scriptArray[num]);
                                                            if (flag33)
                                                            {
                                                                break;
                                                            }
                                                            this.currentExecuteLine++;
                                                            text = this.scriptArray[this.currentExecuteLine];
                                                            Match match3 = ScriplayContext.reg_selectionItem.Match(text);
                                                            string value4 = match3.Groups[1].Value;
                                                            string value5 = match3.Groups[2].Value;
                                                            dictionary2 = this.parseParameter(value5);
                                                            this.addSelection(value4, dictionary2);
                                                        }
                                                        break;
                                                    }
                                                    bool flag34 = text == "";
                                                    if (!flag34)
                                                    {
                                                        Util.info(string.Format("解釈できませんでした：{0}:{1}", this.currentExecuteLine.ToString(), text));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Token: 0x06000037 RID: 55 RVA: 0x00005070 File Offset: 0x00003270
        private void exec_eyeToCam(Dictionary<string, string> paramDict)
        {
            Util.debug(string.Format("line{0} : eyeToCam ", this.currentExecuteLine.ToString()));
            List<ScriplayPlugin.IMaid> list = this.selectMaid(paramDict);
            foreach (ScriplayPlugin.IMaid maid in list)
            {
                ScriplayPlugin.IMaid.EyeHeadToCamState state = ScriplayPlugin.IMaid.EyeHeadToCamState.Auto;
                bool flag = paramDict.ContainsKey("mode");
                if (flag)
                {
                    string text = paramDict["mode"].ToLower();
                    bool flag2 = text == ScriplayPlugin.IMaid.EyeHeadToCamState.Auto.viewStr;
                    if (flag2)
                    {
                        state = ScriplayPlugin.IMaid.EyeHeadToCamState.Auto;
                    }
                    else
                    {
                        bool flag3 = text == ScriplayPlugin.IMaid.EyeHeadToCamState.Yes.viewStr;
                        if (flag3)
                        {
                            state = ScriplayPlugin.IMaid.EyeHeadToCamState.Yes;
                        }
                        else
                        {
                            bool flag4 = text == ScriplayPlugin.IMaid.EyeHeadToCamState.No.viewStr;
                            if (flag4)
                            {
                                state = ScriplayPlugin.IMaid.EyeHeadToCamState.No;
                            }
                            else
                            {
                                Util.info(string.Format("line{0} : モード指定が不適切です:{1}", this.currentExecuteLine.ToString(), text));
                            }
                        }
                    }
                }
                else
                {
                    Util.info(string.Format("line{0} : モードが指定されていません", this.currentExecuteLine.ToString()));
                }
                maid.change_eyeToCam(state);
            }
        }

        // Token: 0x06000038 RID: 56 RVA: 0x000051BC File Offset: 0x000033BC
        private void exec_headToCam(Dictionary<string, string> paramDict)
        {
            Util.debug(string.Format("line{0} : headToCam ", this.currentExecuteLine.ToString()));
            List<ScriplayPlugin.IMaid> list = this.selectMaid(paramDict);
            using (List<ScriplayPlugin.IMaid>.Enumerator enumerator = list.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    ScriplayPlugin.IMaid maid = enumerator.Current;
                    ScriplayPlugin.IMaid.EyeHeadToCamState state = ScriplayPlugin.IMaid.EyeHeadToCamState.Auto;
                    bool flag = paramDict.ContainsKey("mode");
                    if (flag)
                    {
                        string text = paramDict["mode"].ToLower();
                        bool flag2 = text == ScriplayPlugin.IMaid.EyeHeadToCamState.Auto.viewStr;
                        if (flag2)
                        {
                            state = ScriplayPlugin.IMaid.EyeHeadToCamState.Auto;
                        }
                        else
                        {
                            bool flag3 = text == ScriplayPlugin.IMaid.EyeHeadToCamState.Yes.viewStr;
                            if (flag3)
                            {
                                state = ScriplayPlugin.IMaid.EyeHeadToCamState.Yes;
                            }
                            else
                            {
                                bool flag4 = text == ScriplayPlugin.IMaid.EyeHeadToCamState.No.viewStr;
                                if (flag4)
                                {
                                    state = ScriplayPlugin.IMaid.EyeHeadToCamState.No;
                                }
                                else
                                {
                                    Util.info(string.Format("line{0} : モード指定が不適切です:{1}", this.currentExecuteLine.ToString(), text));
                                }
                            }
                        }
                    }
                    else
                    {
                        Util.info(string.Format("line{0} : モードが指定されていません", this.currentExecuteLine.ToString()));
                    }
                    bool flag5 = paramDict.ContainsKey("fade");
                    if (flag5)
                    {
                        float fadeSec = this.parseFloat(paramDict["fade"], new string[]
                        {
                            "sec",
                            "s"
                        });
                        maid.change_headToCam(state, fadeSec);
                    }
                    else
                    {
                        maid.change_headToCam(state, -1f);
                    }
                }
            }
        }

        // Token: 0x06000039 RID: 57 RVA: 0x0000536C File Offset: 0x0000356C
        private Dictionary<string, string> parseParameter(Regex reg, string lineStr)
        {
            bool flag = !reg.IsMatch(lineStr);
            Dictionary<string, string> result;
            if (flag)
            {
                result = new Dictionary<string, string>();
            }
            else
            {
                string value = reg.Match(lineStr).Groups[1].Value;
                result = this.parseParameter(value);
            }
            return result;
        }

        // Token: 0x0600003A RID: 58 RVA: 0x000053BC File Offset: 0x000035BC
        private Dictionary<string, string> parseParameter(string paramStr)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            paramStr = ScriplayContext.parseParameter_regex.Replace(paramStr, " ");
            paramStr = ScriplayContext.parseParameter_regex_header.Replace(paramStr, "");
            paramStr = ScriplayContext.parseParameter_regex_footer.Replace(paramStr, "");
            string[] array = paramStr.Split(new char[]
            {
                ' '
            });
            foreach (string text in array)
            {
                string[] array3 = text.Split(new char[]
                {
                    '='
                });
                bool flag = array3.Length != 2;
                if (flag)
                {
                    Util.info(string.Format("line{0} : パラメータを読み込めませんでした。「key=value」形式になっていますか？ : {1}", this.currentExecuteLine.ToString(), text));
                }
                else
                {
                    dictionary.Add(array3[0], array3[1]);
                }
            }
            return dictionary;
        }

        // Token: 0x0600003B RID: 59 RVA: 0x0000548C File Offset: 0x0000368C
        private float parseFloat(string floatStr, string[] suffix = null)
        {
            float result = -1f;
            bool flag = suffix != null;
            if (flag)
            {
                floatStr = floatStr.ToLower();
                foreach (string oldValue in suffix)
                {
                    floatStr = floatStr.Replace(oldValue, "");
                }
            }
            try
            {
                result = float.Parse(floatStr);
            }
            catch (Exception ex)
            {
                Util.info(string.Format("line{0} : 数値を読み込めませんでした : {1}", this.currentExecuteLine.ToString(), floatStr));
                Util.debug(ex.StackTrace);
            }
            return result;
        }

        // Token: 0x0600003C RID: 60 RVA: 0x0000552C File Offset: 0x0000372C
        private void exec_sound(Dictionary<string, string> paramDict)
        {
            Util.debug(string.Format("line{0} : sound ", this.currentExecuteLine.ToString()));
            bool flag = paramDict.ContainsKey("name");
            if (flag)
            {
                string text = paramDict["name"];
                bool flag2 = text == "stop";
                if (flag2)
                {
                    text = "";
                }
                ScriplayPlugin.change_SE(text);
            }
            else
            {
                ScriplayPlugin.change_SE("");
            }
        }

        // Token: 0x0600003D RID: 61 RVA: 0x000055A0 File Offset: 0x000037A0
        private void exec_require(Dictionary<string, string> paramDict)
        {
            bool flag = paramDict.ContainsKey("maidNum");
            if (flag)
            {
                int num = (int)this.parseFloat(paramDict["maidNum"], null);
                bool flag2 = ScriplayPlugin.maidList.Count < num;
                if (flag2)
                {
                    string message = string.Format("メイドさんが{0}人以上必要です", num);
                    ScriplayPlugin.toast(message);
                    Util.info(message);
                    this.scriptFinished = true;
                }
            }
        }

        // Token: 0x0600003E RID: 62 RVA: 0x00005610 File Offset: 0x00003810
        private void exec_motion(Dictionary<string, string> paramDict)
        {
            Util.debug(string.Format("line{0} : motion ", this.currentExecuteLine.ToString()));
            List<ScriplayPlugin.IMaid> list = this.selectMaid(paramDict);
            foreach (ScriplayPlugin.IMaid maid in list)
            {
                bool flag = paramDict.ContainsKey("name");
                if (flag)
                {
                    maid.change_Motion(paramDict["name"], true, false, -1f, -1f);
                }
                else
                {
                    bool flag2 = paramDict.ContainsKey("category");
                    if (flag2)
                    {
                        List<ScriplayPlugin.MotionInfo> motionList = ScriplayPlugin.MotionTable.queryTable_motionNameBase(paramDict["category"], "-");
                        maid.change_Motion(motionList, true, true);
                    }
                    else
                    {
                        Util.info(string.Format("line{0} : モーションが指定されていません", this.currentExecuteLine.ToString()));
                    }
                }
            }
        }

        // Token: 0x0600003F RID: 63 RVA: 0x0000570C File Offset: 0x0000390C
        private void exec_face(Dictionary<string, string> paramDict)
        {
            Util.debug(string.Format("line{0} : face ", this.currentExecuteLine.ToString()));
            List<ScriplayPlugin.IMaid> list = this.selectMaid(paramDict);
            foreach (ScriplayPlugin.IMaid maid in list)
            {
                float num = -1f;
                bool flag = paramDict.ContainsKey("fade");
                if (flag)
                {
                    num = this.parseFloat(paramDict["fade"], new string[]
                    {
                        "sec",
                        "s"
                    });
                }
                bool flag2 = paramDict.ContainsKey("name");
                if (flag2)
                {
                    string faceAnime = paramDict["name"];
                    bool flag3 = num == -1f;
                    if (flag3)
                    {
                        maid.change_faceAnime(faceAnime, -1f);
                    }
                    else
                    {
                        maid.change_faceAnime(faceAnime, num);
                    }
                }
                else
                {
                    bool flag4 = paramDict.ContainsKey("category");
                    if (flag4)
                    {
                        List<string> faceAnimeList = ScriplayPlugin.FaceTable.queryTable(paramDict["category"]);
                        bool flag5 = num == -1f;
                        if (flag5)
                        {
                            maid.change_faceAnime(faceAnimeList, -1f);
                        }
                        else
                        {
                            maid.change_faceAnime(faceAnimeList, num);
                        }
                    }
                }
                int num2 = -1;
                int num3 = -1;
                bool enableYodare = false;
                bool flag6 = paramDict.ContainsKey("namida") || paramDict.ContainsKey("涙");
                if (flag6)
                {
                    bool flag7 = paramDict.ContainsKey("namida");
                    if (flag7)
                    {
                        num3 = (int)this.parseFloat(paramDict["namida"], null);
                    }
                    bool flag8 = paramDict.ContainsKey("涙");
                    if (flag8)
                    {
                        num3 = (int)this.parseFloat(paramDict["涙"], null);
                    }
                    bool flag9 = num3 < 0 || num3 > 3;
                    if (flag9)
                    {
                        Util.info(string.Format("line{0} : 涙の値は0~3である必要があります。強制的に0にします。", this.currentExecuteLine.ToString()));
                        num3 = 0;
                    }
                }
                bool flag10 = paramDict.ContainsKey("hoho") || paramDict.ContainsKey("頬");
                if (flag10)
                {
                    bool flag11 = paramDict.ContainsKey("hoho");
                    if (flag11)
                    {
                        num2 = (int)this.parseFloat(paramDict["hoho"], null);
                    }
                    bool flag12 = paramDict.ContainsKey("頬");
                    if (flag12)
                    {
                        num2 = (int)this.parseFloat(paramDict["頬"], null);
                    }
                    bool flag13 = num2 < 0 || num2 > 3;
                    if (flag13)
                    {
                        Util.info(string.Format("line{0} : 頬の値は0~3である必要があります。強制的に0にします。", this.currentExecuteLine.ToString()));
                        num2 = 0;
                    }
                }
                bool flag14 = paramDict.ContainsKey("yodare") || paramDict.ContainsKey("よだれ");
                if (flag14)
                {
                    int num4 = -1;
                    bool flag15 = paramDict.ContainsKey("yodare");
                    if (flag15)
                    {
                        num4 = (int)this.parseFloat(paramDict["yodare"], null);
                    }
                    bool flag16 = paramDict.ContainsKey("頬");
                    if (flag16)
                    {
                        num4 = (int)this.parseFloat(paramDict["頬"], null);
                    }
                    bool flag17 = num4 == 1;
                    if (flag17)
                    {
                        enableYodare = true;
                    }
                    maid.change_FaceBlend(-1, -1, enableYodare);
                }
                maid.change_FaceBlend(num2, num3, enableYodare);
            }
        }

        // Token: 0x06000040 RID: 64 RVA: 0x00005A58 File Offset: 0x00003C58
        private void exec_posRelative(Dictionary<string, string> paramDict)
        {
            List<ScriplayPlugin.IMaid> list = this.selectMaid(paramDict);
            foreach (ScriplayPlugin.IMaid maid in list)
            {
                float x = 0f;
                float y = 0f;
                float z = 0f;
                bool flag = paramDict.ContainsKey("x");
                if (flag)
                {
                    x = this.parseFloat(paramDict["x"], null);
                }
                bool flag2 = paramDict.ContainsKey("y");
                if (flag2)
                {
                    y = this.parseFloat(paramDict["y"], null);
                }
                bool flag3 = paramDict.ContainsKey("z");
                if (flag3)
                {
                    z = this.parseFloat(paramDict["z"], null);
                }
                maid.change_positionRelative(x, y, z);
            }
        }

        // Token: 0x06000041 RID: 65 RVA: 0x00005B48 File Offset: 0x00003D48
        private void exec_rotRelative(Dictionary<string, string> paramDict)
        {
            List<ScriplayPlugin.IMaid> list = this.selectMaid(paramDict);
            foreach (ScriplayPlugin.IMaid maid in list)
            {
                float x_deg = 0f;
                float y_deg = 0f;
                float z_deg = 0f;
                bool flag = paramDict.ContainsKey("x");
                if (flag)
                {
                    x_deg = this.parseFloat(paramDict["x"], null);
                }
                bool flag2 = paramDict.ContainsKey("y");
                if (flag2)
                {
                    y_deg = this.parseFloat(paramDict["y"], null);
                }
                bool flag3 = paramDict.ContainsKey("z");
                if (flag3)
                {
                    z_deg = this.parseFloat(paramDict["z"], null);
                }
                maid.change_angleRelative(x_deg, y_deg, z_deg);
            }
        }

        // Token: 0x06000042 RID: 66 RVA: 0x00005C38 File Offset: 0x00003E38
        private void exec_posAbsolute(Dictionary<string, string> paramDict)
        {
            List<ScriplayPlugin.IMaid> list = this.selectMaid(paramDict);
            foreach (ScriplayPlugin.IMaid maid in list)
            {
                bool keepX = true;
                bool keepY = true;
                bool keepZ = true;
                float x = 0f;
                float y = 0f;
                float z = 0f;
                bool flag = paramDict.ContainsKey("x");
                if (flag)
                {
                    keepX = false;
                    x = this.parseFloat(paramDict["x"], null);
                }
                bool flag2 = paramDict.ContainsKey("y");
                if (flag2)
                {
                    keepY = false;
                    y = this.parseFloat(paramDict["y"], null);
                }
                bool flag3 = paramDict.ContainsKey("z");
                if (flag3)
                {
                    keepZ = false;
                    z = this.parseFloat(paramDict["z"], null);
                }
                maid.change_positionAbsolute(x, y, z, keepX, keepY, keepZ);
            }
        }

        // Token: 0x06000043 RID: 67 RVA: 0x00005D40 File Offset: 0x00003F40
        private void exec_rotAbsolute(Dictionary<string, string> paramDict)
        {
            List<ScriplayPlugin.IMaid> list = this.selectMaid(paramDict);
            foreach (ScriplayPlugin.IMaid maid in list)
            {
                bool keepX = true;
                bool keepY = true;
                bool keepZ = true;
                float x_deg = 0f;
                float y_deg = 0f;
                float z_deg = 0f;
                bool flag = paramDict.ContainsKey("x");
                if (flag)
                {
                    keepX = false;
                    x_deg = this.parseFloat(paramDict["x"], null);
                }
                bool flag2 = paramDict.ContainsKey("y");
                if (flag2)
                {
                    keepY = false;
                    y_deg = this.parseFloat(paramDict["y"], null);
                }
                bool flag3 = paramDict.ContainsKey("z");
                if (flag3)
                {
                    keepZ = false;
                    z_deg = this.parseFloat(paramDict["z"], null);
                }
                maid.change_angleAbsolute(x_deg, y_deg, z_deg, keepX, keepY, keepZ);
            }
        }

        // Token: 0x06000044 RID: 68 RVA: 0x00005E48 File Offset: 0x00004048
        private void exec_show(Dictionary<string, string> paramDict, int lineNo = -1)
        {
            Util.debug(string.Format("line{0} : show ", this.currentExecuteLine.ToString()));
            bool flag = paramDict.ContainsKey("text");
            if (flag)
            {
                this.showText = paramDict["text"];
                bool flag2 = paramDict.ContainsKey("wait");
                if (flag2)
                {
                    this.showText_waitTime = this.parseFloat(paramDict["wait"], new string[]
                    {
                        "sec",
                        "s"
                    });
                }
                else
                {
                    this.showText_waitTime = (float)this.showText.Length / 10f;
                    this.showText_waitTime = Math.Max(this.showText_waitTime, 1f);
                }
            }
            else
            {
                Util.info(string.Format("line{0} : 表示するテキストが見つかりません", this.currentExecuteLine.ToString()));
            }
        }

        // Token: 0x06000045 RID: 69 RVA: 0x00005F24 File Offset: 0x00004124
        private void exec_talk(Dictionary<string, string> paramDict, bool loop = false, int lineNo = -1)
        {
            Util.debug(string.Format("line{0} : talk ", this.currentExecuteLine.ToString()));
            List<ScriplayPlugin.IMaid> list = this.selectMaid(paramDict);
            foreach (ScriplayPlugin.IMaid maid in list)
            {
                List<string> list2 = new List<string>();
                bool flag = paramDict.ContainsKey("finish");
                if (flag)
                {
                    bool flag2 = paramDict["finish"] == "1";
                    if (flag2)
                    {
                        this.talk_waitUntilFinishSpeekingDict[maid.maidNo] = true;
                    }
                }
                bool flag3 = paramDict.ContainsKey("name");
                if (flag3)
                {
                    list2.Add(paramDict["name"]);
                }
                else
                {
                    bool flag4 = paramDict.ContainsKey("category");
                    if (flag4)
                    {
                        string text = paramDict["category"];
                        if (loop)
                        {
                            list2 = ScriplayPlugin.LoopVoiceTable.queryTable(maid.sPersonal, text);
                        }
                        else
                        {
                            list2 = ScriplayPlugin.OnceVoiceTable.queryTable(maid.sPersonal, text);
                        }
                        bool flag5 = list2.Count == 0;
                        if (flag5)
                        {
                            Util.info(string.Format("line{0} : カテゴリのボイスが見つかりません。カテゴリ：{1}", this.currentExecuteLine.ToString(), text));
                            break;
                        }
                    }
                }
                bool flag6 = list2.Count == 0 | (list2.Count == 1 && list2[0].ToLower().Equals("stop"));
                if (flag6)
                {
                    maid.change_stopVoice();
                }
                if (loop)
                {
                    maid.change_LoopVoice(list2);
                }
                else
                {
                    maid.change_onceVoice(list2);
                }
            }
        }

        // Token: 0x06000046 RID: 70 RVA: 0x000060F0 File Offset: 0x000042F0
        private List<ScriplayPlugin.IMaid> selectMaid(Dictionary<string, string> paramDict)
        {
            List<ScriplayPlugin.IMaid> list = new List<ScriplayPlugin.IMaid>();
            bool flag = paramDict.ContainsKey("maid");
            if (flag)
            {
                int num = int.Parse(paramDict["maid"]);
                bool flag2 = num < ScriplayPlugin.maidList.Count;
                if (flag2)
                {
                    list.Add(ScriplayPlugin.maidList[num]);
                }
                else
                {
                    Util.info(string.Format("メイドは{0}人しか有効にしていません。maidNo.{1}は無効です", ScriplayPlugin.maidList.Count, num));
                }
            }
            else
            {
                list = new List<ScriplayPlugin.IMaid>(ScriplayPlugin.maidList);
            }
            return list;
        }

        // Token: 0x06000047 RID: 71 RVA: 0x0000618C File Offset: 0x0000438C
        private void exec_goto(string gotoLabel)
        {
            bool flag = !this.labelMap.ContainsKey(gotoLabel);
            if (flag)
            {
                Util.info(string.Format("line{0} : ジャンプ先ラベルが見つかりません。ジャンプ先：{1}", this.currentExecuteLine.ToString(), gotoLabel));
                this.scriptFinished = true;
            }
            this.currentExecuteLine = this.labelMap[gotoLabel];
            Util.debug(string.Format("line{0} : 「{1}」へジャンプしました", this.currentExecuteLine.ToString(), gotoLabel));
        }

        // Token: 0x06000048 RID: 72 RVA: 0x00006204 File Offset: 0x00004404
        public void addSelection(string itemViewStr, Dictionary<string, string> dict)
        {
            string gotoLabel = "";
            bool flag = dict.ContainsKey("goto");
            if (flag)
            {
                gotoLabel = dict["goto"];
            }
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            foreach (string text in dict.Keys)
            {
                bool flag2 = text.Equals("goto");
                if (!flag2)
                {
                    string key = text;
                    try
                    {
                        int value = int.Parse(dict[text].Replace("%", ""));
                        dictionary.Add(key, value);
                    }
                    catch (Exception ex)
                    {
                        Util.info(string.Format("選択肢\u3000自動選択確率の読み込みに失敗 : {0}, {1}, {2} \r\n {3}", new object[]
                        {
                            itemViewStr,
                            text,
                            dict[text],
                            ex.StackTrace
                        }));
                    }
                }
            }
            this.selection_selectionList.Add(new ScriplayContext.Selection(itemViewStr, gotoLabel, dictionary));
            Util.debug(string.Format("選択肢「{0}」を追加", itemViewStr));
        }

        // Token: 0x04000037 RID: 55
        public static ScriplayContext None = new ScriplayContext(" - ", true);

        // Token: 0x04000038 RID: 56
        private IDictionary<string, int> labelMap = new Dictionary<string, int>();

        // Token: 0x04000039 RID: 57
        private string[] scriptArray = new string[0];

        // Token: 0x0400003A RID: 58
        public int currentExecuteLine = -1;

        // Token: 0x0400003B RID: 59
        private float waitSecond = 0f;

        // Token: 0x0400003C RID: 60
        public bool scriptFinished_flag = false;

        // Token: 0x0400003D RID: 61
        private float selection_waitSecond = 0f;

        // Token: 0x0400003E RID: 62
        public ScriplayContext.Selection selection_selectedItem = ScriplayContext.Selection.None;

        // Token: 0x0400003F RID: 63
        public List<ScriplayContext.Selection> selection_selectionList = new List<ScriplayContext.Selection>();

        // Token: 0x04000040 RID: 64
        public Dictionary<int, bool> talk_waitUntilFinishSpeekingDict = new Dictionary<int, bool>();

        // Token: 0x04000041 RID: 65
        public readonly string scriptName = "";

        // Token: 0x04000042 RID: 66
        public string showText = "";

        // Token: 0x04000043 RID: 67
        private float showText_waitTime = -1f;

        // Token: 0x04000044 RID: 68
        private static Regex reg_comment = new Regex("^//.+", RegexOptions.IgnoreCase);

        // Token: 0x04000045 RID: 69
        private static Regex reg_label = new Regex("^#+\\s*(.+)", RegexOptions.IgnoreCase);

        // Token: 0x04000046 RID: 70
        private static Regex reg_scriptInfo = new Regex("^@info\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x04000047 RID: 71
        private static Regex reg_require = new Regex("^@require\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x04000048 RID: 72
        private static Regex reg_auto = new Regex("^@auto\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x04000049 RID: 73
        private static Regex reg_posRelative = new Regex("^@posRelative\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x0400004A RID: 74
        private static Regex reg_posAbsolute = new Regex("^@posAbsolute\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x0400004B RID: 75
        private static Regex reg_rotRelative = new Regex("^@rotRelative\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x0400004C RID: 76
        private static Regex reg_rotAbsolute = new Regex("^@rotAbsolute\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x0400004D RID: 77
        private static Regex reg_sound = new Regex("^@sound\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x0400004E RID: 78
        private static Regex reg_motion = new Regex("^@motion\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x0400004F RID: 79
        private static Regex reg_face = new Regex("^@face\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x04000050 RID: 80
        private static Regex reg_wait = new Regex("^@wait\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x04000051 RID: 81
        private static Regex reg_goto = new Regex("^@goto\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x04000052 RID: 82
        private static Regex reg_show = new Regex("^@show\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x04000053 RID: 83
        private static Regex reg_talk = new Regex("^@talk\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x04000054 RID: 84
        private static Regex reg_talkRepeat = new Regex("^@talkrepeat\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x04000055 RID: 85
        private static Regex reg_selection = new Regex("^@selection\\s*([^\\s]+)?", RegexOptions.IgnoreCase);

        // Token: 0x04000056 RID: 86
        private static Regex reg_selectionItem = new Regex("[-]\\s+([^\\s]+)\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x04000057 RID: 87
        private static Regex reg_eyeToCam = new Regex("^@eyeToCam\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x04000058 RID: 88
        private static Regex reg_headToCam = new Regex("^@headToCam\\s+(.+)", RegexOptions.IgnoreCase);

        // Token: 0x04000059 RID: 89
        private static Regex parseParameter_regex = new Regex("\\s+");

        // Token: 0x0400005A RID: 90
        private static Regex parseParameter_regex_header = new Regex("^\\s+");

        // Token: 0x0400005B RID: 91
        private static Regex parseParameter_regex_footer = new Regex("\\s+$");

        // Token: 0x0400005C RID: 92
        private List<string> autoModeList = new List<string>();

        // Token: 0x0400005D RID: 93
        private const string key_maid = "maid";

        // Token: 0x0400005E RID: 94
        private const string key_name = "name";

        // Token: 0x0400005F RID: 95
        private const string key_category = "category";

        // Token: 0x04000060 RID: 96
        private const string key_mode = "mode";

        // Token: 0x04000061 RID: 97
        private const string key_fade = "fade";

        // Token: 0x04000062 RID: 98
        private const string key_wait = "wait";

        // Token: 0x04000063 RID: 99
        private const string key_finish = "finish";

        // Token: 0x04000064 RID: 100
        private const string key_goto = "goto";

        // Token: 0x04000065 RID: 101
        private const string key_maidNum = "maidNum";

        // Token: 0x04000066 RID: 102
        private const string key_manNum = "manNum";

        // Token: 0x04000067 RID: 103
        private const string key_text = "text";

        // Token: 0x04000068 RID: 104
        private const string key_hoho = "hoho";

        // Token: 0x04000069 RID: 105
        private const string key_namida = "namida";

        // Token: 0x0400006A RID: 106
        private const string key_yodare = "yodare";

        // Token: 0x0400006B RID: 107
        private const string key_頬 = "頬";

        // Token: 0x0400006C RID: 108
        private const string key_涙 = "涙";

        // Token: 0x0400006D RID: 109
        private const string key_よだれ = "よだれ";

        // Token: 0x0400006E RID: 110
        private const string key_x = "x";

        // Token: 0x0400006F RID: 111
        private const string key_y = "y";

        // Token: 0x04000070 RID: 112
        private const string key_z = "z";

        // Token: 0x0200000E RID: 14
        public class Selection
        {
            // Token: 0x06000092 RID: 146 RVA: 0x00008468 File Offset: 0x00006668
            public Selection(string viewStr, string gotoLabel, Dictionary<string, int> autoProbDict)
            {
                this.viewStr = viewStr;
                this.gotoLabel = gotoLabel;
                this.autoProbDict = autoProbDict;
            }

            // Token: 0x040000A7 RID: 167
            public static readonly ScriplayContext.Selection None = new ScriplayContext.Selection("選択肢なし", "", new Dictionary<string, int>());

            // Token: 0x040000A8 RID: 168
            public readonly string viewStr;

            // Token: 0x040000A9 RID: 169
            public readonly string gotoLabel;

            // Token: 0x040000AA RID: 170
            public readonly Dictionary<string, int> autoProbDict;
        }
    }


    // Token: 0x02000003 RID: 3
    public static class Util
    {
        // Token: 0x0600001D RID: 29 RVA: 0x00003FAC File Offset: 0x000021AC
        public static List<string> getFileFullpathList(string searchPath, string suffix)
        {
            suffix = "*." + suffix;
            bool flag = !Directory.Exists(searchPath);
            if (flag)
            {
                DirectoryInfo directoryInfo = Directory.CreateDirectory(searchPath);
            }
            return new List<string>(Directory.GetFiles(searchPath, suffix));
        }

        // Token: 0x0600001E RID: 30 RVA: 0x00003FED File Offset: 0x000021ED
        public static void info(string message)
        {
            Console.WriteLine("[Scriplay]" + Util.PluginMessage(message));
        }

        // Token: 0x0600001F RID: 31 RVA: 0x00004008 File Offset: 0x00002208
        public static void debug(string message)
        {
            bool debugMode = ScriplayPlugin.cfg.debugMode;
            if (debugMode)
            {
                UnityEngine.Debug.Log("[Scriplay]" + Util.PluginMessage(message));
            }
        }

        // Token: 0x06000020 RID: 32 RVA: 0x0000403C File Offset: 0x0000223C
        private static string PluginMessage(string originalMessage)
        {
            return string.Format("{0} > {1}", ScriplayPlugin.cfg.PluginName, originalMessage);
        }

        // Token: 0x06000021 RID: 33 RVA: 0x00004064 File Offset: 0x00002264
        public static int randomInt(int min, int max, List<int> excludeList = null)
        {
            bool flag = min >= max;
            int result;
            if (flag)
            {
                result = min;
            }
            else
            {
                bool flag2 = excludeList == null;
                if (flag2)
                {
                    excludeList = new List<int>();
                }
                Util.debug(string.Format("randomInt min:{0}, max:{1}, exclude{2}", min, max, Util.list2Str<int>(excludeList)));
                List<int> list = Enumerable.Range(min, max).ToList<int>();
                foreach (int item in excludeList)
                {
                    bool flag3 = list.Count == 1;
                    if (flag3)
                    {
                        break;
                    }
                    list.Remove(item);
                }
                result = list[new System.Random().Next(list.ToArray().Length)];
            }
            return result;
        }

        // Token: 0x06000022 RID: 34 RVA: 0x00004138 File Offset: 0x00002338
        public static string list2Str<T>(IEnumerable<T> collection)
        {
            bool flag = collection == null || collection.Count<T>() == 0;
            string result;
            if (flag)
            {
                result = "(要素数\u3000０）";
            }
            else
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (T t in collection)
                {
                    stringBuilder.Append(t.ToString() + ", ");
                }
                result = stringBuilder.ToString();
            }
            return result;
        }

        // Token: 0x06000023 RID: 35 RVA: 0x000041C8 File Offset: 0x000023C8
        public static int randomInt(int min, int max, int excludeValue)
        {
            return Util.randomInt(min, max, new List<int>
            {
                excludeValue
            });
        }

        // Token: 0x06000024 RID: 36 RVA: 0x000041F0 File Offset: 0x000023F0
        public static string getScoreText(int level)
        {
            level = Mathf.Clamp(level, 0, 3);
            return Util.SucoreText[level];
        }

        // Token: 0x06000025 RID: 37 RVA: 0x00004214 File Offset: 0x00002414
        public static void sw_start(string s = "")
        {
            bool flag = !Util.enSW;
            if (!flag)
            {
                bool flag2 = Util.sw_frames++ <= Util.stopwatch_executeFrames;
                if (!flag2)
                {
                    Util.sw.Start();
                }
            }
        }

        // Token: 0x06000026 RID: 38 RVA: 0x00004258 File Offset: 0x00002458
        public static void sw_stop(string s = "")
        {
            bool flag = !Util.enSW;
            if (!flag)
            {
                bool flag2 = Util.sw_frames <= Util.stopwatch_executeFrames;
                if (!flag2)
                {
                    Util.sw_frames = 0;
                    Util.sw.Stop();
                    Util.sw.Reset();
                }
            }
        }

        // Token: 0x06000027 RID: 39 RVA: 0x000042A8 File Offset: 0x000024A8
        public static void sw_showTime(string s = "")
        {
            bool flag = !Util.enSW;
            if (!flag)
            {
                bool flag2 = Util.sw_frames <= Util.stopwatch_executeFrames;
                if (!flag2)
                {
                    Util.sw.Stop();
                    Util.debug(string.Format("{0} 経過時間：{1} ms", s, Util.sw.ElapsedMilliseconds));
                    Util.sw.Reset();
                    Util.sw.Start();
                }
            }
        }

        // Token: 0x06000028 RID: 40 RVA: 0x0000431C File Offset: 0x0000251C
        public static void animate(Maid maid, string motionName, bool isLoop, float fadeTime = 0.5f, float speed = 1f, bool addQue = false)
        {
            try
            {
                bool flag = !addQue;
                if (flag)
                {
                    maid.CrossFadeAbsolute(motionName, false, isLoop, false, fadeTime, 1f);
                    maid.body0.m_Animation[motionName].speed = speed;
                }
                else
                {
                    maid.CrossFade(motionName, false, isLoop, true, fadeTime, 1f);
                }
            }
            catch (Exception ex)
            {
                Util.info("モーション再生失敗" + ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        // Token: 0x06000029 RID: 41 RVA: 0x000043B0 File Offset: 0x000025B0
        public static float var20p(float v)
        {
            return UnityEngine.Random.Range(v * 0.8f, v * 1.2f);
        }

        // Token: 0x0600002A RID: 42 RVA: 0x000043D8 File Offset: 0x000025D8
        public static float var50p(float v)
        {
            return UnityEngine.Random.Range(v * 0.5f, v * 1.5f);
        }

        // Token: 0x0600002B RID: 43 RVA: 0x000043FD File Offset: 0x000025FD
        internal static float var50p(object sio_baseTime)
        {
            throw new NotImplementedException();
        }

        // Token: 0x0600002C RID: 44 RVA: 0x00004408 File Offset: 0x00002608
        public static string pickOneOrEmptyString(List<string> list, int excludeIndex = -1)
        {
            bool flag = list.Count == 0;
            string result;
            if (flag)
            {
                result = "";
            }
            else
            {
                result = list[Util.randomInt(0, list.Count - 1, excludeIndex)];
            }
            return result;
        }

        // Token: 0x0600002D RID: 45 RVA: 0x00004444 File Offset: 0x00002644
        public static string[] readAllText(string filepath)
        {
            Util.debug(string.Format("文字列読み込み開始 {0}", filepath));
            string text = File.ReadAllText(filepath);
            string[] array = text.Split(new string[]
            {
                "\r\n"
            }, StringSplitOptions.None);
            bool flag = array.Length < 2;
            if (flag)
            {
                array = text.Split(new string[]
                {
                    "\n"
                }, StringSplitOptions.None);
            }
            Util.debug(string.Format("文字列読み込み終了 {0}", filepath));
            return array;
        }

        // Token: 0x0600002E RID: 46 RVA: 0x000044B8 File Offset: 0x000026B8
        public static string[][] ReadCsvFile(string file, bool enSkipFirstCol = true)
        {
            string[] array = Util.readAllText(file);
            List<string[]> list = new List<string[]>();
            bool flag = false;
            foreach (string text in array)
            {
                List<string> list2 = new List<string>();
                int num = 0;
                bool flag2 = !flag;
                if (flag2)
                {
                    flag = true;
                }
                else
                {
                    string[] array3;
                    if (enSkipFirstCol)
                    {
                        array3 = text.Split(new char[]
                        {
                            ','
                        }).Skip(1).ToArray<string>();
                    }
                    else
                    {
                        array3 = text.Split(new char[]
                        {
                            ','
                        }).ToArray<string>();
                    }
                    foreach (string text2 in array3)
                    {
                        bool flag3 = text2 != "";
                        if (flag3)
                        {
                            list2.Add(text2);
                        }
                        else
                        {
                            bool flag4 = num <= 3 && text2 == "";
                            if (flag4)
                            {
                                list2.Add("0");
                            }
                        }
                        num++;
                    }
                    list.Add(list2.ToArray());
                }
            }
            Util.debug(string.Format("文字配列へ分割終了 {0}", file));
            return list.ToArray();
        }

        // Token: 0x04000032 RID: 50
        private static string[] SucoreText = new string[]
        {
            "☆ ☆ ☆",
            "★ ☆ ☆",
            "★ ★ ☆",
            "★ ★ ★"
        };

        // Token: 0x04000033 RID: 51
        private static bool enSW = false;

        // Token: 0x04000034 RID: 52
        private static Stopwatch sw = new Stopwatch();

        // Token: 0x04000035 RID: 53
        private static readonly int stopwatch_executeFrames = 60;

        // Token: 0x04000036 RID: 54
        private static int sw_frames = 0;
    }
}
