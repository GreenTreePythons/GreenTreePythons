public static string GetStringWithEmoji(Color emojiColor, string emojiName, float fontSize,
        string desc = null, bool useBothSameColor = false, bool useEmojiScaleFactor = false)
{
    string colorCode = $"<color=#{ColorUtility.ToHtmlStringRGBA(emojiColor)}>";
    string emoji = $"<sprite name=\"{emojiName}\" tint>";

    var emojiSize = 30.0f;
    if (desc != null)
    {
        emojiSize = fontSize * 1.13f;
        return useBothSameColor
            ? useEmojiScaleFactor ? $"<size={emojiSize}>{colorCode}{emoji}</size>{desc}</color>" : $"{colorCode}{emoji}{desc}</color>"
            : useEmojiScaleFactor ? $"<size={emojiSize}>{colorCode}{emoji}</color></size>{desc}" : $"{colorCode}{emoji}</color>{desc}";
    }

    return useEmojiScaleFactor ? $"<size={emojiSize}>{colorCode}{emoji}</color></size>" : $"{colorCode}{emoji}</color>";
}

public static bool TryGetEmoji(float size, EmojiType type, out string emojiString)
{
    emojiString = string.Empty;
    if (Enum.TryParse<EmojiType>(type.ToString(), out var emoji))
    {
        emojiString = $"<size={size}> <sprite={(int)emoji}> </size>";
        return true;
    }
    return false;
}

public static string GetSufficientText(TMP_Text target, long amount, bool isSufficient)
{
    var currentTextStyle = target.textStyle.name;
    target.text = isSufficient ? $"<style={currentTextStyle}>{amount}</style>" : $"<style={"Negative"}>{amount}</style>";
    return target.text;
}

public static string GetPositiveText(string str, bool isPositive)
    => $"<style={(isPositive ? "Positive" : "Negative")}>{str}</style>";

public static string GetTextColor(this string str, Color colorValue)
{
    var colorHex = ColorUtility.ToHtmlStringRGBA(colorValue);
    return $"<color=#{colorHex}>{str}</color>";
}

public static string GetTextStyle(this string str, string style)
{
    return $"<style={style}>{str}</style>";
}

public static string GetTextStyle(this string str, TextStyle style)
{
    var styleName = Enum.GetName(typeof(TextStyle), style);
    return $"<style={styleName}>{str}</style>";
}

public static void SetTextStyle(this TextMeshProUGUI text, string str, TextStyle style)
{
    var styleName = Enum.GetName(typeof(TextStyle), style);
    text.text = $"<style={styleName}>{str}</style>";
}

public static string GetItemCountFormat(long count)
{
    if (count == 0) return "0";
    return string.Format("{0:#,###}", count);
}

public static string ParseStatString(BattleStat stat, double value)
{
    return stat switch
    {
        BattleStat.CriticalAccuracyRate => (value * 100).ToString("#,###") + "%",
        BattleStat.CriticalMultiplier => (value * 100 + 100).ToString("#,###") + "%",
        _ => value.ToString("#,###0")
    };
}

public static string GetAppliedAccountStat(AccountStat stat, float value)
    => GetAppliedAccountStatString(stat) + GetAppliedAccountStatValueString(stat, value);

public static string GetAppliedAccountStat(AccountStat stat, string value)
    => GetAppliedAccountStatString(stat) + value;

public static string GetAppliedAccountStatString(AccountStat stat) => StringTable.Get($"{stat}".ToLower());

public static string GetAppliedAccountStatValueString(AccountStat stat, float value)
{
    var plusString = value > 0 ? "+" : string.Empty;
    var statModifierSid = ProtoData.Current.AccountCreationStats.SingleOrDefault(s => s.Stat == stat)?.StatModifierSID;
    return statModifierSid != null ? $"{plusString}" + string.Format(StringTable.Get(statModifierSid), value) : $"{plusString}{value}";
}

public static string GetStringByStatType(AccountStatType statType) => $"accountstattype_{statType.ToString().ToLower()}";