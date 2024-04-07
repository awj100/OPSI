using Opsi.Common;

namespace Opsi.AzureStorage.Types
{
    public struct VersionInfo
    {
        public VersionInfo(int versionIndex, string? assignedTo = null) : this(String.Empty, versionIndex, assignedTo)
        {}
        
        public VersionInfo(string id, int versionIndex, string? assignedTo = null)
        {
            AssignedTo = assignedTo != null ? Option<string>.Some(assignedTo) : Option<string>.None();
            Id = id;
            Index = versionIndex;
        }

        /// <summary>
        /// Gets or sets the Azure Storage-provided version ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the numerical version of a resource.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets an <see cref="Option{string}"/> specifying the username of the user to whom the resource is assigned.
        /// </summary>
        public Option<string> AssignedTo { get; set; }

        /// <summary>
        /// Obtains an instance of <see cref="VersionInfo"/> for the next version of the resource.
        /// </summary>
        /// <param name="shouldAssignToSameUser">
        /// If <c>true</c>, the current instance's <see cref="AssignedTo"/> property is cloned to the returned instance.
        /// </param>
        /// <returns>A <see cref="VersionInfo"/> object with <see cref="Index"/> incremented.</returns>
        public VersionInfo GetNextVersionInfo(bool shouldAssignToSameUser = false)
        {
            var lockTo = shouldAssignToSameUser && AssignedTo.IsSome
                ? AssignedTo.Value
                : null;

            return new VersionInfo(Index + 1, lockTo);
        }

        public override string ToString()
        {
            var assignedToInfo = AssignedTo.IsSome
                ? $"Assigned to: {AssignedTo.Value}"
                : "(Unlocked)";

            return $"Index {Index}; {assignedToInfo}";
        }
    }
}
