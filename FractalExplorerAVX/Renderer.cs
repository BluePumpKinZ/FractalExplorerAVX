using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FractalExplorer.Coloring;
using FractalExplorer.Fractals;
using FractalExplorer.Numerics;
using FractalExplorerAVX;

namespace FractalExplorer.Rendering {

	public static class Renderer {

		public enum RenderState { Preview, RenderImage, RenderImageSequence }
		public static Thread displayThread;
		public static Thread renderStageThread;
		public static Thread[] renderThreads;
		public static int threadCount = Environment.ProcessorCount;
		public static MainWindow window;
		public static WriteableBitmap displayMap;
		public static DecimalContext decimalContext = new DecimalContext (8);
		public static uint[] iterations;
		public static uint[] pixels;
		public static List<uint[]> indeces;
		public static int windowWidth;
		public static int windowHeight;
		public static int renderWidth = 1920;
		public static int renderHeight = 1080;
		// public static DynamicDecimal xPoint = new DynamicDecimal ("-1.7891690186048231066744683411888387638173618368159070155822017397181006156270275749142369245820396054406395755675312183271534128923049471434097690222315419202715383264050159131947029173677395015878767362862533310902938210320944254687547978131490470033844932429737957988993527433284731029281036319982917020893879163375991363440322779998", decimalContext);
		// public static DynamicDecimal yPoint = new DynamicDecimal ( "0.0000003393685157671825660282302661468127283482188945938569013974696942388736569110136147219176174266842972236468548795141989046122450230790250469659063534132826324119846599278074403635939133245821264547306595273202030703233351649340865807732843742179652779805674267353155895025480481872482185430928492885563968576611689865103371289111", decimalContext);
		// public static DynamicDecimal scale = new DynamicDecimal (  "0.000000000000000000000000000000000000000000000500000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", decimalContext);
		public static DynamicDecimal xPoint = DynamicDecimal.zero;
		public static DynamicDecimal yPoint = DynamicDecimal.zero;
		public static DynamicDecimal scale = 10 * DynamicDecimal.one;
		public static FractalType fractalType = FractalType.MandelbrotPower2;
		public static int limit = 100;
		public static RenderState renderState = RenderState.Preview;
		public static ColorMode colorMode = ColorMode.Monochrome;

		public static void Initialize () {
			ResizeWindow ();

			ThreadStart threadStart = new ThreadStart (() => DisplayThread ());
			displayThread = new Thread (threadStart);
			displayThread.Start ();

			threadStart = new ThreadStart (() => CheckRenderStageThread ());
			renderStageThread = new Thread (threadStart);
			renderStageThread.Start ();

			renderThreads = new Thread[threadCount];
			for (int i = 0; i < threadCount; i++) {
				int j = i;
				ThreadStart renderThreadStart = new ThreadStart (() => RenderThread (j));
				renderThreads[i] = new Thread (renderThreadStart);
				renderThreads[i].Start ();
			}
		}

		public static void ShuffleOrderArrays (int arrayWidth, int arrayHeight) {
			indeces = new List<uint[]> ();
			Random r = new Random ();
			for (int i = 0; i < 5; i++) {
				int width = (arrayWidth / squareLengths[i]);
				int height = (arrayHeight / squareLengths[i]);
				int length = width * height;
				indeces.Add (new uint[length]);
				int index = 0;
				for (uint x = 0; x < width; x++) {
					for (uint y = 0; y < height; y++) {
						uint value = (y << 16) + x;
						indeces[i][index] = value;
						index++;
					}
				}
				for (int j = 0; j < indeces[i].Length; j++)
					Swap (i, j, r.Next (indeces[i].Length));
			}
		}

		public static void Swap (int a, int i1, int i2) {
			uint temp = indeces[a][i1];
			indeces[a][i1] = indeces[a][i2];
			indeces[a][i2] = temp;
		}

		public static int GetWidth () {
			return (int)(window.Width - window.IO_PreviewImage.Margin.Left - window.IO_PreviewImage.Margin.Right);
		}

		public static int GetHeight () {
			return (int)(window.Height - window.IO_PreviewImage.Margin.Top - window.IO_PreviewImage.Margin.Bottom);
		}

		public static void ResizeWindow (bool forceOverride = false) {
			if (!forceOverride) {
				if (renderState != RenderState.Preview)
					return;
				int newWidth = GetWidth ();
				int newHeight = GetHeight ();
				if (windowWidth == newWidth && windowHeight == newHeight)
					return;
			}
			windowWidth = GetWidth ();
			windowHeight = GetHeight ();
			iterations = new uint[windowWidth * windowHeight];
			pixels = new uint[windowWidth * windowHeight];
			window.ExecuteOnMainThread (delegate () {
				displayMap = new WriteableBitmap (windowWidth, windowHeight, 96, 96, PixelFormats.Bgra32, null);
				window.IO_PreviewImage.Source = displayMap;
			});
			ShuffleOrderArrays (windowWidth, windowHeight);
			Fractal.SetState (GetWidth (), GetHeight (), xPoint, yPoint, scale, decimalContext, fractalType, limit);
			ResetRender ();
		}

		public static void UpdateDisplay () {
			window.ExecuteOnMainThread (delegate () {
				ResizeWindow ();
				window.IO_PreviewImage.Source = displayMap;
				try {
					if (renderState == RenderState.Preview) {
						displayMap.WritePixels (new Int32Rect (0, 0, windowWidth, windowHeight), pixels, windowWidth * 4, 0);
					} else {
						displayMap.WritePixels (new Int32Rect (0, 0, renderWidth, renderHeight), pixels, renderWidth * 4, 0);
					}
				} catch (Exception) { }
			});
		}

		public static void AbortAllThreads () {
			displayThread.Abort ();
			renderStageThread.Abort ();
			for (int i = 0; i < threadCount; i++) {
				renderThreads[i].Abort ();
			}
		}

		static void DisplayThread () {
			for (; ; ) {
				UpdateDisplay ();
				if (renderState == RenderState.Preview) {
					int tot = 0;
					for (int i = 0; i < threadProgress.Length; i++)
						tot += threadProgress[i];
					tot /= threadCount;

					double progress = (double)tot / indeces[renderStage].Length;
					progress *= progressScales[renderStage];
					progress += progressOffsets[renderStage];
					progress *= progress;

					window.ExecuteOnMainThread (delegate () {
						window.IO_ProgressBar.Value = progress * 100;
						window.IO_ProgressLabel.Content = "Rendering: " + (int)(progress * 100) + "%";
					});
				}
				Thread.Sleep (50);
			}
		}

		public static void MoveFractalView (double xMove, double yMove) {
			DynamicDecimal xMoveDec = new DynamicDecimal ((0.0005 * xMove).ToString (), decimalContext);
			DynamicDecimal yMoveDec = new DynamicDecimal ((0.0005 * yMove).ToString (), decimalContext);
			xMoveDec *= scale;
			yMoveDec *= scale;
			xPoint -= xMoveDec;
			yPoint -= yMoveDec;
			ApplyFractalView ();
		}

		public static void ZoomFractalView (double delta) {
			scale *= new DynamicDecimal ((1 - delta * 0.0005).ToString (), decimalContext);
			ApplyFractalView ();
		}

		public static void ChangeFractalLimit (int delta) {
			limit += delta;
			if (limit <= 0)
				limit = 100;
			ApplyFractalView ();
		}

		public static void ApplyFractalView () {
			if (renderState != RenderState.Preview)
				return;

			if (RequiresMorePercision ()) {
				decimalContext = new DecimalContext (decimalContext.Precision + 8);
				AdjustVariablePrecision ();
				Debug.WriteLine ("Increased Presicion");
			} else if (RequiresLessPercision ()) {
				decimalContext = new DecimalContext (decimalContext.Precision - 8);
				AdjustVariablePrecision ();
				Debug.WriteLine ("Decreased Presicion");
			}

			Fractal.SetState (windowWidth, windowHeight, xPoint, yPoint, scale, decimalContext, fractalType, limit);
			ResetRender ();
		}

		static void AdjustVariablePrecision () {
			xPoint = DynamicDecimal.Parse (xPoint.ToString (), decimalContext);
			yPoint = DynamicDecimal.Parse (yPoint.ToString (), decimalContext);
			scale = DynamicDecimal.Parse (scale.ToString (), decimalContext);
		}

		static bool RequiresMorePercision () {
			for (int i = 0; i < scale.Data.Length - 1; i++) {
				if (scale.Data[i] != 0)
					return false;
			}
			return scale.Data[scale.Data.Length - 1] < 10000000;
		}

		static bool RequiresLessPercision () {
			if (scale.Data.Length == 1)
				return false;
			for (int i = 0; i < scale.Data.Length - 2; i++) {
				if (scale.Data[i] != 0)
					return true;
			}
			return scale.Data[scale.Data.Length - 2] > 10000000;
		}

		static void CheckRenderStageThread () {
			for (; ; ) {
				Thread.Sleep (50);
				bool done = true;
				for (int i = 0; done && i < finishedStage.Length; i++)
					if (!finishedStage[i])
						done = false;
				if (!done)
					continue;
				if (renderStage > 0) {
					renderStage--;
					for (int i = 0; i < threadCount; i++) {
						finishedStage[i] = false;
						threadProgress[i] = i;
					}
				}
			}
		}

		public static int renderStage = 4;
		static bool[] finishedStage = new bool[threadCount];
		static int[] threadProgress = new int[threadCount];
		static int[] squareLengths = new int[] { 1, 2, 4, 8, 16 };
		static double[] progressScales = new double[] { 0.5, 0.25, 0.125, 0.0625, 0.0625 };
		static double[] progressOffsets = new double[] { 0.5, 0.25, 0.125, 0.0625, 0 };
		public static void ResetRender () {
			if (renderState != RenderState.Preview)
				return;
			renderStage = 4;
			for (int i = 0; i < threadCount; i++) {
				finishedStage[i] = false;
			}
		}

		public static void SetRenderResolution (int width, int height) {
			renderWidth = width;
			renderHeight = height;
		}

		public static void RenderImage () {
			if (renderState == RenderState.Preview)
				renderState = RenderState.RenderImage;
			displayMap = new WriteableBitmap (renderWidth, renderHeight, 96, 96, PixelFormats.Bgra32, null);
			window.IO_PreviewImage.Source = displayMap;
			ShuffleOrderArrays (renderWidth, renderHeight);
			iterations = new uint[renderWidth * renderHeight];
			pixels = new uint[renderWidth * renderHeight];
			Fractal.SetState (renderWidth, renderHeight, xPoint, yPoint, scale, decimalContext, fractalType, limit);
			renderStage = 1;
			for (int i = 0; i < threadCount; i++) {
				finishedStage[i] = false;
				threadProgress[i] = i;
			}
			ThreadStart threadStart = new ThreadStart (() => RenderImageThread ());
			renderImageThread = new Thread (threadStart);
			renderImageThread.Start ();
		}

		public static void RenderImageSequence () {
			renderState = RenderState.RenderImageSequence;
			displayMap = new WriteableBitmap (renderWidth * 2, renderHeight * 2, 96, 96, PixelFormats.Bgra32, null);
			window.IO_PreviewImage.Source = displayMap;
			ShuffleOrderArrays (renderWidth * 2, renderHeight * 2);
			iterations = new uint[renderWidth * renderHeight * 4];
			pixels = new uint[renderWidth * renderHeight * 4];
			// Fractal.SetState (renderWidth * 2, renderHeight * 2, xPoint, yPoint, scale, decimalContext, fractalType, limit);
			ThreadStart threadStart = new ThreadStart (() => RenderImageSequenceThread ());
			renderImageThread = new Thread (threadStart);
			renderImageThread.Start ();
		}

		public static void RenderImageThread () {
			for (; ; ) {
				Thread.Sleep (100);
				try {
					int tot = 0;
					for (int i = 0; i < threadProgress.Length; i++)
						tot += threadProgress[i];
					tot /= threadCount;

					double progress = (double)tot / indeces[renderStage].Length;
					if (renderStage == 1) {
						progress *= 0.25;
					}
					if (renderStage == 0) {
						progress *= 0.75;
						progress += 0.25;
					}

					window.ExecuteOnMainThread (delegate () {
						window.IO_ProgressLabel.Content = "Rendering Image " + Math.Round (progress * 100) + "%";
						window.IO_ProgressBar.Value = progress * 100;
					});
				} catch (Exception) { }


				bool done = true;
				for (int i = 0; i < threadCount; i++) {
					if (!finishedStage[i])
						done = false;
				}
				if (!done)
					continue;
				if (renderStage > 0)
					continue;

				string path = Environment.GetFolderPath (Environment.SpecialFolder.Desktop) + "/Render" + renderWidth + "x" + renderHeight + ".png";
				window.ExecuteOnMainThread (() => SaveDisplayImage (path));
				StopRendering ();
				return;
			}
		}

		public static void RenderImageSequenceThread () {

			DynamicDecimal limitScale = scale;
			int frameLimit = 0;
			DynamicDecimal scaleTest = new DynamicDecimal (10, decimalContext);
			while (scaleTest > limitScale) {
				scaleTest *= new DynamicDecimal (0.5, decimalContext);
				frameLimit++;
			}

			for (int i = 0; i < frameLimit; i++) {

				DynamicDecimal frameScale = new DynamicDecimal (10, decimalContext);
				for (int j = 0; j < i; j++)
					frameScale *= new DynamicDecimal (0.5, decimalContext);

				Fractal.SetState (renderWidth * 2, renderHeight * 2, xPoint, yPoint, frameScale, decimalContext, fractalType, limit);

				renderStage = 1;
				for (int j = 0; j < threadCount; j++) {
					finishedStage[j] = false;
					threadProgress[j] = j;
				}
			FailLabel:

				Thread.Sleep (100);
				try {
					int tot = 0;
					for (int j = 0; j < threadProgress.Length; j++)
						tot += threadProgress[j];
					tot /= threadCount;

					double progress = (double)tot / indeces[renderStage].Length;
					if (renderStage == 1) {
						progress *= 0.25;
					}
					if (renderStage == 0) {
						progress *= 0.75;
						progress += 0.25;
					}

					window.ExecuteOnMainThread (delegate () {
						window.IO_ProgressLabel.Content = "Rendering Image " + Math.Round (progress * 100) + "%";
						window.IO_ProgressBar.Value = progress * 100;
					});
				} catch (Exception) { }


				bool done = true;
				for (int j = 0; j < threadCount; j++) {
					if (!finishedStage[j])
						done = false;
				}
				if (!done)
					goto FailLabel;
				if (renderStage > 0)
					goto FailLabel;

				string path = Environment.GetFolderPath (Environment.SpecialFolder.Desktop) + "/Render" + renderWidth + "x" + renderHeight + ".png";
				window.ExecuteOnMainThread (() => SaveDisplayImage (path));
			}

			StopRendering ();
		}

		private static void SaveDisplayImage (string path) {
			BitmapEncoder encoder = new PngBitmapEncoder ();
			encoder.Frames.Add (BitmapFrame.Create (displayMap));
			FileStream stream = new FileStream (path, FileMode.OpenOrCreate);
			encoder.Save (stream);
			stream.Close ();
		}

		public static Thread renderImageThread;
		public static Thread renderImageSequenceThread;
		public static void StopRendering () {
			ResizeWindow (true);
			renderState = RenderState.Preview;
			ResetRender ();
		}

		static void RenderThread (int threadIndex) {
			for (; ; ) {
				try {
					threadProgress[threadIndex] = threadIndex;

					int width = 0;
					int height = 0;
					switch (renderState) {
					case RenderState.Preview:
						width = windowWidth;
						height = windowHeight;
						break;
					case RenderState.RenderImage:
						width = renderWidth;
						height = renderHeight;
						break;
					case RenderState.RenderImageSequence:
						width = renderWidth * 2;
						height = renderHeight * 2;
						break;
					}

					for (threadProgress[threadIndex] = threadIndex; threadProgress[threadIndex] < indeces[renderStage].Length; threadProgress[threadIndex] += threadCount) {
						uint stagedPixelValue = indeces[Math.Max (Math.Min (renderStage, 4), 0)][threadProgress[threadIndex]];

						int x = (int)(stagedPixelValue & 0x0000ffff);
						int y = (int)((stagedPixelValue & 0xffff0000) >> 16);

						x *= squareLengths[renderStage];
						y *= squareLengths[renderStage];

						int pixelValue = x + width * y;
						if (renderStage == 0) {
							if ((x + y) % 2 != 0) {
								if (pixelValue > width && pixelValue < (width * height) - width)

									if (iterations[pixelValue - 1] == iterations[pixelValue + 1] &&
										iterations[pixelValue - 1] == iterations[pixelValue + width] &&
										iterations[pixelValue - 1] == iterations[pixelValue - width]) {

										iterations[pixelValue] = iterations[pixelValue - 1];

										continue;
									}
							}
						}

						uint iter = Fractal.GetIterationForPixel (x, y);
						uint color = FractalColors.GetColor (iter, colorMode);
						RendererUtils.FillRectangle (pixelValue, width, iter, color);
					}
				} catch (Exception) { }
				finishedStage[threadIndex] = true;
				while (finishedStage[threadIndex])
					Thread.Sleep (50);
			}
		}

	}
}
