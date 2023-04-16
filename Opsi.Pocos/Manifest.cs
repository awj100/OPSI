using System;
using System.Collections.Generic;

namespace Opsi.Pocos
{
	public class Manifest
	{
		public Manifest()
		{
			CallbackUri = String.Empty;
			PackageName = String.Empty;
			ProjectId = Guid.Empty;
            ResourceExclusionPaths = new List<string>();
        }

		public string CallbackUri { get; set; }

		public string HandlerQueue { get; set; }

		public string PackageName { get; set; }

		public Guid ProjectId { get; set; }

        public List<string> ResourceExclusionPaths { get; set; }

        public string GetPackagePathForStore()
		{
			return $"{ProjectId}/{PackageName}";
		}
	}
}
