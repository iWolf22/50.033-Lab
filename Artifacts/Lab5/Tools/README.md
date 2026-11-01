# Recording tools

These scripts run only in the ignored `.unity-validation` copy. They do not belong in the shipped Assets directory.

- `TeenSetup.cs`: asset/prefab migration used for this lab; operates on saved scenes without rebuilding their layout. `FinalizeLayout` adds/positions only the flower box, and `WireFatalState` assigns the fatal-fall FSM state.
- `TeenRecordingTests.cs`: copy into the validation copy's PlayMode test directory, then run the `TeenRecordingTests` filter. Records focused collision-based demonstrations, PNG frames, captions and mixed audio. It uses setup positions, rather than a continuous course run.
- `TeenArchitectureCapture.cs`: copy into the validation copy's Editor directory, then run `TeenArchitectureCapture.Capture` in a GUI editor. Captures Unity's own Inspector, Hierarchy, Game View and source-window framebuffers. It temporarily disables URP's optional rendering debugger for the capture, so that Unity's own persistent debug helper is absent. It does not hide or delete gameplay objects.
- `EncodeRecordings.ps1`: expects a standalone `ffmpeg.exe` in the validation project. Encodes the PNG frames, adds captions, keeps the mixed audio and combines the two clips. Writes decoder/audio verification logs.

The selected test names are in `../Verification/test-selection.txt`. Unity test invocations omit `-quit`; editor utility invocations use `-executeMethod` (and `-batchmode -quit` when no GUI is needed). Do not run the course builder or copy a generated level over the working scenes.
