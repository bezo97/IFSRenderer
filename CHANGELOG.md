# Changelog
All notable changes to this project will be documented in this file.

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

[1.2.0]: https://github.com/bezo97/IFSRenderer/releases/tag/v1.2.0
[1.1.0]: https://github.com/bezo97/IFSRenderer/releases/tag/v1.1.0
[1.0.0]: https://github.com/bezo97/IFSRenderer/releases/tag/v1.0.0
