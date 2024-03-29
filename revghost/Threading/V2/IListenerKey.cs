﻿using System.Collections.Generic;

namespace revghost.Threading.V2;

public interface IKey<TKeyType, TValueType>
{
	void ThrowIfNotValid(IDictionary<TKeyType, List<TValueType>> keyMap, TValueType toInsert);
	void Insert(IDictionary<TKeyType, List<TValueType>>          keyMap, TValueType toInsert);
}

public interface IListenerKey : IKey<IListenerKey, IListener>
{
}