using System;
using UnityEngine;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace DaftAppleGames.Editor.ModPublisher
{
    [Serializable]
    public class ModVersion : IComparable<ModVersion>
    {
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [HorizontalGroup("Version")]
        [LabelWidth(42)]
        [MinValue(0)]
#endif
        [SerializeField] private int major;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [HorizontalGroup("Version")]
        [LabelWidth(42)]
        [MinValue(0)]
#endif
        [SerializeField] private int minor;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [HorizontalGroup("Version")]
        [LabelWidth(42)]
        [MinValue(0)]
#endif
        [SerializeField] private int patch;

        public int Major => major;
        public int Minor => minor;
        public int Patch => patch;

        /// <summary>
        /// Copies the components from another version
        /// </summary>
        public void Set(ModVersion other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            major = other.major;
            minor = other.minor;
            patch = other.patch;
        }

        /// <summary>
        /// Compares this version with another semantic version
        /// </summary>
        public int CompareTo(ModVersion other)
        {
            if (other is null)
            {
                return 1;
            }

            int majorComparison = major.CompareTo(other.major);
            if (majorComparison != 0)
            {
                return majorComparison;
            }

            int minorComparison = minor.CompareTo(other.minor);
            return minorComparison != 0 ? minorComparison : patch.CompareTo(other.patch);
        }

        /// <summary>
        /// Increases the major component and resets the lower components
        /// </summary>
        public void IncrementMajor()
        {
            major++;
            minor = 0;
            patch = 0;
        }

        /// <summary>
        /// Increases the minor component and resets the patch component
        /// </summary>
        public void IncrementMinor()
        {
            minor++;
            patch = 0;
        }

        /// <summary>
        /// Increases the patch component
        /// </summary>
        public void IncrementPatch()
        {
            patch++;
        }

        /// <summary>
        /// Returns the version in semantic version format
        /// </summary>
        public override string ToString() => $"{major}.{minor}.{patch}";
    }
}
