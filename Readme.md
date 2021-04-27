# Shader Graph Markdown

![Unity Version Compatibility](https://img.shields.io/badge/Unity-2019.4%20%E2%80%94%202021.1-brightgreen) [![openupm](https://img.shields.io/npm/v/com.needle.shadergraph-markdown?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.needle.shadergraph-markdown/)

## What's this?

**Shader Graph Markdown** contains a custom ShaderGUI to use with Unity's Shader Graph.  
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

1. In Shader Graph, tell your shader to use the custom ShaderGUI `Needle.MarkdownShaderGUI`.
![ShaderGUI 10.x](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/06_ShaderGUI_URP10.png)  
_Shader Graph 10 / 11_  
![ShaderGUI 7.x](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/07_ShaderGUI_URP7.png)  
_Shader Graph 7 / 8_  

1. Create a new bool property. This is our "dummy" — its only purpose is to specify how the UI is drawn.  
2. Name it `## Hello Foldout`. You don't need to change the reference name.  
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

Boolean/Enum keyword conditions are specified in the form  `KeywordReference_OptionSuffix`.

You can also use boolean properties and textures as conditionals:

![Conditional Properties](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/sgmarkdown-property-conditions.png)  

Currently only single conditions are allowed (you can't combine these with `&&` or `||` right now).

### Debug Section

At the bottom of your shader, there's a Debug section that contains info and helpers to configure your custom  UI, and to work with keywords in general.

Especially the option to `Debug Conditional Properties` is helpful when you're setting those up, as it will show all properties and allow you to quickly check for wrongly set up conditions.

![Debug Section](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/09_Debug.png)  

#### Keywords

Shader Variants in Unity are controlled through [Shader Keywords](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html). URP and HDRP use these extensively to control shader behaviour (basically all the produced shaders are "Uber Shaders"). If something goes wrong that can result in wrong rendering, so the options here help you to
- see which keywords a shader uses (local and global)
- see which keywords are currently active
- clear and reset shader keywords to the ones HDRP/URP want to define for the current state.

### Quickly enable / disable MarkdownShaderGUI

A context menu entry on every Shader Graph-based material allows to quickly switch the material inspector between the default one and MarkdownShaderGUI. This is useful for debugging (e.g. finding some missing Conditionals).

### Custom Drawers

You can totally make your own and go crazy, the system is pretty extendable. The shipped ones (`MinMaxDrawer`, `VectorSlidersDrawer`, `GradientDrawer` etc.) should be a pretty good starting point. Feel free to jump into our support discord and let us know if you need help or something isn't working as expected!

## Attribute Reference
1. `# Foldout`  
   A foldout header
6. `## Header`
   A header, similar to the `[Header]` attribute in scripts
1. Append `&&` to Texture properties - this will render the _next_ property inline (most useful for Color or Float properties)
3. Append `&` to Vector properties to have them display as 0..1 sliders
    - You can optionally specify the slider names: `Vector with Sliders (Amplitude, Frequency, Pattern) &`
    - If you leave them out you'll simply get a bunch of X,Y,Z,W sliders
3. Append `&` to Texture properties to render them as small texture slot  
  (not the monstrous default Shader Graph one, the nice one that URP/HDRP use for everything)
8. Prepend a number of dashes (e.g. `-` or `---`) to indent properties.  
   Nice for organization and showing where conditionals belong.
9. `!NOTE Any note text`<a href="#footnote-1"><sup>1</sup></a>  
8. `[Link Text](URL)`<a href="#footnote-1"><sup>1</sup></a>  
   A web link.  
10. `!REF KEYWORD_NAME`  
  A reference to a bool/enum keyword to be drawn here - by default they end up at the end of your shader, with this you can control exactly where they go.
4. Conditional properties: Append `[SOME_KEYWORD]` to your properties to only make them show up when the condition is met. Conditions can be
    - boolean keywords (make sure to include the `_ON` part)
    - enum keywords
    - texture properties (when the texture is not null)
    - boolean properties (when the bool is true)
7. `!DRAWER SubclassOfMarkdownMaterialPropertyDrawer`  
  This will draw custom code, similar to a `PropertyDrawer` for the Inspector. Drawers are specified as subclasses of `MarkdownMaterialPropertyDrawer`.
    Example:
    - Define `Some Vector (Amplitude, Frequency, Pattern)` with reference name `_SomeVector`.  
    _Before_ that vector, add a markdown property `!DRAWER VectorSlider _SomeVector`.  
    This will neatly split up the vector into three sliders!  
    - Define `!DRAWER Gradient _MyTextureProperty` to render a nice gradient drawer that will generate gradient textures in the background for you.
    Shader Graph Markdown ships with some drawers already:
    - 

1. `#`  (hash with nothing else)  
   End the current foldout. This is useful if you want to show properties outside a foldout, in the main area, again.

<sup>[1](footnote-1)</sup>: Will not be shown if the previous property was conditionally excluded.

A lot of the above can be combined - so you can totally do `--!DRAWER MinMax _MinMaxVector.x _MinMaxVector.y [OPTIONAL_KEYWORD]` and that will give you a double-indented minmax slider that only shows up when OPTIONAL_KEYWORD is true.


## Notes

### HDRP Support
HDRP Shader Graphs are supported. A speciality there is that these already have custom shader inspectors. Shader Graph Markdown finds and displays the "original" inspector in addition to your own properties.  

![HDRP Support](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/08_HDRP_Support.png)  

That being said, HDRP does some keyword magic (resetting material keywords at times); if you find something doesn't work as expected, you can use the "Debug" section to reset keywords and/or show the original property list.  

### Amplify Support
Amplify works as-is, you can specify `Needle.MarkdownShaderGUI` as custom shader inspector and then create properties as usual. You'll have to arrange them somewhere on the board though (they need to be somewhere). Also, turn on "Auto-Register" on the markdown properties, otherwise they'll be stripped away since they are not actually "used" in the shader.

![Amplify Properties](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/sgmarkdown-amplify-01.png)  
![Amplify Auto-Register](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/sgmarkdown-amplify-02.png)  

### Built-In and other Shaders
Nothing about the custom shader inspector in Shader Graph Markdown is actually Shader Graph-specific! You can use the same properties and drawers for all shaders, be it built-in, surface shaders, BetterShaders, other editors... In many cases, you could also use custom attributes, but especially for foldouts and drawers, Shader Graph Markdown gives a lot of flexibility.

## Contact
<b>[needle — tools for unity](https://needle.tools)</b> • 
[Discord Community](https://discord.gg/UHwvwjs9Vp) • 
[@NeedleTools](https://twitter.com/NeedleTools) • 
[@marcel_wiessler](https://twitter.com/marcel_wiessler) • 
[@hybridherbst](https://twitter.com/hybridherbst)
