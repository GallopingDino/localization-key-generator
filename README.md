

Localization Key Generator
===
This package provides a set of attributes to generate localization keys and comments from code.
It requires [Unity Localization](https://docs.unity3d.com/Packages/com.unity.localization@1.0/manual/Installation.html) and [Odin Inspector](https://odininspector.com/) to work.

## Setup
### Installation
Find the manifest.json file in the Packages folder of your project and add the following line to `dependencies` field:

     "com.dino.localization-key-generator": "https://github.com/GallopingDino/localization-key-generator.git"

### Settings
Use menu item `Window/Localization Key Generator/Settings` to create a new or focus an existing `LocalizationKeyGeneratorSettings` asset.

To begin with Localization Key Generator, add one or multiple existing locales to the `PreviewLocales` list in the inspector. These locales will be displayed on the "Auto" tab in a simplified LocalizedString drawer. The default drawer with the full list of locales will still be available on the "Manual" tab.

You can also add project-wide parameters into `Parameters` array.

##  Usage
###  Generating keys

Use `AutoKeyAttribute` on a LocalizedString field or a collection of LocalizedStrings and provide a key [format string](#format-string).

```c#
public class InventoryItem : ScriptableObject {
    public ItemType Type;
    [AutoKey("{Type}/{rootName}/name")]
    public LocalizedString Name;
    [AutoKey("{Type}/{rootName}/desc")]
    public LocalizedString Description;
}
```

![](/Documentation~/images/AutoKey.png)

In the inspector window, you will see a simplified drawer for your LocalizedString. Select localization table and press `Generate` or just start typing localized text into a text field to generate key and create a new localization table entry. Or press `Find` to generate a key and use it to search for an existing table entry.

Press `Regenerate` to change the key for the current entry. Press `✕` to remove current entry from localization table, `◯` to set entry reference empty without removing current entry from the table or `❐` to duplicate current entry with a new key.

If a generated key matches an existing key, it will be appended with an index, e.g. `Weapon/Tommy Gun/name-1`. The index position can be specified in the key format string. This ensures that even the first generated key will have a 0 index appended.



```c#
[AutoKey("{Type}/{rootName}/name-{index}")]
public LocalizedString Name;
```

###  Generating comments

Unity Localization library allows [adding metadata](https://docs.unity3d.com/Packages/com.unity.localization@1.0/manual/Metadata.html) to your localized strings. One of the built-in metadata types is `Comment`. It contains a single string field which can be exported to [Google Sheets](https://docs.unity3d.com/Packages/com.unity.localization@1.0/manual/Google-Sheets.html) or a [CSV](https://docs.unity3d.com/Packages/com.unity.localization@1.0/manual/CSV.html) file along with localized strings and can be used to provide context for translators.

You can generate comments from code by using `AutoCommentAttribute`:


```c#
public class Dialog : ScriptableObject {
    public List<Line> Lines;
      
    [Serializable]
    public class Line {
	public Character Character;
	public Mood Mood;
	[AutoKey("{rootName}/line-{index}"), AutoComment("{Character}, {Mood}")]
	public LocalizedString Text;
    }
}
```

![](/Documentation~/images/AutoComment.png)

### Format string
A format string can contain both simple text pieces and resolvable strings enclosed in curly brackets. These resolvable strings will be evaluated during key generation and can be of one of the following types:

- Containing type member names, e. g. `{Character}`
- [Parameter names](#passing-parameters), e. g. `{rootName}`
- [Odin style expressions](https://odininspector.com/tutorials/value-and-action-resolvers/resolving-strings-to-stuff) starting with @, e. g. `{@{index} + 1}`



Resolvable strings can be nested. For example, if you want your key index to start from 1, you can use `index` parameter inside of expression:


```c#
public class Dialog : ScriptableObject {
    public List<Line> Lines;
      
    [Serializable]
    public class Line {
	public Character Character;
	public Mood Mood;
	[AutoKey("{rootName}/line-{@{index} + 1}"), AutoComment("{Character}, {Mood}")]
	public LocalizedString Text;
    }
}
``` 

###  Passing parameters

There are three types of parameters you might use in your format strings:

- Global parameters set in `LocalizationKeyGeneratorSettings` asset. These can be referenced anywhere in your project.
- Built-in parameters. These are:
	- `rootName` - the name of the root ScriptableObject or MonoBehaviour
	- `uuid` - the GUID of the root ScriptableObject asset
	- `listIndex` - the index of LocalizedString or any of its parents in collection
	- `dictionaryKey` - the key corresponding to LocalizedString or any of its parents in a dictionary
	- `index` - the repeating key index. Only exists for key format strings
- Custom parameters

To introduce custom parameters use `AutoKeyParamsAttribute`. This attribute can be applied to a field limiting this parameter's scope to this field and any nested `LocalizedStrings` in the object graph. Alternatively it can be applied to the whole class making this parameter accessible from any `LocalizedString` field in this class or any nested fields. Provide parameter name and a format string that will be resolved to parameter value.


```c#    
[AutoKeyParams("type", "Type")]
public class InventoryItem : ScriptableObject {
    public ItemType Type;
    public ItemLevel[] Levels;
}
        
[Serializable]
public class ItemLevel {
    [AutoKey("{type}/{rootName}/level-{listIndex}/name")]
    public LocalizedString Name;  
    [AutoKey("{type}/{rootName}/level-{listIndex}/desc")]
    public LocalizedString Description;  
}
```

![](/Documentation~/images/AutoKeyParams.png)

### Parameter processors

Sometimes you might want to declare custom parameters in the code you don't have access to. Or you might want some of your classes and assemblies to not depend on Localization Key Generator assembly, but still be able to provide custom parameters. Or you might need to access `SerializedProperty` data in your parameters. In these cases parameter processors will come to the rescue.

Parameter processors are used to declare custom parameters based on `InspectorProperties`, which are Odin's more powerful equivalent to `SerializedProperty`.

Every time the format string is being resolved, all suitable parameter processors are applied to every `InspectorProperty` in hierarchy: from resolved `LocalizedString` field up to the root object.

To create a new parameter processor, inherit the base `ParameterProcessor` class. You can use one of the built-in processors such as `RootNameScriptableObjectProcessor`, `UuidScriptableObjectProcessor` and `ListIndexProcessor` as a reference:


```c#
internal sealed class UuidScriptableObjectProcessor : ParameterProcessor {  
    public override string ParameterName => "uuid";
        	    
    public override bool CanProcess(InspectorProperty property) {  
    	return property.ValueEntry?.WeakSmartValue is ScriptableObject;  
    }
    	    
    public override object Process(InspectorProperty property) {  
    	var scriptable = (ScriptableObject) property.ValueEntry.WeakSmartValue;  
    	if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(scriptable, out var guid, out long _)) {  
	    return guid;  
	}  
	return string.Empty;
    }  
}
```

### Resolvable string formatting

You can use interpolated string-like syntax to provide string format for resolvable strings. For example you can use [standard numeric format](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings) to set minimum number of digits for the listIndex parameter:


```c#
[AutoKey("{type}/{rootName}/level-{listIndex:D2}/name")]
public LocalizedString Name;
```

### Case style formatting
In addition to [standard format strings](https://learn.microsoft.com/en-us/dotnet/standard/base-types/formatting-types#standard-format-strings)  you can use special formats to specify case style for resolvable strings that resolve to string or enum values.

| Case            | Format                     |
| --------------- | -------------------------- |
| camelCase       | aaBb                       |
| PascalCase      | AaBb                       |
| kebab-case      | aa-bb                      |
| SCREAMING_SNAKE | AA_BB                      |
| UNALTERED_Case  | \*_*                       |

You can create your own custom case style using combinations of lower and capital letters with `*` character to keep original case intact and various separator characters.


```c#
[AutoKeyParams("type", "Type")]
public class InventoryItem : ScriptableObject {
    public ItemType Type;
    public ItemLevel[] Levels;
}
        
[Serializable]
public class ItemLevel {
    [AutoKey("{type:aa_bb}/{rootName:aa_bb}/level-{@{listIndex} + 1:D2}/name")]
    public LocalizedString Name;  
    [AutoKey("{type:aa_bb}/{rootName:aa_bb}/level-{@{listIndex} + 1:D2}/desc")]
    public LocalizedString Description;
}
```

![](/Documentation~/images/CaseStyle.png)

You can achieve the same result by setting default case style format in `LocalizationKeyGeneratorSettings` using `Default Key / Comment String Format` fields. If by default you prefer to keep original case style, set these fields empty.

## Author
Vladimir Kuznetsov

[![](https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/vladimir-kuznetsov-games/)[![](https://img.shields.io/badge/Twitter-1DA1F2?style=for-the-badge&logo=twitter&logoColor=white)](https://x.com/GallopingDino)


## License

This library is under the MIT License.