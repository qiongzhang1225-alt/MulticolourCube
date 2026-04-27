using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 全局设置面板（独立 Canvas，sortOrder=500）。
///
/// 功能页签：
///   [设置] BGM 音量、音效音量、全屏、分辨率、清除存档
///   [帮助] 游戏介绍与操作说明
///
/// 打开方式：
///   - 代码调用 SettingsPanel.Instance.Open()
///   - 任意场景按 Escape 时（若暂停菜单未拦截）
///
/// 持久化：所有设置通过 PlayerPrefs 保存，下次启动自动加载。
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    public static SettingsPanel Instance { get; private set; }

    // ══════════════════════════════════════
    //  UI 引用
    // ══════════════════════════════════════

    [Header("面板控制")]
    public CanvasGroup panelGroup;

    [Header("页签按钮")]
    public Button settingsTabBtn;
    public Button helpTabBtn;
    public Button closeBtn;

    [Header("页签内容")]
    public GameObject settingsPage;
    public GameObject helpPage;

    [Header("设置页 —— 音量")]
    public Slider bgmSlider;
    public TextMeshProUGUI bgmValueText;
    public Slider sfxSlider;
    public TextMeshProUGUI sfxValueText;

    [Header("设置页 —— 画面")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;

    [Header("设置页 —— 数据")]
    public Button clearDataBtn;
    public TextMeshProUGUI clearDataTip;

    [Header("帮助页")]
    public TextMeshProUGUI helpContentText;

    // ══════════════════════════════════════
    //  PlayerPrefs 键
    // ══════════════════════════════════════

    private const string KEY_BGM_VOL    = "Settings_BGMVolume";
    private const string KEY_SFX_VOL    = "Settings_SFXVolume";
    private const string KEY_FULLSCREEN = "Settings_Fullscreen";
    private const string KEY_RESOLUTION = "Settings_Resolution";

    // ══════════════════════════════════════
    //  内部
    // ══════════════════════════════════════

    private Resolution[] availableResolutions;
    private bool isOpen = false;
    private float previousTimeScale = 1f;
    private bool clearDataConfirm = false;

    // ══════════════════════════════════════
    //  生命周期
    // ══════════════════════════════════════

    void Awake()
    {
        Instance = this;
        Hide();
    }

    void Start()
    {
        // ── 绑定按钮 ──
        if (settingsTabBtn != null) settingsTabBtn.onClick.AddListener(() => SwitchTab(true));
        if (helpTabBtn != null)     helpTabBtn.onClick.AddListener(() => SwitchTab(false));
        if (closeBtn != null)       closeBtn.onClick.AddListener(Close);
        if (clearDataBtn != null)   clearDataBtn.onClick.AddListener(OnClearDataClicked);

        // ── 绑定滑条 ──
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        // ── 绑定全屏 ──
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

        // ── 绑定分辨率 ──
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        // ── 初始化分辨率列表 ──
        BuildResolutionList();

        // ── 加载已保存的设置 ──
        LoadSettings();

        // ── 填充帮助文本 ──
        FillHelpContent();
    }

    // ══════════════════════════════════════
    //  打开 / 关闭
    // ══════════════════════════════════════

    /// <summary>打开设置面板。</summary>
    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        panelGroup.alpha = 1f;
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;

        SwitchTab(true); // 默认显示设置页
        clearDataConfirm = false;
        if (clearDataTip != null) clearDataTip.text = "";
    }

    /// <summary>关闭设置面板。</summary>
    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        Hide();
        SaveSettings();
    }

    /// <summary>切换打开/关闭。</summary>
    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public bool IsOpen => isOpen;

    private void Hide()
    {
        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }
    }

    // ══════════════════════════════════════
    //  页签切换
    // ══════════════════════════════════════

    private void SwitchTab(bool showSettings)
    {
        if (settingsPage != null) settingsPage.SetActive(showSettings);
        if (helpPage != null)     helpPage.SetActive(!showSettings);

        // 页签按钮高亮
        SetTabHighlight(settingsTabBtn, showSettings);
        SetTabHighlight(helpTabBtn, !showSettings);
    }

    private void SetTabHighlight(Button btn, bool active)
    {
        if (btn == null) return;
        var colors = btn.colors;
        colors.normalColor = active ? new Color(1f, 1f, 1f, 1f) : new Color(0.7f, 0.7f, 0.7f, 0.8f);
        btn.colors = colors;
    }

    // ══════════════════════════════════════
    //  音量
    // ══════════════════════════════════════

    private void OnBGMVolumeChanged(float value)
    {
        if (bgmValueText != null)
            bgmValueText.text = Mathf.RoundToInt(value * 100) + "%";

        if (BGMManager.Instance != null)
            BGMManager.Instance.SetBGMVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (sfxValueText != null)
            sfxValueText.text = Mathf.RoundToInt(value * 100) + "%";

        // 音效音量：通过全局静态值供 AudioSource.PlayClipAtPoint 使用
        SFXVolume = value;
    }

    /// <summary>全局音效音量（静态，供各处播放音效时读取）。</summary>
    public static float SFXVolume { get; private set; } = 0.8f;

    // ══════════════════════════════════════
    //  画面设置
    // ══════════════════════════════════════

    private void BuildResolutionList()
    {
        if (resolutionDropdown == null) return;

        availableResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new System.Collections.Generic.List<string>();
        int currentIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            var r = availableResolutions[i];
            string label = r.width + " x " + r.height;
            // 避免重复（不同刷新率）
            if (options.Count > 0 && options[options.Count - 1] == label) continue;
            options.Add(label);

            if (r.width == Screen.currentResolution.width &&
                r.height == Screen.currentResolution.height)
                currentIndex = options.Count - 1;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    private void OnResolutionChanged(int index)
    {
        if (availableResolutions == null || index < 0) return;
        // 查找对应分辨率
        string selected = resolutionDropdown.options[index].text;
        foreach (var r in availableResolutions)
        {
            if ((r.width + " x " + r.height) == selected)
            {
                Screen.SetResolution(r.width, r.height, Screen.fullScreen);
                break;
            }
        }
    }

    // ══════════════════════════════════════
    //  清除存档
    // ══════════════════════════════════════

    private void OnClearDataClicked()
    {
        if (!clearDataConfirm)
        {
            clearDataConfirm = true;
            if (clearDataTip != null)
                clearDataTip.text = "再次点击确认清除所有存档！";
            return;
        }

        // 第二次点击：真正清除
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        clearDataConfirm = false;
        if (clearDataTip != null)
            clearDataTip.text = "存档已清除！重启游戏生效。";
    }

    // ══════════════════════════════════════
    //  帮助内容
    // ══════════════════════════════════════

    private void FillHelpContent()
    {
        if (helpContentText == null) return;

        helpContentText.text =
            "<size=28><b>Multicolour Cube</b></size>\n\n" +

            "<size=22><b>--- 游戏介绍 ---</b></size>\n" +
            "你是一个有四种颜色面的方块。\n" +
            "通过翻滚改变朝下的面，利用不同颜色\n" +
            "与关卡中的机关互动，收集星星到达终点。\n\n" +

            "<size=22><b>--- 操作说明 ---</b></size>\n" +
            "<b>移动</b>          A / D 或 方向键\n" +
            "<b>跳跃</b>          W 或 空格\n" +
            "<b>翻滚</b>          点击鼠标左键\n" +
            "                   (点击方块左侧向左滚，\n" +
            "                    点击右侧向右滚)\n\n" +

            "<size=22><b>--- 颜色机制 ---</b></size>\n" +
            "方块有四个颜色面：\n" +
            "  黄色(底)  蓝色(顶)  红色(左)  绿色(右)\n" +
            "接触变色方块时，方块的接触面颜色\n" +
            "会传递给变色方块。\n" +
            "当所有变色方块颜色匹配要求时，\n" +
            "即可触发机关（开门、移动平台等）。\n\n" +

            "<size=22><b>--- 收集物 ---</b></size>\n" +
            "<b>星星</b>  每关最多 3 颗，用于解锁新章节\n" +
            "<b>金币</b>  散布在关卡中，考验探索能力\n\n" +

            "<size=22><b>--- 提示 ---</b></size>\n" +
            "Esc  暂停游戏 / 返回\n" +
            "死亡后有短暂无敌时间（闪烁中）\n" +
            "善用检查点，它会记住你的进度！";
    }

    // ══════════════════════════════════════
    //  持久化
    // ══════════════════════════════════════

    private void SaveSettings()
    {
        if (bgmSlider != null) PlayerPrefs.SetFloat(KEY_BGM_VOL, bgmSlider.value);
        if (sfxSlider != null) PlayerPrefs.SetFloat(KEY_SFX_VOL, sfxSlider.value);
        PlayerPrefs.SetInt(KEY_FULLSCREEN, Screen.fullScreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        // BGM 音量
        float bgmVol = PlayerPrefs.GetFloat(KEY_BGM_VOL, 0.5f);
        if (bgmSlider != null)
        {
            bgmSlider.value = bgmVol;
            OnBGMVolumeChanged(bgmVol);
        }

        // SFX 音量
        float sfxVol = PlayerPrefs.GetFloat(KEY_SFX_VOL, 0.8f);
        if (sfxSlider != null)
        {
            sfxSlider.value = sfxVol;
            OnSFXVolumeChanged(sfxVol);
        }

        // 全屏
        bool fs = PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1;
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = fs;
        }
    }
}
