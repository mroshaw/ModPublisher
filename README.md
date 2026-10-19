# Mod Publisher

Mod Publisher is a Unity Editor tool for managing mod versions and publishing generated mod archives to Nexus Mods and GitHub Releases.

## Installation

Install the package from its Git repository with Unity Package Manager:

1. Open **Window > Package Manager**.
2. Open the **+** menu and choose **Add package from git URL**.
3. Enter `https://github.com/mroshaw/ModPublisher.git`.
4. Open **Tools > Mod Publisher** after Unity finishes compiling.

## Configuration

Connection details are configured in **Publishing Connections**. The GitHub owner and repository are serialized in `Assets/ModVersionSettings.asset` alongside the project's mod settings, so they can be tracked in version control. API credentials are saved separately in the current user's Unity Editor preferences and are shared between projects; they are not written to the settings asset.

When upgrading from the global repository setting, enter the GitHub owner and repository once in each project. The old global destination is deliberately not copied because it may belong to another project.

The connection section starts collapsed to keep credentials out of the way during normal publishing. Expand it when credentials or the GitHub repository details need to be changed.

- **Nexus personal API key**: the personal key used by the Nexus Mods API.
- **GitHub repository owner**: the user or organisation that owns the mod repository.
- **GitHub repository name**: the shared repository that receives every mod release.
- **GitHub personal access token**: use a fine-grained token with **Contents: write** permission for the repository.

Each mod entry has a **Publishing Sites** flags field. Check **Nexus**, **GitHub**, or both. Existing entries default to Nexus. Site-specific metadata remains on the mod:

- Nexus stores the file group, game/mod identifiers, description, and upload behavior.
- GitHub stores the tag/title templates, optional target branch or commit, draft/prerelease state, and generated-release-notes preference.

For the first Nexus upload, create the mod page on Nexus, enter its **Game Domain** and **Game Scoped Mod Id**, and leave **File Group Id** blank. Publishing creates a new file and automatically saves its returned Group ID for future versions. The ID is saved as soon as the file is created, even if adding the change log or publishing to another site subsequently fails. To update an existing file, enter its Group ID from the file's API Info dialog. A missing or invalid existing Group ID is reported as an error; it does not trigger automatic file creation. Nexus currently marks the file creation endpoint as experimental.

Use **Add Mod** below the mod list to create a clean entry for a first `1.0.0` release. Increasing the list size manually is also supported; new entries are reset to the same defaults instead of inheriting the previous mod's values.

The GitHub owner and repository apply to all mods in the current project. For example, configure `mroshaw/SubnauticaThunderKitMods` in the Subnautica project and `mroshaw/BelowZeroThunderKitMods` in the Below Zero project.

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

Each publishing row can be collapsed or expanded by clicking its mod name. Collapsed rows keep the mod name, version, and current published version visible, while hiding the version and publishing controls. Rows start collapsed, and their collapsed state follows the mod when the list is sorted.

The same staged ZIP and change log are sent to every checked site. Targets run independently, so a failure on one does not prevent the remaining selected targets from being attempted. The change log is retained after publishing until you edit or clear it manually.

GitHub publishing creates a release and then uploads the ZIP as its release asset. The selected token must have access to the configured repository, and the requested tag must not already have a release.
