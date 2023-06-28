﻿using System;
using System.Collections.Generic;

namespace Opsi.Pocos
{
	public class InternalManifest : Manifest
	{
        // Required for deserialisation.
        public InternalManifest()
        {
            PackageName = String.Empty;
            ProjectId = Guid.Empty;
            ResourceExclusionPaths = new List<string>();
            WebhookUri = String.Empty;
        }

        public InternalManifest(Manifest manifest, string username)
		{
            foreach (var propertyInfo in manifest.GetType()
                                                 .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                propertyInfo.SetValue(this, propertyInfo.GetValue(manifest));
            }

            Username = username;
        }

		public string Username { get; set; }
    }
}
