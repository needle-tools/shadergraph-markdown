# Changelog
All notable changes to this package will be documented in this file.
The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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