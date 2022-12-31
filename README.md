
Localization Key Generator
===
This package provides a set of attributes to generate localization keys and comments from code.
It requires [Unity Localization](https://docs.unity3d.com/Packages/com.unity.localization@1.0/manual/Installation.html) and [Odin Inspector](https://odininspector.com/) to work.

## Setup
### Installation
Find the manifest.json file in the Packages folder of your project and add following line to `dependencies` field:

     "com.dino.localization-key-generator": "https://GallopingDino@bitbucket.org/GallopingDino/localization-key-generator.git"

### Settings
Use menu item `Window/Localization Key Generator/Settings` to create a new or focus an existing `LocalizationKeyGeneratorSettings` asset. 

To begin with Localization Key Generator, add one or multiple existing locales to the `PreviewLocales` list in the inspector. These locals will show on the "Auto" tab in a new simplified LocalizedString drawer. The default drawer with the full list of locales will still be available on the "Manual" tab.

You can also add project-wide parameters into `Parameters` array.

##  Usage
###  Generating keys

Use `AutoKeyAttribute` on a LocalizedString field or a collection of LocalizedStrings and provide a key [format string](#markdown-header-format).

    public class InventoryItem : ScriptableObject {
	    public ItemType Type;
	    [AutoKey("{Type}/{rootName}/name")]
	    public LocalizedString Name;
	    [AutoKey("{Type}/{rootName}/desc")]
	    public LocalizedString Description;
    }

![](/Documentation~/images/AutoKey.png)

In the inspector window you will see a simplified drawer for your LocalizedString. Select localization table and press `Generate` or just start typing localized text into a text field to generate key and create a new localization table entry.

Press `Regenerate` to change key for the current entry. Press `✕` to remove current entry from the table or `◯` to set entry reference empty without removing current entry from the table.

If a generated key matches an existing key, it will be appended with an index, e. g. `weapon/tommy_gun/name-1`. You can specify position and format of the index in the key format string:


    [AutoKey("{Type}/{rootName}/name-{index:D2}")]
    public LocalizedString Name;

###  Generating comments

Unity Localization library allows [adding metadata](https://docs.unity3d.com/Packages/com.unity.localization@1.0/manual/Metadata.html) to your localized strings. One of the built-in metadata types is `Comment`. It contains a single string field which can be exported to [Google Sheets](https://docs.unity3d.com/Packages/com.unity.localization@1.0/manual/Google-Sheets.html) or a [CSV](https://docs.unity3d.com/Packages/com.unity.localization@1.0/manual/CSV.html) file along with localized strings and can be used to provide context for translators.

You can generate comments from code using `AutoCommentAttribute`:



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

![](/Documentation~/images/AutoComment.png)

### Format string
A format string can contain both simple text pieces and resolvable strings enclosed in curly brackets. Those resolvable strings will be evaluated during key generation and can be of one of the following types:

 - Enclosing type member names, e. g. `{Character}` 
 - [Parameter names](#markdown-header-passing-parameters), e. g. `{rootName}`
 - [Odin style expressions](https://odininspector.com/tutorials/value-and-action-resolvers/resolving-strings-to-stuff) starting with @, e. g. `{@{index} + 1}`
 

 
 Resolvable strings can be nested. For example, if you want your key index to start from 1, you can use `index` parameter inside of expression: 

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
    

###  Passing parameters

There are three types of parameters you might use in your format strings:

 - Global parameters set in `LocalizationKeyGeneratorSettings` asset. Those can be referenced anywhere in your project.
 - Built-in parameters. These are: 
	 - `rootName` - a name of the root ScriptableObject or MonoBehaviour  in a snake_case
	 - `uuid` - a GUID of the root ScriptableObject asset
	 - `listIndex` - an index of LocalizedString or any of its parents in collection
	 - `index` - a repeating key index. Only exists for key format strings
- Custom parameters

To introduce custom parameters use `AutoKeyParamsAttribute`. This attribute can be applied to a field limiting this parameter's scope to this field and any nested `LocalizedStrings` in the object graph. Alternatively it can be applied to the whole class making this parameter accessible from any `LocalizedString` field in this class or any nested fields. Provide parameter name and a format string that will be resolved to parameter value.

      
	[AutoKeyParams("type", "Type")]
    public class InventoryItem : ScriptableObject {
       	public ItemType Type;
       	public ItemLevel[] Levels;
    }
        
    [Serializable]
    public class ItemLevel {
       	[AutoKey("{type}/{rootName}/level-{listIndex:D2}/name")]
       	public LocalizedString Name;  
       	[AutoKey("{type}/{rootName}/level-{listIndex:D2}/desc")]
       	public LocalizedString Description;  
    }
   
   ![](/Documentation~/images/AutoKeyParams.png)

### Parameter processors

Sometimes you might want to declare custom parameters in the code you don't have access to. Or you might want some of your classes and assemblies to not depend on Localization Key Generator assembly, but still be able to provide custom parameters. Or you might need to access `SerializedProperty` data in your parameters. In those cases parameter processors could come to the rescue.

Parameter processors are used to declare custom parameters based on `InspectorProperties`, which are Odin's more powerful equivalent to `SerializedProperty`. 

Every time the format string is being resolved, all suitable parameter processors are applied to every `InspectorProperty` in hierarchy: from resolved `LocalizedString` field up to the root object.

To create a new parameter processor, inherit the base `ParameterProcessor` class. You can use one of the built-in processors such as `RootNameScriptableObjectProcessor`, `UuidScriptableObjectProcessor` and `ListIndexProcessor` as a reference:

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

## Author
Vladimir Kuznetsov, a game developer.
https://twitter.com/gallopingdino

## License

This library is under the MIT License.
