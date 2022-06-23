# Changelog
All notable changes to this package will be documented in this file.
The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.5.1] 2022-06-24
- fixed weird crash when text displayed in DialogueWindowComplex was too long
- added: refactor multiple properties in one go to the PropertyRefactor window
- added: copy list of properties to be refactored from/to clipboard as plain text format
- added ability to refactor property names in scripts as well

## [1.5.0] 2022-05-12
- fixed indenting issue with `VectorSlider` drawer
- fixed property name warnings showing in SubGraphs
- fixed `MinMax` drawer showing property swizzles by default, can now be turned on for debugging with "Show Reference Names"
- fixed reflection access error due to internal API change
- fixed incorrect display properties that are both inlined and conditional in the blackboard
- added Markdown Tools option to "Show Reference Names" of all properties, useful for scripting and animation
- added shortcut to quickly show reference names, use <kbd>SHIFT</kbd> while having the material inspector focussed
- added an optional parameter to specify a display name for MinMax drawers
- added local and global keyword state toggles to quickly see what's on
- added support for `LocalKeyword` and `GlobalKeyword` in 2021.2+ to get the new info available there
- added support for global keywords in conditional properties
- added support for global keywords in `!REF`
- added `!DRAWER` documentation to the Readme, added more info to the attribute reference
- changed Markdown Tools inspector order to better reflect common workflows

## [1.4.0] 2022-04-19
- added new markup: `!TOOLTIP` or short `!TIP` will add a tooltip to the next property

## [1.3.2] 2022-03-28
- fixed generated gradient textures being compressed, now uncompressed (better quality)

## [1.3.1] 2021-12-13
- fixed issue with Surface Options not showing in some package combinations
- verified basic compatiblity with Unity 2022

## [1.3.0] 2021-11-26
- added Global Illumination mode dropdown when shader uses `_EmissionColor` and `_EmissionMap` and/or declares the `_EMISSION` keyword
- add Shader Property Refactoring and Global Illumination to the Readme
- updated known Unity bugs in the Readme to include version where issues are known to be resolved

## [1.3.0-pre.2] 2021-11-09
- fixed an issue with shaders on 2021.2+ that don't have blackboard categories (e.g. Amplify shaders used in URP)

## [1.3.0-pre] 2021-11-01
- fixed properties in an old default reference format not displayed in red
- fixed categories not being displayed correctly in some Editor versions
- fixed foldout header state for categories with duplicate names
- fixed compilation errors on 2019.4 and 2021.1
- fixed material validation not always being called in 2021.2+
- changed "Debug" category to be called "Markdown Tools"
- added Shader Property Refactor window (experimental)
- added toggle for development options into the Markdown Tools category
- added help link for Markdown Tools in foldout header
- added context menu for shader property refactoring

## [1.2.0] - 2021-10-02
- bumped version to stable for AssetStore release after testing in production

## [1.2.0-pre.2] - 2021-09-15
- fixed usage with Amplify on Built-In only (no SRPs in project)
- changed Amplify sample shader to use Built-In, not URP

## [1.2.0-pre] - 2021-09-15
- added support for blackboard categories in 2021.2+
- added support for keywords in subgraphs (first-level only, same as ShaderGraph itself supports)
- added settings option to show/hide Markdown in the blackboard
- improved performance for parsing some ShaderGraph data blocks
- fixed potential errors with HDRP being in the project but not used for shaders
- fixed a number of warnings
- fixed various SRP compatibility and versioning issues

## [1.1.4] - 2021-06-28
- added explicitly specified inlined properties (`Base Map && _BaseColor`) to allow e.g. virtual texture slots to use inlined properties
- added callback `MarkdownSGExtensions.RegisterCustomBaseShaderGUI` to specify a base shader GUI
   - for HDRP ShaderGraph shaders the base shader GUI was already automatically added
   - for Amplify, you can now use the above callback to register one per shader (e.g. `UnityEditor.Rendering.HighDefinition.LitShaderGraphGUI`)
   - the base shader GUI is called with all properties that haven't been processed by `MarkdownShaderGUI`

## [1.1.3] - 2021-06-15
- fixed some issues with conditions parsing and exceptions
- fixed log message when editing gradients
- fixed applying gradients didn't work in some cases due to UI overlaps
- updated samples

## [1.1.2] - 2021-06-05
- fixed regression where gradient always saved texture on change (press Apply to see changes)
- fixed gradient texture picker sometimes showing texture preview instead
- added Uber shader sample with complex conditions
- added GradientDrawer script (same as GradientGeneratorDrawer) and removed legacy GradientDrawer ScriptableObjects

## [1.1.0] - 2021-06-05
- fixed some inline drawing issues
- fixed GradientGenerator drawer sometimes losing gradient reference
- added: experimental support for Shader Graph 12.x, needs to be updated once 2021.2 beta comes out.
- added: complex conditions (e.g. `[_Value > 0]`, `[_OPTION_A || _OPTION_B]`)
- added: generate gradients from arbitrary textures to GradientGenerator drawer
- added: quickly switch to other gradients based on generated textures
- added: conditions can now be used for entire foldout groups as well

## [1.0.3] - 2021-05-18
- fixed rare NullReference exception when changing shaders on a closed material that uses MarkdownShaderGUI
- fixed samples in AssetStore version

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