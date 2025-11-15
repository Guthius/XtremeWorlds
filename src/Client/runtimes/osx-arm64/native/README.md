Place macOS native libraries here if you want them bundled with the client.

Expected filenames:
- libSDL3.0.dylib
- libFNA3D.0.dylib

Options:
- Install SDL3 via Homebrew: `brew install sdl3`
- Build FNA3D from source and copy `libFNA3D.0.dylib` into this folder.
- Or create symlinks to your Homebrew-installed libraries (e.g. in /opt/homebrew/Cellar) so the files are present here during packaging.

The project file (`Client.csproj`) is configured to copy any files placed here to the build output directory.
