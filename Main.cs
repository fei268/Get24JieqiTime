using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using System.Reflection.Emit;
using Label = UnityEngine.UIElements.Label;

public class Main : MonoBehaviour
{
    UIDocument uiDocument;

    string year;
    string jieqiName;
    string jieqiPinyin;

    private Dictionary<string, string> jieqiDict = new Dictionary<string, string>()
    {
        {"lichun","立春" },
        {"yushui","雨水" },
        {"jingzhe","惊蛰" },
        {"chunfen","春分" },
        {"qingming","清明" },
        {"guyu","谷雨" },
        {"lixia","立夏" },
        {"xiaoman","小满" },
        {"mangzhong","芒种" },
        {"xiazhi","夏至" },
        {"xiaoshu","小暑" },
        {"dashu","大暑" },
        {"liqiu","立秋" },
        {"chushu","处暑" },
        {"bailu","白露" },
        {"qiufen","秋分" },
        {"hanlu","寒露" },
        {"shuangjiang","霜降" },
        {"lidong","立冬" },
        {"xiaoxue","小雪" },
        {"daxue","大雪" },
        {"dongzhi","冬至" },
        {"xiaohan","小寒" },
        {"dahan","大寒" }
    };

    private ListView listView;
    private List<string> jieqiResult = new List<string>();

    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    void Start()
    {
        var root = uiDocument.rootVisualElement;

        TextField inputYear = root.Q<TextField>("InputYear");
        TextField inputName = root.Q<TextField>("InputName");
        Button btn = root.Q<Button>("Button");
        listView = root.Q<ListView>("Contents");

        listView.itemsSource = jieqiResult;
        listView.makeItem = () => new Label();
        listView.bindItem = (element, i) =>
        {
            (element as Label).text = jieqiResult[i];
        };

        btn.clicked += () =>
        {
            if (!int.TryParse(inputYear.value.Trim(), out int baseYear))
            {
                Debug.LogError("请输入正确的年份");
                return;
            }

            jieqiPinyin = inputName.value.Trim().ToLower();

            if (jieqiDict.ContainsKey(jieqiPinyin))
            {
                jieqiName = jieqiDict[jieqiPinyin];
                _ = GetDates(baseYear);   // 调用循环查询
            }
            else
            {
                Debug.LogError($"未找到节气拼音映射: {jieqiPinyin}");
            }
        };

        listView.RegisterCallback<ClickEvent>(evt =>
        {
            string allText = string.Join("\n", jieqiResult);
            GUIUtility.systemCopyBuffer = allText;
            Debug.Log("ListView 所有内容已复制到剪贴板");
        });
    }

    void Update()
    {
        
    }

    async Task GetDates(int baseYear)
    {
        jieqiResult.Clear();

        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                for (int i = 0; i < 14; i++) // 共14次查询
                {
                    int yearToQuery = baseYear + i * 10;
                    string url = $"https://jieqi.bmcx.com/{yearToQuery}_{jieqiPinyin}__jieqi/";

                    try
                    {
                        string html = await client.GetStringAsync(url);
                        string textOnly = Regex.Replace(html, "<.*?>", "");

                        // 临时列表保存当前组的数据
                        List<string> currentGroup = new List<string>();

                        // 正则1：匹配 “xxxx年的立夏时间 公历：yyyy年MM月dd日 HH:mm:ss”
                        string pattern1 = $@"\d{{4}}年的{jieqiName}时间\s*公历[:：]\s*(\d{{4}}年\d{{2}}月\d{{2}}日 \d{{2}}:\d{{2}}:\d{{2}})";
                        Regex regex1 = new Regex(pattern1);
                        MatchCollection matches1 = regex1.Matches(textOnly);

                        foreach (Match match in matches1)
                        {
                            string value = $"\"{match.Groups[1].Value}\",";
                            currentGroup.Add(value);
                        }

                        // 正则2：匹配 “xxxx年立夏 开始时间是 yyyy年MM月dd日 HH:mm:ss”
                        string pattern2 = $@"\d{{4}}年{jieqiName}\s*开始时间是\s*(\d{{4}}年\d{{2}}月\d{{2}}日 \d{{2}}:\d{{2}}:\d{{2}})";
                        Regex regex2 = new Regex(pattern2);
                        Match match2 = regex2.Match(textOnly);

                        if (match2.Success)
                        {
                            string startTime = $"\"{match2.Groups[1].Value}\",";

                            // 插入到当前组的中间位置
                            int insertIndex = currentGroup.Count / 2;
                            currentGroup.Insert(insertIndex, startTime);
                        }

                        // 将当前组数据加入总列表
                        jieqiResult.AddRange(currentGroup);

                        if (currentGroup.Count == 0)
                        {
                            jieqiResult.Add($"未找到匹配的公历时间 ({yearToQuery})");
                        }
                    }
                    catch (Exception innerEx)
                    {
                        jieqiResult.Add($"错误({yearToQuery}): {innerEx.Message}");
                    }
                }
            }

            // 刷新 ListView
            listView.RefreshItems();
        }
        catch (Exception ex)
        {
            Debug.Log("发生错误: " + ex.Message);
            jieqiResult.Clear();
            jieqiResult.Add($"错误: {ex.Message}");
            listView.RefreshItems();
        }
    }

}
