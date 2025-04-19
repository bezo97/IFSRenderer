# Changelog

All notable changes to this project will be documented in this file.

## [1.4.1] - 2025-04-19
### Added
 - Control camera roll and field-of-view by dragging mouse right button.
 - Option to generate palettes with pigment-based color mixing.
 - Option to import keyframes from CSV file.
 - Support for planetarium systems where the projector is off-center, using the Tilt sliders.

### Changed
 - Updated dependencies.
 - Strength of the fog effect is changed so it's easier to adjust within reasonable limits.
 - Optimized hashing gives a significant performance boost.

### Fixed
 - Fix various startup problems.
 - Fix laggy animation editor when using many keyframes.
 - Fix graphics related startup error on AMD hardware.
 - Various fixes on camera projections:
   - Fix dark poles in equirectangular projection. Might take more rendertime to clear up the noise.
   - Fix bright vertical line in equirectangular projection.
   - Fix white spot in the image center.

### Removed
 - Temporarily removed the ability to pop-out panels into separate windows until fix.

## [1.4.0] - 2024-02-25
### Added
 - Fisheye camera projection that enables exporting to fulldome planetarium projection systems.
 - Option to include metadata in exported images, such as author names and parameters. Images that include params can be can be drag & dropped the same way as ifsjson files. This must be enabled in the Settings first.
 - A feature that marks focused areas in red while scrolling the `Focus Distance` and `Depth of Field` camera parameters.
 - Plugin Includes, a feature that allows plugin developers to share functions between transforms. See the updated wiki for details.
 - Plugin files can be drag & dropped on the window to save them in your transform library.

### Changed
 - Small improvement to the equirectangular projection. The camera now keeps its orientation when switching between projection types.

### Fixed
 - A bug that corrupted part of the rendering state that resulted in wrong fractal calculation (this may break your old saves).
 - Clicking on the fractal while the window was not focused would reset the accumulation.
 - Animation frames can now be exported with transparent background.
 - An encoding bug that prevented users to load the attractors transform pack.

## [1.3.1] - 2024-01-02
### Fixed
 - Hotfix crash after first frame of animation export

## [1.3.0] - 2023-12-31
### Added
 - Animations
   - Basic features that let the user animate any value using keyframes and render frames and video
   - You can also load an audio file and animate the fractal to the beat
   - See the wiki for more info
 - 360-sphere camera mode (using equirectangular projection)
 - Camera Navigation panel (to precisely control position and orientation)
 - More transforms
   - They can now be searched by name and tags
   - An optional ["Attractors" transform pack](https://github.com/bezo97/ifsr-attractors)
 - A welcome screen
   - Shows a featured artwork each release
   - Options to quickstart different workflows
   - Can be disabled
 - `Split` operation - similar to `Duplicate` but weights are adjusted to keep the same look of the fractal
 - Countless small UX/UI improvements
   - The layout can now be rearranged by the user
   - Panels can be popped out into small tool windows that stay on top
   - You can now click and drag on the palette to select a color
   - A small icon on the bottom bar shows when a gamepad controller is properly connected
 - Separate `Save` / `Save as...` functions
 - Ability to load recently edited files
 - Ability to customize image resolution presets and video encoding presets
 - Prompt user to save unsaved changes before quitting
 - Option to start blank params with simple white palette instead of random colors
 - `Target Iteration Level` render setting, which is used to tell when the animation frame is considered finished
   - A progress bar shows the rendering progress in the bottom bar
 - A `discarded_point` const available for plugin developers

### Changed
 - Editor nodes have a new look
   - Added a small grabbing area to move the node
   - A button to connect the node to itself
 - Generated fractals look nicer thanks to preferring certain transforms and avoiding others
 - Palettes are now generated in HSV color space
 - The installer release is now self-contained, just like the the portable release. This means that installing the .NET Runtime is no longer a requirement, which was a confusion to many

### Fixed
 - File dialogs now remember the last directory
 - Freezing GUI while the generator window is rendering
 - Unreachable dialogs stuck under other windows
 - Nodes jumping around occassionally when dragging a new connection
 - Nodes jumping around after undo/redo and reloading a file
 - Losing selection after undo/redo operation
 - Black thumbnails of mutated fractals
 - Wrong color taken from palette when index is 1.0
 - Rendering bug that caused occassional glitches on the edge of the image
 - Rendering bug that caused a white dot in the center
 - Vendor-specific rendering related small fixes (eg. undefined behaviors, buffer layouts)

### Removed
 - Broken TAA pass
 - Options to invert X and Z axis of the controller

## [1.2.0] - 2022-03-08
### Added
- Pause/Resume rendering button
- Ability to name nodes
- Copy/paste parameters from clipboard
- Drag & drop files to load parameters and palettes
- Reset parameters to their default values
  - Either by using the Reset button from the context menu,
  - or by double clicking on the parameters, similar to Apophysis
- Support for gradient files exported from UltraFractal
- Adjustable batch size in generator window
- Keyboard usability improvements
  - increment parameters with up/down arrow keys
  - tab-navigation improvements
- Bunch of under-the-hood improvements for developers
  - CI pipeline for pull requests
  - use latest dotnet/c# features

### Changed
- Node positions are now saved into param files, so arrangement is kept between sessions
- use Ctrl instead of Alt key to step parameters in larger increment
- New version of the Spherical transform that now has a Radius parameter
- Small visual changes to node editor and generator window

### Fixed
- Black screen after successfully reloading plugins after a failed attempt
- Black screen after opening window from minimized state
- Nodes sometimes not following the mouse while rearranging
- Nodes jumping around to random positions on undo/redo
- Unreachable palette window after alt-tabbing away
- Loose gamepad thumbsticks controlling the camera even in neutral state
- Crash when loading file that uses an older plugin version that has missing parameters
- Frozen progressbar on startup

## [1.1.0] - 2021-11-27
### Added
- Gamepad support (for XInput compatible devices, such as XBox controller)
  - Left thumbstick - move around
  - Right thumbstick - look around
  - <kbd>LB</kbd>/<kbd>RB</kbd> buttons - rolling motion
  - <kbd>LT</kbd>/<kbd>RT</kbd> buttons - adjust focus distance
- 3 new cropping transforms (thanks to Rychveldir!)
- Option to invert camera control rotation axes
- Sensitivity setting to fine-tune camera motion controls
- Show the color of the iterator on the node border
- Use shift, alt keys to modify the increment size when navigating and changing values.
- More shortcut keys added to the editor:
  - <kbd>Ctrl+W</kbd> - Flip weight of selected iterator between 0/1
  - <kbd>Ctrl+O</kbd> - Flip opacity of selected iterator between 0/1
  - <kbd>Ctrl+D</kbd> - Duplicate selected iterator
  - <kbd>Delete</kbd> - Delete selected nodes and arrows
- Artwork title and file name is synchronized when unset
- Ability to pin loaded fractal to generator, unpin pinned fractals
- Newly pinned fractals are shown in the main window

### Changed
- Improved tab-key navigation: the +/- buttons are skipped and tabbing on a value makes it editable.

### Fixed
- Occassional wrong camera orientation after opening a file.

### Removed
- Ability to load .ugr gradient files, which aren't supported.

## [1.0.0] - 2021-11-15
### Added
- First release

[1.4.1]: https://github.com/bezo97/IFSRenderer/releases/tag/v1.4.1
[1.4.0]: https://github.com/bezo97/IFSRenderer/releases/tag/v1.4.0
[1.3.1]: https://github.com/bezo97/IFSRenderer/releases/tag/v1.3.1
[1.3.0]: https://github.com/bezo97/IFSRenderer/releases/tag/v1.3.0
[1.2.0]: https://github.com/bezo97/IFSRenderer/releases/tag/v1.2.0
[1.1.0]: https://github.com/bezo97/IFSRenderer/releases/tag/v1.1.0
[1.0.0]: https://github.com/bezo97/IFSRenderer/releases/tag/v1.0.0
