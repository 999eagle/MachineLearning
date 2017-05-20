using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Car_ID3
{
	class ValueHelper<T> where T : struct
	{
		public static readonly Func<int, T> Convert = GenerateConverter();

		static Func<int, T> GenerateConverter()
		{
			var parameter = Expression.Parameter(typeof(int));
			var method = Expression.Lambda<Func<int, T>>(
				Expression.ConvertChecked(parameter, typeof(T)),
				parameter);
			return method.Compile();
		}
	}

	class ValueHelper
	{
		public static T Convert<T>(int value) where T : struct => ValueHelper<T>.Convert(value);
	}
}
