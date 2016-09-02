using UnityEngine;
using System.Collections;

public class Utils {
	static uint prime = 4294967291;
	static uint ord = 4294967290;
	static uint generator = 4294967279;
	static uint sy;
	static uint xs;
	static uint xy;
	public static float getFloat(uint x, uint y, uint seed)
	{
		//will return values 1=> x >0; replace 'ord' with 'prime' to get 1> x >0
		//one call to modPow would be enough if all data fits into an ulong
		sy = modPow(generator, (((ulong)seed) << 32) + (ulong)y, prime);
		xs = modPow(generator, (((ulong)x) << 32) + (ulong)seed, prime);
		xy = modPow(generator, (((ulong)sy) << 32) + (ulong)xy, prime);
		return ((float)xy) / ord;
	}
	public static int RandomRange(int x, int y, int seed, int low, int high)
	{
		return low + (int)(getFloat((uint)x, (uint)y, (uint)seed) * (high - low));
	}
	static ulong b;
	static ulong ret;
	static uint modPow(uint bb, ulong e, uint m)
	{
		b = bb;
		ret = 1;
		while (e > 0)
		{
			if (e % 2 == 1)
			{
				ret = (ret * b) % m;
			}
			e = e >> 1;
			b = (b * b) % m;
		}
		return (uint)ret;
	}
}
