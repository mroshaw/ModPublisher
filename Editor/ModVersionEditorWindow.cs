using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace DaftAppleGames.Editor.ModPublisher
{
    public sealed class ModVersionEditorWindow : EditorWindow
    {
        private enum VersionComponent
        {
            Major,
            Minor,
            Patch
        }

        private const string WindowTitle = "Mod Publisher";
        private const string NexusApiKeyEditorPrefsKey = "DaftAppleModTools.NexusMods.ApiKey";
        private const string GitHubOwnerEditorPrefsKey = "DaftAppleModTools.GitHub.Owner";
        private const string GitHubTokenEditorPrefsKey = "DaftAppleModTools.GitHub.Token";
        private const string StagingArchiveFolder = "ThunderKit/NexusMods";
        private const double PersistenceDelaySeconds = 0.75d;
        private const float EditorLabelWidth = 230.0f;
        private const string VersionConstantPattern =
            "(?<prefix>\\b(?:private|internal|public|protected)\\s+const\\s+string\\s+VersionString\\s*=\\s*\")(?<version>[^\"]*)(?<suffix>\"\\s*;)";
        private const string BaseUnityPluginClassPattern =
            "\\bclass\\s+[A-Za-z_][A-Za-z0-9_]*\\s*:\\s*[^\\{;]*\\bBaseUnityPlugin\\b";

        private SerializedObject settingsObject;
        private SerializedProperty modsProperty;
        private Vector2 scrollPosition;
        private string nexusApiKey;
        private string gitHubOwner;
        private string gitHubToken;
        private string publishChangelog = string.Empty;
        private int uploadingModIndex = -1;
        private float uploadProgress;
        private string uploadStatus;
        private CancellationTokenSource uploadCancellation;
        private bool settingsSavePending;
        private bool connectionSavePending;
        private double persistenceDueTime;
        private readonly IModPublishingTarget[] publishingTargets =
        {
            new NexusModPublishingTarget(),
            new GitHubModPublishingTarget()
        };

        [MenuItem("Tools/Mod Publisher")]
        public static void ShowWindow()
        {
            ModVersionEditorWindow window = GetWindow<ModVersionEditorWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(820.0f, 360.0f);
        }

        private void OnEnable()
        {
            settingsObject = new SerializedObject(ModVersionSettings.Instance);
            modsProperty = settingsObject.FindProperty("mods");
            nexusApiKey = EditorPrefs.GetString(NexusApiKeyEditorPrefsKey, string.Empty);
            gitHubOwner = EditorPrefs.GetString(GitHubOwnerEditorPrefsKey, string.Empty);
            gitHubToken = EditorPrefs.GetString(GitHubTokenEditorPrefsKey, string.Empty);
            EditorApplication.update -= SavePendingChanges;
            EditorApplication.update += SavePendingChanges;
        }

        private void OnDisable()
        {
            EditorApplication.update -= SavePendingChanges;
            SavePendingChanges(true);

            if (uploadCancellation != null)
            {
                uploadCancellation.Cancel();
            }
        }

        private void OnGUI()
        {
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = EditorLabelWidth;
            settingsObject.Update();

            EditorGUILayout.HelpBox(
                "Bumping updates the master version, plugin VersionString, and Manifest version. Publishing sends the generated ThunderKit/NexusMods ZIP to every site checked on the mod.",
                MessageType.Info);

            DrawConnections();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.PropertyField(modsProperty, true);
            ApplySettingsChanges();
            EditorGUILayout.Space();
            DrawPublishChangelog();
            EditorGUILayout.Space();

            for (int index = 0; index < modsProperty.arraySize; index++)
            {
                DrawVersionButtons(index);
            }

            EditorGUILayout.EndScrollView();

            ApplySettingsChanges();

            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        private void DrawVersionButtons(int index)
        {
            SerializedProperty entryProperty = modsProperty.GetArrayElementAtIndex(index);
            SerializedProperty nameProperty = entryProperty.FindPropertyRelative("name");
            SerializedProperty versionProperty = entryProperty.FindPropertyRelative("version");
            SerializedProperty currentPublishedVersionProperty =
                entryProperty.FindPropertyRelative("currentPublishedVersion");
            string displayName = string.IsNullOrWhiteSpace(nameProperty.stringValue)
                ? $"Mod {index + 1}"
                : nameProperty.stringValue;
            string version = GetVersionString(versionProperty);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{displayName}  v{version}", EditorStyles.boldLabel, GUILayout.MinWidth(180.0f));
            EditorGUILayout.LabelField(
                $"Current published: v{GetVersionString(currentPublishedVersionProperty)}",
                GUILayout.Width(190.0f));

            if (GUILayout.Button("Major +", GUILayout.Width(90.0f)))
            {
                BumpVersion(index, VersionComponent.Major);
            }

            if (GUILayout.Button("Minor +", GUILayout.Width(90.0f)))
            {
                BumpVersion(index, VersionComponent.Minor);
            }

            if (GUILayout.Button("Patch +", GUILayout.Width(90.0f)))
            {
                BumpVersion(index, VersionComponent.Patch);
            }

            EditorGUILayout.EndHorizontal();

            DrawPublishButtons(index, displayName, version);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawPublishChangelog()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Change Log for Next Publish", EditorStyles.boldLabel);
            publishChangelog = EditorGUILayout.TextArea(
                publishChangelog,
                GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 3.0f));
            EditorGUILayout.HelpBox(
                "This change log is sent to each selected site and cleared when all selected publishes succeed.",
                MessageType.None);
            EditorGUILayout.EndVertical();
        }

        private void DrawConnections()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Publishing Connections", EditorStyles.boldLabel);
            string changedNexusApiKey = EditorGUILayout.PasswordField("Nexus personal API key", nexusApiKey);
            if (changedNexusApiKey != nexusApiKey)
            {
                nexusApiKey = changedNexusApiKey;
                SchedulePersistence(false, true);
            }

            string changedGitHubOwner = EditorGUILayout.TextField("GitHub repository owner", gitHubOwner);
            if (changedGitHubOwner != gitHubOwner)
            {
                gitHubOwner = changedGitHubOwner;
                SchedulePersistence(false, true);
            }

            string changedGitHubToken = EditorGUILayout.PasswordField("GitHub personal access token", gitHubToken);
            if (changedGitHubToken != gitHubToken)
            {
                gitHubToken = changedGitHubToken;
                SchedulePersistence(false, true);
            }

            EditorGUILayout.HelpBox(
                "Connection details are stored in this user's Unity Editor preferences and are not written to the project asset. The GitHub token needs Contents: write access to each target repository.",
                MessageType.None);
            EditorGUILayout.EndVertical();
        }

        private void DrawPublishButtons(
            int index,
            string displayName,
            string version)
        {
            if (index < 0 || index >= ModVersionSettings.Instance.Mods.Count)
            {
                return;
            }

            ModVersionEntry entry = ModVersionSettings.Instance.Mods[index];
            string generatedZipPath = GetGeneratedZipPath(entry);
            string sites = GetSelectedSiteNames(entry);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(18.0f);
            EditorGUILayout.LabelField(
                File.Exists(generatedZipPath)
                    ? $"Archive: {Path.GetFileName(generatedZipPath)}  →  {sites}"
                    : $"Archive not built: {Path.GetFileName(generatedZipPath)}",
                GUILayout.MinWidth(300.0f));

            GUI.enabled = uploadingModIndex < 0;
            if (GUILayout.Button($"Publish {displayName} v{version}", GUILayout.Width(280.0f)))
            {
                Publish(index);
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (uploadingModIndex == index)
            {
                Rect progressRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                EditorGUI.ProgressBar(progressRect, uploadProgress, uploadStatus);
                if (GUILayout.Button("Cancel upload", GUILayout.Width(120.0f)))
                {
                    uploadCancellation.Cancel();
                }
            }
        }

        private void ApplySettingsChanges()
        {
            if (!settingsObject.ApplyModifiedProperties())
            {
                return;
            }

            EditorUtility.SetDirty(ModVersionSettings.Instance);
            SchedulePersistence(true, false);
        }

        private void SchedulePersistence(bool saveSettings, bool saveConnections)
        {
            settingsSavePending |= saveSettings;
            connectionSavePending |= saveConnections;
            persistenceDueTime = EditorApplication.timeSinceStartup + PersistenceDelaySeconds;
        }

        private void SavePendingChanges()
        {
            SavePendingChanges(false);
        }

        private void SavePendingChanges(bool force)
        {
            if (!settingsSavePending && !connectionSavePending)
            {
                return;
            }

            if (!force && EditorApplication.timeSinceStartup < persistenceDueTime)
            {
                return;
            }

            if (settingsSavePending)
            {
                ModVersionSettings.Instance.SaveSettings();
                settingsSavePending = false;
            }

            if (connectionSavePending)
            {
                EditorPrefs.SetString(NexusApiKeyEditorPrefsKey, nexusApiKey);
                EditorPrefs.SetString(GitHubOwnerEditorPrefsKey, gitHubOwner);
                EditorPrefs.SetString(GitHubTokenEditorPrefsKey, gitHubToken);
                connectionSavePending = false;
            }
        }

        private async void Publish(int index)
        {
            settingsObject.ApplyModifiedProperties();
            ModVersionEntry entry = ModVersionSettings.Instance.Mods[index];
            string archivePath = GetGeneratedZipPath(entry);
            ModPublishingContext context = new ModPublishingContext(
                entry,
                archivePath,
                publishChangelog,
                nexusApiKey,
                gitHubOwner,
                gitHubToken);
            List<IModPublishingTarget> selectedTargets = GetSelectedTargets(entry);
            if (!TryValidatePublish(context, selectedTargets, out string error))
            {
                EditorUtility.DisplayDialog(WindowTitle, error, "OK");
                return;
            }

            if (entry.Version.CompareTo(entry.CurrentPublishedVersion) <= 0)
            {
                EditorUtility.DisplayDialog(
                    WindowTitle,
                    $"Version {entry.Version} cannot be published because the current published version is {entry.CurrentPublishedVersion}.\n\nIncrement the mod version before publishing again.",
                    "OK");
                return;
            }

            string version = entry.Version.ToString();
            StringBuilder targetDescription = new StringBuilder();
            foreach (IModPublishingTarget target in selectedTargets)
            {
                targetDescription.Append("\n• ");
                targetDescription.Append(target.Describe(context));
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Publish Mod",
                $"Upload '{archivePath}' as {entry.Name} v{version} to:{targetDescription}?\n\nThe change log will be cleared when every publish succeeds.",
                "Yes, Publish",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            uploadingModIndex = index;
            uploadProgress = 0.0f;
            uploadStatus = "Starting upload...";
            CancellationTokenSource cancellation = new CancellationTokenSource();
            uploadCancellation = cancellation;
            List<string> successes = new List<string>();
            List<string> failures = new List<string>();

            try
            {
                for (int targetIndex = 0; targetIndex < selectedTargets.Count; targetIndex++)
                {
                    string versionId = await client.UploadNewVersionAsync(
                        entry.NexusMods,
                        generatedZipPath,
                        version,
                        nexusChangelog,
                        progress,
                        cancellation.Token);
                    entry.CurrentPublishedVersion.Set(entry.Version);
                    ModVersionSettings.Instance.SaveSettings();
                    settingsObject.Update();
                    nexusChangelog = string.Empty;
                    EditorUtility.DisplayDialog(
                        WindowTitle,
                        $"Published {entry.Name} {version} successfully. Nexus version ID: {versionId}",
                        "OK");
                    IModPublishingTarget target = selectedTargets[targetIndex];
                    int capturedTargetIndex = targetIndex;
                    Progress<UploadProgress> progress = new Progress<UploadProgress>(value =>
                        UpdateUploadProgress(value, target.DisplayName, capturedTargetIndex, selectedTargets.Count));
                    try
                    {
                        ModPublishingResult result = await target.PublishAsync(
                            context,
                            progress,
                            cancellation.Token);
                        successes.Add($"{target.DisplayName}: {result.Message}");
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                        failures.Add($"{target.DisplayName}: {exception.Message}");
                    }
                }

                if (failures.Count == 0)
                {
                    publishChangelog = string.Empty;
                }

                string heading = failures.Count == 0
                    ? $"Published {entry.Name} {version} successfully."
                    : $"Publishing {entry.Name} {version} completed with errors.";
                string details = string.Join("\n", successes.ToArray());
                if (failures.Count > 0)
                {
                    details += (details.Length == 0 ? string.Empty : "\n\n") +
                               "Failed:\n" + string.Join("\n", failures.ToArray());
                }

                EditorUtility.DisplayDialog(WindowTitle, heading + "\n\n" + details, "OK");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"Publishing {entry.Name} was cancelled.");
            }
            finally
            {
                cancellation.Dispose();
                if (uploadCancellation == cancellation)
                {
                    uploadCancellation = null;
                }

                uploadingModIndex = -1;
                Repaint();
            }
        }

        private void UpdateUploadProgress(
            UploadProgress progress,
            string targetName,
            int targetIndex,
            int targetCount)
        {
            uploadProgress = (targetIndex + progress.Progress) / targetCount;
            uploadStatus = $"{targetName}: {progress.Status}";
            Repaint();
        }

        private static bool TryValidatePublish(
            ModPublishingContext context,
            IReadOnlyList<IModPublishingTarget> selectedTargets,
            out string error)
        {
            error = null;
            if (selectedTargets.Count == 0)
            {
                error = "Check at least one Publishing Site for this mod.";
                return false;
            }

            if (context.Entry.Manifest == null || context.Entry.Manifest.Identity == null ||
                string.IsNullOrWhiteSpace(context.Entry.Manifest.Identity.Name))
            {
                error = "Assign a valid ThunderKit Manifest so the generated archive can be located.";
                return false;
            }

            if (!File.Exists(context.ArchivePath))
            {
                error = $"The generated archive does not exist:\n{context.ArchivePath}\n\nRun the ThunderKit build/deploy pipeline first.";
                return false;
            }

            foreach (IModPublishingTarget target in selectedTargets)
            {
                if (!target.TryValidate(context, out string targetError))
                {
                    error = $"{target.DisplayName}: {targetError}";
                    return false;
                }
            }

            return true;
        }

        private List<IModPublishingTarget> GetSelectedTargets(ModVersionEntry entry)
        {
            List<IModPublishingTarget> selected = new List<IModPublishingTarget>();
            foreach (IModPublishingTarget target in publishingTargets)
            {
                if (entry.PublishesTo(target.Site))
                {
                    selected.Add(target);
                }
            }

            return selected;
        }

        private string GetSelectedSiteNames(ModVersionEntry entry)
        {
            List<IModPublishingTarget> selected = GetSelectedTargets(entry);
            if (selected.Count == 0)
            {
                return "No sites selected";
            }

            string[] names = new string[selected.Count];
            for (int index = 0; index < selected.Count; index++)
            {
                names[index] = selected[index].DisplayName;
            }

            return string.Join(", ", names);
        }

        private static string GetGeneratedZipPath(ModVersionEntry entry)
        {
            string manifestName = entry.Manifest == null || entry.Manifest.Identity == null
                ? entry.Name
                : entry.Manifest.Identity.Name;
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot, StagingArchiveFolder, manifestName + ".zip"));
        }

        private void BumpVersion(int index, VersionComponent component)
        {
            settingsObject.ApplyModifiedProperties();
            ModVersionEntry entry = ModVersionSettings.Instance.Mods[index];

            if (!TryValidateEntry(entry, out string pluginPath, out string error))
            {
                EditorUtility.DisplayDialog(WindowTitle, error, "OK");
                return;
            }

            Undo.RecordObject(ModVersionSettings.Instance, $"Bump {entry.Name} {component} version");
            Undo.RecordObject(entry.Manifest.Identity, $"Update {entry.Name} manifest version");

            IncrementVersion(entry.Version, component);
            string newVersion = entry.Version.ToString();
            string source = File.ReadAllText(pluginPath);
            string updatedSource = Regex.Replace(
                source,
                VersionConstantPattern,
                match => match.Groups["prefix"].Value + newVersion + match.Groups["suffix"].Value,
                RegexOptions.CultureInvariant);

            File.WriteAllText(pluginPath, updatedSource);
            entry.Manifest.Identity.Version = newVersion;
            EditorUtility.SetDirty(entry.Manifest.Identity);
            ModVersionSettings.Instance.SaveSettings();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(entry.PluginScript));
            AssetDatabase.SaveAssets();
            settingsObject.Update();

            Debug.Log($"Updated {entry.Name} to version {newVersion}.");
        }

        private static bool TryValidateEntry(ModVersionEntry entry, out string pluginPath, out string error)
        {
            pluginPath = null;
            error = null;

            if (entry.PluginScript == null)
            {
                error = "Assign the plugin script before changing its version.";
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(entry.PluginScript);
            pluginPath = Path.GetFullPath(assetPath);
            string source = File.ReadAllText(pluginPath);
            Type pluginType = entry.PluginScript.GetClass();
            bool isBaseUnityPlugin = pluginType != null && InheritsFromBaseUnityPlugin(pluginType);
            if (!isBaseUnityPlugin && !Regex.IsMatch(
                    source,
                    BaseUnityPluginClassPattern,
                    RegexOptions.CultureInvariant))
            {
                error = "The assigned script must contain a class derived from BaseUnityPlugin.";
                return false;
            }

            if (entry.Manifest == null || entry.Manifest.Identity == null)
            {
                error = "Assign a valid ThunderKit Manifest with an identity before changing its version.";
                return false;
            }

            MatchCollection matches = Regex.Matches(source, VersionConstantPattern, RegexOptions.CultureInvariant);
            if (matches.Count != 1)
            {
                error = matches.Count == 0
                    ? "The plugin script does not contain a VersionString constant."
                    : "The plugin script contains more than one VersionString constant.";
                return false;
            }

            return true;
        }

        private static bool InheritsFromBaseUnityPlugin(Type pluginType)
        {
            Type currentType = pluginType.BaseType;
            while (currentType != null)
            {
                if (currentType.FullName == "BepInEx.BaseUnityPlugin")
                {
                    return true;
                }

                currentType = currentType.BaseType;
            }

            return false;
        }

        private static void IncrementVersion(ModVersion version, VersionComponent component)
        {
            switch (component)
            {
                case VersionComponent.Major:
                    version.IncrementMajor();
                    break;
                case VersionComponent.Minor:
                    version.IncrementMinor();
                    break;
                case VersionComponent.Patch:
                    version.IncrementPatch();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(component), component, null);
            }
        }

        private static string GetVersionString(SerializedProperty versionProperty)
        {
            int major = versionProperty.FindPropertyRelative("major").intValue;
            int minor = versionProperty.FindPropertyRelative("minor").intValue;
            int patch = versionProperty.FindPropertyRelative("patch").intValue;
            return $"{major}.{minor}.{patch}";
        }
    }
}
