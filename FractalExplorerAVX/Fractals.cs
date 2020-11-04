using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using FractalExplorer.Numerics;


namespace FractalExplorer.Fractals {

	public enum FractalType {
		MandelbrotPower2,
		MandelbrotPower3,
		MandelbrotPower4,
		MandelbrotPower5,
		MandelbrotPower6,
		MandelbrotPower7,
		MandelbrotPower8,
		MandelbrotPower2decimal,
		Julia,
		Circle
	}

	public static class Fractal {

		public static DecimalContext context;
		public static DynamicDecimal xPoint;
		public static DynamicDecimal yPoint;
		public static DynamicDecimal scale;
		public static FractalType fractalType;
		public static double rhalf;
		public static int width;
		public static int height;
		public static int limit;

		public static void MultComplex (DynamicDecimal a, DynamicDecimal b, DynamicDecimal c, DynamicDecimal d, out DynamicDecimal x, out DynamicDecimal y) {
			x = a * c - b * d;
			y = a * d + b * c;
		}

		public static void SetState (int newWidth, int newheight, DynamicDecimal x, DynamicDecimal y, DynamicDecimal s, DecimalContext c, FractalType t, int l) {
			width = newWidth;
			height = newheight;
			rhalf = 0.5 * width / height;
			xPoint = x;
			yPoint = y;
			scale = s;
			context = c;
			fractalType = t;
			limit = l;
		}

		public static void TransformPoint (int x, int y, out DynamicDecimal cX, out DynamicDecimal cY) {
			double dx = (double)x / height - rhalf;
			double dy = (double)y / height - 0.5;
			cX = new DynamicDecimal (dx, context);
			cY = new DynamicDecimal (-dy, context);

			cX = DynamicDecimal.Multiply (cX, scale, context);
			cY = DynamicDecimal.Multiply (cY, scale, context);

			cX = DynamicDecimal.Add (cX, xPoint, context);
			cY = DynamicDecimal.Add (cY, yPoint, context);
		}

		public static uint GetIterationForPixel (int x, int y) {

			TransformPoint (x, y, out DynamicDecimal xC, out DynamicDecimal yC);

			uint result = 0;
			switch (fractalType) {
			case FractalType.MandelbrotPower2decimal:

				decimal dzx = 0;
				decimal dzy = 0;

				decimal dzxSquared = 0;
				decimal dzySquared = 0;

				decimal dxC = decimal.Parse (xC.ToString ());
				decimal dyC = decimal.Parse (yC.ToString ());

				while (dzxSquared + dzySquared < 4 && result < limit) {

					decimal dtempZX = dzxSquared - dzySquared + dxC;
					decimal dtempZY = 2 * dzx * dzy + dyC;

					dzx = dtempZX;
					dzy = dtempZY;

					dzxSquared = dzx * dzx;
					dzySquared = dzy * dzy;

					result++;
				}

				break;
			case FractalType.MandelbrotPower2:

				DynamicDecimal zx = DynamicDecimal.zero;
				DynamicDecimal zy = DynamicDecimal.zero;

				DynamicDecimal zxSquared = DynamicDecimal.zero;
				DynamicDecimal zySquared = DynamicDecimal.zero;

				DynamicDecimal four = new DynamicDecimal (4, context);

				DynamicDecimal tempZX, tempZY; 

				while (zxSquared + zySquared < four && result < limit) {

					tempZX = zxSquared - zySquared + xC;
					tempZY = 2 * zx * zy + yC;

					zx = tempZX;
					zy = tempZY;

					zxSquared = zx * zx;
					zySquared = zy * zy;

					result++;

				}
				break;
			case FractalType.MandelbrotPower3:

				zx = DynamicDecimal.zero;
				zy = DynamicDecimal.zero;

				DynamicDecimal zxPower2 = DynamicDecimal.zero;
				DynamicDecimal zyPower2 = DynamicDecimal.zero;

				DynamicDecimal zxPower3 = DynamicDecimal.zero;
				DynamicDecimal zyPower3 = DynamicDecimal.zero;

				four = new DynamicDecimal (4, context);

				while (zxPower2 + zyPower2 < four && result < limit) {

					DynamicDecimal tempX = zxPower3 - 3 * zx * zyPower2;
					DynamicDecimal tempY = 3 * zxPower2 * zy - zyPower3;

					zx = tempX + xC;
					zy = tempY + yC;

					zxPower2 = zx * zx;
					zyPower2 = zy * zy;

					zxPower3 = zxPower2 * zx;
					zyPower3 = zyPower2 * zy;

					result++;
				}
				break;
			case FractalType.MandelbrotPower4:

				zx = DynamicDecimal.zero;
				zy = DynamicDecimal.zero;

				zxPower2 = DynamicDecimal.zero;
				zyPower2 = DynamicDecimal.zero;

				zxPower3 = DynamicDecimal.zero;
				zyPower3 = DynamicDecimal.zero;

				DynamicDecimal zxPower4 = DynamicDecimal.zero;
				DynamicDecimal zyPower4 = DynamicDecimal.zero;

				four = new DynamicDecimal (4, context);

				while (zxPower2 + zyPower2 < four && result < limit) {

					DynamicDecimal tempX = zxPower4 - 6 * zxPower2 * zyPower2 + zyPower4;
					DynamicDecimal tempY = 4 * (zxPower3 * zy - zx * zyPower3);

					zx = tempX + xC;
					zy = tempY + yC;

					zxPower2 = zx * zx;
					zyPower2 = zy * zy;

					zxPower3 = zxPower2 * zx;
					zyPower3 = zyPower2 * zy;

					zxPower4 = zxPower3 * zx;
					zyPower4 = zyPower3 * zy;

					result++;
				}
				break;
			case FractalType.MandelbrotPower5:

				zx = DynamicDecimal.zero;
				zy = DynamicDecimal.zero;

				zxPower2 = DynamicDecimal.zero;
				zyPower2 = DynamicDecimal.zero;

				zxPower3 = DynamicDecimal.zero;
				zyPower3 = DynamicDecimal.zero;

				zxPower4 = DynamicDecimal.zero;
				zyPower4 = DynamicDecimal.zero;

				DynamicDecimal zxPower5 = DynamicDecimal.zero;
				DynamicDecimal zyPower5 = DynamicDecimal.zero;

				four = new DynamicDecimal (4, context);

				while (zxPower2 + zyPower2 < four && result < limit) {

					DynamicDecimal tempX = zxPower5 - 10 * zxPower3 * zyPower2 + 5 * zx * zyPower4;
					DynamicDecimal tempY = 5 * zxPower4 * zy - 10 * zxPower2 * zyPower3 + zyPower5;

					zx = tempX + xC;
					zy = tempY + yC;

					zxPower2 = zx * zx;
					zyPower2 = zy * zy;

					zxPower3 = zxPower2 * zx;
					zyPower3 = zyPower2 * zy;

					zxPower4 = zxPower3 * zx;
					zyPower4 = zyPower3 * zy;

					zxPower5 = zxPower4 * zx;
					zyPower5 = zyPower4 * zy;

					result++;
				}
				break;
			case FractalType.MandelbrotPower6:

				zx = DynamicDecimal.zero;
				zy = DynamicDecimal.zero;

				zxPower2 = DynamicDecimal.zero;
				zyPower2 = DynamicDecimal.zero;

				zxPower3 = DynamicDecimal.zero;
				zyPower3 = DynamicDecimal.zero;

				zxPower4 = DynamicDecimal.zero;
				zyPower4 = DynamicDecimal.zero;

				zxPower5 = DynamicDecimal.zero;
				zyPower5 = DynamicDecimal.zero;

				DynamicDecimal zxPower6 = DynamicDecimal.zero;
				DynamicDecimal zyPower6 = DynamicDecimal.zero;

				four = new DynamicDecimal (4, context);

				while (zxPower2 + zyPower2 < four && result < limit) {

					DynamicDecimal tempX = zxPower6 - 15 * zxPower4 * zyPower2 + 15 * zxPower2 * zyPower4 - zyPower6;
					DynamicDecimal tempY = 6 * zxPower5 * zy - 20 * zxPower3 * zyPower3 + 6 * zx * zyPower5;

					zx = tempX + xC;
					zy = tempY + yC;

					zxPower2 = zx * zx;
					zyPower2 = zy * zy;

					zxPower3 = zxPower2 * zx;
					zyPower3 = zyPower2 * zy;

					zxPower4 = zxPower3 * zx;
					zyPower4 = zyPower3 * zy;

					zxPower5 = zxPower4 * zx;
					zyPower5 = zyPower4 * zy;

					zxPower6 = zxPower5 * zx;
					zyPower6 = zyPower5 * zy;

					result++;
				}
				break;
			case FractalType.Circle:
				if (DynamicDecimal.one > (xC * xC + yC * yC))
					result += 250;
				break;
			}

			return result;
		}

	}
}
