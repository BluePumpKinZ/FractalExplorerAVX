using System;
using System.CodeDom;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace FractalExplorer.Numerics {

	public struct DynamicDecimal {

		private bool sign;
		private uint[] data; // 8 digits per int.
		private DecimalContext context;
		public uint[] Data { get { return data; } }
		public static readonly DecimalContext defaultContext = new DecimalContext (8);
		public static readonly DynamicDecimal zero = new DynamicDecimal (0, defaultContext);
		public static readonly DynamicDecimal one = new DynamicDecimal (1, defaultContext);

		private static int GetDataLength (DecimalContext context) {
			return (int)Math.Ceiling (context.Precision / 8.0) + 1;
		}

		private DynamicDecimal (uint[] data, bool sign, DecimalContext context) {
			this.data = data;
			this.sign = sign;
			this.context = context;
		}

		public DynamicDecimal (short value, DecimalContext context) {

			this.context = context;
			data = new uint[GetDataLength (context)];
			data[0] = (uint)Math.Abs (value);
			sign = value < 0;
		}

		public DynamicDecimal (ushort value, DecimalContext context) {

			this.context = context;
			data = new uint[(int)Math.Ceiling (this.context.Precision / 8.0) + 1];
			data[0] = (uint)Math.Abs (value);
			sign = false;
		}

		public DynamicDecimal (int value, DecimalContext context) {

			this.context = context;
			data = new uint[(int)Math.Ceiling (this.context.Precision / 8.0) + 1];
			data[0] = (uint)Math.Abs (value);
			sign = value < 0;
		}

		public DynamicDecimal (uint value, DecimalContext context) {

			this.context = context;
			data = new uint[(int)Math.Ceiling (this.context.Precision / 8.0) + 1];
			data[0] = (uint)Math.Abs (value);
			sign = false;
		}

		public DynamicDecimal (double value, DecimalContext context) {
			this.context = context;
			data = new uint[(int)Math.Ceiling (this.context.Precision / 8.0) + 1];
			data[0] = (uint)Math.Floor (Math.Abs (value));
			data[1] = (uint)(Math.Abs (value) % 1 * 100000000);
			sign = value < 0;
		}

		public DynamicDecimal (string value, DecimalContext context) {
			this = Parse (value, context);
		}

		public static DynamicDecimal Parse (string str, DecimalContext context) {
			bool sign = str.Contains ('-');
			uint[] data = new uint[(int)Math.Ceiling (context.Precision / 8.0) + 1];
			bool isDecimal = str.Contains ('.') || str.Contains (',');
			if (!isDecimal) {
				data[0] = (uint)Math.Abs (int.Parse (str));
				return new DynamicDecimal (data, sign, context);
			}
			string[] splitStr = str.Split (',', '.');
			string beforeZero = splitStr[0];
			string afterZero = splitStr[1];
			data[0] = (uint)Math.Abs (int.Parse (beforeZero));
			int requiredAfterZeroStringLength = (GetDataLength (context) - 1) * 8;
			while (afterZero.Length < requiredAfterZeroStringLength)
				afterZero += "0";
			for (int i = 0; i < GetDataLength (context) - 1; i++) {
				string dataValue = afterZero.Substring (i * 8, 8);
				data[i + 1] = uint.Parse (dataValue);
			}
			return new DynamicDecimal (data, sign, context);
		}

		private static bool IsAbsEqual (uint[] aValue, uint[] bValue) {
			int length = Math.Min (aValue.Length, bValue.Length);
			for (int i = 0; i < length; i++) {
				if (aValue[i] != bValue[i])
					return false;
			}
			return true;
		}

		private static bool IsAbsGreater (uint[] aValue, uint[] bValue) {
			int length = Math.Min (aValue.Length, bValue.Length);
			for (int i = 0; i < length; i++) {
				if (aValue[i] > bValue[i])
					return true;
				if (aValue[i] < bValue[i])
					return false;
			}
			return false;
		}

		private static bool IsAbsSmaller (uint[] aValue, uint[] bValue) {
			int length = Math.Min (aValue.Length, bValue.Length);
			for (int i = 0; i < length; i++) {
				if (aValue[i] < bValue[i])
					return true;
				if (aValue[i] > bValue[i])
					return false;
			}
			return false;
		}

		private static bool IsAbsGreaterOrEqual (uint[] aValue, uint[] bValue) {
			return IsAbsEqual (aValue, bValue) || IsAbsGreater (aValue, bValue);
		}

		private static bool IsAbsSmallerOrEqual (uint[] aValue, uint[] bValue) {
			return IsAbsEqual (aValue, bValue) || IsAbsSmaller (aValue, bValue);
		}

		/*private static uint[] AddArray (uint[] aValue, uint[] bValue, DecimalContext context) {
			uint[] output = new uint[GetDataLength (context)];
			int aLength = Math.Min (aValue.Length, output.Length);
			Array.Copy (aValue, output, aLength);
			int bLength = Math.Min (bValue.Length, output.Length);
			for (int i = bLength - 1; i >= 0; i--) {
				output[i] += bValue[i];
				if (output[i] >= 100000000) {
					output[i] -= 100000000;
					if (i != 0)
						output[i - 1]++;
				}
			}
			return output;
		}*/

		private static unsafe uint[] AddArray (uint[] aValue, uint[] bValue, DecimalContext context) {
			try {
				uint[] output = new uint[GetDataLength (context)];

				fixed (uint* aPtr = new uint[aValue.Length]) {
					fixed (uint* bPtr = new uint[bValue.Length]) {

						Vector256<uint> a, b, c;

						for (int i = 0; i < output.Length; i += 8) {

							int cap = Math.Min (i + 8, output.Length);
							switch (cap - i - 1) {
							case 7:
								aPtr[7] = aValue[i + 7];
								bPtr[7] = bValue[i + 7];
								goto case 6;
							case 6:
								aPtr[6] = aValue[i + 6];
								bPtr[6] = bValue[i + 6];
								goto case 5;
							case 5:
								aPtr[5] = aValue[i + 5];
								bPtr[5] = bValue[i + 5];
								goto case 4;
							case 4:
								aPtr[4] = aValue[i + 4];
								bPtr[4] = bValue[i + 4];
								goto case 3;
							case 3:
								aPtr[3] = aValue[i + 3];
								bPtr[3] = bValue[i + 3];
								goto case 2;
							case 2:
								aPtr[2] = aValue[i + 2];
								bPtr[2] = bValue[i + 2];
								goto case 1;
							case 1:
								aPtr[1] = aValue[i + 1];
								bPtr[1] = bValue[i + 1];
								goto case 0;
							case 0:
								aPtr[0] = aValue[i];
								bPtr[0] = bValue[i];
								break;
							}

							a = Avx2.LoadVector256 (aPtr);
							b = Avx2.LoadVector256 (bPtr);
							c = Avx2.Add (a, b);

							switch (cap - i - 1) {
							case 7:
								output[i + 7] = c.GetElement (7);
								goto case 6;
							case 6:
								output[i + 6] = c.GetElement (6);
								goto case 5;
							case 5:
								output[i + 5] = c.GetElement (5);
								goto case 4;
							case 4:
								output[i + 4] = c.GetElement (4);
								goto case 3;
							case 3:
								output[i + 3] = c.GetElement (3);
								goto case 2;
							case 2:
								output[i + 2] = c.GetElement (2);
								goto case 1;
							case 1:
								output[i + 1] = c.GetElement (1);
								goto case 0;
							case 0:
								output[i] = c.GetElement (0);
								break;
							}
						}
					}
				}
				for (int i = 0; i < output.Length - 1; i++) {
					output[i] += output[i + 1] / 100000000;
					output[i + 1] %= 100000000;
				}
				return output;
			} catch (IndexOutOfRangeException e) {
				Debug.WriteLine (e);
				throw e;
			}
		}

		private static uint[] SubstractArray (uint[] aValue, uint[] bValue, DecimalContext context) {
			uint[] output = new uint[GetDataLength (context)];
			int aLength = Math.Min (aValue.Length, output.Length);
			Array.Copy (aValue, output, aLength);
			int bLength = Math.Min (bValue.Length, output.Length);
			for (int i = bLength - 1; i >= 0; i--) {
				output[i] -= bValue[i];
				if (output[i] >= 0xFA0A1EFF) {
					output[i] += 100000000;
					if (i != 0)
						output[i - 1]--;
				}
			}
			return output;
		}

		private static uint[] MultiplyArray (uint[] aValue, uint[] bValue, DecimalContext context) {
			uint[] output = new uint[GetDataLength (context)];
			for (int i = aValue.Length - 1; i >= 0; i--) {
				for (int j = bValue.Length - 1; j >= 0; j--) {
					int outputIndex = i + j;
					if (outputIndex >= output.Length) {
						if (outputIndex == output.Length) {
							long value = (long)aValue[i] * bValue[j] / 100000000;
							output[outputIndex - 1] += (uint)value;
						}
						continue;
					}
					long addedValue = (long)aValue[i] * bValue[j] + output[outputIndex];
					long carry = Math.DivRem (addedValue, 100000000, out long rem);
					output[outputIndex] = (uint)rem;
					if (outputIndex > 0)
						output[outputIndex - 1] += (uint)carry;
				}
			}
			return output;
		}

		private static uint[] MultiplyArraySimple (uint[] aValue, uint b) {
			uint[] output = new uint[aValue.Length];
			for (int i = aValue.Length - 1; i >= 0; i--) {
				long v = aValue[i] * (long)b + output[i];
				long div = Math.DivRem (v, 100000000, out long rem);
				output[i] = (uint)rem;
				if (i > 0)
					output[i - 1] = (uint)div;
			}
			return output;
		}

		public static DynamicDecimal Add (DynamicDecimal a, DynamicDecimal b, DecimalContext context) {
			bool sign;
			uint[] data;

			if (a.sign == b.sign) {
				sign = a.sign;
				data = AddArray (a.data, b.data, context);
			} else {
				if (IsAbsGreaterOrEqual (a.data, b.data)) {
					sign = a.sign;
					data = SubstractArray (a.data, b.data, context);
				} else {
					sign = b.sign;
					data = SubstractArray (b.data, a.data, context);
				}
			}
			return new DynamicDecimal (data, sign, context);
		}

		public static DynamicDecimal Subtract (DynamicDecimal a, DynamicDecimal b, DecimalContext context) {
			bool sign;
			uint[] data;

			if (a.sign == b.sign) {
				if (IsAbsGreaterOrEqual (a.data, b.data)) {
					sign = a.sign;
					data = SubstractArray (a.data, b.data, context);
				} else {
					sign = !a.sign;
					data = SubstractArray (b.data, a.data, context);
				}
			} else {
				sign = a.sign;
				data = AddArray (a.data, b.data, context);
			}
			return new DynamicDecimal (data, sign, context);
		}

		public static DynamicDecimal Multiply (DynamicDecimal a, DynamicDecimal b, DecimalContext context) {
			bool sign = a.sign ^ b.sign;
			uint[] data = MultiplyArray (a.data, b.data, context);
			return new DynamicDecimal (data, sign, context);
		}

		public static DynamicDecimal operator + (DynamicDecimal a, DynamicDecimal b) {
			DecimalContext context = DecimalContext.Merge (a.context, b.context, ContextMergeOperation.Max);
			return Add (a, b, context);
		}

		public static DynamicDecimal operator - (DynamicDecimal a, DynamicDecimal b) {
			DecimalContext context = DecimalContext.Merge (a.context, b.context, ContextMergeOperation.Max);
			return Subtract (a, b, context);
		}

		public static DynamicDecimal operator * (DynamicDecimal a, DynamicDecimal b) {
			DecimalContext context = DecimalContext.Merge (a.context, b.context, ContextMergeOperation.Max);
			return Multiply (a, b, context);
		}

		public static DynamicDecimal operator * (int a, DynamicDecimal b) {
			bool sign = (a < 0) ^ b.sign;
			uint[] data = MultiplyArraySimple (b.data, (uint)Math.Abs (a));
			return new DynamicDecimal (data, sign, b.context);
		}

		public static bool operator > (DynamicDecimal a, DynamicDecimal b) {
			if (a.sign ^ b.sign)
				return b.sign;
			return IsAbsGreater (a.data, b.data);
		}

		public static bool operator < (DynamicDecimal a, DynamicDecimal b) {
			if (a.sign ^ b.sign)
				return a.sign;
			return IsAbsSmaller (a.data, b.data);
		}

		public static bool operator == (DynamicDecimal a, DynamicDecimal b) {
			if (a.sign != b.sign)
				return false;
			return IsAbsEqual (a.data, b.data);
		}

		public static bool operator != (DynamicDecimal a, DynamicDecimal b) {
			if (a.sign != b.sign)
				return true;
			return !IsAbsEqual (a.data, b.data);
		}

		public static bool operator >= (DynamicDecimal a, DynamicDecimal b) {
			return a > b || a == b;
		}

		public static bool operator <= (DynamicDecimal a, DynamicDecimal b) {
			return a < b || a == b;
		}

		public override string ToString () {
			string total = "";
			if (sign)
				total = "-";
			total += data[0].ToString ();
			total += ",";
			for (int i = 1; i < data.Length; i++) {
				string dataValue = data[i].ToString ();
				while (dataValue.Length < 8)
					dataValue = "0" + dataValue;
				if (i == data.Length - 1) {
					total += dataValue.Substring (0, (context.Precision - 1) % 8 + 1);
					continue;
				}
				total += dataValue;
			}
			return total;
		}
	}
}

namespace FractalExplorer.Numerics {
	public enum ContextMergeOperation { Min, Avg, Max }

	public struct DecimalContext {

		public static readonly int defaultPrecision = 8;

		/// <summary>
		/// The number of digits
		/// </summary>
		private int precision;
		public int Precision { get { return precision; } set { precision = value; if (value < 0) precision = defaultPrecision; } }

		public DecimalContext (int precision) {

			this.precision = precision;
			if (precision < 0)
				this.precision = defaultPrecision;

		}

		public static DecimalContext Merge (DecimalContext a, DecimalContext b, ContextMergeOperation o) {
			switch (o) {
			case ContextMergeOperation.Min:
				return new DecimalContext (Math.Min (a.precision, b.precision));
			case ContextMergeOperation.Avg:
				return new DecimalContext ((a.precision + b.precision) / 2);
			case ContextMergeOperation.Max:
				return new DecimalContext (Math.Max (a.precision, b.precision));
			default:
				return DynamicDecimal.defaultContext;
			}
		}
	}
}
