# Shader Graph Markdown

![Unity Version Compatibility](https://img.shields.io/badge/Unity-2019.4%20%E2%80%94%202023.3-brightgreen) 

## License

Shader Graph Markdown is [available on the Asset Store](http://u3d.as/2was) for commercial use.  
Other versions (git, OpenUPM) are only allowed to be used **non-commercially** and **only if you're entitled to use Unity Personal** (the same restrictions apply).  

For all other uses, **please buy a commercial license** to ensure continued support! Thank you.

## What's this?

**Shader Graph Markdown** supercharges your creativity by allowing you to create great-looking, easy-to-use custom shader editors right from Unity's Shader Graph.  
It uses "dummy properties" to draw a nice inspector for your materials, and decorates the Blackboard (where you edit properties) so that the markdown is readable and looks good.  

The property naming syntax is inspired by the simplicity of markdown. Please see the [Attribute Reference](#attribute-reference)! 

![Workflow](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/04_Workflow.gif)

This doesn't affect your shader's functionality - it just makes it much nicer to work with it!  
(if someone doesn't have the package, then you just have some extra "dummy" properties)  

![ShaderGUI](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/01_ShaderGUI.gif)  

You can make properties display conditionally, that is, only if a specific keyword option is set:  

![Conditional Properties](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/02_ConditionalProperties.gif)  

The Shader Graph UI and blackboard are modified to render all "markdown dummy properties" differently, to increase legibility.  

![Shader Graph Blackboard](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/03_ShaderGraphUI.png)  

## Quick Start
Install via OpenUPM: https://openupm.com/packages/com.needle.shadergraph-markdown/

The ⚡ **️Fast Way** to switch your Shader Graph to Markdown:
  1. Select a material using your shader
  2. Right-click the material header and select `Toggle ShaderGraph Markdown`.

The 🐌 **Slow Way** to switch your Shader Graph to Markdown:
1. In Shader Graph, tell your shader to use the custom ShaderGUI `Needle.MarkdownShaderGUI`.  

![ShaderGUI 10.x](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/06_ShaderGUI_URP10.png)  
_Shader Graph 10+_  

![ShaderGUI 7.x](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/07_ShaderGUI_URP7.png)  
_Shader Graph 7 / 8_  

1. Create a new bool property. This is our "dummy" — its only purpose is to specify how the UI is drawn.  
2. Name it `# Hello Foldout`. You don't need to change the reference name.  
3. You should see the property display change in the Blackboard, to differentiate it from actual properties in your shader.   
4. Save your shader, and select a material that uses this shader — done!

To see a more complex example, you can also import the sample shown in this Readme via `Package Manager > Shader Graph Markdown > Samples`.

## Features

### Markdown in your Shader Graph
Simply create "dummy properties", e.g. floats or bools, name them with markdown (e.g. `# My Foldout`) and they'll display beatifully in your Shader Graph blackboard.

![Workflow](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/04_Workflow.gif)

Additionally, Shader Graph Markdown encourages you to use "good" property names and not the default ones, by displaying Blackboard Hints - a little red warning for "you're using the default reference name, don't do this!" and a little yellow warning for "your reference name doesn't start with _, that's recommended!".  

![Blackboard Hints](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/sgmarkdown-blackboard-hints.png)

Why? Because using default reference names *will* come back and bite you once you want to switch to a different shader on the same material, start animating material values, want to access anything from a script, etc. - Unity uses those reference names for a lot of things, and it's best practice to get them right from the start.

### Custom Shader GUI
Set ShaderGraph Markdown as Custom Shader GUI to get nice, customizable editors for your shaders - without a line of code, and scaling to a high complexity that would otherwise require writing custom editors.

![Complex Editor](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/sgmarkdown-full-sample.png)

### Inline Properties

Textures can have `&` appended to render them as smaller inline textures. If you use `&&`, the next property will be rendered right next to it, a common pattern throughout URP and HDRP for drawing sliders, colors etc. right next to a texture property. They'll be auto-indented in the Blackboard.  

![Inline Properties](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/sgmarkdown-inline-properties.png)

### Conditional Properties
All properties — both regular ones and "markdown" properties — can have a condition appended to their display name. This will make them only display if that specific keyword is set.  

![Blackboard Conditions](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/05_BlackboardConditions.png)  

The `!REF` property draws an enum or boolean keyword.  
Other properties can specify a "condition" in which to draw them; this is very useful to de-clutter inspectors when you have specific properties in specific conditions.  

![Conditional Properties](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/02_ConditionalProperties.gif)  

Boolean/Enum keyword conditions are specified in the form  `KEYWORD_OPTION`.

You can also use boolean properties and textures as conditionals:

![Conditional Properties](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/sgmarkdown-property-conditions.png)  

You can construct more complex conditions by using `and`, `&&`, `or`, `||`, `!`, `>`, `<=` ... and other operators - please let us know if something doesn't work as expected!  
You can also combine boolean keywords, enum keywords, textures, vectors, colors, floats in the same condition. Vectors compare by length, colors compare by max(r,g,b).

### Refactoring Shader Properties

This window helps you to find and rename wrongly named properties, refactor names across your project, update reference names, ... in shaders, materials, animations and scripts.  

![Refactoring Shader Properties](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/10_RefactorProperties.png)   

There's three ways to open the refactoring window:
- Right-click any material and select `Refactor Shader Properties`
- or open `Window > Needle > Refactor Shader Properties`
- or click <kbd>Refactor Shader Properties</kbd> in the `Markdown Tools` section of the material inspector.

### Markdown Tools Section

At the bottom of your shader, there's a `Markdown Tools` section (formerly: Debug) that contains info and helpers to configure your custom  UI, and to work with properties and keywords.  

![Debug Section](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/09_Debug.png)  

It also contains a button to quickly open the Property Refactoring window.  

Especially the option to `Debug Conditional Properties` is helpful when you're setting those up, as it will show all properties and allow you to quickly check for wrongly set up conditions.

### Expand/Collapse All Properties for URP/HDRP 7
In URP and HDRP 7, properties are configured right in the Blackboard, taking up lots of space. They can be expanded/collapsed, but that takes forever... Shader Graph Markdown fixes this by allowing you to Alt + Click on the foldouts to expand/collapse all properties.

#### Keywords

Shader Variants in Unity are controlled through [Shader Keywords](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html). URP and HDRP use these extensively to control shader behaviour (basically all the produced shaders are "Uber Shaders"). If something goes wrong that can result in wrong rendering, so the options here help you to
- see which keywords a shader uses (local and global)
- see which keywords are currently active
- clear and reset shader keywords to the ones HDRP/URP want to define for the current state.

### Quickly enable / disable MarkdownShaderGUI

A context menu entry on every Shader Graph-based material allows to quickly switch the material inspector between the default one and MarkdownShaderGUI. This is useful for debugging (e.g. finding some missing Conditionals).

### Custom Drawers

You can totally make your own and go crazy, the system is pretty extendable. The shipped ones (`MinMaxDrawer`, `VectorSliderDrawer`, `GradientDrawer` etc.) should be a pretty good starting point. 
When referencing a custom drawer, you can both use `!DRAWER DoSomethingDrawer` and `!DRAWER DoSomething`.  
Drawers are ScriptableObjects and can thus have persistent settings.  

Feel free to jump into our support discord and let us know if you need help or something isn't working as expected!

#### MinMax
`!DRAWER MinMax _MyVector`  
Draws a min-max slider that goes from `_MyVector.z` to `_MyVector.w` with `min = _MyVector.x` and `max = _MyVector.y`.

`!DRAWER MinMax _MyVector DisplayName`  
Uses DisplayName as label for the property.  

`!DRAWER MinMax _MyVector.x _MyVector.y`  
Draws a min-max slider that goes from 0 to 1 with `min = _MyVector.x` and `max = _MyVector.y`.

`!DRAWER MinMax _MyVector.x _MyVector.y DisplayName`  
Uses DisplayName as label for the property. This is useful when using multiple parts of the same vector, e.g.
```
!DRAWER MinMax _MyVector.x _MyVector.y MetallicRemap
!DRAWER MinMax _MyVector.z _MyVector.w SmoothnessRemap
```

#### VectorSlider

`!DRAWER VectorSlider _MyVector (First Slider, Second Slider, Third Slider)`  
Draws separate 0..1 sliders for the invidiual vector properties.  
The display name will come from _MyVector.  

VectorSlider drawers can be used with a shorthand (`&`):  
`My Vector (First Slider, Second Slider, Third Slider) &`

You can also use this to hide parts of a vector:  
`My Vector (R, G)` will only draw R and G sliders and ignore the remaining ZW values.

#### Gradient

`!DRAWER Gradient _MyRampTextureSlot`  
Draws a gradient field that automatically generates a gradient lookup texture whenever the value is changed.

#### MultiProperty (experimental)

`!DRAWER MultiProperty _Color1 _Color2 _Color3`
Draws all properties in the same line.  
This works pretty well for combining a couple of colors or numbers into inline drawers – please note that there's a good amount of combinations where this will fail, simply because there's not enough space on one line to hold the data you might want to show. 

## Attribute Reference
1. `# Foldout`  
   A foldout header
6. `## Header`
   A header, similar to the `[Header]` attribute in scripts
4. `### Label`
   A regular label, not bold and with no extra space.   
   Useful before indented properties.
1. Append `&&` to Texture properties
    - this will render the _next_ property inline (most useful for Color or Float properties)<a href="#footnote-2"><sup>2</sup></a>  
    - if the next property is named `_MyTex_ST` (with `_MyTex` matching the texture property name), a tiling/offset field will be drawn
3. Append `&` to Vector properties to have them display as 0..1 sliders
    - You can optionally specify the slider names: `Vector with Sliders (Amplitude, Frequency, Pattern) &`
    - If you leave them out you'll simply get a bunch of X,Y,Z,W sliders
    - If the vector property starts with `_Tiling` or `_Tile` or ends with `_ST`, it will be drawn as Tiling/Offset property field
3. Append `&` to Texture properties to render them as small texture slot  
  (not the monstrous default Shader Graph one, the nice one that URP/HDRP use for everything)
8. Prepend a number of dashes (e.g. `-` or `---`) to indent properties.  
   Nice for organization and showing where conditionals belong.
9. `!NOTE Any note text`<a href="#footnote-1"><sup>1</sup></a>  
8. `[Link Text](URL)`<a href="#footnote-1"><sup>1</sup></a>  
   A web link.  
9. `!TOOLTIP Any text`</a>  
   Tooltip for the following property. `!TIP` has the same effect.
10. `!REF KEYWORD_NAME`  
  A reference to a bool/enum keyword to be drawn here - by default they end up at the end of your shader, with this you can control exactly where they go.  
  You can also reference and thus edit a global keyword, but be aware that changing that value will change it, well, globally!  
4. Conditional properties: Append `[SOME_KEYWORD]` to your foldouts, drawers or properties to only make them show up when the condition is met. Conditions can be
    - boolean keywords (make sure to include the `_ON` part)
    - global keywords
    - enum keywords
    - texture properties (when the texture is not null)
    - boolean properties (when the bool is true)
    - float properties (compare using `<, >, ==` etc.)
    - color properties (max(r,g,b) is used as comparison value)
    - vector properties (vector length is used as comparison value)
    Conditions always come _last_, even for inline properties.<a href="#footnote-2"><sup>2</sup></a>    
7. `!DRAWER MyDrawer`  
  This will draw custom code, similar to a `PropertyDrawer` for the Inspector. Drawers are specified as subclasses of `MarkdownMaterialPropertyDrawer`.
    Examples:
    - Define `!DRAWER Gradient _MyTextureProperty` to render a nice gradient drawer that will generate gradient textures in the background for you.
    - Define some colors, and _before_ them add `!DRAWER MultiProperty _Col1 _Col2 _Col3`. This will render three colors all in one line.

8. `#`  (hash with nothing else)  
   End the current foldout. This is useful if you want to show properties outside a foldout, in the main area, again.

<sup>[1](footnote-1)</sup>: Will not be shown if the previous property was conditionally excluded.  
<sup>[2](footnote-2)</sup>: When you're using conditional properties as well, conditions come after inlining, and the next property needs to have the same condition. `_MyTex && [_SOME_CONDITION]` and on the next line `_MyColor [_SOME_CONDITION]`.  

A lot of the above can be combined - so you can totally do `--!DRAWER MinMax _MinMaxVector.x _MinMaxVector.y [_OPTIONAL_KEYWORD && _Value > 0.5]` and that will give you a double-indented minmax slider that only shows up when _OPTIONAL_KEYWORD is set and `_Value` has a value of greater than 0.5.

## Notes

### HDRP Support
HDRP Shader Graphs are supported. A speciality there is that these already have custom shader inspectors. Shader Graph Markdown finds and displays the "original" inspector in addition to your own properties.  

![HDRP Support](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/08_HDRP_Support.png)  

That being said, HDRP does some keyword magic (resetting material keywords at times); if you find something doesn't work as expected, you can use the "Debug" section to reset keywords and/or show the original property list.  

### Amplify Shader Editor Support
Amplify works as-is, you can specify `Needle.MarkdownShaderGUI` as custom shader inspector and then create properties as usual. You'll have to arrange them somewhere on the board though (they need to be somewhere). Also, turn on "Auto-Register" on the markdown properties, otherwise they'll be stripped away since they are not actually "used" in the shader.

![Amplify Properties](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/sgmarkdown-amplify-01.png)  
![Amplify Auto-Register](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/sgmarkdown-amplify-02.png)  

### Built-In and other Shaders
Nothing about the custom shader inspector in Shader Graph Markdown is actually Shader Graph-specific! You can use the same properties and drawers for all shaders, be it built-in, surface shaders, BetterShaders, other editors... In many cases, you could also use custom attributes, but especially for foldouts and drawers, Shader Graph Markdown gives a lot of flexibility.

### Global Illumination Support
Unity's GI systems rely on "magic property names" to work. Namely, `_EmissionColor` (Color) and `_EmissionMap` (Texture) have to be present; if either is missing, the resulting emission will be black (no emission). If both are present, SG Markdown will render the lightmap dropdown to choose "Baked", "Realtime" or "None" for Global Illumination.  
If the `_Emission` (or `_EMISSION`) property toggle is present, you can override this, and e.g. make materials that are emissive (they are bright) but don't contribute to GI.  
  
This is a typical setup that will hide the EmissionColor and EmissionMap properties if emission is turned off:   
```
[Toggle(_EMISSION)]_Emission("Emission", Int) = 0
[HDR]_EmissionColor("EmissionColor [_EMISSION]", Color) = (0,0,0,1)
_EmissionMap("EmissionMap [_EMISSION]", 2D) = "white" {}
```   
  
### Why isn't there Feature X?
A design goal of Shader Graph Markdown was to keep simplicity to allow for a fast workflow and nice editors, without writing any code. Custom drawers, on the other hand, give a lot of flexibility. The in-between, where you use some kind of markup to describe very complex behaviour, is explicitly not in scope for Shader Graph Markdown.  

This is also the reason why there's no options to change header colors, specify pixel spacing values, ... and do other purely stylistic changes.  
If you want that, there's other options; one that allows for tons of customization (and thus has a steeper learning curve) is the [Thry Editor](https://github.com/Thryrallo/ThryEditor).  

With the above in mind, feel free to reach out via Discord and request features that fit to this philosophy – and please submit bugs if you find them!

## Known Issues

- _Changing keyword property entries does not refresh the Enum dropdown in the inspector_  
  This is a Unity bug fixed in 2020.3.18f1+ and 2021.1.18f1+. [Case 1176077](https://issuetracker.unity3d.com/product/unity/issues/guid/1176077)  
  Workaround: reimport any script (Rightclick > Reimport) to trigger a domain reload.  
- _Vector fields don't show animation state (blue/red overlay)_  
  This is a Unity bug fixed in 2021.2.0b1+. [Case 1333416](https://issuetracker.unity3d.com/product/unity/issues/guid/1333416)
- _Conditionally excluded properties can't be inline properties_
  The condition will be ignored inside an inlined property. Don't inline them if you want them to use the condition.  
- _Built-In ShaderGraph shaders (2021.2+) don't use custom shader GUIs_
  This is a Unity bug fixed in 2022.2.0a8. [Case 1380485](https://issuetracker.unity3d.com/product/unity/issues/guid/1380485)
- _Texture and Vector properties don't support tooltips_ [Case 1421274]()
  This is a Unity bug. A workaround is included to make tooltips work on these property types nonetheless.  
  

## Contact
<b>[needle — tools for unity](https://needle.tools)</b> • 
[Discord Community](https://discord.gg/UHwvwjs9Vp) • 
[@NeedleTools](https://twitter.com/NeedleTools) • 
[@marcel_wiessler](https://twitter.com/marcel_wiessler) • 
[@hybridherbst](https://twitter.com/hybridherbst)
