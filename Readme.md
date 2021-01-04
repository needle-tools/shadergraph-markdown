# ShaderGraph Markdown

![Unity Version Compatibility](https://img.shields.io/badge/Unity-2019.4%20%E2%80%94%202021.1-brightgreen) [![openupm](https://img.shields.io/npm/v/com.needle.shadergraph-markdown?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.needle.shadergraph-markdown/)

## What's this?

**ShaderGraph Markdown** contains a custom ShaderGUI to use with Unity's ShaderGraph.  
It uses "dummy properties" to draw a nice inspector for your materials, and decorates the Blackboard (where you edit properties) so that the markdown is readable and looks good.  

The property naming syntax is inspired by the simplicity of markdown. Please see the [Attribute Reference](#attribute-reference)! 

![Workflow](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/04_Workflow.gif)

This is a purely cosmetic operation, so your shader's functionality isn't affected.  
(if you don't have the package, then you just have some extra "dummy" properties)  

![ShaderGUI](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/01_ShaderGUI.gif)  

A nice feature is that you can make properties display conditionally, that is, only if a specific keyword option is set:  

![Conditional Properties](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/02_ConditionalProperties.gif)  

The ShaderGraph UI and blackboard are modified to render all "markdown dummy properties" differently, to increase legibility.  

![ShaderGraph Blackboard](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/03_ShaderGraphUI.png)  

## Quick Start
Install via OpenUPM: https://openupm.com/packages/com.needle.shadergraph-markdown/

1. In ShaderGraph, tell your shader to use the custom ShaderGUI `Needle.MarkdownShaderGUI`.
![ShaderGUI 10.x](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/06_ShaderGUI_URP10.png)  
_ShaderGraph 10 / 11_  
![ShaderGUI 7.x](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/07_ShaderGUI_URP7.png)  
_ShaderGraph 7 / 8_  

1. Create a new bool property. This is our "dummy" — its only purpose is to specify how the UI is drawn.  
2. Name it `## Hello Foldout`. You don't need to change the reference name.  
3. You should see the property display change in the Blackboard, to differentiate it from actual properties in your shader.   
4. Save your shader, and select a material that uses this shader — done!

To see a more complex example, you can also import the sample shown in this Readme via `Package Manager > ShaderGraph Markdown > Samples`.

## Attribute Reference
1. `# Foldout`  
   A foldout header
6. `## Header`  
   A header, similar to the `[Header]` attribute in scripts
8. `[Link Text](URL)`<a href="#footnote-1"><sup>1</sup></a>  
   A web link.  
9. `!NOTE Any note text`<a href="#footnote-1"><sup>1</sup></a>  
10. `!REF KEYWORD_NAME`  
  A reference to a keyword to be drawn here
7. `!DRAWER SubclassOfMarkdownMaterialPropertyDrawer`  
This will draw custom code, similar to a `PropertyDrawer` for the Inspector. Drawers are specified as subclasses of `MarkdownMaterialPropertyDrawer`, and an example of that is provided as package sample (install via PackMan).
1. `#`  
   End the current foldout. This is useful if you want to show properties "outside" a foldout again.

<sup>[1](footnote-1)</sup>: Will not be shown if the previous property was conditionally excluded.

## Conditional Properties
All properties — both regular ones and "markdown" properties — can have a condition appended to their display name. This will make them only display if that specific keyword is set.  

![Blackboard Conditions](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/05_BlackboardConditions.png)  

The `!REF` property draws an enum or boolean keyword.  
Other properties can specify a "condition" in which to draw them; this is very useful to de-clutter inspectors when you have specific properties in specific conditions.  

![Conditional Properties](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/02_ConditionalProperties.gif)  

The condition is specified in the form  `KeywordReference_OptionSuffix`.

Currently only single conditions are allowed (you can't combine these with `&&` or `||` right now).

## Notes

### HDRP Support
HDRP ShaderGraphs are supported. A speciality there is that these already have custom shader inspectors. ShaderGraph Markdown finds and displays the "original" inspector in addition to your own properties.  

![Conditional Properties](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/08_HDRP_Support.png)  

That being said, HDRP does some keyword magic (resetting material keywords at times); if you find something doesn't work as expected, you can use the "Debug" section to reset keywords and/or show the original property list.  

### Quickly enable / disable MarkdownShaderGUI

A context menu entry on every ShaderGraph-based material allows to quickly switch the material inspector between the default one and MarkdownShaderGUI. This is useful for debugging (e.g. finding some missing Conditionals).

## Contact
<b>[needle — tools for unity](https://needle.tools)</b> • 
[@NeedleTools](https://twitter.com/NeedleTools) • 
[@marcel_wiessler](https://twitter.com/marcel_wiessler) • 
[@hybridherbst](https://twitter.com/hybdridherbst)
