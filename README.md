# Mod Publisher

Mod Publisher is a Unity Editor tool for managing mod versions and publishing generated mod archives to Nexus Mods and GitHub Releases.

## Installation

Install the package from its Git repository with Unity Package Manager:

1. Open **Window > Package Manager**.
2. Open the **+** menu and choose **Add package from git URL**.
3. Enter `https://github.com/mroshaw/ModPublisher.git`.
4. Open **Tools > Mod Publisher** after Unity finishes compiling.

## Configuration

Connection details are configured once in **Publishing Connections**. They are saved in the current user's Unity Editor preferences and are not written to `Assets/ModVersionSettings.asset`.

- **Nexus personal API key**: the personal key used by the Nexus Mods API.
- **GitHub repository owner**: the user or organisation that owns the mod repositories.
- **GitHub personal access token**: use a fine-grained token with **Contents: write** permission for each repository that will receive releases.

Each mod entry has a **Publishing Sites** flags field. Check **Nexus**, **GitHub**, or both. Existing entries default to Nexus. Site-specific metadata remains on the mod:

- Nexus stores the file group, game/mod identifiers, description, and upload behavior.
- GitHub stores the repository name, tag/title templates, optional target branch or commit, draft/prerelease state, and generated-release-notes preference.

The GitHub owner is intentionally connection-level. Enter only the repository name (without `owner/`) on each mod.

## GitHub release naming

GitHub templates support these tokens:

- `{displayName}`: the mod entry's friendly name.
- `{archiveName}`: the ThunderKit manifest identity name, also used for the staged ZIP filename.
- `{version}`: the current semantic version without a leading `v`.

The defaults are:

- Tag: `{archiveName}-v{version}`
- Release title: `{displayName} v{version}`

For a display name of `Auto Locker Labels SV`, a manifest identity of `AutoLockerLabels_SN`, and version `1.6.3`, these produce `AutoLockerLabels_SN-v1.6.3` and `Auto Locker Labels SV v1.6.3`.

## Publishing

1. Build/deploy the mod so ThunderKit creates `ThunderKit/NexusMods/<manifest-name>.zip`.
2. Enter the change log for the next publish.
3. Click the mod's **Publish** button and confirm the listed targets.

The same staged ZIP and change log are sent to every checked site. Targets run independently, so a failure on one does not prevent the remaining selected targets from being attempted. The change log is cleared only when every selected publish succeeds.

GitHub publishing creates a release and then uploads the ZIP as its release asset. The selected token must have access to the configured repository, and the requested tag must not already have a release.
