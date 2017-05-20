using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Car_ID3
{
	interface IClassificator<TValueIndex>
	{
		TValueIndex Classify(TValueIndex[] instance);
	}
}
