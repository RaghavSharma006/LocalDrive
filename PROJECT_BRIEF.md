# LocalDrive build prompt

Build LocalDrive as a complete, standalone Windows desktop app for mounting the user's TeraBox account as a read/write drive. Give it its own clear, restrained visual identity and a polished, easy-to-understand interface. Do not require, launch, bundle, or display branding from other drive products.

The app should sign in to TeraBox in its own secure sign-in surface, validate access before saving a session, and protect saved credentials with Windows DPAPI. It should mount a configurable drive letter through WinFsp and the bundled-at-build-time rclone TeraBox backend. It should run quietly in the notification area, use little idle CPU, and safely wait for pending writes before disconnecting.

Let the user choose any writable local cache folder and set a cache cleanup target. Explain that reads are downloaded on demand, writes are staged locally then uploaded, and the target is a cleanup limit rather than a reserved disk allocation. Keep light and dark themes, show connection and pending-transfer status, and make sign-in, connect, open, disconnect, cache selection, and exit behavior clear.

Ship a Git-ready source project, a complete README with install/build/use/security/troubleshooting guidance, license notices, a reproducible Windows package build, and a GitHub release workflow. Exclude generated binaries, caches, secrets, old application files, and unrelated integrations from the source bundle. Preserve existing TeraBox, read/write, cache, and mount behavior. Verify compilation and package contents; clearly distinguish those checks from a live account test.
