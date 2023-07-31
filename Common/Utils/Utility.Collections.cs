﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Macrocosm.Common.Utils
{
	public static partial class Utility
	{
		/// <summary> Concatenates IEnumberable collections into a single collection</summary>
		public static IEnumerable<T> Concatenate<T>(params IEnumerable<T>[] collections)
			=> collections.Aggregate((acc, collection) => acc.Concat(collection));

	}
}
