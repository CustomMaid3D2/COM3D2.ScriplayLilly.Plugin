using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityInjector;
using UnityInjector.Attributes;

namespace GearMenu
{
    // Token: 0x02000002 RID: 2
    public static class Buttons
    {
        // Token: 0x17000001 RID: 1
        // (get) Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
        public static string Name
        {
            get
            {
                return Buttons.Name_;
            }
        }

        // Token: 0x17000002 RID: 2
        // (get) Token: 0x06000002 RID: 2 RVA: 0x00002057 File Offset: 0x00000257
        public static string Version
        {
            get
            {
                return Buttons.Version_;
            }
        }

        // Token: 0x06000003 RID: 3 RVA: 0x0000205E File Offset: 0x0000025E
        public static GameObject Add(PluginBase plugin, byte[] pngData, Action<GameObject> action)
        {
            return Buttons.Add(null, plugin, pngData, action);
        }

        // Token: 0x06000004 RID: 4 RVA: 0x0000206C File Offset: 0x0000026C
        public static GameObject Add(string name, PluginBase plugin, byte[] pngData, Action<GameObject> action)
        {
            PluginNameAttribute pluginNameAttribute = Attribute.GetCustomAttribute(plugin.GetType(), typeof(PluginNameAttribute)) as PluginNameAttribute;
            PluginVersionAttribute pluginVersionAttribute = Attribute.GetCustomAttribute(plugin.GetType(), typeof(PluginVersionAttribute)) as PluginVersionAttribute;
            string arg = (pluginNameAttribute == null) ? plugin.Name : pluginNameAttribute.Name;
            string arg2 = (pluginVersionAttribute == null) ? string.Empty : pluginVersionAttribute.Version;
            string label = string.Format("{0} {1}", arg, arg2);
            return Buttons.Add(name, label, pngData, action);
        }

        // Token: 0x06000005 RID: 5 RVA: 0x000020EA File Offset: 0x000002EA
        public static GameObject Add(string label, byte[] pngData, Action<GameObject> action)
        {
            return Buttons.Add(null, label, pngData, action);
        }

        // Token: 0x06000006 RID: 6 RVA: 0x000020F8 File Offset: 0x000002F8
        public static GameObject Add(string name, string label, byte[] pngData, Action<GameObject> action)
        {
            GameObject goButton = null;
            if (Buttons.Contains(name))
            {
                Buttons.Remove(name);
            }
            if (action == null)
            {
                return goButton;
            }
            try
            {
                goButton = NGUITools.AddChild(Buttons.Grid, UTY.GetChildObject(Buttons.Grid, "Config", true));
                if (name != null)
                {
                    goButton.name = name;
                }
                EventDelegate.Set(goButton.GetComponent<UIButton>().onClick, delegate ()
                {
                    action(goButton);
                });
                UIEventTrigger component = goButton.GetComponent<UIEventTrigger>();
                EventDelegate.Add(component.onHoverOut, delegate ()
                {
                    Buttons.SysShortcut.VisibleExplanation(null, false);
                });
                EventDelegate.Add(component.onDragStart, delegate ()
                {
                    Buttons.SysShortcut.VisibleExplanation(null, false);
                });
                Buttons.SetText(goButton, label);
                if (pngData == null)
                {
                    pngData = DefaultIcon.Png;
                }
                UISprite component2 = goButton.GetComponent<UISprite>();
                component2.type = UIBasicSprite.Type.Filled;
                component2.fillAmount = 0f;
                Texture2D texture2D = new Texture2D(1, 1);
                texture2D.LoadImage(pngData);
                UITexture uitexture = NGUITools.AddWidget<UITexture>(goButton);
                uitexture.material = new Material(uitexture.shader);
                uitexture.material.mainTexture = texture2D;
                uitexture.MakePixelPerfect();
                Buttons.Reposition();
            }
            catch
            {
                if (goButton != null)
                {
                    NGUITools.Destroy(goButton);
                    goButton = null;
                }
                throw;
            }
            return goButton;
        }

        // Token: 0x06000007 RID: 7 RVA: 0x000022A4 File Offset: 0x000004A4
        public static void Remove(string name)
        {
            Buttons.Remove(Buttons.Find(name));
        }

        // Token: 0x06000008 RID: 8 RVA: 0x000022B1 File Offset: 0x000004B1
        public static void Remove(GameObject go)
        {
            NGUITools.Destroy(go);
            Buttons.Reposition();
        }

        // Token: 0x06000009 RID: 9 RVA: 0x000022BE File Offset: 0x000004BE
        public static bool Contains(string name)
        {
            return Buttons.Find(name) != null;
        }

        // Token: 0x0600000A RID: 10 RVA: 0x000022CC File Offset: 0x000004CC
        public static bool Contains(GameObject go)
        {
            return Buttons.Contains(go.name);
        }

        // Token: 0x0600000B RID: 11 RVA: 0x000022D9 File Offset: 0x000004D9
        public static void SetFrameColor(string name, Color color)
        {
            Buttons.SetFrameColor(Buttons.Find(name), color);
        }

        // Token: 0x0600000C RID: 12 RVA: 0x000022E8 File Offset: 0x000004E8
        public static void SetFrameColor(GameObject go, Color color)
        {
            UITexture componentInChildren = go.GetComponentInChildren<UITexture>();
            if (componentInChildren == null)
            {
                return;
            }
            Texture2D texture2D = componentInChildren.mainTexture as Texture2D;
            if (texture2D == null)
            {
                return;
            }
            for (int i = 1; i < texture2D.width - 1; i++)
            {
                texture2D.SetPixel(i, 0, color);
                texture2D.SetPixel(i, texture2D.height - 1, color);
            }
            for (int j = 1; j < texture2D.height - 1; j++)
            {
                texture2D.SetPixel(0, j, color);
                texture2D.SetPixel(texture2D.width - 1, j, color);
            }
            texture2D.Apply();
        }

        // Token: 0x0600000D RID: 13 RVA: 0x0000237A File Offset: 0x0000057A
        public static void ResetFrameColor(string name)
        {
            Buttons.ResetFrameColor(Buttons.Find(name));
        }

        // Token: 0x0600000E RID: 14 RVA: 0x00002387 File Offset: 0x00000587
        public static void ResetFrameColor(GameObject go)
        {
            Buttons.SetFrameColor(go, Buttons.DefaultFrameColor);
        }

        // Token: 0x0600000F RID: 15 RVA: 0x00002394 File Offset: 0x00000594
        public static void SetText(string name, string label)
        {
            Buttons.SetText(Buttons.Find(name), label);
        }

        // Token: 0x06000010 RID: 16 RVA: 0x000023A4 File Offset: 0x000005A4
        public static void SetText(GameObject go, string label)
        {
            UIEventTrigger component = go.GetComponent<UIEventTrigger>();
            component.onHoverOver.Clear();
            EventDelegate.Add(component.onHoverOver, delegate ()
            {
                Buttons.SysShortcut.VisibleExplanation(label, label != null);
            });
            if (go.GetComponent<UIButton>().state == UIButtonColor.State.Hover)
            {
                Buttons.SysShortcut.VisibleExplanation(label, label != null);
            }
        }

        // Token: 0x06000011 RID: 17 RVA: 0x00002410 File Offset: 0x00000610
        private static GameObject Find(string name)
        {
            Transform transform = Buttons.GridUI.GetChildList().FirstOrDefault((Transform c) => c.gameObject.name == name);
            if (!(transform == null))
            {
                return transform.gameObject;
            }
            return null;
        }

        // Token: 0x06000012 RID: 18 RVA: 0x00002457 File Offset: 0x00000657
        private static void Reposition()
        {
            Buttons.SetAndCallOnReposition(Buttons.GridUI);
            Buttons.GridUI.repositionNow = true;
        }

        // Token: 0x06000013 RID: 19 RVA: 0x00002470 File Offset: 0x00000670
        private static void SetAndCallOnReposition(UIGrid uiGrid)
        {
            string onRepositionVersion = Buttons.GetOnRepositionVersion(uiGrid);
            if (onRepositionVersion == null)
            {
                return;
            }
            if (onRepositionVersion == string.Empty || string.Compare(onRepositionVersion, Buttons.Version, false) < 0)
            {
                uiGrid.onReposition = new UIGrid.OnReposition(new Buttons.OnRepositionHandler(Buttons.Version).OnReposition);
            }
            if (uiGrid.onReposition != null)
            {
                object target = uiGrid.onReposition.Target;
                if (target != null)
                {
                    MethodInfo method = target.GetType().GetMethod("PreOnReposition");
                    if (method != null)
                    {
                        method.Invoke(target, new object[0]);
                    }
                }
            }
        }

        // Token: 0x06000014 RID: 20 RVA: 0x000024F8 File Offset: 0x000006F8
        private static string GetOnRepositionVersion(UIGrid uiGrid)
        {
            if (uiGrid.onReposition == null)
            {
                return string.Empty;
            }
            object target = uiGrid.onReposition.Target;
            if (target == null)
            {
                return null;
            }
            Type type = target.GetType();
            if (type == null)
            {
                return null;
            }
            FieldInfo field = type.GetField("Version", BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
            {
                return null;
            }
            string text = field.GetValue(target) as string;
            if (text == null || !text.StartsWith(Buttons.Name))
            {
                return null;
            }
            return text;
        }

        // Token: 0x17000003 RID: 3
        // (get) Token: 0x06000015 RID: 21 RVA: 0x00002563 File Offset: 0x00000763
        public static SystemShortcut SysShortcut
        {
            get
            {
                return GameMain.Instance.SysShortcut;
            }
        }

        // Token: 0x17000004 RID: 4
        // (get) Token: 0x06000016 RID: 22 RVA: 0x0000256F File Offset: 0x0000076F
        public static UIPanel SysShortcutPanel
        {
            get
            {
                return Buttons.SysShortcut.GetComponent<UIPanel>();
            }
        }

        // Token: 0x17000005 RID: 5
        // (get) Token: 0x06000017 RID: 23 RVA: 0x0000257C File Offset: 0x0000077C
        public static UISprite SysShortcutExplanation
        {
            get
            {
                FieldInfo field = typeof(SystemShortcut).GetField("m_spriteExplanation", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null)
                {
                    return null;
                }
                return field.GetValue(Buttons.SysShortcut) as UISprite;
            }
        }

        // Token: 0x17000006 RID: 6
        // (get) Token: 0x06000018 RID: 24 RVA: 0x000025B5 File Offset: 0x000007B5
        public static GameObject Base
        {
            get
            {
                return Buttons.SysShortcut.gameObject.transform.Find("Base").gameObject;
            }
        }

        // Token: 0x17000007 RID: 7
        // (get) Token: 0x06000019 RID: 25 RVA: 0x000025D5 File Offset: 0x000007D5
        public static UISprite BaseSprite
        {
            get
            {
                return Buttons.Base.GetComponent<UISprite>();
            }
        }

        // Token: 0x17000008 RID: 8
        // (get) Token: 0x0600001A RID: 26 RVA: 0x000025E1 File Offset: 0x000007E1
        public static GameObject Grid
        {
            get
            {
                return Buttons.Base.gameObject.transform.Find("Grid").gameObject;
            }
        }

        // Token: 0x17000009 RID: 9
        // (get) Token: 0x0600001B RID: 27 RVA: 0x00002601 File Offset: 0x00000801
        public static UIGrid GridUI
        {
            get
            {
                return Buttons.Grid.GetComponent<UIGrid>();
            }
        }

        // Token: 0x04000001 RID: 1
        private static string Name_ = "CM3D2.GearMenu.Buttons";

        // Token: 0x04000002 RID: 2
        private static string Version_ = Buttons.Name_ + " 0.0.2.0";

        // Token: 0x04000003 RID: 3
        public static readonly Color DefaultFrameColor = new Color(1f, 1f, 1f, 0f);

        // Token: 0x02000008 RID: 8
        private class OnRepositionHandler
        {
            // Token: 0x06000048 RID: 72 RVA: 0x000054E3 File Offset: 0x000036E3
            public OnRepositionHandler(string version)
            {
                this.Version = version;
            }

            // Token: 0x06000049 RID: 73 RVA: 0x00005394 File Offset: 0x00003594
            public void OnReposition()
            {
            }

            // Token: 0x0600004A RID: 74 RVA: 0x000054F4 File Offset: 0x000036F4
            public void PreOnReposition()
            {
                UIGrid gridUI = Buttons.GridUI;
                UISprite baseSprite = Buttons.BaseSprite;
                float num = 0.75f;
                float pixelSizeAdjustment = UIRoot.GetPixelSizeAdjustment(Buttons.Base);
                gridUI.cellHeight = gridUI.cellWidth;
                gridUI.arrangement = UIGrid.Arrangement.CellSnap;
                gridUI.sorting = UIGrid.Sorting.None;
                gridUI.pivot = UIWidget.Pivot.TopRight;
                gridUI.maxPerLine = (int)((float)Screen.width / (gridUI.cellWidth / pixelSizeAdjustment) * num);
                List<Transform> childList = gridUI.GetChildList();
                int count = childList.Count;
                int num2 = Math.Min(gridUI.maxPerLine, count);
                int num3 = Math.Max(1, (count - 1) / gridUI.maxPerLine + 1);
                int num4 = (int)(gridUI.cellWidth * 3f / 2f + 8f);
                int num5 = (int)(gridUI.cellHeight / 2f);
                float num6 = (float)num5 * 1.5f + 1f;
                baseSprite.pivot = UIWidget.Pivot.TopRight;
                baseSprite.width = (int)((float)num4 + gridUI.cellWidth * (float)num2);
                baseSprite.height = (int)((float)num5 + gridUI.cellHeight * (float)num3 + 2f);
                Buttons.Base.transform.localPosition = new Vector3(946f, 502f + num6, 0f);
                Buttons.Grid.transform.localPosition = new Vector3(-2f + (float)(-(float)num2 - 1 + num3 - 1) * gridUI.cellWidth, -1f - num6, 0f);
                int num7 = 0;
                string[] array = GameMain.Instance.CMSystem.NetUse ? Buttons.OnRepositionHandler.OnlineButtonNames : Buttons.OnRepositionHandler.OfflineButtonNames;
                foreach (Transform transform in childList)
                {
                    int num8 = num7++;
                    int num9 = Array.IndexOf<string>(array, transform.gameObject.name);
                    if (num9 >= 0)
                    {
                        num8 = num9;
                    }
                    float x = (float)(-(float)num8 % gridUI.maxPerLine + num2 - 1) * gridUI.cellWidth;
                    float num10 = (float)(num8 / gridUI.maxPerLine) * gridUI.cellHeight;
                    transform.localPosition = new Vector3(x, -num10, 0f);
                }
                UISprite sysShortcutExplanation = Buttons.SysShortcutExplanation;
                Vector3 localPosition = sysShortcutExplanation.gameObject.transform.localPosition;
                localPosition.y = Buttons.Base.transform.localPosition.y - (float)baseSprite.height - (float)sysShortcutExplanation.height;
                sysShortcutExplanation.gameObject.transform.localPosition = localPosition;
            }

            // Token: 0x04000038 RID: 56
            public string Version;

            // Token: 0x04000039 RID: 57
            private static string[] OnlineButtonNames = new string[]
            {
                "Config",
                "Ss",
                "SsUi",
                "Shop",
                "ToTitle",
                "Info",
                "Exit"
            };

            // Token: 0x0400003A RID: 58
            private static string[] OfflineButtonNames = new string[]
            {
                "Config",
                "Ss",
                "SsUi",
                "ToTitle",
                "Info",
                "Exit"
            };
        }
    }



    // Token: 0x02000003 RID: 3
    internal static class DefaultIcon
    {
        // Token: 0x04000004 RID: 4
        public static byte[] Png = new byte[]
        {
            137,
            80,
            78,
            71,
            13,
            10,
            26,
            10,
            0,
            0,
            0,
            13,
            73,
            72,
            68,
            82,
            0,
            0,
            0,
            32,
            0,
            0,
            0,
            32,
            8,
            6,
            0,
            0,
            0,
            115,
            122,
            122,
            244,
            0,
            0,
            0,
            4,
            115,
            66,
            73,
            84,
            8,
            8,
            8,
            8,
            124,
            8,
            100,
            136,
            0,
            0,
            0,
            9,
            112,
            72,
            89,
            115,
            0,
            0,
            14,
            196,
            0,
            0,
            14,
            196,
            1,
            149,
            43,
            14,
            27,
            0,
            0,
            5,
            0,
            73,
            68,
            65,
            84,
            88,
            133,
            197,
            151,
            93,
            72,
            84,
            91,
            20,
            199,
            127,
            231,
            140,
            142,
            88,
            210,
            213,
            160,
            50,
            155,
            4,
            75,
            141,
            169,
            137,
            99,
            42,
            66,
            92,
            232,
            131,
            62,
            31,
            138,
            202,
            50,
            33,
            140,
            2,
            83,
            164,
            158,
            130,
            98,
            30,
            36,
            123,
            40,
            194,
            94,
            123,
            168,
            9,
            74,
            40,
            73,
            8,
            75,
            162,
            4,
            131,
            30,
            163,
            122,
            169,
            57,
            199,
            143,
            41,
            48,
            36,
            17,
            195,
            9,
            67,
            52,
            97,
            166,
            211,
            124,
            236,
            30,
            230,
            206,
            110,
            198,
            57,
            51,
            138,
            221,
            238,
            93,
            111,
            107,
            239,
            181,
            207,
            byte.MaxValue,
            119,
            214,
            89,
            123,
            237,
            125,
            20,
            33,
            132,
            224,
            127,
            180,
            172,
            63,
            45,
            16,
            10,
            133,
            24,
            30,
            30,
            70,
            215,
            117,
            52,
            77,
            99,
            243,
            230,
            205,
            127,
            22,
            32,
            26,
            141,
            50,
            62,
            62,
            142,
            215,
            235,
            69,
            215,
            117,
            6,
            7,
            7,
            9,
            6,
            131,
            0,
            204,
            204,
            204,
            252,
            25,
            128,
            169,
            169,
            41,
            12,
            195,
            64,
            215,
            117,
            12,
            195,
            224,
            235,
            215,
            175,
            150,
            113,
            186,
            174,
            167,
            140,
            253,
            22,
            64,
            95,
            95,
            31,
            79,
            159,
            62,
            101,
            116,
            116,
            116,
            65,
            241,
            159,
            63,
            127,
            102,
            114,
            114,
            146,
            21,
            43,
            86,
            196,
            135,
            132,
            186,
            88,
            113,
            191,
            223,
            207,
            173,
            91,
            183,
            44,
            197,
            179,
            179,
            179,
            113,
            185,
            92,
            150,
            235,
            222,
            189,
            123,
            151,
            232,
            138,
            69,
            101,
            64,
            8,
            65,
            71,
            71,
            7,
            161,
            80,
            8,
            0,
            155,
            205,
            70,
            89,
            89,
            25,
            154,
            166,
            81,
            81,
            81,
            129,
            211,
            233,
            228,
            245,
            235,
            215,
            12,
            13,
            13,
            165,
            172,
            213,
            117,
            157,
            253,
            251,
            247,
            199,
            93,
            101,
            65,
            0,
            129,
            64,
            128,
            39,
            79,
            158,
            16,
            14,
            135,
            81,
            85,
            21,
            33,
            4,
            126,
            191,
            159,
            218,
            218,
            90,
            52,
            77,
            195,
            229,
            114,
            177,
            100,
            201,
            18,
            25,
            31,
            12,
            6,
            233,
            232,
            232,
            144,
            126,
            93,
            93,
            29,
            221,
            221,
            221,
            0,
            244,
            247,
            247,
            19,
            141,
            70,
            81,
            213,
            88,
            242,
            231,
            5,
            8,
            6,
            131,
            180,
            181,
            181,
            225,
            243,
            249,
            228,
            216,
            217,
            179,
            103,
            185,
            113,
            227,
            70,
            218,
            53,
            143,
            31,
            63,
            150,
            133,
            88,
            83,
            83,
            67,
            67,
            67,
            3,
            207,
            158,
            61,
            227,
            251,
            247,
            239,
            204,
            204,
            204,
            48,
            58,
            58,
            202,
            186,
            117,
            235,
            0,
            200,
            88,
            3,
            129,
            64,
            128,
            75,
            151,
            46,
            37,
            137,
            55,
            53,
            53,
            113,
            240,
            224,
            193,
            180,
            107,
            190,
            124,
            249,
            194,
            163,
            71,
            143,
            98,
            111,
            151,
            149,
            69,
            115,
            115,
            51,
            118,
            187,
            29,
            77,
            211,
            100,
            76,
            66,
            29,
            164,
            47,
            194,
            64,
            32,
            144,
            242,
            230,
            103,
            206,
            156,
            225,
            200,
            145,
            35,
            105,
            197,
            133,
            16,
            220,
            189,
            123,
            23,
            211,
            52,
            1,
            56,
            124,
            248,
            48,
            107,
            214,
            172,
            1,
            160,
            170,
            170,
            74,
            198,
            37,
            108,
            71,
            195,
            18,
            192,
            42,
            237,
            141,
            141,
            141,
            212,
            214,
            214,
            162,
            40,
            74,
            90,
            0,
            159,
            207,
            199,
            203,
            151,
            47,
            1,
            200,
            207,
            207,
            167,
            190,
            190,
            94,
            206,
            37,
            2,
            188,
            127,
            byte.MaxValue,
            30,
            211,
            52,
            81,
            20,
            165,
            42,
            5,
            192,
            42,
            237,
            13,
            13,
            13,
            28,
            61,
            122,
            52,
            163,
            120,
            36,
            18,
            193,
            227,
            241,
            72,
            byte.MaxValue,
            212,
            169,
            83,
            228,
            229,
            229,
            73,
            127,
            245,
            234,
            213,
            20,
            21,
            21,
            1,
            96,
            154,
            38,
            31,
            62,
            124,
            0,
            230,
            212,
            128,
            85,
            218,
            143,
            31,
            63,
            206,
            137,
            19,
            39,
            50,
            138,
            3,
            188,
            120,
            241,
            130,
            145,
            145,
            17,
            0,
            74,
            75,
            75,
            217,
            179,
            103,
            79,
            210,
            188,
            162,
            40,
            84,
            86,
            86,
            2,
            177,
            109,
            187,
            124,
            249,
            114,
            32,
            97,
            23,
            88,
            137,
            151,
            151,
            151,
            179,
            97,
            195,
            6,
            222,
            188,
            121,
            147,
            81,
            92,
            8,
            193,
            189,
            123,
            247,
            164,
            223,
            220,
            220,
            140,
            205,
            102,
            75,
            137,
            171,
            174,
            174,
            166,
            183,
            183,
            151,
            67,
            135,
            14,
            81,
            92,
            92,
            28,
            3,
            19,
            66,
            8,
            43,
            241,
            197,
            218,
            246,
            237,
            219,
            113,
            187,
            221,
            150,
            25,
            11,
            4,
            2,
            180,
            180,
            180,
            112,
            243,
            230,
            77,
            242,
            242,
            242,
            80,
            20,
            69,
            81,
            1,
            166,
            167,
            167,
            153,
            152,
            152,
            248,
            109,
            113,
            128,
            85,
            171,
            86,
            165,
            253,
            92,
            217,
            217,
            217,
            172,
            92,
            185,
            146,
            220,
            220,
            92,
            57,
            166,
            196,
            47,
            36,
            227,
            227,
            227,
            184,
            221,
            110,
            166,
            166,
            166,
            228,
            100,
            73,
            73,
            9,
            91,
            182,
            108,
            153,
            87,
            84,
            8,
            65,
            111,
            111,
            47,
            161,
            80,
            8,
            187,
            221,
            206,
            237,
            219,
            183,
            41,
            44,
            44,
            76,
            137,
            243,
            249,
            124,
            92,
            184,
            112,
            129,
            115,
            231,
            206,
            113,
            224,
            192,
            1,
            20,
            69,
            249,
            213,
            138,
            29,
            14,
            7,
            215,
            175,
            95,
            79,
            130,
            24,
            27,
            27,
            227,
            228,
            201,
            147,
            108,
            221,
            186,
            117,
            94,
            136,
            172,
            172,
            44,
            186,
            187,
            187,
            249,
            241,
            227,
            7,
            119,
            238,
            220,
            161,
            181,
            181,
            53,
            37,
            19,
            111,
            223,
            190,
            5,
            224,
            254,
            253,
            251,
            108,
            219,
            182,
            13,
            152,
            179,
            11,
            226,
            16,
            241,
            10,
            141,
            68,
            34,
            92,
            187,
            118,
            77,
            46,
            204,
            100,
            245,
            245,
            245,
            20,
            20,
            20,
            0,
            240,
            234,
            213,
            43,
            6,
            7,
            7,
            83,
            98,
            188,
            94,
            47,
            0,
            179,
            179,
            179,
            242,
            160,
            74,
            233,
            3,
            115,
            33,
            194,
            225,
            48,
            87,
            175,
            94,
            165,
            191,
            191,
            63,
            35,
            192,
            210,
            165,
            75,
            57,
            125,
            250,
            180,
            244,
            61,
            30,
            15,
            145,
            72,
            68,
            250,
            211,
            211,
            211,
            124,
            252,
            248,
            49,
            38,
            170,
            170,
            242,
            184,
            182,
            236,
            132,
            14,
            135,
            131,
            246,
            246,
            118,
            249,
            70,
            166,
            105,
            114,
            249,
            242,
            101,
            6,
            6,
            6,
            50,
            66,
            236,
            222,
            189,
            155,
            178,
            178,
            50,
            0,
            62,
            125,
            250,
            196,
            243,
            231,
            207,
            229,
            156,
            97,
            24,
            196,
            239,
            191,
            235,
            215,
            175,
            103,
            217,
            178,
            101,
            8,
            33,
            122,
            210,
            158,
            5,
            107,
            215,
            174,
            77,
            129,
            104,
            107,
            107,
            203,
            8,
            161,
            170,
            42,
            45,
            45,
            45,
            210,
            239,
            236,
            236,
            100,
            118,
            118,
            22,
            32,
            233,
            51,
            38,
            20,
            246,
            223,
            25,
            79,
            195,
            226,
            226,
            98,
            75,
            136,
            120,
            27,
            181,
            50,
            167,
            211,
            201,
            206,
            157,
            59,
            129,
            216,
            37,
            180,
            171,
            171,
            11,
            33,
            132,
            252,
            254,
            144,
            116,
            46,
            252,
            165,
            44,
            228,
            191,
            96,
            108,
            108,
            12,
            143,
            199,
            67,
            56,
            28,
            6,
            192,
            110,
            183,
            227,
            112,
            56,
            168,
            168,
            168,
            192,
            229,
            114,
            37,
            245,
            124,
            128,
            201,
            201,
            73,
            154,
            154,
            154,
            48,
            77,
            19,
            155,
            205,
            198,
            197,
            139,
            23,
            105,
            111,
            111,
            7,
            32,
            39,
            39,
            135,
            135,
            15,
            31,
            146,
            147,
            147,
            3,
            16,
            93,
            16,
            192,
            92,
            139,
            70,
            163,
            156,
            63,
            127,
            158,
            225,
            225,
            97,
            84,
            85,
            165,
            180,
            180,
            20,
            77,
            211,
            208,
            52,
            141,
            141,
            27,
            55,
            146,
            155,
            155,
            75,
            87,
            87,
            23,
            157,
            157,
            157,
            64,
            108,
            139,
            198,
            225,
            171,
            171,
            171,
            185,
            114,
            229,
            138,
            124,
            212,
            162,
            0,
            224,
            87,
            83,
            153,
            107,
            54,
            155,
            13,
            167,
            211,
            73,
            121,
            121,
            57,
            61,
            61,
            61,
            41,
            243,
            141,
            141,
            141,
            28,
            59,
            118,
            76,
            2,
            44,
            250,
            86,
            188,
            105,
            211,
            38,
            246,
            237,
            219,
            151,
            210,
            108,
            34,
            145,
            8,
            67,
            67,
            67,
            150,
            226,
            128,
            60,
            17,
            byte.MaxValue,
            49,
            101,
            209,
            25,
            136,
            219,
            183,
            111,
            223,
            146,
            126,
            74,
            252,
            126,
            127,
            218,
            216,
            252,
            252,
            124,
            30,
            60,
            120,
            32,
            47,
            164,
            128,
            248,
            109,
            128,
            68,
            19,
            66,
            48,
            49,
            49,
            129,
            215,
            235,
            197,
            48,
            12,
            6,
            6,
            6,
            228,
            54,
            4,
            216,
            177,
            99,
            7,
            110,
            183,
            59,
            105,
            201,
            191,
            10,
            48,
            215,
            194,
            225,
            48,
            35,
            35,
            35,
            232,
            186,
            142,
            174,
            235,
            236,
            221,
            187,
            151,
            93,
            187,
            118,
            253,
            119,
            0,
            11,
            177,
            159,
            20,
            180,
            74,
            142,
            27,
            157,
            22,
            100,
            0,
            0,
            0,
            0,
            73,
            69,
            78,
            68,
            174,
            66,
            96,
            130
        };
    }
}


