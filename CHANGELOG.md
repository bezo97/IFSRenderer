# Changelog
All notable changes to this project will be documented in this file.

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

[1.1.0]: https://github.com/bezo97/IFSRenderer/releases/tag/v1.1.0
[1.0.0]: https://github.com/bezo97/IFSRenderer/releases/tag/v1.0.0
