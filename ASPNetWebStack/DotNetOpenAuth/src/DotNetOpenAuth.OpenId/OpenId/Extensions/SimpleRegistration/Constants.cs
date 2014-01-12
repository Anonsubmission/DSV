﻿// <auto-generated/> // disable StyleCop on this file
//-----------------------------------------------------------------------
// <copyright file="Constants.cs" company="Outercurve Foundation">
//     Copyright (c) Outercurve Foundation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.OpenId.Extensions.SimpleRegistration {
	using System;
	using System.Collections.Generic;
	using System.Text;

	/// <summary>
	/// Simple Registration constants
	/// </summary>
	public static class Constants {
		/// <summary>
		/// Commonly used type URIs to represent the Simple Registration extension.
		/// </summary>
		public static class TypeUris {
			/// <summary>
			/// The URI "http://openid.net/extensions/sreg/1.1".  
			/// </summary>
			/// <remarks>
			/// This is the type URI prescribed by the Simple Registration 1.1 spec. 
			/// http://openid.net/specs/openid-simple-registration-extension-1_1-01.html#anchor3
			/// </remarks>
			public const string Standard = "http://openid.net/extensions/sreg/1.1";

			/// <summary>
			/// The URI "http://openid.net/sreg/1.0"
			/// </summary>
			public const string Variant10 = "http://openid.net/sreg/1.0";

			/// <summary>
			/// The URI "http://openid.net/sreg/1.1"
			/// </summary>
			public const string Variant11 = "http://openid.net/sreg/1.1";
		}

		internal const string sreg_compatibility_alias = "sreg";
		internal const string policy_url = "policy_url";
		internal const string optional = "optional";
		internal const string required = "required";
		internal const string nickname = "nickname";
		internal const string email = "email";
		internal const string fullname = "fullname";
		internal const string dob = "dob";
		internal const string gender = "gender";
		internal const string postcode = "postcode";
		internal const string country = "country";
		internal const string language = "language";
		internal const string timezone = "timezone";
		internal static class Genders {
			internal const string Male = "M";
			internal const string Female = "F";
		}

		/// <summary>
		/// Additional type URIs that this extension is sometimes known by remote parties.
		/// </summary>
		internal static readonly string[] AdditionalTypeUris = new string[] {
			Constants.TypeUris.Variant10,
			Constants.TypeUris.Variant11,
		};
	}
}