using Opsi.Common;

namespace Opsi.AzureStorage.Types
{
    public struct VersionInfo
    {
        public VersionInfo(int version, string? lockedTo = null)
        {
            LockedTo = lockedTo != null ? Option<string>.Some(lockedTo) : Option<string>.None();
            Index = version;
        }

        /// <summary>
        /// Gets or sets the numerical version of a resource.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets an <see cref="Option{string}"/> specifying the username of the user to whom the resource is locked.
        /// </summary>
        public Option<string> LockedTo { get; set; }

        /// <summary>
        /// Obtains an instance of <see cref="VersionInfo"/> for the next version of the resource.
        /// </summary>
        /// <param name="shouldLockToSameUser">
        /// If <c>true</c>, the current instance's <see cref="LockedTo"/> property is cloned to the returned instance.
        /// </param>
        /// <returns>A <see cref="VersionInfo"/> object with <see cref="Index"/> incremented.</returns>
        public VersionInfo GetNextVersionInfo(bool shouldLockToSameUser = false)
        {
            var lockTo = shouldLockToSameUser && LockedTo.IsSome
                ? LockedTo.Value
                : null;

            return new VersionInfo(Index + 1, lockTo);
        }
    }
}
