using UnityEngine;

namespace EnhancedPython.Options;

public enum OptionUIType
{
    Slider,
    Cycle,
    KeyBind
}

public class Option
{
    public string Name;
    public OptionUIType Type = OptionUIType.Cycle;
    public string Tooltip;
    public string DefaultValue = "";
    public float Importance = -1.0f;

    public void Setup(OptionSO scriptableObject)
    {
        scriptableObject.optionName = Name;
        scriptableObject.tooltip = Tooltip;
        scriptableObject.defaultValue = DefaultValue;
        scriptableObject.importance = Importance;
        scriptableObject.category = "mods";
    }
}

public class CycleOption: Option
{
    public new OptionUIType Type = OptionUIType.Cycle;
    public List<string> options;
    
    public new void Setup(OptionSO scriptableObject)
    {
        base.Setup(scriptableObject);
        (scriptableObject as CycleOptionSO)!.options = options;
    }
}

[Serializable]
public class OptionsManager
{
    private GameObject gameObject = null;
    private Dictionary<OptionUIType, OptionUI> UITypes = new();
    
    private string optionCacheHash = "";
    private OptionSO[] optionCache = Array.Empty<OptionSO>();

    public OptionSO[] UpdateOptions(OptionSO[] options)
    {
    start:
        if (UITypes.Count > 0)
        {
            var newHash = string.Join(",", Options.Select(opt => opt.Name));
            if (newHash != optionCacheHash) {
                // repopulate options
                optionCacheHash = newHash;
                optionCache = Options.Select<Option, OptionSO>(opt =>
                {
                    // TODO: Add the other types.
                    switch (opt.GetType().Name)
                    {
                        case "CycleOption":
                        {
                            var cycleOpt = (CycleOption)opt;
                            var so = ScriptableObject.CreateInstance<CycleOptionSO>();
                            
                            so.optionUI = UnityEngine.Object.Instantiate(UITypes[cycleOpt.Type]);
                            cycleOpt.Setup(so);

                            return so;
                        }
                        
                        default: throw new NotImplementedException();
                    }
                }).ToArray();
            }
            
            Plugin.Log.LogInfo($"options {newHash}");
            return options.Concat(optionCache).ToArray();
        }
        
        // Initialize the UI types
        foreach (var optionSo in options)
        {
            OptionUIType? uiType = optionSo.optionUI.GetType().Name switch
            {
                "SliderOptionUI" => OptionUIType.Slider,
                "CycleOptionUI" => OptionUIType.Cycle,
                "KeyBindOptionUI" => OptionUIType.KeyBind,
                _ => null,
            };
            
            if (uiType is null || UITypes.ContainsKey(uiType.Value)) continue;
            UITypes[uiType.Value] = optionSo.optionUI;
        }

        // TODO: Fix this, as it doesn't work correctly?
        // gameObject = new GameObject("OptionsManager");
        //
        // UITypes[OptionUIType.Slider] = gameObject.AddComponent<SliderOptionUI>();
        // UITypes[OptionUIType.Cycle] = gameObject.AddComponent<CycleOptionUI>();
        // UITypes[OptionUIType.KeyBind] = gameObject.AddComponent<KeyBindOptionUI>();

        // Go back to the start
        goto start;
    }

    public static List<Option> Options = new()
    {
        new CycleOption() { Name = "Test", Tooltip = "Test option", DefaultValue = "hi", options = new() { "hi", "there" }}
    };
}
