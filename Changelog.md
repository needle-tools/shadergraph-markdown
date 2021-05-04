# Changelog
All notable changes to this package will be documented in this file.
The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.2] - 2021-05-03
- fixed short-named drawers going amiss after leaving Play mode with domain reload disabled
- fixed indentation issues with inline drawers
- improved string trimming for drawer parameters and vector slider names
- added ability to use duplicated empty headers `## (1)`

## [1.0.1] - 2021-05-03
- fixed Undo not showing changed values in some cases
- fixed disappearing custom drawers when exiting play mode with Domain Reload disabled
- fixed vector sliders not showing animation state (red/blue overlay)
- fixed hidden lightmap texture array being displayed as texture property in some case
- added `### Label` to show a non-bold label without space above, useful for indented properties coming afterwards
- added ability to use inline textures with custom drawers
- added MultiPropertyDrawer that can draw multiple properties on a single line (experimental)

## [1.0.0] - 2021-04-28
- fixed settings location which had an incorrect space in the path
- added more samples for SRP 10+, Amplify, Built-In RP
- added ability to collapse/expand all foldouts in SRP 7 with alt+click
- added texture property parameter to GradientGenerator
- added 2021.1 / SRP 11+ support
- removed Shader Graph/Core RP dependencies, works with Built-in as well
- adjusted colors
- changed license to Asset Store License, if SG Markdown is aquired elsewhere you many only use it for non-commercial purposes.

## [0.5.1-exp] - 2021-02-10
- fixed texture field not being square in some cases
- added tiling/offset inline drawer. Usage:
  ```
  _MyTexture && (Texture2D)
  _MyTexture_ST (Vector)
  ```
- added texture keyword helpers - bool keywords with the same name as a texture will automatically be set on texture changes.
  This helps with performance improvements (making variants without texture access). Usage:
  ```
  _BumpMap (Texture2D)
  _BUMPMAP (Bool Keyword) // will be set automatically depending on texture being set
  ```

## [0.5.0-exp] - 2021-02-02
- fixed errors on non-ShaderGraph HDRP shaders (e.g. from Amplify)
- fixed UI not refreshing when changing some shader properties
- fixed duplicate split line before Additional Options
- changed inline texture format to "&" for inline texture and "&&" for inline texture + property
- added ability to make foldouts closed by default, just append "-"

## [0.4.5-exp] - 2021-01-30
- added shorthand for InlineTexture and VectorSliders - append & to property name
- fixes and better error display for incorrect drawer usage

## [0.4.4-exp] - 2021-01-18
- added auto-generation of drawer ScriptableObjects from their type name if no ScriptableObject with that name is found
- added ability for drawers to specify which properties are used by them / should be hidden
- added InlineTexture drawer
- added MinMax drawer
- added VectorSlider drawer
- added samples for all of the above in the SRP10-ShaderGraph
- improved performance of MarkdownShaderGUI by caching some things 

## [0.4.3-exp] - 2021-01-17
- fixed confusing reference naming of bool keywords
- added support for bool values in conditional properties
- added indent support by prefixing properties with one or more '-' characters

## [0.4.2-exp] - 2021-01-08
- added warnings/hints in Blackboard for default reference names and names not starting with "_"
- added SettingsProvider to configure blackboard modifications
- added an experimental Property Wizard to bulk add ShaderGraph properties
  (this is useful if you want to create a ShaderGraph version of an existing shader/material)

## [0.4.1-exp] - 2021-01-04
- fixed warnings on Unity 2021 for empty asmdef
- updated Readme with notes on conditional compilation

## [0.4.0-exp] - 2021-01-03
- fixed HDRP 11 compatibility
- fixed multi-target samples (SRP 10+), added HDRP sample
- changed foldout syntax to #, header syntax to ## and #REF/#NOTE/#DRAWER to !REF/!NOTE/!DRAWER to align better with markdown
- added foldout state persistance via SessionState, defaults to expanded

## [0.3.0-exp] - 2021-01-02
- added HDRP support
- added context menu helper to toggle MarkdownShaderGUI
- added debug option to show original property list
- changed link syntax (removed #LINK prefix, is now the same as markdown)

## [0.2.0-exp] - 2020-12-30
- added ShaderGraph UI modifications
- fixed editor compatibility from 2019.4—2020.2 and URP7—URP11

## [0.1.0-preview] - 2020-12-29
- initial version