# ShaderGraph Markdown

![Unity Version Compatibility](https://img.shields.io/badge/Unity-2019.4%20%E2%80%94%202021.1-brightgreen) [![openupm](https://img.shields.io/npm/v/com.needle.shadergraph-markdown?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.needle.shadergraph-markdown/)

## What's this?

**ShaderGraph Markdown** contains a custom ShaderGUI to use with Unity's ShaderGraph.  
It uses "dummy properties" to draw a nice inspector for your materials, and decorates the Blackboard (where you edit properties) so that the markdown is readable and looks good.  

The dummy properties kind of look like markdown, which is where the name comes from. Please see the [Attribute Reference](#attribute-reference)! 

![Workflow](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/04_Workflow.gif)

This is a purely cosmetic operation, so your shader's functionality isn't affected.  
(if you don't have the package, then you just have some extra "dummy" properties)  

![ShaderGUI](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/01_ShaderGUI.gif)  

A nice feature is that you can make properties display conditionally, that is, only if a specific keyword option is set:  

![Conditional Properties](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/02_ConditionalProperties.gif)  

The ShaderGraph UI and blackboard are modified to render all "markdown dummy properties" differently, to increase legibility.  

![ShaderGraph Blackboard](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/03_ShaderGraphUI.gif)  

## Quick Start
Install via OpenUPM: https://openupm.com/packages/com.needle.shadergraph-markdown/

1. In ShaderGraph, tell your shader to use the custom ShaderGUI `Needle.MarkdownShaderGUI`.
![ShaderGUI 10.x](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/06_ShaderGUI_URP10.png)  
_ShaderGraph 10 / 11_  
![ShaderGUI 7.x](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/07_ShaderGUI_URP7.png)  
_ShaderGraph 7 / 8_  

1. Create a new bool property. This is our "dummy" — its only purpose is to specify how the UI is drawn.  
2. Name it `## Hello Foldout`.  
2. You should see the property display change in the Blackboard, to differentiate it from actual properties in your shader.   
2. Save your shader, and select a material that uses this shader — done!

## Attribute Reference
1. `## Foldout`  
   A foldout header
2. `##`  
  Foldout breaker (no foldout)
6. `### Header`  
   A header, similar to the `[Header]` attribute in scripts
7. `#NOTE Any note text`  
8. `#LINK [Link Text](URL)`  
9.  `#REF KEYWORD_NAME`  
  A reference to a keyword to be drawn here
7. `#DRAWER SubclassOfMarkdownMaterialPropertyDrawer`  
This will draw custom code, similar to a `PropertyDrawer` for the Inspector. Drawers are specified as subclasses of `MarkdownMaterialPropertyDrawer`, and an example of that is provided as package sample (install via PackMan).

## Conditional Properties
All properties — both regular ones and "markdown" properties — can have a condition appended to their display name. This will make them only display if that specific keyword is set.  

![Blackboard Conditions](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/05_BlackboardConditions.png)  

The `#REF` property pulls in an enum keyword, and other properties then draw depending on the specified condition.  

![Conditional Properties](https://github.com/needle-tools/shadergraph-markdown/wiki/Images/02_ConditionalProperties.gif)  

Note that the condition is specified in the form  `KeywordReference_OptionSuffix`.

Also note that currently only single conditions are allowed (you can't combine these with `&&` or `||` right now).

## Contact
<b>[needle — tools for unity](https://needle.tools)</b> • 
[@NeedleTools](https://twitter.com/NeedleTools) • 
[@marcel_wiessler](https://twitter.com/marcel_wiessler) • 
[@hybridherbst](https://twitter.com/hybdridherbst)
