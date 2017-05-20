﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Car_ID3
{
	class ValueHelper<T> where T : struct
	{
		public static readonly Func<int, T> Convert = GenerateConverter<int, T>();
		public static readonly Func<T, int> ConvertBack = GenerateConverter<T, int>();

		static Func<Tin, Tout> GenerateConverter<Tin, Tout>()
		{
			var parameter = Expression.Parameter(typeof(Tin));
			var method = Expression.Lambda<Func<Tin, Tout>>(
				Expression.ConvertChecked(parameter, typeof(Tout)),
				parameter);
			return method.Compile();
		}
	}

	class ValueHelper
	{
		public static T Convert<T>(int value) where T : struct => ValueHelper<T>.Convert(value);
	}
}
